using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Un4seen.Bass.Misc;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using System.Threading.Tasks;
using System.Timers;

namespace NodeBoothSender
{
    public class SysTrayApp : Form
    {
        //Main worker for Hardware Info
        BackgroundWorker aidaUpdateWorker = new BackgroundWorker();

        //static BPMCounter bassBeatDetector;
        //static BassWasapiHandler wasapi;

        //Initialize the szabBeatDetector and the needed variables.
        SpectrumBeatDetector szabBeatDetector;
        List<byte> beatValueList = new List<byte>();
        byte beatValue = 0;

        //HTTP string which points to the node server.
        string serverUrl = "http://192.168.178.38:8101";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new SysTrayApp());
        }

        private NotifyIcon      trayIcon;
        private ContextMenu     trayMenu;
        public Debug            debugWindow;

        //HttpClient for API calls
        private HttpClient httpClient = new HttpClient(new HttpClientHandler    {UseProxy = false}  );

        //Initialize PerformanceCounters for fast HardwareInfos.
        //This helps delivering hardwareInfos faster than the AIDA64 API can handle.
        protected PerformanceCounter cpuCounter = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "_TOTAL");

        protected PerformanceCounter cpuCounter1 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "0");
        protected PerformanceCounter cpuCounter2 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "1");
        protected PerformanceCounter cpuCounter3 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "2");
        protected PerformanceCounter cpuCounter4 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "3");
        protected PerformanceCounter cpuCounter5 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "4");
        protected PerformanceCounter cpuCounter6 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "5");
        protected PerformanceCounter cpuCounter7 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "6");
        protected PerformanceCounter cpuCounter8 = new PerformanceCounter("Prozessor", "Prozessorzeit (%)", "7");

        protected PerformanceCounter lanUpCounter = new PerformanceCounter("Netzwerkadapter", "Bytes gesendet/s", "Realtek PCIe GBE Family Controller");
        protected PerformanceCounter lanDownCounter = new PerformanceCounter("Netzwerkadapter", "Empfangene Bytes/s", "Realtek PCIe GBE Family Controller");

        public SysTrayApp()
        {
            // Create a tray menu
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayMenu.MenuItems.Add("Debug", OnDebug);

            // Create a tray icon.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NodeBooth Sender";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            //Initialize the debugWindow with several debug visualizations on it.
            debugWindow = new Debug();

            //Start the AIDA64 + PerformanceCounter HardwareInfo node API call worker.
            aidaUpdateWorker.DoWork += new DoWorkEventHandler(aidaUpdateWorkerDoWork);
            aidaUpdateWorker.WorkerSupportsCancellation = true;
            aidaUpdateWorker.RunWorkerAsync(aidaUpdateWorker);

            //Start the szabBeatDetector
            szabBeatDetector = new SpectrumBeatDetector(3);
            szabBeatDetector.Subscribe(beatDetected);
            szabBeatDetector.StartAnalysis();


            //BPM Calculation
            /*BassNet.Registration("marc.berchtold@hotmail.de", "");
            Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

            wasapi = new BassWasapiHandler(3, false, 48000, 2, 0f, 0f);
            wasapi.Init();
            Console.WriteLine(wasapi.InputChannel);
            wasapi.Start();

            System.Timers.Timer bassBPMTimer = new System.Timers.Timer(5);
            bassBPMTimer.Elapsed += bassBPMTimerEvent;
            bassBPMTimer.AutoReset = true;
            bassBPMTimer.Enabled = true;

            bassBeatDetector = new BPMCounter(5, 44100);
            bassBeatDetector.MaxBPM = 200;
            bassBeatDetector.MinBPM = 70;
            bassBeatDetector.Reset(44100);*/
        }

        /*private static void bassBPMTimerEvent(Object source, ElapsedEventArgs e)
        {
            bool beat = bassBeatDetector.ProcessAudio(wasapi.InputChannel, true);
            if (beat)
            {
                Console.WriteLine(bassBeatDetector.BPM.ToString("#00.0"));
            }
        }*/

        void aidaUpdateWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker localbg = (BackgroundWorker)e.Argument;
            while (!localbg.CancellationPending)
            {
                //Call the updateAidaInformation and send the results to the node API
                sendAidaInformation(updateAidaInformation());

                int threadSleepDuration = 250;
                Thread.Sleep(threadSleepDuration);
            }
        }

        private string updateAidaInformation()
        {
            string tempString = string.Empty;
            //Read the infos from AIDA64 Shared Memory and create a valid XML-Document
            try
            {
                tempString += "<AIDA64>";

                MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("AIDA64_SensorValues");
                MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
                tempString = tempString + "";
                for (int i = 0; i < accessor.Capacity; i++)
                {
                    tempString = tempString + (char)accessor.ReadByte(i);
                }
                tempString = tempString.Replace("\0", "");
                tempString = tempString + "";
                accessor.Dispose();
                mmf.Dispose();

                tempString += "</AIDA64>";
            }
            catch (FileNotFoundException)
            {
                return "Error getting Memory Section... Is AIDA64 running and Shared Memory activated?";
            }

            //Add the faster updating Hardware Infos from the PerformanceCounters and change the according values in the XML file.
            try
            {
                XDocument aidaXML = XDocument.Parse(tempString);

                var coreUtilizationElements = aidaXML.Element("AIDA64").Elements("sys");
                foreach (var core in coreUtilizationElements)
                {
                    int usage;
                    switch (core.Element("id").Value)
                    {
                        case "SCPUUTI":
                            usage = (int)cpuCounter.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;


                        case "SCPU1UTI":
                            usage = (int)cpuCounter1.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU2UTI":
                            usage = (int)cpuCounter2.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU3UTI":
                            usage = (int)cpuCounter3.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU4UTI":
                            usage = (int)cpuCounter4.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU5UTI":
                            usage = (int)cpuCounter5.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU6UTI":
                            usage = (int)cpuCounter6.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU7UTI":
                            usage = (int)cpuCounter7.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SCPU8UTI":
                            usage = (int)cpuCounter8.NextValue();
                            core.Element("value").Value = usage.ToString();
                            break;


                        case "SNIC2DLRATE":
                            usage = (int)lanDownCounter.NextValue()/1000;
                            core.Element("value").Value = usage.ToString();
                            break;
                        case "SNIC2ULRATE":
                            usage = (int)lanUpCounter.NextValue() / 1000;
                            core.Element("value").Value = usage.ToString();
                            break;
                    }
                }
                //Convert the XML-file to a JSON string.
                var Json = JsonConvert.SerializeXNode(aidaXML, Formatting.None, true);

                return Json;
            }
            catch (Exception)
            {
                return "Error parsing XML from Memory Section";
            }
        }

        private async void sendAidaInformation(string jsonAidaInformation)
        {
            try
            {
                await httpClient.PostAsync(serverUrl, new StringContent(
                    jsonAidaInformation.ToString(),
                    Encoding.UTF8,
                    "application/json"
                ));
                beatValue = 0;
            }
            catch (Exception)
            {

                return;
            }
        }



        void beatDetected(byte Value)
        {
            beatValueList.Add(Value);

            if (beatValueList.Count > 6)
                beatValueList.RemoveAt(0);

            int successCounter = 0;
            foreach (byte value in beatValueList)
            {
                switch (value)
                {
                    case 1:
                        successCounter++;
                        break;
                    default:
                        successCounter = 0;
                        break;
                }
            }

            if (successCounter >= 6)
            {
                //Actually a good beat
                beatValue = Value;
                sendBeatInformation(Value);

                try
                {
                    debugWindow.Invoke(debugWindow.updateBeatProgressBar, 100);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Open DEBUG Menu!");
                }
            }
        }

        private async void sendBeatInformation(byte Value)
        {
            string json = "{\"beatValue\": " + Value + "}";
            Console.WriteLine(json);
            try
            {
                await httpClient.PostAsync(serverUrl+"/beat", new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                ));
                beatValue = 0;
            }
            catch (Exception)
            {
                Console.WriteLine("Beat Value sending failed");
                return;
            }
        }



        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnDebug(object sender, EventArgs e)
        {
            debugWindow.Show();
        }


        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}

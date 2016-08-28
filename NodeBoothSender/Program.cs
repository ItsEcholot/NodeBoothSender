using System;
using System.Linq;
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

namespace NodeBoothSender
{
    public class SysTrayApp : Form
    {
        BackgroundWorker aidaUpdateWorker = new BackgroundWorker();
        Audio audioWorker;
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

        private bool            serverPingable;
        private HttpClient httpClient = new HttpClient(new HttpClientHandler    {UseProxy = false}  );


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
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayMenu.MenuItems.Add("Debug", OnDebug);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NodeBooth Sender";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            debugWindow = new Debug();

            aidaUpdateWorker.DoWork += new DoWorkEventHandler(aidaUpdateWorkerDoWork);
            aidaUpdateWorker.WorkerSupportsCancellation = true;
            aidaUpdateWorker.RunWorkerAsync(aidaUpdateWorker);

            audioWorker = new Audio(debugWindow);
        }

        void aidaUpdateWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker localbg = (BackgroundWorker)e.Argument;
            while (!localbg.CancellationPending)
            {
                //DateTime startTime = DateTime.Now;

                sendAidaInformation(updateAidaInformation());


                /*DateTime stopTime = DateTime.Now;
                TimeSpan elapsedTime = DateTime.Parse(stopTime.ToString()).Subtract(DateTime.Parse(startTime.ToString()));*/

                int threadSleepDuration = 250; //- (int)elapsedTime.TotalMilliseconds;
                Thread.Sleep(threadSleepDuration);
            }
        }



        private string updateAidaInformation()
        {
            string tempString = string.Empty;
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
            }
            catch (Exception)
            {

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

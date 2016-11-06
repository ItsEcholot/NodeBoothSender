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
using Newtonsoft.Json.Linq;

using SpotifyAPI.Local;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;

//Thanks to mdjarv: https://github.com/mdjarv/assettocorsasharedmemory
using AssettoCorsaSharedMemory;

namespace NodeBoothSender
{
    public class SysTrayApp : Form
    {
        //Main worker for Hardware Info
        BackgroundWorker aidaUpdateWorker = new BackgroundWorker();
        BackgroundWorker spotifyWorker = new BackgroundWorker();
        BackgroundWorker gameIntegrationUpdateWorker = new BackgroundWorker();

        AssettoCorsa assettoCorsa = new AssettoCorsa();
        SpotifyLocalAPI spotifyLocalApi = new SpotifyLocalAPI();



        //HTTP string which points to the node server.
        string serverUrl = "http://192.168.178.66:8101";

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

            //Start the spotify API worker
            spotifyWorker.DoWork += new DoWorkEventHandler(spotifyWorkerDoWork);
            spotifyWorker.WorkerSupportsCancellation = true;
            spotifyWorker.RunWorkerAsync(spotifyWorker);

            

            //Start the GameIntegration worker
            gameIntegrationUpdateWorker.DoWork += new DoWorkEventHandler(gameIntegrationWorkerDoWork);
            gameIntegrationUpdateWorker.WorkerSupportsCancellation = true;
            gameIntegrationUpdateWorker.RunWorkerAsync(gameIntegrationUpdateWorker);
        }

        void aidaUpdateWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker localbg = (BackgroundWorker)e.Argument;
            while (!localbg.CancellationPending)
            {
                //Call the updateAidaInformation and send the results to the node API
                sendAidaInformation(updateAidaInformation());

                Thread.Sleep(250);
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
                            usage = (int)lanDownCounter.NextValue() / 1000;
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
                var result = httpClient.PostAsync(serverUrl, new StringContent(
                    jsonAidaInformation.ToString(),
                    Encoding.UTF8,
                    "application/json"
                )).Result;

                string resultJson = result.Content.ReadAsStringAsync().Result;

                if (resultJson != "\"\"")
                {
                    JToken token = JObject.Parse(resultJson);

                    if (token.SelectToken("lineOut").Value<String>() == "speaker")
                    {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        process.StartInfo.FileName = "C:\\NIRCMD\\Speaker.bat";
                        process.StartInfo.WorkingDirectory = "C:\\NIRCMD";
                        process.StartInfo.UseShellExecute = true;
                        process.Start();
                    }
                    else if (token.SelectToken("lineOut").Value<String>() == "headset")
                    {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        process.StartInfo.FileName = "C:\\NIRCMD\\HeadsetRazer.bat";
                        process.StartInfo.WorkingDirectory = "C:\\NIRCMD";
                        process.StartInfo.UseShellExecute = true;
                        process.Start();

                        if (token.SelectToken("lineIn").Value<String>() == "headset")
                        {
                            System.Diagnostics.Process process2 = new System.Diagnostics.Process();
                            process2.StartInfo.FileName = "C:\\NIRCMD\\HeadsetMic.bat";
                            process2.StartInfo.WorkingDirectory = "C:\\NIRCMD";
                            process2.StartInfo.UseShellExecute = true;
                            process2.Start();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }



        async void spotifyWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker localbg = (BackgroundWorker) e.Argument;
            StatusResponse localSpotifyStatusResponse;
            string currentTrack = "";

            while (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                Thread.Sleep(500);
            }
            while (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                Thread.Sleep(500);
            }
            while (!spotifyLocalApi.Connect())
            {
                Thread.Sleep(500);
            }

            while (!localbg.CancellationPending)
            {
                localSpotifyStatusResponse = spotifyLocalApi.GetStatus();
                try
                {
                    var result = httpClient.PostAsync(serverUrl + "/spotify", new StringContent(
                        JsonConvert.SerializeObject(localSpotifyStatusResponse),
                        Encoding.UTF8,
                        "application/json"
                        )).Result;

                    string resultJson = result.Content.ReadAsStringAsync().Result;

                    if (resultJson != "\"\"")
                    {
                        JToken resultToken = JObject.Parse(resultJson);

                        if (resultToken.SelectToken("playing").Value<Boolean>() && !localSpotifyStatusResponse.Playing)
                            spotifyLocalApi.Play();
                        else if(resultToken.SelectToken("playing").Value<Boolean>() == false && localSpotifyStatusResponse.Playing)
                            spotifyLocalApi.Pause();

                        if (resultToken.SelectToken("nextPrevious").Value<String>() != "")
                            switch (resultToken.SelectToken("nextPrevious").Value<String>())
                            {
                                case ("next"):
                                    spotifyLocalApi.Skip();
                                    break;
                                case ("previous"):
                                    spotifyLocalApi.Previous();
                                    break;
                            }

                        if(resultToken.SelectToken("volume").Value<Int32>() != -1)
                            spotifyLocalApi.SetSpotifyVolume(resultToken.SelectToken("volume").Value<Int32>());


                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Sending spotify data failed");
                    continue;
                }

                try
                {
                    if (currentTrack != localSpotifyStatusResponse.Track.TrackResource.Name)
                    {
                        currentTrack = localSpotifyStatusResponse.Track.TrackResource.Name;
                        string albumArtUrl = localSpotifyStatusResponse.Track.GetAlbumArtUrl(AlbumArtSize.Size320);
                        try
                        {
                            await httpClient.PostAsync(serverUrl + "/spotifyCover", new StringContent(
                                "{\"albumArtUrl\":\""+albumArtUrl+"\"}",
                                Encoding.UTF8,
                                "application/json"
                                ));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Sending spotify cover url failed");
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }


                Thread.Sleep(200);
            }
        }



        void gameIntegrationWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            bool assettoCorsaRunning = false;

            BackgroundWorker localbg = (BackgroundWorker)e.Argument;
            while (!localbg.CancellationPending)
            {

                if(Process.GetProcessesByName("acs").Length > 0)
                {
                    if(assettoCorsaRunning)
                    {

                    }
                    else
                    {
                        assettoCorsa.StaticInfoInterval = 5000;
                        assettoCorsa.PhysicsInterval = 100;
                        assettoCorsa.PhysicsUpdated += assettoCorsa_PhysicsUpdated;
                        assettoCorsa.Start();

                        assettoCorsaRunning = true;
                        Console.WriteLine("Started assettoCorsa");
                    }
                }
                else
                {
                    if(assettoCorsaRunning)
                    {
                        assettoCorsa.Stop();

                        assettoCorsaRunning = false;
                        Console.WriteLine("Stoped assettoCorsa");
                    }
                }

                Thread.Sleep(500);
            }
        }



        private static void assettoCorsa_PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            string jsonSubstring = "{";

            jsonSubstring += "\"gas\": " + e.Physics.Gas;
            jsonSubstring += ",\"brake\": " + e.Physics.Brake;
            jsonSubstring += ",\"gear\": " + e.Physics.Gear;
            jsonSubstring += ",\"rpms\": " + e.Physics.Rpms;
            jsonSubstring += ",\"speedKmh\": " + e.Physics.SpeedKmh;
            jsonSubstring += ",\"accG\": " + JsonConvert.SerializeObject(e.Physics.AccG);
            jsonSubstring += ",\"tyreWear\": " + JsonConvert.SerializeObject(e.Physics.TyreWear);
            jsonSubstring += ",\"tyreCoreTemperature\": " + JsonConvert.SerializeObject(e.Physics.TyreCoreTemperature);

            jsonSubstring += "}";

            Console.WriteLine(jsonSubstring);
        }

        private async void sendAssettoCorsaData(string jsonString)
        {
            try
            {
                await httpClient.PostAsync(serverUrl + "/assettocorsa", new StringContent(
                    jsonString,
                    Encoding.UTF8,
                    "application/json"
                ));
            }
            catch (Exception)
            {
                Console.WriteLine("Assetto Corsa data sending failed");
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
            Environment.Exit(0);
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

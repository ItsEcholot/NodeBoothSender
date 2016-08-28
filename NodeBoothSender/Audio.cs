using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeBoothSender
{
    class Audio
    {
        //private IWaveIn lineIn;
        //private Stopwatch runTimeStopwatch;

        //private SampleAggregator sampleAggregator = new SampleAggregator(8192);
        //private BeatDetector beatDetector;
        //private BeatDetectorCPP beatDetectorCPP;
        private SpectrumBeatDetector szabBeatDetector;
        private Debug debugWindow;

        private List<byte> valueList = new List<byte>();

        public Audio(Debug debugWindow)
        {
            //sampleAggregator.FftCalculated += new EventHandler<FftEventArgs>(FftCalculated);
            //sampleAggregator.PerformFFT = true;

            //lineIn = new WasapiLoopbackCapture();
            //lineIn.DataAvailable += OnAudioDataAvailable;
            //lineIn.StartRecording();

            //runTimeStopwatch = Stopwatch.StartNew();

            //beatDetector = new BeatDetector(85.0f, 169.0f);
            //beatDetectorCPP = new BeatDetectorCPP();

            this.debugWindow = debugWindow;

            szabBeatDetector = new SpectrumBeatDetector(3);
            szabBeatDetector.Subscribe(beatDetected);
            szabBeatDetector.StartAnalysis();
        }

        void beatDetected(byte Value)
        {
            valueList.Add(Value);

            if (valueList.Count > 6)
                valueList.RemoveAt(0);

            int successCounter = 0;
            foreach(byte value in valueList)
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

        /*void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(OnAudioDataAvailable), sender, e);
            }
            else
            {
                byte[] buffer = e.Buffer;
                int bytesRecorded = e.BytesRecorded;
                int bufferIncrement = lineIn.WaveFormat.BlockAlign;

                for (int index = 0; index < bytesRecorded; index += bufferIncrement)
                {
                    float sample32 = BitConverter.ToSingle(buffer, index);
                    sampleAggregator.Add(sample32);
                }
            //}
        }*/

        
    }
}

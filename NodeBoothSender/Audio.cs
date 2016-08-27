using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeBoothSender
{
    class Audio
    {
        private IWaveIn lineIn;
        private SampleAggregator sampleAggregator = new SampleAggregator(8192);
        private BeatDetector beatDetector;
        private Timer secTimer;

        private int recSeconds;

        public Audio()
        {
            sampleAggregator.FftCalculated += new EventHandler<FftEventArgs>(FftCalculated);
            sampleAggregator.PerformFFT = true;

            lineIn = new WasapiLoopbackCapture();
            lineIn.DataAvailable += OnAudioDataAvailable;
            lineIn.StartRecording();

            recSeconds = 0;

            secTimer = new Timer(secTimerTick);
            secTimer.Change(0, 1000);

            beatDetector = new BeatDetector(85.0f, 169.0f);
        }

        void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            /*if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(OnAudioDataAvailable), sender, e);
            }
            else
            {*/
                byte[] buffer = e.Buffer;
                int bytesRecorded = e.BytesRecorded;
                int bufferIncrement = lineIn.WaveFormat.BlockAlign;

                for (int index = 0; index < bytesRecorded; index += bufferIncrement)
                {
                    float sample32 = BitConverter.ToSingle(buffer, index);
                    sampleAggregator.Add(sample32);
                }
            //}
        }

        void FftCalculated(object sender, FftEventArgs e)
        {
            //Console.WriteLine(e.Result[0].X + " - " + e.Result[0].Y);
            beatDetector.Process(recSeconds, e.Result);
        }

        private void secTimerTick(object sender)
        {
            recSeconds += 1;
        }
    }
}

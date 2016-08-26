using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetector
{
    /*
     * BeatDetektor.js
     *
     * BeatDetektor - CubicFX Visualizer Beat Detection & Analysis Algorithm
     * C# port by Marc Berchtold <shock2provide>
     *  
     * Copyright (c) 2009 Charles J. Cliffe.
     *
     * BeatDetektor is distributed under the terms of the MIT License.
     * http://opensource.org/licenses/MIT
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     * copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
     * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
     * IN THE SOFTWARE.
     */

    public class BeatDetector
    {
        //Variables///////////////////////////////////////////
        float BPM_MIN, BPM_MAX;
        int beat_counter, half_counter, quarter_counter;

        float[] a_freq_range;
        float[] ma_freq_range;
        float[] maa_freq_range;
        float[] last_detection;
        float[] ma_bpm_range;
        float[] maa_bpm_range;
        float[] detection_quality;
        bool[] detection;

        int ma_quality_avg, ma_quality_total;
        List<float> bpm_contest, bpm_contest_lo;
        float quality_total, quality_avg;
        float current_bpm, current_bpm_lo;
        float winning_bpm, win_val, winning_bpm_lo, win_val_lo;
        int win_bpm_int, win_bpm_int_lo;
        int bpm_predict;
        bool is_erratic;
        float bpm_offset, last_timer, last_update;
        float bpm_timer;
        //////////////////////////////////////////////////////

        public BeatDetector(float bpm_minimum, float bpm_maximum)
        {
            if (bpm_minimum == 0)
                bpm_minimum = 85.0f;
            if (bpm_maximum == 0)
                bpm_maximum = 169.0f;

            this.BPM_MIN = bpm_minimum;
            this.BPM_MAX = bpm_maximum;

            this.beat_counter = 0;
            this.half_counter = 0;
            this.quarter_counter = 0;


            // current average (this sample) for range n
            this.a_freq_range = new float[Properties.Settings.Default.BD_DETECTION_RANGES];
            // moving average of frequency range n
            this.ma_freq_range = new float[Properties.Settings.Default.BD_DETECTION_RANGES];
            // moving average of moving average of frequency range n
            this.maa_freq_range = new float[Properties.Settings.Default.BD_DETECTION_RANGES];
            // timestamp of last detection for frequecy range n
            this.last_detection = new float[Properties.Settings.Default.BD_DETECTION_RANGES];

            // moving average of gap lengths
            this.ma_bpm_range = new float[Properties.Settings.Default.BD_DETECTION_RANGES];
            // moving average of moving average of gap lengths
            this.maa_bpm_range = new float[Properties.Settings.Default.BD_DETECTION_RANGES];

            // range n quality attribute, good match  = quality+, bad match  = quality-, min  = 0
            this.detection_quality = new float[Properties.Settings.Default.BD_DETECTION_RANGES];

            // current trigger state for range n
            this.detection = new bool[Properties.Settings.Default.BD_DETECTION_RANGES];

            this.Reset();

            Console.WriteLine("BeatDetector(" + this.BPM_MIN + "," + this.BPM_MAX + ") created.");
        }

        public void Reset()
        {
            this.ma_quality_avg = 0;
            this.ma_quality_total = 0;

            this.bpm_contest = new List<float>();
            this.bpm_contest_lo = new List<float>();

            this.quality_total = 0.0f;
            this.quality_avg = 0.0f;

            this.current_bpm = 0.0f;
            this.current_bpm_lo = 0.0f;

            this.winning_bpm = 0.0f;
            this.win_val = 0.0f;
            this.winning_bpm_lo = 0.0f;
            this.win_val_lo = 0.0f;

            this.win_bpm_int = 0;
            this.win_bpm_int_lo = 0;

            this.bpm_predict = 0;

            this.is_erratic = false;
            this.bpm_offset = 0.0f;
            this.last_timer = 0.0f;
            this.last_update = 0.0f;

            this.bpm_timer = 0.0f;
            this.beat_counter = 0;
            this.half_counter = 0;
            this.quarter_counter = 0;
        }
    }
}

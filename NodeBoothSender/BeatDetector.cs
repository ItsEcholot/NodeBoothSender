using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeBoothSender
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

        double[] a_freq_range;
        double[] ma_freq_range;
        double[] maa_freq_range;
        double[] last_detection;
        double[] ma_bpm_range;
        double[] maa_bpm_range;
        double[] detection_quality;
        bool[] detection;

        double ma_quality_avg, maa_quality_avg, ma_quality_total;
        List<double> bpm_contest, bpm_contest_lo;
        double quality_total, quality_avg;
        double current_bpm, current_bpm_lo;
        double winning_bpm, win_val, winning_bpm_lo, win_val_lo;
        double win_bpm_int, win_bpm_int_lo;
        double bpm_predict;
        bool is_erratic;
        double bpm_offset, last_timer, last_update;
        double bpm_timer;
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
            this.a_freq_range = new double[Properties.Settings.Default.BD_DETECTION_RANGES];
            // moving average of frequency range n
            this.ma_freq_range = new double[Properties.Settings.Default.BD_DETECTION_RANGES];
            // moving average of moving average of frequency range n
            this.maa_freq_range = new double[Properties.Settings.Default.BD_DETECTION_RANGES];
            // timestamp of last detection for frequecy range n
            this.last_detection = new double[Properties.Settings.Default.BD_DETECTION_RANGES];

            // moving average of gap lengths
            this.ma_bpm_range = new double[Properties.Settings.Default.BD_DETECTION_RANGES];
            // moving average of moving average of gap lengths
            this.maa_bpm_range = new double[Properties.Settings.Default.BD_DETECTION_RANGES];

            // range n quality attribute, good match  = quality+, bad match  = quality-, min  = 0
            this.detection_quality = new double[Properties.Settings.Default.BD_DETECTION_RANGES];

            // current trigger state for range n
            this.detection = new bool[Properties.Settings.Default.BD_DETECTION_RANGES];

            this.Reset();

            Console.WriteLine("BeatDetector(" + this.BPM_MIN + "," + this.BPM_MAX + ") created.");
        }

        public void Reset()
        {
            this.ma_quality_avg = 0.0d;
            this.ma_quality_total = 0;

            this.bpm_contest = new List<double>();
            this.bpm_contest_lo = new List<double>();

            this.quality_total = 0.0d;
            this.quality_avg = 0.0d;

            this.current_bpm = 0.0d;
            this.current_bpm_lo = 0.0d;

            this.winning_bpm = 0.0d;
            this.win_val = 0.0d;
            this.winning_bpm_lo = 0.0d;
            this.win_val_lo = 0.0d;

            this.win_bpm_int = 0;
            this.win_bpm_int_lo = 0;

            this.bpm_predict = 0;

            this.is_erratic = false;
            this.bpm_offset = 0.0d;
            this.last_timer = 0.0d;
            this.last_update = 0.0d;

            this.bpm_timer = 0.0d;
            this.beat_counter = 0;
            this.half_counter = 0;
            this.quarter_counter = 0;
        }

        public void Process(int timer_seconds, NAudio.Dsp.Complex[] fft)
        {
            // ignore 0 start time
            if (this.last_timer == 0)   {
                this.last_timer = timer_seconds;
                return;
            }  

            // ignore if timer_seconds is lower than before
            if (this.last_timer > timer_seconds) {
                this.Reset();
                return;
            }

            int timestamp = timer_seconds;

            this.last_update = timer_seconds - this.last_timer;
            this.last_timer = timer_seconds;

            if (this.last_update > 1.0) {
                this.Reset();
                return;
            }

            int i, x;
            int v;

            double bpm_floor = 60.0 / this.BPM_MAX;
            double bpm_ceil = 60.0 / this.BPM_MIN;

            int range_step = (fft.Count() / Properties.Settings.Default.BD_DETECTION_RANGES);
            int range = 0;

            for (x = 0; x < fft.Count(); x += range_step)
            {
                this.a_freq_range[range] = 0;

                // accumulate frequency values for this range
                for (i = x; i < x + range_step; i++)
                {
                    v = Math.Abs((int)fft[i].X);
                    this.a_freq_range[range] += v;
                }

                // average for range
                this.a_freq_range[range] /= range_step;

                // two sets of averages chase this one at a 

                // moving average, increment closer to a_freq_range at a rate of 1.0 / BD_DETECTION_RATE seconds
                this.ma_freq_range[range] -= (this.ma_freq_range[range] - this.a_freq_range[range]) * this.last_update * Properties.Settings.Default.BD_DETECTION_RATE;
                // moving average of moving average, increment closer to this.ma_freq_range at a rate of 1.0 / BD_DETECTION_RATE seconds
                this.maa_freq_range[range] -= (this.maa_freq_range[range] - this.ma_freq_range[range]) * this.last_update * Properties.Settings.Default.BD_DETECTION_RATE;

                // if closest moving average peaks above trailing (with a tolerance of BD_DETECTION_FACTOR) then trigger a detection for this range 
                var det = (this.ma_freq_range[range] * Properties.Settings.Default.BD_DETECTION_FACTOR >= this.maa_freq_range[range]);

                // compute bpm clamps for comparison to gap lengths

                // clamp detection averages to input ranges
                if (this.ma_bpm_range[range] > bpm_ceil) this.ma_bpm_range[range] = bpm_ceil;
                if (this.ma_bpm_range[range] < bpm_floor) this.ma_bpm_range[range] = bpm_floor;
                if (this.maa_bpm_range[range] > bpm_ceil) this.maa_bpm_range[range] = bpm_ceil;
                if (this.maa_bpm_range[range] < bpm_floor) this.maa_bpm_range[range] = bpm_floor;

                var rewarded = false;

                // new detection since last, test it's quality
                if (!this.detection[range] && det)
                {
                    // calculate length of gap (since start of last trigger)
                    var trigger_gap = timestamp - this.last_detection[range];

                    // trigger falls within acceptable range, 
                    if (trigger_gap < bpm_ceil && trigger_gap > (bpm_floor))
                    {
                        // compute gap and award quality

                        // use our tolerances as a funnel to edge detection towards the most likely value
                        for (i = 0; i < Properties.Settings.Default.BD_REWARD_TOLERANCES.Count(); i++)
                        {
                            if (Math.Abs(this.ma_bpm_range[range] - trigger_gap) < this.ma_bpm_range[range] * Properties.Settings.Default.BD_REWARD_TOLERANCES[i])
                            {
                                this.detection_quality[range] += Properties.Settings.Default.BD_QUALITY_REWARD * Properties.Settings.Default.BD_REWARD_MULTIPLIERS[i];
                                rewarded = true;
                            }
                        }

                        if (rewarded)
                        {
                            this.last_detection[range] = timestamp;
                        }
                    }
                    else if (trigger_gap >= bpm_ceil) // low quality, gap exceeds maximum time
                    {
                        // start a new gap test, next gap is guaranteed to be longer

                        // test for 1/2 beat
                        trigger_gap /= 2.0;

                        if (trigger_gap < bpm_ceil && trigger_gap > (bpm_floor))
                            for (i = 0; i < Properties.Settings.Default.BD_REWARD_TOLERANCES.Count(); i++)
                            {
                                if (Math.Abs(this.ma_bpm_range[range] - trigger_gap) < this.ma_bpm_range[range] * Properties.Settings.Default.BD_REWARD_TOLERANCES[i])
                                {
                                    this.detection_quality[range] += Properties.Settings.Default.BD_QUALITY_REWARD * Properties.Settings.Default.BD_REWARD_MULTIPLIERS[i];
                                    rewarded = true;
                                }
                            }


                        // decrement quality if no 1/2 beat reward
                        if (!rewarded)
                        {
                            trigger_gap *= 2.0;
                        }
                        this.last_detection[range] = timestamp;
                    }

                    if (rewarded)
                    {
                        var qmp = (this.detection_quality[range] / this.quality_avg) * Properties.Settings.Default.BD_QUALITY_STEP;
                        if (qmp > 1.0)
                        {
                            qmp = 1.0;
                        }

                        this.ma_bpm_range[range] -= (this.ma_bpm_range[range] - trigger_gap) * qmp;
                        this.maa_bpm_range[range] -= (this.maa_bpm_range[range] - this.ma_bpm_range[range]) * qmp;
                    }
                    else if (trigger_gap >= bpm_floor && trigger_gap <= bpm_ceil)
                    {
                        if ((this.detection_quality[range] < this.quality_avg * Properties.Settings.Default.BD_QUALITY_TOLERANCE) && this.current_bpm != 0)
                        {
                            this.ma_bpm_range[range] -= (this.ma_bpm_range[range] - trigger_gap) * Properties.Settings.Default.BD_QUALITY_STEP;
                            this.maa_bpm_range[range] -= (this.maa_bpm_range[range] - this.ma_bpm_range[range]) * Properties.Settings.Default.BD_QUALITY_STEP;
                        }
                        this.detection_quality[range] -= Properties.Settings.Default.BD_QUALITY_STEP;
                    }
                    else if (trigger_gap >= bpm_ceil)
                    {
                        if ((this.detection_quality[range] < this.quality_avg * Properties.Settings.Default.BD_QUALITY_TOLERANCE) && this.current_bpm != 0)
                        {
                            this.ma_bpm_range[range] -= (this.ma_bpm_range[range] - this.current_bpm) * 0.5;
                            this.maa_bpm_range[range] -= (this.maa_bpm_range[range] - this.ma_bpm_range[range]) * 0.5;
                        }
                        this.detection_quality[range] -= Properties.Settings.Default.BD_QUALITY_STEP;
                    }

                }

                if ((!rewarded && timestamp - this.last_detection[range] > bpm_ceil) || (det && Math.Abs(this.ma_bpm_range[range] - this.current_bpm) > this.bpm_offset))
                    this.detection_quality[range] -= this.detection_quality[range] * Properties.Settings.Default.BD_QUALITY_STEP * Properties.Settings.Default.BD_QUALITY_DECAY * this.last_update;

                // quality bottomed out, set to 0
                if (this.detection_quality[range] < 0.001) this.detection_quality[range] = 0.001;

                this.detection[range] = det;

                range++;
            }


            // total contribution weight
            this.quality_total = 0;

            // total of bpm values
            double bpm_total = 0.0d;
            // number of bpm ranges that contributed to this test
            int bpm_contributions = 0;


            // accumulate quality weight total
            for (x = 0; x < Properties.Settings.Default.BD_DETECTION_RANGES; x++)
            {
                this.quality_total += this.detection_quality[x];
            }


            this.quality_avg = this.quality_total / Properties.Settings.Default.BD_DETECTION_RANGES;

            if (this.quality_total != 0)
            {
                // determine the average weight of each quality range
                this.ma_quality_avg += (this.quality_avg - this.ma_quality_avg) * this.last_update * Properties.Settings.Default.BD_DETECTION_RATE / 2.0;

                this.maa_quality_avg += (this.ma_quality_avg - this.maa_quality_avg) * this.last_update;
                this.ma_quality_total += (this.quality_total - this.ma_quality_total) * this.last_update * Properties.Settings.Default.BD_DETECTION_RATE / 2.0;

                this.ma_quality_avg -= 0.98 * this.ma_quality_avg * this.last_update * 3.0;
            }
            else
            {
                this.quality_avg = 0.001;
            }

            if (this.ma_quality_total <= 0) this.ma_quality_total = 0.001;
            if (this.ma_quality_avg <= 0) this.ma_quality_avg = 0.001;

            double avg_bpm_offset = 0.0;
            double offset_test_bpm = this.current_bpm;
            List<double> draft = new List<double>();

            if (this.quality_avg != 0)
                for (x = 0; x < Properties.Settings.Default.BD_DETECTION_RANGES; x++)
                {
                    // if this detection range weight*tolerance is higher than the average weight then add it's moving average contribution 
                    if (this.detection_quality[x] * Properties.Settings.Default.BD_QUALITY_TOLERANCE >= this.ma_quality_avg)
                    {
                        if (this.ma_bpm_range[x] < bpm_ceil && this.ma_bpm_range[x] > bpm_floor)
                        {
                            bpm_total += this.maa_bpm_range[x];

                            double draft_float = Math.Round((60.0 / this.maa_bpm_range[x]) * 1000.0);

                            draft_float = (Math.Abs(Math.Ceiling(draft_float) - (60.0 / this.current_bpm) * 1000.0) < (Math.Abs(Math.Floor(draft_float) - (60.0 / this.current_bpm) * 1000.0))) ? Math.Ceiling(draft_float / 10.0) : Math.Floor(draft_float / 10.0);
                            int draft_int = (int)(draft_float / 10.0);
                            //	if (draft_int) console.log(draft_int);
                            if (draft[draft_int] == 0.0)
                                draft[draft_int] = 0;

                            draft[draft_int] += this.detection_quality[x] / this.quality_avg;
                            bpm_contributions++;
                            if (offset_test_bpm == 0.0) offset_test_bpm = this.maa_bpm_range[x];
                            else
                            {
                                avg_bpm_offset += Math.Abs(offset_test_bpm - this.maa_bpm_range[x]);
                            }


                            }
                        }
                    }

            // if we have one or more contributions that pass criteria then attempt to display a guess
            bool has_prediction = (bpm_contributions >= Properties.Settings.Default.BD_MINIMUM_CONTRIBUTIONS) ? true : false;

            double draft_winner = 0;
            double win_val = 0;

            if (has_prediction)
            {
		        foreach (double draft_i in draft)
                {
                    if (draft_i > win_val)
                    {
                        win_val = draft_i;
                        draft_winner = draft_i;
                    }
                }

                this.bpm_predict = 60.0 / (draft_winner / 10.0);

                avg_bpm_offset /= bpm_contributions;
                this.bpm_offset = avg_bpm_offset;

                if (this.current_bpm == 0)
                {
                    this.current_bpm = this.bpm_predict;
                }
            }

            if (this.current_bpm != 0 && this.bpm_predict != 0)
                this.current_bpm -= (this.current_bpm - this.bpm_predict) * this.last_update;

            // hold a contest for bpm to find the current mode
            double contest_max = 0;
	
	        foreach (double contest_i in this.bpm_contest)
            {
                if (contest_max < contest_i)
                    contest_max = contest_i;
                if (contest_i > Properties.Settings.Default.BD_FINISH_LINE / 2.0)
                {
                    int draft_int_lo = (int)(Math.Round((contest_i) / 10.0));
                    if (this.bpm_contest_lo[draft_int_lo] != this.bpm_contest_lo[draft_int_lo]) this.bpm_contest_lo[draft_int_lo] = 0;
                    this.bpm_contest_lo[draft_int_lo] += (contest_i / 6.0) * this.last_update;
                }
            }

            // normalize to a finish line
            if (contest_max > Properties.Settings.Default.BD_FINISH_LINE)
            {
                for(int contest_i = 0; contest_i<this.bpm_contest.Count(); contest_i++)
                {
                    this.bpm_contest[contest_i] = (this.bpm_contest[contest_i] / contest_max) * Properties.Settings.Default.BD_FINISH_LINE;
                }
            }

            contest_max = 0;
	        foreach (var contest_i in this.bpm_contest_lo)
            {
                if (contest_max < contest_i)
                    contest_max = contest_i;
            }

            // normalize to a finish line
            if (contest_max > Properties.Settings.Default.BD_FINISH_LINE)
            {
                for (int contest_i = 0; contest_i < this.bpm_contest_lo.Count(); contest_i++)
                {
                    this.bpm_contest_lo[contest_i] = (this.bpm_contest_lo[contest_i] / contest_max) * Properties.Settings.Default.BD_FINISH_LINE;
                }
            }


            // decay contest values from last loop
            for(int contest_i=0; contest_i<this.bpm_contest.Count(); contest_i++)
            {
                this.bpm_contest[contest_i] -= this.bpm_contest[contest_i] * (this.last_update / Properties.Settings.Default.BD_DETECTION_RATE);
            }

            // decay contest values from last loop
            for(int contest_i=0; contest_i<this.bpm_contest_lo.Count(); contest_i++)
            {
                this.bpm_contest_lo[contest_i] -= this.bpm_contest_lo[contest_i] * (this.last_update / Properties.Settings.Default.BD_DETECTION_RATE);
            }

            this.bpm_timer += this.last_update;

            double winner = 0;
            double winner_lo = 0;

            // attempt to display the beat at the beat interval ;)
            if (this.bpm_timer > (this.winning_bpm / 4.0) && this.current_bpm != 0)
            {
                this.win_val = 0;
                this.win_val_lo = 0;

                if (this.winning_bpm != 0)
                    while (this.bpm_timer > this.winning_bpm / 4.0)
                        this.bpm_timer -= this.winning_bpm / 4.0;

                // increment beat counter

                this.quarter_counter++;
                this.half_counter = (int)(this.quarter_counter / 2);
                this.beat_counter = (int)(this.quarter_counter / 4);

                // award the winner of this iteration
                var idx = (int)(Math.Round((60.0 / this.current_bpm) * 10.0));
                if (this.bpm_contest[idx] == 0.0)
                    this.bpm_contest[idx] = 0;
                this.bpm_contest[idx] += Properties.Settings.Default.BD_QUALITY_REWARD;
		
		
		        // find the overall winner so far
		        foreach (double contest_i in this.bpm_contest)
                {
                    if (this.win_val < contest_i)
                    {
                        winner = contest_i;
                        this.win_val = contest_i;
                    }
                }

                if (winner != 0)
                {
                    this.win_bpm_int = (int)(winner);
                    this.winning_bpm = (60.0 / (winner / 10.0));
                }
		
		        // find the overall winner so far
		        foreach (double contest_i in this.bpm_contest_lo)
                {
                    if (this.win_val_lo < contest_i)
                    {
                        winner_lo = contest_i;
                        this.win_val_lo = contest_i;
                    }
                }

                if (winner_lo != 0)
                {
                    this.win_bpm_int_lo = (int)(winner_lo);
                    this.winning_bpm_lo = 60.0 / winner_lo;
                }


                if ((this.beat_counter % 4) == 0)
                    Console.WriteLine("BeatDetektor(" + this.BPM_MIN + "," + this.BPM_MAX + "): [ Current Estimate: " + winner + " BPM ] [ Time: " + ((int)(timer_seconds * 1000.0) / 1000.0) + "s, Quality: " + ((int)(this.quality_total * 1000.0) / 1000.0) + ", Rank: " + ((int)(this.win_val * 1000.0) / 1000.0) + ", Jitter: " + ((int)(this.bpm_offset * 1000000.0) / 1000000.0) + " ]");
            }
        }
    }
}

using System;
using System.Windows.Forms;

namespace NodeBoothSender
{
    partial class Debug
    {
        private Timer decayTimer;

        public delegate void updateBeatProgressBarDelegate(int value);
        public updateBeatProgressBarDelegate updateBeatProgressBar;

        void updateProgressBar1(int value)
        {
            beatProgressBar.Value = value;
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.beatProgressBar = new System.Windows.Forms.ProgressBar();
            this.decayTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // beatProgressBar
            // 
            this.beatProgressBar.Location = new System.Drawing.Point(12, 75);
            this.beatProgressBar.Name = "beatProgressBar";
            this.beatProgressBar.Size = new System.Drawing.Size(260, 31);
            this.beatProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.beatProgressBar.TabIndex = 0;
            this.beatProgressBar.Value = 100;
            // 
            // decayTimer
            // 
            this.decayTimer.Enabled = true;
            this.decayTimer.Tick += new System.EventHandler(this.decayBeatProgressBar);
            // 
            // Debug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.beatProgressBar);
            this.Name = "Debug";
            this.Text = "Debug";
            this.ResumeLayout(false);

            updateBeatProgressBar = new updateBeatProgressBarDelegate(updateProgressBar1);
            this.Show();
        }

        private void decayBeatProgressBar(object sender, EventArgs e)
        {
            /*if (this.beatProgressBar.Value >= 10)
                this.beatProgressBar.Value -= 10;*/
            this.beatProgressBar.Value = 0;
        }

        #endregion

        private System.Windows.Forms.ProgressBar beatProgressBar;
    }
}
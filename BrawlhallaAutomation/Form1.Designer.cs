namespace BrawlhallaAutomation
{
    partial class Form1
    {
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
            this.txtLog = new System.Windows.Forms.TextBox();
            this.titleBar = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnMinimize = new System.Windows.Forms.Button();
            // ... declare all controls

            this.SuspendLayout();

            // Initialize your controls here
            this.txtLog.Location = new System.Drawing.Point(0, 30);
            this.txtLog.Size = new System.Drawing.Size(760, 445);
            // ... set all properties

            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.titleBar);
            // ... add all controls

            this.ResumeLayout(false);
        }

        #endregion
    }
}

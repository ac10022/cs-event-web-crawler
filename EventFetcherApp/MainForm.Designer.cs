namespace EventFetcherApp
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            EventCollection = new CheckedListBox();
            EventsLabel = new Label();
            GetEventsButton = new Button();
            DestSelector = new SaveFileDialog();
            FetchingLabel = new Label();
            SuspendLayout();
            // 
            // EventCollection
            // 
            EventCollection.FormattingEnabled = true;
            EventCollection.Location = new Point(21, 55);
            EventCollection.Name = "EventCollection";
            EventCollection.Size = new Size(180, 144);
            EventCollection.TabIndex = 0;
            // 
            // EventsLabel
            // 
            EventsLabel.AutoSize = true;
            EventsLabel.Location = new Point(20, 24);
            EventsLabel.Name = "EventsLabel";
            EventsLabel.Size = new Size(156, 25);
            EventsLabel.TabIndex = 1;
            EventsLabel.Text = "Get events from ...";
            // 
            // GetEventsButton
            // 
            GetEventsButton.Location = new Point(281, 136);
            GetEventsButton.Name = "GetEventsButton";
            GetEventsButton.Size = new Size(112, 63);
            GetEventsButton.TabIndex = 2;
            GetEventsButton.Text = "Start";
            GetEventsButton.UseVisualStyleBackColor = true;
            GetEventsButton.Click += FetchEvents;
            // 
            // FetchingLabel
            // 
            FetchingLabel.AutoSize = true;
            FetchingLabel.BorderStyle = BorderStyle.FixedSingle;
            FetchingLabel.Location = new Point(217, 72);
            FetchingLabel.Name = "FetchingLabel";
            FetchingLabel.Size = new Size(188, 52);
            FetchingLabel.TabIndex = 3;
            FetchingLabel.Text = "Fetching...\r\n(this may take a while)\r\n";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(425, 230);
            Controls.Add(FetchingLabel);
            Controls.Add(GetEventsButton);
            Controls.Add(EventsLabel);
            Controls.Add(EventCollection);
            Name = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckedListBox EventCollection;
        private Label EventsLabel;
        private Button GetEventsButton;
        private SaveFileDialog DestSelector;
        private Label FetchingLabel;
    }
}

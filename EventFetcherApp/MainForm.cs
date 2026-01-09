using EventFetcher;
using System.Diagnostics;
using System.Linq.Expressions;

namespace EventFetcherApp
{
    public partial class MainForm : Form
    {
        private readonly IList<TargetWrapper> labels = new List<TargetWrapper> {
            new TargetWrapper("TechUK", CrawlerTarget.TechUK),
            new TargetWrapper("CenturyClub", CrawlerTarget.CenturyClub),
            new TargetWrapper("Meetup", CrawlerTarget.Meetup),
            new TargetWrapper("Eventbrite", CrawlerTarget.Eventbrite),
        };

        private string destinationPath = String.Empty;

        public MainForm()
        {
            InitializeComponent();
            PopulateEventCollection();
        }

        private void PopulateEventCollection()
        {
            foreach (TargetWrapper label in labels) 
                EventCollection.Items.Add(label);
        }

        private string SelectDestination()
        {
            try
            {
                DestSelector.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // default to desktop
                DestSelector.Title = "Choose where to save the events.";
                DestSelector.DefaultExt = @".csv";
                DestSelector.CheckPathExists = true;
                DestSelector.Filter = @"CSV files (*.csv)|*.csv";
                DestSelector.RestoreDirectory = true;
                DestSelector.AddExtension = true;

                if (DestSelector.ShowDialog() == DialogResult.OK) 
                    return DestSelector.FileName;
                else 
                    throw new Exception("No destination selected.");
            }
            catch (Exception e) 
            {
                ErrorHandler.Show("Form", "Could not save file.", e);
                return String.Empty;
            }
        }

        private void FetchEvents(object sender, EventArgs e)
        {
            if (EventCollection.CheckedItems.Count == 0)
            {
                ErrorHandler.Show("Form", "No source selected.");
                return;
            }
            
            destinationPath = SelectDestination();

            if (destinationPath == String.Empty)
            {
                ErrorHandler.Show("Form", "No destination selected."); ;
                return;
            }

            IList<CrawlerTarget> targets = EventCollection.CheckedItems
                                                .Cast<TargetWrapper>()
                                                .Select(x => x.Target)
                                                .ToList();

            GetEventsButton.Enabled = false;

            if (new Exporter(targets, destinationPath).Result == ExporterResult.OK)
                OpenExport();
            else
                ErrorHandler.Show("Form", "Fatal error: could not fetch events.");

            GetEventsButton.Enabled = true;
        }

        private void OpenExport()
        {
            DialogResult res = MessageBox.Show($"Successfully exported events to {destinationPath}. Open file?", "Success", MessageBoxButtons.OK);

            if (res == DialogResult.OK)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = destinationPath,
                        UseShellExecute = true
                    };
                    
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    ErrorHandler.Show("Form", $"Unable to open exported file: {destinationPath}", ex);
                }
            }
        }
    }

    public class TargetWrapper
    {
        private string label;
        private CrawlerTarget target;

        public string Label => label;
        public CrawlerTarget Target => target;

        public TargetWrapper(string label, CrawlerTarget target)
        {
            this.label = label;
            this.target = target;
        }

        public override string ToString() 
            => label;
    }
}
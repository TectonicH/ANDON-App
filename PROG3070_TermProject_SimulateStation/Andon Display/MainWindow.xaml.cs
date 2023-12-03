using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Configuration;

namespace Andon_Display
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // DispatcherTimer for periodic updates
        private DispatcherTimer _timer;
        private ObservableCollection<PartStatus> _parts;

        public MainWindow()
        {
            InitializeComponent();
            _parts = new ObservableCollection<PartStatus>();
            ListViewBins.ItemsSource = _parts;

            // Set up the timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5); // Update every 5 seconds
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initial load
            LoadData();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            // Fetch data from the database and update the UI
            _parts.Clear();
            var partsData = GetStationProgressAsync(1); // Use the correct station ID here
            foreach (var part in partsData)
            {
                _parts.Add(part);
            }
        }

        private async Task<StationProgress> GetStationProgressAsync(int stationId)
        {
            var progress = new StationProgress();
            string connectionString = "your_connection_string"; // Replace with your actual connection string

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                // Query to select from the function
                string sql = $"SELECT * FROM dbo.AssemblyStationProgress({stationId})";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Map the data from the reader to the StationProgress object
                            progress.StationID = (int)reader["StationID"];
                            progress.LampInProgress = (int)reader["LampInProgress"];
                            progress.LampsCompletedSuccessfully = (int)reader["LampsCompletedSuccessfully"];
                            progress.DefectiveAssembledLamps = (int)reader["DefectiveAssembledLamps"];
                            progress.HarnessPartCount = (int)reader["HarnessPartCount"];
                            progress.HousingPartCount = (int)reader["HousingPartCount"];
                            progress.ReflectorPartCount = (int)reader["ReflectorPartCount"];
                            progress.BulbPartCount = (int)reader["BulbPartCount"];
                            progress.BezelPartCount = (int)reader["BezelPartCount"];
                            progress.LensPartCount = (int)reader["LensPartCount"];
                            progress.IsActive = (bool)reader["IsActive"];
                            progress.RunnerTasksBeingCompleted = (int)reader["RunnerTasksBeingCompleted"];
                            progress.RunnerTasksPending = (int)reader["RunnerTasksPending"];
                        }
                    }
                }
            }

            return progress;
        }

    }

    public class PartStatus
    {
        public string PartId { get; set; }
        public int CurrentQuantity { get; set; }
        public string Status { get; set; }
        public SolidColorBrush StatusColor => new SolidColorBrush(GetColorByStatus(Status));

        private Color GetColorByStatus(string status)
        {
            return status switch
            {
                "OK" => Colors.Green,
                "SUPPORT" => Colors.Yellow,
                "STOPPED" => Colors.Red,
                _ => Colors.Gray,
            };
        }
    }
}
}
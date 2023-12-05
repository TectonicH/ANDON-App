using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Data.SqlClient;

namespace RunnerStationStatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Bin> bins = new ObservableCollection<Bin>();

        public MainWindow()
        {
            InitializeComponent();
            ListViewBins.ItemsSource = bins;
            RefreshBins();
        }

        private async void RefreshBins()
        {
            while (true) 
            {
                var binsToUpdate = await GetBinsNeedingReplacementAsync();
                UpdateBinStatus(binsToUpdate);
                await Task.Delay(5000); // Refresh every 5 seconds
            }
        }

        private async Task<Bin[]> GetBinsNeedingReplacementAsync()
        {
            var binsList = new ObservableCollection<Bin>();
            string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string sql = "SELECT BinID, PartID, StationID, TaskInProgress FROM RunnerStationTaskBreakdownView";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var bin = new Bin
                            {
                                BinId = reader.GetInt32(0),
                                PartId = reader.GetString(1),
                                StationId = reader.GetInt32(2),
                                TaskInProgress = reader.GetBoolean(3),
                                Status = reader.GetBoolean(3) ? "In Progress" : "Pending"
                            };
                            binsList.Add(bin);
                        }
                    }
                }

            }
            return binsList.ToArray();
        }

        private void UpdateBinStatus(Bin[] binsToUpdate)
        {
            Dispatcher.Invoke(() =>
            {
                bins.Clear();
                foreach (var bin in binsToUpdate)
                {
                    bins.Add(bin);
                }
            });
        }
    }

    public class Bin
    {
        public int BinId { get; set; }
        public string PartId { get; set; }
        public int StationId { get; set; }
        public bool TaskInProgress { get; set; }
        public string Status { get; set; }
    }

}

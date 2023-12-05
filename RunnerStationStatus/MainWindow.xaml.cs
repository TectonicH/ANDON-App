/*
* FILE : MainWindow.xaml.cs
* PROJECT : PROG3070 - Final Project
* PROGRAMMER : Kristian Biviens & Elizabeth deVries
* FIRST VERSION : 2023-12-05
* DESCRIPTION : This functions in this file are used monitor and update the status of bins 
*               in the production environment. 
*/

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

    public partial class MainWindow : Window
    {
        private ObservableCollection<Bin> bins = new ObservableCollection<Bin>();

        public MainWindow()
        {
            InitializeComponent();
            ListViewBins.ItemsSource = bins;
            RefreshBins();
        }

        /*
        * FUNCTION: RefreshBins
        * DESCRIPTION: Continuously updates the bin status by fetching data at regular intervals.
        *              The data refresh happens every 5 seconds asynchronously.
        */
        private async void RefreshBins()
        {
            while (true) 
            {
                var binsToUpdate = await GetBinsNeedingReplacementAsync();
                UpdateBinStatus(binsToUpdate);
                await Task.Delay(5000); // Refresh every 5 seconds
            }
        }

        /*
         * FUNCTION: GetBinsNeedingReplacementAsync
         * DESCRIPTION: Asynchronously retrieves an array of bins that need replacement or attention.
         *              Connects to a database to fetch current bin status.
         * RETURNS: Task<Bin[]>: A task that represents the asynchronous operation and returns an array of Bin objects.
         */
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

       /*
        * FUNCTION: UpdateBinStatus
        * DESCRIPTION: Updates the UI with the latest bin status. It clears the existing bins
        *              and adds the updated bins from the binsToUpdate array.
        * PARAMETERS: Bin[] binsToUpdate: An array of bins to update in the UI.
        */
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

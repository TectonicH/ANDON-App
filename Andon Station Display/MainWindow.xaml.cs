/*
* FILE : MainWindow.xaml.cs
* PROJECT : PROG3070 - Final Project
* PROGRAMMER : Kristian Biviens & Elizabeth deVries
* FIRST VERSION : 2023-12-05
* DESCRIPTION : This functions in this file are used to create a view of an assembly station management, showing 
*               real-time monitoring, inventory management, and operational efficiency. 
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
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
using System.Windows.Threading;
using System.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

namespace Andon_Station_Display
{
    
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Properties for binding to the UI
        private int lampsInProgress;
        private int lampsCompleted;
        private int lampsDefective;
        private int minPartCountForNotification;
        private Visibility runnerSignalVisibility;
        private ObservableCollection<BinStatus> binStatuses;
        private int selectedStationID;
        public int SelectedStationID
        {
            get => selectedStationID;
            set
            {
                if (selectedStationID != value)
                {
                    selectedStationID = value;
                    OnPropertyChanged(nameof(SelectedStationID));
                }
            }
        }

        public int LampsInProgress
        {
            get => lampsInProgress;
            set
            {
                lampsInProgress = value;
                OnPropertyChanged();
            }
        }

        public int LampsCompleted
        {
            get => lampsCompleted;
            set
            {
                lampsCompleted = value;
                OnPropertyChanged();
            }
        }

        public int LampsDefective
        {
            get => lampsDefective;
            set
            {
                lampsDefective = value;
                OnPropertyChanged();
            }
        }

        public int MinPartCountForNotification
        {
            get => minPartCountForNotification;
            set
            {
                minPartCountForNotification = value;
                OnPropertyChanged();
            }
        }

        public Visibility RunnerSignalVisibility
        {
            get => runnerSignalVisibility;
            set
            {
                runnerSignalVisibility = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BinStatus> BinStatuses
        {
            get { return binStatuses; }
            set
            {
                binStatuses = value;
                OnPropertyChanged(nameof(BinStatuses));
            }
        }

        // DispatcherTimer for refreshing data
        private DispatcherTimer refreshTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            BinStatuses = new ObservableCollection<BinStatus>();


            // Prompting the user for the station ID
            string input = Interaction.InputBox("Please enter the station ID:", "Station ID", "1", -1, -1);

            if (int.TryParse(input, out int stationId) && stationId > 0)
            {
                SelectedStationID = stationId;
                LoadData();
            }
            else
            {
                MessageBox.Show("You must enter a valid station ID to continue.");
                Close(); // Close the application if no valid ID is provided
            }



            // Set up the DispatcherTimer
            refreshTimer = new DispatcherTimer();
            refreshTimer.Tick += new EventHandler(RefreshData);
            refreshTimer.Interval = new TimeSpan(0, 0, 5); // refresh every 5 seconds
            refreshTimer.Start();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

       /*
        * FUNCTION: OnPropertyChanged
        * DESCRIPTION: Notifies clients that a property value has changed.
        * PARAMETERS: string propertyName = null: The name of the property that changed.
        */
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       /*
        * FUNCTION: RefreshData
        * DESCRIPTION: Refreshes the data from the database at regular intervals. It fetches 
        *              minimum part count for notification, lamp status, and bin status.
        * PARAMETERS: object sender: The source of the event.
        *             EventArgs e: An object that contains the event data.
        */
        private void RefreshData(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                int stationId = this.SelectedStationID; 
                FetchMinPartCountForNotification(connection);
                GetLampStatus(connection, stationId);
                GetBinStatus(connection, stationId);
            }
        }

       /*
        * FUNCTION: GetLampStatus
        * DESCRIPTION: Retrieves the status of lamps from the database.
        * PARAMETERS: SqlConnection connection: The database connection.
        *             int stationId: The ID of the station to fetch data for.
        */
        private void GetLampStatus(SqlConnection connection, int stationId)
        {
            string query = @" 
        SELECT [Status], COUNT(*) as Count
        FROM FogLamps 
        WHERE StationID = @stationId 
        GROUP BY [Status]";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@stationId", stationId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // Reset the counts
                    LampsInProgress = 0;
                    LampsCompleted = 0;
                    LampsDefective = 0;

                    while (reader.Read())
                    {
                        string status = reader.GetString(0).Trim();
                        int count = reader.GetInt32(1);

                        switch (status)
                        {
                            case "InProgress":
                                LampsInProgress = count;
                                break;
                            case "Completed":
                                LampsCompleted = count;
                                break;
                            case "Defective":
                                LampsDefective = count;
                                break;
                        }
                    }
                }
            }
        }

       /*
        * FUNCTION: FetchMinPartCountForNotification
        * DESCRIPTION: Retrieves the minimum part count for notification setting from the database.
        * PARAMETERS: SqlConnection connection: The database connection.
        */
        private void GetBinStatus(SqlConnection connection, int stationId)
        {
            string query = @"
        SELECT b.PartID, b.BinID, b.CurrentQuantity
        FROM Bins b
        INNER JOIN AssemblyStations a ON a.HarnessBin = b.BinID OR
                                            a.ReflectorBin = b.BinID OR
                                            a.BulbBin = b.BinID OR
                                            a.BezelBin = b.BinID OR
                                            a.HousingBin = b.BinID OR
                                            a.LensBin = b.BinID
        WHERE a.StationID = @stationId";

            bool shouldNotifyRunner = false;
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@stationId", stationId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    BinStatuses.Clear();
                    while (reader.Read())
                    {
                        int currentQuantity = reader.GetInt32(2);
                        BinStatuses.Add(new BinStatus
                        {
                            PartId = reader.GetString(0).Trim(),
                            BinId = reader.GetInt32(1),
                            CurrentQuantity = currentQuantity,
                            // The status text could be "Low" or "Sufficient" depending on your requirements
                            Status = currentQuantity <= MinPartCountForNotification ? "Low" : "Sufficient"
                        });

                        if (currentQuantity <= MinPartCountForNotification)
                        {
                            shouldNotifyRunner = true; // At least one bin is below threshold
                        }
                    }
                }
            }
            // Update the visibility of the replenish parts signal based on the flag
            RunnerSignalVisibility = shouldNotifyRunner ? Visibility.Visible : Visibility.Collapsed;
        }

       /*
        * FUNCTION: FetchMinPartCountForNotification
        * DESCRIPTION: Retrieves the minimum part count for notification setting from the database.
        * PARAMETERS: SqlConnection connection: The database connection.
        */
        private void FetchMinPartCountForNotification(SqlConnection connection)
        {
            string query = "SELECT ConfigValue FROM Configurations WHERE ConfigKey = 'MinPartCountForNotification'";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                object result = command.ExecuteScalar();
                if (result != null)
                {
                    MinPartCountForNotification = Convert.ToInt32(result);
                }
            }
        }

       /*
        * FUNCTION: LoadData
        * DESCRIPTION: Loads initial data from the database, including lamp status and bin status.
        *              Handles connection opening and error handling.
        */
        private void LoadData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Fetch the minimum part count for notification from the configuration.
                    FetchMinPartCountForNotification(connection);

                    // Fetch the status of lamps (in progress, completed, defective).
                    GetLampStatus(connection, SelectedStationID);

                    // Fetch the status and quantity of parts in bins.
                    GetBinStatus(connection, SelectedStationID);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while loading data: {ex.Message}");
                }
            }
        }

        // Define the BinStatus class according to the database structure
        public class BinStatus
        {
            public string PartId { get; set; }
            public int BinId { get; set; }
            public int CurrentQuantity { get; set; }
            public string Status { get; set; }
        }
    }
}

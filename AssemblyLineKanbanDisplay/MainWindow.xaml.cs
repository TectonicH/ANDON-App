using System;
using System.Collections.Generic;
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
using System.ComponentModel;
using System.Data;
using System.Threading;

namespace AssemblyLineKanbanDisplay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int REFRESH_RATE_SECONDS = 5;
        private SqlConnection connection = null;
        private Thread refreshThread = null;

        private static readonly string TOTAL_ORDERS = "TotalOrders";
        private static readonly string ORDERS_IN_SYSTEM = "TotalLampsInSystem";
        private static readonly string ORDERS_IN_PROGRESS = "TotalLampsInProgress";
        private static readonly string ORDER_ASSEMBLIES_FINISHED = "TotalLampsProduced";
        private static readonly string ORDERS_COMPLETED_SUCCESSFULLY = "TotalSuccessfulLampsYield";
        private static readonly string ORDERS_STATIONS = "TotalAssemblyStations";

        private static readonly string RUNNERS_TOTAL = "TotalRunnerTasksInSystem";
        private static readonly string RUNNERS_PENDING = "TotalPendingRunnerTasks";
        private static readonly string RUNNERS_ACTIVE = "TotalActiveRunnerTasks";

        private static readonly string DATA_LOADING = "Connecting to the Database...";
        private static readonly string DATA_LOADED = $"Data loaded. Refreshing in {REFRESH_RATE_SECONDS} seconds.";

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // get the string and test that the string isn't empty before continuing
            string connectionString = ConfigurationManager.ConnectionStrings["connection"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("No Connection string set. Please add a connection string to the App.Config file.");
                this.Close();
            }

            connection = new SqlConnection(connectionString);

            refreshThread = new Thread(new ThreadStart(RefreshLoop));
            refreshThread.Start();
        }

        private void RefreshLoop()
        {
            // keep going until the user closes the program
            while(true)
            {
                this.Dispatcher.Invoke(RefreshData);

                Thread.Sleep(REFRESH_RATE_SECONDS * 1000);
            }
        }

        private void RefreshData()
        {
            databaseConnectionStatusLabel.Content = DATA_LOADING;
            try
            {
                connection.Open();
                connection.Close();
            }
            catch (SqlException err)
            {
                MessageBox.Show("The connection string failed. Please check that the connection string is valid and restart the program.");
                this.Close();
            }

            DataTable orderTable = new DataTable();
            DataTable runnerTable = new DataTable();

            using(SqlCommand orderSummaryCommand = connection.CreateCommand())
            using (SqlCommand runnerSummaryCommand = connection.CreateCommand())
            {
                bool noExceptions = true;

                // we know that we want everything from the view because the view is tailored for this program
                orderSummaryCommand.CommandText = $"SELECT {TOTAL_ORDERS}, " +
                    $"{ORDERS_IN_SYSTEM}, " +
                    $"{ORDERS_IN_PROGRESS}, " +
                    $"{ORDER_ASSEMBLIES_FINISHED}, " +
                    $"{ORDERS_COMPLETED_SUCCESSFULLY}, " +
                    $"{ORDERS_STATIONS} FROM AssemblyLineLampView";

                runnerSummaryCommand.CommandText = $"SELECT {RUNNERS_TOTAL}, {RUNNERS_ACTIVE}, {RUNNERS_PENDING} FROM AssemblyLineTaskView";

                SqlDataAdapter orderAdapter = new SqlDataAdapter(orderSummaryCommand);
                SqlDataAdapter runnerAdapter = new SqlDataAdapter(runnerSummaryCommand);

                try
                {
                    connection.Open();
                    orderAdapter.Fill(orderTable);
                    runnerAdapter.Fill(runnerTable);
                 
                }
                catch
                {
                    MessageBox.Show("There was an error accessing the database.");
                    noExceptions = false;
                }
                finally
                {
                    connection.Close();
                }

                if (noExceptions)
                {
                    // both of the views called only use aggregate columns and as such will only have one row, so translate the table into key value pairs and get the first one
                    DataRow orderFields = orderTable.AsEnumerable().FirstOrDefault();
                    DataRow runnerFields = runnerTable.AsEnumerable().FirstOrDefault();

                    if (orderFields != null)
                    {
                        try
                        {
                            double totalOrders = int.Parse(orderFields[TOTAL_ORDERS] as string);
                            double ordersSuccessful = (int)orderFields[ORDERS_COMPLETED_SUCCESSFULLY];
                            double ordersInProgress = (int)orderFields[ORDERS_IN_PROGRESS];
                            double assemblyStations = (int)orderFields[ORDERS_STATIONS];
                            double ordersAttempted = (int)orderFields[ORDER_ASSEMBLIES_FINISHED];
                            double ordersDefective = ordersAttempted - ordersSuccessful;

                            totalOrdersNum.Content = $"{totalOrders - ordersSuccessful}/{totalOrders} orders to be completed";
                            totalOrdersPercent.Content = totalOrders == 0 ? "-%" : $"{Math.Round((totalOrders - ordersSuccessful) / totalOrders * 100)}%";

                            pendingOrdersNum.Content = $"{ordersInProgress}/{assemblyStations} assembly stations in use";
                            pendingOrdersPercent.Content = assemblyStations == 0 ? "-%" : $"{Math.Round(ordersInProgress / assemblyStations * 100)}%";

                            completedOrdersNum.Content = $"{ordersSuccessful}/{totalOrders} orders completed";
                            completedOrdersPercent.Content = totalOrders == 0 ? "-%" : $"{Math.Round(ordersSuccessful / totalOrders * 100)}%";

                            defectiveOrdersNum.Content = $"{ordersDefective}/{ordersAttempted} produced orders defective";
                            defectiveOrdersPercent.Content = ordersAttempted == 0 ? "-%" : $"{Math.Round(ordersDefective / ordersAttempted * 100)}%";

                            int totalRunnerTasks = (int)runnerFields[RUNNERS_TOTAL];
                            int activeRunnerTasks = (int)runnerFields[RUNNERS_ACTIVE];
                            int pendingRunnerTasks = (int)runnerFields[RUNNERS_PENDING];

                            pendingRunnersNum.Content = $"{pendingRunnerTasks}/{totalRunnerTasks} tasks out of total waiting to be fulfilled";
                            pendingRunnersPercent.Content = totalRunnerTasks == 0 ? "-%" : $"{pendingRunnerTasks / totalRunnerTasks * 100}%";

                            activeRunnersNum.Content = $"{activeRunnerTasks}/{totalRunnerTasks} tasks out of total being fulfilled";
                            activeRunnersPercent.Content = totalRunnerTasks == 0 ? "-%" : $"{activeRunnerTasks / totalRunnerTasks * 100}%";
                        }
                        // if the values can't be parsed, then there is no way to display them
                        catch(ArgumentNullException)
                        {
                            MessageBox.Show("The values are unexpected and cannot be parsed.");
                            this.Close();
                        }
                    }
                }
                else
                {
                    this.Close();
                }

            }

            databaseConnectionStatusLabel.Content = DATA_LOADED;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.connection.Dispose();
            this.refreshThread.Abort();
        }
    }
}

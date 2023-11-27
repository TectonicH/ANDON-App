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
            // TODO: Replace this actual database logic
            await Task.Delay(1000); // Simulate database call delay
            return new Bin[]
            {
                new Bin { BinId = 1, PartId = "Lens", CurrentQuantity = 3, Status = "Low" },
                new Bin { BinId = 2, PartId = "Bulb", CurrentQuantity = 2, Status = "Critical" }
                // TODO: Add more bins
            };
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
        public int CurrentQuantity { get; set; }
        public string Status { get; set; }
    }

}

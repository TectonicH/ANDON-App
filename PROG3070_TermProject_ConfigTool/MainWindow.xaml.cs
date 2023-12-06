///
/// FILE : MainWindow.xaml.cs
/// PROJECT : PROG3070 Milestone 1
/// PROGRAMMERS : Elizabeth deVries and Kristian Biviens
/// SUBMISSION DATE : Wednesday November 22, 2023
/// DESCRIPTION : This file represents the code behind logic for the configuration table for the PROG3070 term project.
///               The data for the datagrid is handled by the entity framework Database first model, and the save button updates the data.
///               The data can only be saved if one row is checked as the active config and the timescale is not set to 0.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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

namespace PROG3070_TermProject_ConfigTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SQLMilestoneDbContext dbContext = new SQLMilestoneDbContext();
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// METHOD : Window_Loaded
        /// DESCRIPTION : This method loads the data from the database into the dbContext class field. It throws a message box with an error on a failure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Data.CollectionViewSource configurationViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("configurationViewSource")));
                // Load data by setting the CollectionViewSource.Source property:
                // configurationViewSource.Source = [generic data source]

                dbContext.Configurations.Load();

                configurationViewSource.Source = dbContext.Configurations.Local;

            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex.Message}\nCheck the connection string in app.config.");
            }

        }

        /// <summary>
        /// METHOD : saveButton_Click
        /// DESCRIPTION : this method handles saving the datagrid's content updates to the database.
        ///                 It will only allow a save if the timescale is not 0 and only 1 configuration is marked as active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dbContext.SaveChanges();
            }
            catch(Exception err)
            {
                MessageBox.Show(err.InnerException.InnerException.Message);
            }
            

            // Refresh the grids so the database generated values show up.
            this.configurationDataGrid.Items.Refresh();

        }

        /// <summary>
        /// METHOD : OnClosing
        /// DESCRIPTION : This overridden method cleans up the dbContext class field after performing its base method.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.dbContext.Dispose();
        }
    }
}

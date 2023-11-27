using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workstation_Sim
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Console.Write("Enter the number of workstations to simulate: ");
            if (!int.TryParse(Console.ReadLine(), out int numberOfWorkstations))
            {
                Console.WriteLine("Invalid input. Please enter a number.");
                return;
            }

            var tasks = new List<Task>();
            for (int i = 1; i <= numberOfWorkstations; i++)
            {
                tasks.Add(Task.Run(() => SimulateWorkstation(i)));
            }

            await Task.WhenAll(tasks); // Wait for all tasks to complete before exiting
        }


        static async Task SimulateWorkstation(int stationId)
        {
            Console.WriteLine($"Workstation {stationId} starting simulation...");

            string connectionString = "Server=KRISTIANSYOGA;Database=myDataBase;Trusted_Connection=True";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                while (true) // TODO: Replace with condition to stop the simulation
                {
                    // Fetch current state and bins
                    var workstationInfo = await FetchWorkstationInfo(connection, stationId);
                    if (!workstationInfo.IsActive)
                    {
                        Console.WriteLine($"Workstation {stationId} is currently inactive.");
                        break; // Exit the loop if the station isn't active
                    }

                    // Simulate the assembly of a new lamp
                    // Create the new lamp record in the database and get the generated LampID
                    int lampId = await CreateNewLampRecord(connection, stationId, workstationInfo.CurrentWorkerId);

                    // Decrease part quantities in bins and check if the runner needs to be notified
                    await UpdateBinsAndNotifyRunner(connection, workstationInfo);

                    // Simulate the assembly time based on the worker's skill level
                    int assemblyTime = CalculateAssemblyTime(workstationInfo.WorkerSkillLevel);
                    await Task.Delay(assemblyTime * 1000); // Convert seconds to milliseconds

                    // Update the lamp status after assembly
                    string lampStatus = DetermineLampStatus(workstationInfo.DefectRate);
                    await UpdateLampStatus(connection, lampId, lampStatus, assemblyTime);

                    // Log completion and wait before starting the next lamp
                    Console.WriteLine($"Workstation {stationId} has completed assembly of lamp {lampId} with status: {lampStatus}");
                    await Task.Delay(1000); // Wait for a second before the next assembly
                }
                Console.WriteLine($"Workstation {stationId} simulation ending...");
            }
        }

        // Fetch the current state and associated bin info for the workstation
        static async Task<WorkstationInfo> FetchWorkstationInfo(SqlConnection connection, int stationId)
        {
            // TODO: Implement the SQL query and return the workstation info
            // TODO: Add implementation
            return new WorkstationInfo
            {
                IsActive = true,
                CurrentWorkerId = 1,
                WorkerSkillLevel = "Intermediate",
                // TODO: decide if there are other properties
            };
        }

        // Create a new lamp record in the FogLamps table and return the LampID
        static async Task<int> CreateNewLampRecord(SqlConnection connection, int stationId, int workerId)
        {
            // TODO: Implement the SQL INSERT command and return the new LampID
            // TODO: Add implementation
            return 1; // Dummy LampID
        }

        // Update bin quantities and notify the runner if needed
        static async Task UpdateBinsAndNotifyRunner(SqlConnection connection, WorkstationInfo workstationInfo)
        {
            // TODO: Implement the SQL UPDATE command for bins and INSERT command for runner tasks
            // TODO: Add implementation
        }

        // Calculate assembly time based on worker's skill level
        static int CalculateAssemblyTime(string workerSkillLevel)
        {
            // TODO: Implement logic to determine assembly time based on skill level
            // TODO: Add implementation
            return 60; // Default to 60 seconds
        }

        // Determine if the assembled lamp is defective
        static string DetermineLampStatus(decimal defectRate)
        {
            // TODO: Implement logic to determine if lamp is defective based on defect rate
            // TODO: Add implementation
            return new Random().NextDouble() < (double)defectRate ? "Defective" : "Completed";
        }

        // Update the lamp status in the FogLamps table
        static async Task UpdateLampStatus(SqlConnection connection, int lampId, string status, int assemblyTime)
        {
            // TODO: Implement the SQL UPDATE command for the lamp status
            // TODO: Add implementation
        }
    }

    class WorkstationInfo
    {
        public bool IsActive { get; set; }
        public int CurrentWorkerId { get; set; }
        public string WorkerSkillLevel { get; set; }
        public decimal DefectRate { get; set; }
    }

}

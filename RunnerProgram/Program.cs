using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class RunnerProgram
{
    static async Task Main(string[] args)
    {
        string connectionString = "Server=KRISTIANSYOGA;Database=PROG3070_TermProjectDB;Trusted_Connection=True";
        await RunRunnerSimulation(connectionString);
    }

    static async Task RunRunnerSimulation(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            while (true) // replace with a proper stopping condition
            {
                int runnerTime = await GetRunnerTime(connection);
                if (runnerTime <= 0)
                {
                    Console.WriteLine("Invalid runner time. Ending simulation.");
                    break;
                }

                Console.WriteLine("Refreshing runner tasks...");
                await RefreshRunnerLoop(connection);

                Console.WriteLine($"Waiting for {runnerTime} seconds before next run...");
                await Task.Delay(runnerTime * 1000); // Wait for the duration of runnerTime
            }
        }
    }

    static async Task<int> GetRunnerTime(SqlConnection connection)
    {
        // This is a placeholder implementation
        return 60; // Example: 60 seconds
    }

    static async Task RefreshRunnerLoop(SqlConnection connection)
    {
        Console.WriteLine("Runner is checking bins and refilling as needed...");
        // Placeholder implementation
    }
}


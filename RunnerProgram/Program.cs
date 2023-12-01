using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class RunnerProgram
{
    static async Task Main(string[] args)
    {
        string connectionString = "Server=DESKTOP-G1JU1VE;Database=PROG3070_TermProjectDB;Trusted_Connection=True";
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
        using (var command = new SqlCommand("dbo.GetRunnerTime", connection))
        {
            command.CommandType = CommandType.StoredProcedure;

            try
            {
                var result = await command.ExecuteScalarAsync();

                // Check if the result is not null 
                if (result != null && !(result is DBNull))
                {
                    return (int)result;
                }
                else
                {
                    Console.WriteLine("GetRunnerTime returned a null or DBNull value.");
                    return -1; // Return an error code or a default value
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching runner time: {ex.Message}");
                return -1; // Return an error code
            }
        }
    }



    static async Task RefreshRunnerLoop(SqlConnection connection)
    {
        using (var command = new SqlCommand("dbo.RefreshRunnerLoop", connection))
        {
            command.CommandType = CommandType.StoredProcedure;

            try
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("Runner tasks have been refreshed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during runner loop refresh: {ex.Message}");
            }
        }
    }

}


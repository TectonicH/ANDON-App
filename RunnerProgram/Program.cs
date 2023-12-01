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

            while (true) 
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
                await Task.Delay(runnerTime * 1000); 
            }
        }
    }

    static async Task<int> GetRunnerTime(SqlConnection connection)
    {
        string sql = "SELECT dbo.GetRunnerTime();";
        using (var command = new SqlCommand(sql, connection))
        {
            try
            {
                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    Console.WriteLine("GetRunnerTime returned null or DBNull.");
                    return -1; 
                }

                return (int)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching runner time: {ex.Message}");
                return -1;
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


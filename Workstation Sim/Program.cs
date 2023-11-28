using System;
using System.Collections.Generic;
using System.Data;
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
            Console.WriteLine("Enter the Station ID:"); // Will remove this later 
            int stationId = int.Parse(Console.ReadLine());
            string connectionString = "Server=KRISTIANSYOGA;Database=PROG3070_TermProjectDB;Trusted_Connection=True;"; // Will be placing in another file later 

            await SimulateWorkstation(stationId, connectionString);
        }

        static async Task SimulateWorkstation(int stationId, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                while (true) // TODO: Gotta replace this with a proper stopping condition
                {
                    var (success, lampId) = await BeginAssembly(connection, stationId);
                    if (!success || lampId == -1)
                    {
                        Console.WriteLine($"Error at workstation {stationId}. Unable to begin assembly.");
                        break; 
                    }

                    int assemblyTime = await GetAssemblyTime(connection, lampId);

                    // check if assemblyTime is valid
                    if (assemblyTime <= 0)
                    {
                        Console.WriteLine($"Invalid assembly time retrieved for lamp {lampId} at workstation {stationId}.");
                        break; 
                    }

                    Console.WriteLine($"Assembling lamp {lampId} for {assemblyTime} seconds...");
                    await Task.Delay(assemblyTime * 1000); // Simulate assembly time

                    await FinishAssembly(connection, lampId);
                    Console.WriteLine($"Finished assembling lamp {lampId}.");

                    await Task.Delay(1000); // Delay before next assembly
                }
            }
        }


        static async Task<(bool Success, int LampId)> BeginAssembly(SqlConnection connection, int stationId)
        {
            using (var command = new SqlCommand("BeginAssembly", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@stationID", stationId);
                var lampIdParam = new SqlParameter("@lampID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(lampIdParam);

                try
                {
                    await command.ExecuteNonQueryAsync();
                    int lampId = (int)lampIdParam.Value;
                    return (true, lampId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BeginAssembly: {ex.Message}");
                    return (false, -1);
                }
            }
        }

        static async Task FinishAssembly(SqlConnection connection, int lampId)
        {
            using (var command = new SqlCommand("FinishAssembly", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@lampID", lampId);

                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in FinishAssembly: {ex.Message}");
                }
            }
        }

        static async Task<int> GetAssemblyTime(SqlConnection connection, int lampId)
        {
            using (var command = new SqlCommand("SELECT dbo.GetAssemblyTime(@lampID)", connection))
            {
                command.Parameters.AddWithValue("@lampID", lampId);

                try
                {
                    var result = await command.ExecuteScalarAsync();
                    return result != DBNull.Value ? Convert.ToInt32(result) : -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching assembly time: {ex.Message}");
                    return -1;
                }
            }
        }

    }

}

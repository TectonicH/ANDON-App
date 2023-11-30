using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Workstation_Sim
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the Station ID:"); // Will remove this later 
            int stationId = int.Parse(Console.ReadLine());
            string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;

            await SimulateWorkstation(stationId, connectionString);
        }

        static async Task SimulateWorkstation(int stationId, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                while (true) // Replace with a proper stopping condition
                {
                    // Start the assembly process and get the new lamp ID
                    // error codes are any negative int return and real ids will be 1 or greater
                    // 0 means that the procedure wasn't called correctly and we didn't even get to a try catch
                    // -1 error means that the station wasn't active or one of the bins is already empty
                    // -2 means that the transaction to remove the parts and create the lamp failed
                    // -3 means that the provided stationID is null or not > 0
                    var (returnCode, lampId) = await BeginAssembly(connection, stationId);
                    if (returnCode != 0 || lampId == -1)
                    {
                        Console.WriteLine($"Error at workstation {stationId}. Unable to begin assembly.");
                        break; // or continue, depending on error handling strategy
                    }

                    // Fetch the assembly time for the new lamp
                    int assemblyTime = await GetAssemblyTime(connection, lampId);
                    if (assemblyTime <= 0)
                    {
                        Console.WriteLine($"Invalid assembly time for lamp {lampId} at workstation {stationId}.");
                        break; // or handle appropriately
                    }

                    Console.WriteLine($"Assembling lamp {lampId} for {assemblyTime} seconds...");
                    await Task.Delay(assemblyTime * 1000); // Simulate assembly time

                    // Finish the assembly process and determine the final status of the lamp
                    await FinishAssembly(connection, lampId, assemblyTime);
                    Console.WriteLine($"Finished assembling lamp {lampId}.");

                    await Task.Delay(1000); // Optional delay before next assembly
                }
            }
        }


        static async Task<(int returnCode, int LampId)> BeginAssembly(SqlConnection connection, int stationId)
        {
            using (var command = new SqlCommand("BeginAssembly", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                var stationIdParam = new SqlParameter("@stationID", stationId) { SqlDbType = SqlDbType.Int };
                var lampIdParam = new SqlParameter("@lampID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var returnParam = new SqlParameter("@returnValue", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                
                command.Parameters.Add(lampIdParam);
                command.Parameters.Add(stationIdParam);
                command.Parameters.Add(returnParam);

                try
                {
                    await command.ExecuteNonQueryAsync();
                    int lampId = (int)lampIdParam.Value;
                    int returnResult = (int)returnParam.Value;
                    return (returnResult, lampId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BeginAssembly: {ex.Message}");
                    return (0, -1);
                }
            }
        }

        static async Task FinishAssembly(SqlConnection connection, int lampId, int assemblyTime)
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
                    var result= await command.ExecuteScalarAsync();
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;
using System.Threading;

namespace Workstation_Sim
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;
            while (true)
            {
                Console.Write("Enter the Station ID (or enter 0 to exit):");
                var input = Console.ReadLine();

                if (input == "0")
                {
                    Console.WriteLine("Exiting the program.");
                    break;
                }

                if (!int.TryParse(input, out int stationId) || stationId <= 0)
                {
                    Console.WriteLine("Invalid input. Please enter a positive number or 0 to exit.");
                    continue;
                }

                await SimulateWorkstation(connectionString, stationId);
            }
        }

        static async Task SimulateWorkstation(string connectionString, int stationId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                while (true) 
                {       

                    var (returnCode, lampId) = await BeginAssembly(connection, stationId);

                    if (returnCode != 0 || lampId == -1)
                    {
                        switch (returnCode)
                        {
                            case -1:
                                Console.WriteLine($"Error: Station {stationId} is not active or one of the bins is already empty.");
                                break;
                            case -2:
                                Console.WriteLine($"Error: Transaction failed at station {stationId}.");
                                break;
                            case -3:
                                Console.WriteLine($"Error: Invalid station ID {stationId} provided.");
                                break;
                            default:
                                Console.WriteLine($"Error at workstation {stationId}. Unable to begin assembly.");
                                break;
                        }

                        if (lampId == -1)
                        {
                            Console.WriteLine($"Error at workstation {stationId}. Lamp ID not set correctly.");
                        }

                        continue; 
                    }

                    int assemblyTime = await GetAssemblyTime(connection, lampId);
                    if (assemblyTime <= 0)
                    {
                        Console.WriteLine($"Invalid assembly time for lamp {lampId} at workstation {stationId}.");
                        continue;
                    }

                    Console.WriteLine($"Assembling lamp {lampId} for {assemblyTime} seconds...");
                    await Task.Delay(assemblyTime * 1000);

                    var finishAssemblyReturnCode = await FinishAssembly(connection, lampId, assemblyTime);
                    if (finishAssemblyReturnCode != 0)
                    {
                        switch (finishAssemblyReturnCode)
                        {
                            case -1:
                                Console.WriteLine($"Error: Lamp {lampId} is either not in progress or has already been finished.");
                                break;
                            case -2:
                                Console.WriteLine($"Error: Missing data or link in the database for lamp {lampId}.");
                                break;
                            case -3:
                                Console.WriteLine($"Error: Unexpected error during the finishing process for lamp {lampId}.");
                                break;
                            default:
                                Console.WriteLine($"Unknown error occurred while finishing assembly for lamp {lampId}.");
                                break;
                        }

                        continue; 
                    }

                    Console.WriteLine($"Finished assembling lamp {lampId}.");
                    await Task.Delay(1000); 
                }
            }
        }


        static async Task<(int returnCode, int lampId)> BeginAssembly(SqlConnection connection, int stationId)
        {
            using (var command = new SqlCommand("BeginAssembly", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                var stationIdParam = new SqlParameter("@stationID", stationId) { SqlDbType = SqlDbType.Int };
                var lampIdParam = new SqlParameter("@lampID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var returnParam = new SqlParameter("@returnValue", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };

                command.Parameters.Add(stationIdParam);
                command.Parameters.Add(lampIdParam);
                command.Parameters.Add(returnParam);

                try
                {
                    await command.ExecuteNonQueryAsync();

                    int lampId = lampIdParam.Value != DBNull.Value ? Convert.ToInt32(lampIdParam.Value) : -1;

                    int returnCode = returnParam.Value != DBNull.Value ? Convert.ToInt32(returnParam.Value) : -1;

                    return (returnCode, lampId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BeginAssembly: {ex.Message}");
                    return (0, -1);
                }
            }
        }


        static async Task<int> FinishAssembly(SqlConnection connection, int lampId, int assemblyTime)
        {
            using (var command = new SqlCommand("FinishAssembly", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@lampID", lampId);
                command.Parameters.AddWithValue("@assemblyTime", assemblyTime);

                var returnParameter = command.Parameters.Add("@ReturnCode", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                await command.ExecuteNonQueryAsync();

                return (int)returnParameter.Value;
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

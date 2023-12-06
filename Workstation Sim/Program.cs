//
// FILE : Program.cs
// PROJECT : PROG3070 Milestone 2
// PROGRAMMERS : Elizabeth deVries and Kristian Biviens
// SUBMISSION DATE : Friday December 1, 2023
// DESCRIPTION : This application is designed to simulate the operations at a workstation in a manufacturing environment. 
//               It prompts the user to enter a station ID, checks if the station is available, and then proceeds to simulate
//               assembly tasks. 

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

            // Declares and initializes the stationId.
            int stationId = -1;

            // Continues to prompt for a valid station ID until the user exits or provides a valid ID.
            while (stationId <= 0)
            {
                Console.Write("Enter the Station ID (or enter 0 to exit):");
                var input = Console.ReadLine();

                // Exits the program if the user enters '0'.
                if (input == "0")
                {
                    Console.WriteLine("Exiting the program.");
                    break;
                }

                // Validates the user's input and ensures it is a positive integer.
                if (!int.TryParse(input, out stationId) || stationId <= 0)
                {
                    Console.WriteLine("Invalid input. Please enter a positive number or 0 to exit.");
                    continue;
                }

                // Checks if the selected station is available for simulation.
                if (!await IsStationAvailable(connectionString, stationId))
                {
                    Console.WriteLine($"Station {stationId} is not available. Please enter a different Station ID.");
                    stationId = -1;
                    continue;
                }

                // If the station is available, starts simulating workstation operations.
                await SimulateWorkstation(connectionString, stationId);
            }
        }

        /*
        * FUNCTION: SimulateWorkstation
        * DESCRIPTION: Simulates the operations of a specific workstation. It handles the assembly process 
        *              including starting and finishing assembly tasks and handling errors during the process.
        * PARAMETERS: string connectionString: Connection string for the database.
        *             int stationId: The ID of the station to simulate.
        * RETURNS: Task: Represents an asynchronous operation.
        */
        static async Task SimulateWorkstation(string connectionString, int stationId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    while (true)
                    {
                        // Attempts to begin assembly at the station and captures the return code and lamp ID.
                        var (returnCode, lampId) = await BeginAssembly(connection, stationId);

                        // Handles various errors that might occur during assembly.
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

                            // Waits for a short period before attempting to begin assembly again.
                            await Task.Delay(TimeSpan.FromSeconds(3));
                            continue;
                        }

                        // Retrieves the assembly time for the current lamp and station.
                        int assemblyTime = await GetAssemblyTime(connection, lampId);
                        if (assemblyTime <= 0)
                        {
                            Console.WriteLine($"Invalid assembly time for lamp {lampId} at workstation {stationId}.");
                            continue;
                        }

                        // Logs the start of the assembly process and waits for the duration of the assembly time.
                        Console.WriteLine($"Assembling lamp {lampId} for {assemblyTime} seconds...");
                        await Task.Delay(assemblyTime * 1000);

                        // Completes the assembly process and checks for any errors.
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
            catch (SqlException ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                return;
            }
        }

        /*
        * FUNCTION: BeginAssembly
        * DESCRIPTION: Starts the assembly process at the specified station and returns the outcome 
        *              including the lamp ID and return code.
        * PARAMETERS: SqlConnection connection: The active database connection.
        *             int stationId: The ID of the station to start the assembly process.
        * RETURNS: Task<(int returnCode, int lampId)>: A task that represents the asynchronous operation 
        *          and returns a tuple of return code and lamp ID.
        */
        static async Task<(int returnCode, int lampId)> BeginAssembly(SqlConnection connection, int stationId)
        {
            try
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
            catch (SqlException ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                return (0, -1);
            }
        }

        /*
        * FUNCTION: FinishAssembly
        * DESCRIPTION: Completes the assembly process for a given lamp and returns the outcome.
        * PARAMETERS: SqlConnection connection: The active database connection.
        *             int lampId: The ID of the lamp whose assembly is to be finished.
        *             int assemblyTime: The time taken for the assembly.
        * RETURNS: Task<int>: A task that represents the asynchronous operation and returns a return code.
        */
        static async Task<int> FinishAssembly(SqlConnection connection, int lampId, int assemblyTime)
        {
            try
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
            catch (SqlException ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                return -1;
            }
        }

        /*
        * FUNCTION: GetAssemblyTime
        * DESCRIPTION: Retrieves the assembly time for a specific lamp.
        * PARAMETERS: SqlConnection connection: The active database connection.
        *             int lampId: The ID of the lamp to fetch the assembly time for.
        * RETURNS: Task<int>: A task that represents the asynchronous operation and returns the assembly time.
        */
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

        /*
        * FUNCTION: IsStationAvailable
        * DESCRIPTION: Checks if the specified station is available for assembly.
        * PARAMETERS: string connectionString: Connection string for the database.
        *             int stationId: The ID of the station to check availability.
        * RETURNS: Task<bool>: A task that represents the asynchronous operation and returns true if the station is available, otherwise false.
        */
        static async Task<bool> IsStationAvailable(string connectionString, int stationId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT StationID FROM AssemblyStations WHERE StationID = @stationID";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@stationID", stationId);

                        var result = await command.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToBoolean(result);
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching assembly time: {ex.Message}");
                return false;
            }

        }
    }
}

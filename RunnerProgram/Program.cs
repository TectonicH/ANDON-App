﻿//
// FILE : Program.cs
// PROJECT : PROG3070 Milestone 2
// PROGRAMMERS : Elizabeth deVries and Kristian Biviens
// SUBMISSION DATE : Friday December 1, 2023
// DESCRIPTION : This program simulates the actions of a runner in a factory environment. The main responsibilities of this program are to 
//               execute a continuous simulation loop that refreshes runner tasks and wait for a defined time before the next iteration of tasks begins.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;


class RunnerProgram
{
    static async Task Main(string[] args)
    {
        string connectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;

        // Starts the runner simulation loop.
        await RunRunnerSimulation(connectionString);
    }

    /*
    * FUNCTION: RunRunnerSimulation
    * DESCRIPTION: The core simulation loop for the runner. It establishes a connection to the SQL database
    *              and continuously refreshes runner tasks at a set interval.
    * PARAMETERS: string connectionString: Connection string for the database.
    * RETURNS: Task: Represents an asynchronous operation.
    */
    static async Task RunRunnerSimulation(string connectionString)
    {
        try
        {
            // Establishes a connection to the SQL database using the provided connection string.
            using (var connection = new SqlConnection(connectionString))
            {
                // Opens the database connection asynchronously.
                await connection.OpenAsync();

                // Infinite loop to keep the simulation running.
                while (true)
                {
                    // Retrieves the runner time interval from the database.
                    int runnerTime = await GetRunnerTime(connection);
                    // If an invalid runner time is returned, logs a message and exits the loop.
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
        catch (SqlException ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            return;
        }

    }

    /*
    * FUNCTION: GetRunnerTime
    * DESCRIPTION: Retrieves the runner time interval from the database. If the time is invalid, 
    *              it logs an error and returns a negative value.
    * PARAMETERS: SqlConnection connection: The active database connection.
    * RETURNS: Task<int>: A task that represents the asynchronous operation and returns the runner time.
    */
    static async Task<int> GetRunnerTime(SqlConnection connection)
    {
        string sql = "SELECT dbo.GetRunnerTime();";

        try
        {
            using (var command = new SqlCommand(sql, connection))
            {
                try
                {
                    // Executes the command and retrieves the result asynchronously.
                    var result = await command.ExecuteScalarAsync();

                    // If no result is returned, logs an error and returns -1.
                    if (result == null || result == DBNull.Value)
                    {
                        Console.WriteLine("GetRunnerTime returned null or DBNull.");
                        return -1;
                    }

                    // Returns the retrieved runner time.
                    return (int)result;
                }
                catch (Exception ex)
                {
                    // If an error occurs during the execution, logs the exception and returns -1.
                    Console.WriteLine($"Error fetching runner time: {ex.Message}");
                    return -1;
                }
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            return -1;
        }


    }

    /*
    * FUNCTION: RefreshRunnerLoop
    * DESCRIPTION: Executes a stored procedure to refresh runner tasks and logs the outcome. 
    *              Handles any exceptions that occur during the process.
    * PARAMETERS: SqlConnection connection: The active database connection.
    * RETURNS: Task: Represents an asynchronous operation.
    */
    static async Task RefreshRunnerLoop(SqlConnection connection)
    {
        try
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
        catch (SqlException ex) 
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
            return;
        }

    }

}


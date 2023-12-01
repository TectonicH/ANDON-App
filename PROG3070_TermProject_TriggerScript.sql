-- FILE : PROG3070TermProject_TriggerScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This script contains the triggers for the term project database.
--			     The trigger runs on the Bins table and triggers when the bin count reaches a specific minimum
--				 as described in the configurations table
--				 This script also drops the trigger and recreates it if it already existed.

USE PROG3070_TermProjectDB;

GO
DROP TRIGGER IF EXISTS BinCountTrigger
GO

-- TRIGGER : BinCountTrigger
-- DESCRIPTION : This trigger runs after a bin update and checks for any part counts below the minimum value
--				 The minimum value is pulled from configuration
--			     We don't need to worry about error catching because the trigger is already a transaction where rollback is covered
CREATE TRIGGER BinCountTrigger
ON Bins AFTER UPDATE
AS
BEGIN
	-- get the minimum value
	DECLARE @minimumValue int = (SELECT ConfigValue FROM Configurations 
									WHERE ConfigKey = 'minPartCountForNotification');

	-- find any bins with lower than minimum part count that don't have their own task, according to our design it should only ever be 1 bin at a time
	-- however we want to handle multiple incase we ever change the bin capacities, part usage etc to allow multiple bins to go past the minimum together
	-- we also grab from whole table instead of from deleted pseudotable so we can hopefully account for any low bins that may have been missed (for example if a partial bin was inserted with <= minimum quantity)
	DECLARE @stationID int;
	DECLARE @partID nchar(9);
	DECLARE binCursor CURSOR FOR 
		SELECT AssemblyStations.StationID, Bins.PartID FROM Bins 
			INNER JOIN AssemblyStations ON HarnessBin = BinID
				OR HousingBin = BinID 
				OR ReflectorBin = BinID
				OR BezelBin = BinID 
				OR BulbBin = BinID
				OR LensBin = BinID
			LEFT JOIN RunnerTasks ON AssemblyStations.StationID = RunnerTasks.StationID AND Bins.PartID = RunnerTasks.PartID
			WHERE CurrentQuantity <= @minimumValue AND RunnerTasks.TaskID IS NULL;

	OPEN binCursor
	FETCH NEXT FROM binCursor INTO @stationID, @partID
	WHILE (@@FETCH_STATUS = 0)
	BEGIN
		-- create a runner task for each bin
		INSERT INTO RunnerTasks(PartID, StationID, TaskInProgress) VALUES
			(@partID, @stationID, 0);
		FETCH NEXT FROM binCursor INTO @stationID, @partID;
	END
	CLOSE binCursor;
	DEALLOCATE binCursor;
END
-- FILE : PROG3070TermProject_procedureScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This script contains the stored procedures for the term project database.
--				 These procedures simulate the main employee workflow with workers assembling lamps and runners refreshing bins
--				 This script also drops and remakes the procedures if they exist so it can also rewrite previous versions.

USE PROG3070_TermProjectDB;

GO
DROP PROCEDURE IF EXISTS BeginAssembly;
GO

-- PROCEDURE : BeginAssembly
-- DESCIPTION : This stored procedure removes the parts from each bin of an assembly station
--				and creates an in progress fog lamp with no assembly time as part of a transaction.
--				It will be the responsibility of the simulation to calculate the assembly time, or the worker/assembly line
--				to record their actual assembly time in a real life production.
-- PARAMETERS : @stationID : the assembly station being used to make the fog lamp
-- RETURNS    : @lampID : the id of the lamp created
CREATE PROCEDURE BeginAssembly
@stationID int = 0,
@lampID int = -1 OUTPUT
AS
BEGIN
	DECLARE @result int = 0;
	BEGIN TRY
		-- check station id first to make sure it was sent properly (due to auto increment identity we know it has to be >= 1)
		IF @stationID IS NOT NULL AND @stationID > 0
		BEGIN
			-- check the activity level and bin counts
			DECLARE @isActive bit;
			DECLARE @harnessQuantity int;
			DECLARE @housingQuantity int;
			DECLARE @reflectorQuantity int;
			DECLARE @bulbQuantity int;
			DECLARE @bezelQuantity int;
			DECLARE @lensQuantity int;
			DECLARE @workerID int;

			SELECT @isActive = IsActive,
				@harnessQuantity = harnessBin.CurrentQuantity,
				@housingQuantity = housingBin.CurrentQuantity,
				@reflectorQuantity = reflectorBin.CurrentQuantity,
				@bulbQuantity = bulbBin.CurrentQuantity,
				@bezelQuantity = bezelBin.CurrentQuantity,
				@lensQuantity = lensBin.CurrentQuantity,
				@workerID = CurrentWorkerID
				FROM AssemblyStations 
				INNER JOIN Bins harnessBin ON HarnessBin = harnessBin.BinID
				INNER JOIN Bins housingBin ON HousingBin = housingBin.BinID
				INNER JOIN Bins reflectorBin ON ReflectorBin = reflectorBin.BinID
				INNER JOIN Bins bulbBin ON BulbBin = bulbBin.BinID
				INNER JOIN Bins bezelBin ON BezelBin = bezelBin.BinID
				INNER JOIN Bins lensBin ON LensBin = lensBin.BinID
				WHERE StationID = @stationID;

			-- check for active station (IsActive = 1), and all bin quantities are not 0
			IF @isActive = 1 
			AND @harnessQuantity > 0
			AND @housingQuantity > 0
			AND @reflectorQuantity > 0
			AND @bulbQuantity > 0
			AND @bezelQuantity > 0
			AND @lensQuantity > 0
			BEGIN
				BEGIN TRY

				SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
				BEGIN TRAN
					-- remove bin parts
					UPDATE Bins
						SET CurrentQuantity = CurrentQuantity - 1
						WHERE BinID = (SELECT HarnessBin FROM AssemblyStations WHERE StationID = @stationID) 
							OR BinID = (SELECT HousingBin FROM AssemblyStations WHERE StationID = @stationID)
							OR BinID = (SELECT BezelBin FROM AssemblyStations WHERE StationID = @stationID)
							OR BinID = (SELECT ReflectorBin FROM AssemblyStations WHERE StationID = @stationID)
							OR BinID = (SELECT BulbBin FROM AssemblyStations WHERE StationID = @stationID)
							OR BinID = (SELECT LensBin FROM AssemblyStations WHERE StationID = @stationID);

					-- create lamp
					INSERT INTO FogLamps(StationID, WorkerID, [Status]) VALUES
						(@stationID, @workerID, 'InProgress');

					-- get newly inserted id, which we can get by finding the largest LampID because we're in a snapshot
					SELECT TOP 1 @lampID = LampID FROM FogLamps ORDER BY LampID DESC; 

				-- end transaction and return lamp
				COMMIT TRAN
				END TRY
				BEGIN CATCH
					IF @@TRANCOUNT > 0
					BEGIN
						ROLLBACK TRAN;
						SET @result = -2;
					END
				END CATCH
			END
			ELSE
			BEGIN
				SET @result = -1;
			END
		END
		ELSE
		BEGIN
			SET @result = -3;
		END
	END TRY
	-- we handle the transaction error, so if anything happens outside it we just want to stop the procedure from continuing
	BEGIN CATCH
		SET @result = -4;
		THROW;
	END CATCH

	RETURN @result;
END

GO
DROP PROCEDURE IF EXISTS FinishAssembly
GO

-- PROCEDURE : FinishAssembly
-- DESCRIPTION : This procedure will update the lamp to its finished state, which will be either completed or defective.
--				 Whether the lamp was a success or not will be randomly calculated using the defect rate of the worker who created the lamp.
CREATE PROCEDURE FinishAssembly
@lampID int,
@assemblyTime int
AS
BEGIN
	DECLARE @returnCode int = 0;
	BEGIN TRY
		-- check lamp status first, if it is not in progress, then an already finished lamp was accidentally sent instead
		-- and we should return an error value
		DECLARE @lampStatus nchar(10);
		SELECT @lampStatus = [Status] FROM FogLamps WHERE LampID = @lampID;
		IF @lampStatus <> 'InProgress'
		BEGIN
			SET @returnCode = -1;
		END
		ELSE
		BEGIN
			DECLARE @defectRate decimal(8,4);
			SELECT @defectRate = DefectRate FROM WorkerLevels
				INNER JOIN Workers ON Workers.SkillLevel = WorkerLevels.SkillLevel
				INNER JOIN FogLamps ON FogLamps.WorkerID = Workers.WorkerID AND LampID = @lampID;

			-- if the defect rate is null, there's a missing table link somewhere so we should return an error code
			IF @defectRate IS NULL
			BEGIN
				SET @returnCode = -2;
			END 
			ELSE 
			BEGIN
				-- if the random lamp success value matches or exceeds the failure rate, its a pass and if it's under, it's a fail
				-- we are including the exact match of the failure rate as a success to counter act the fact that rand returns 0 <= x < 1
				-- so we want to make up for 1 not being inclusive
				DECLARE @newLampStatus nchar(10);
				DECLARE @lampSuccessValue decimal(8,4);
				SELECT @lampSuccessValue = RandResult FROM GetRandValueView;

				IF @lampSuccessValue >= @defectRate
				BEGIN
					SET @newLampStatus = 'Completed';
				END
				ELSE
				BEGIN
					SET @newLampStatus = 'Defective';
				END

				-- update with the new state and the time it ended up taking
				UPDATE FogLamps
					SET [Status] = @newLampStatus, AssemblyTimeInSeconds = @assemblyTime
					WHERE LampID = @lampID;
			END
		END
	END TRY
	-- if we catch an error here, it's not something we want to handle, so stop execution and throw it back
	BEGIN CATCH
		SET @returnCode = -3;
		THROW;
	END CATCH

	RETURN @returnCode;
END

GO
DROP PROCEDURE IF EXISTS RefreshRunnerLoop
GO

-- PROCEDURE : RefreshRunnerLoop
-- DESCRIPTION : this procedure performs the actions of the runner. It completes all active runner tasks by 
--				 finding the part bin of the assigned station associated with the assigned part, removing it, and assigning
--				 a new bin to the station, with any leftover quantity from the old bin added to the new bin quantity ontop of its usual capacity,
--			     before deleting the task, which all takes place under a transaction. After the transaction, all currently not active tasks
--				 will be changed to active for the next runner loop.
CREATE PROCEDURE RefreshRunnerLoop
AS
BEGIN
	DECLARE @returnCode int = 0;
	BEGIN TRY
		-- check for runner tasks, we can leave early if there are none,
		-- or skip to flipping to inprogress if there are only not-in-progress tasks
		IF (SELECT count(TaskID) FROM RunnerTasks) > 0
		BEGIN
			DECLARE @activeTaskCount int;
			SELECT @activeTaskCount = count(TaskID) FROM RunnerTasks WHERE TaskInProgress = 1;
			IF @activeTaskCount > 0
			BEGIN
				-- create a loop to deal with each task
				WHILE @activeTaskCount > 0
				BEGIN
					DECLARE @currentTaskID int;
					SELECT TOP 1 @currentTaskID = TaskID FROM RunnerTasks 
						WHERE TaskInProgress = 1
						ORDER BY TaskID;

						-- find PartID, StationID from task
						DECLARE @currentPartID nchar(9);
						DECLARE @currentStationID int;

						SELECT @currentPartID = PartID, @currentStationID = StationID FROM RunnerTasks
							WHERE TaskID = @currentTaskID;

						-- find the right bin id to replace by checking against the part id
						DECLARE @binToReplace int;
						SET @binToReplace = CASE
								WHEN @currentPartID = 'Housing' 
									THEN (SELECT HousingBin FROM AssemblyStations
										WHERE StationID = @currentStationID)
								WHEN @currentPartID = 'Harness'
									THEN (SELECT HarnessBin FROM AssemblyStations
										WHERE StationID = @currentStationID)
								WHEN @currentPartID = 'Reflector'
									THEN (SELECT ReflectorBin FROM AssemblyStations
										WHERE StationID = @currentStationID)
								WHEN @currentPartID = 'Bulb'
									THEN (SELECT BulbBin FROM AssemblyStations
										WHERE StationID = @currentStationID)
								WHEN @currentPartID = 'Bezel'
									THEN (SELECT BezelBin FROM AssemblyStations
										WHERE StationID = @currentStationID)
								WHEN @currentPartID = 'Lens'
									THEN (SELECT LensBin FROM AssemblyStations
										WHERE StationID = @currentStationID)
							END;

						-- get the remaining quantity from the bin we're replacing by finding the bin id	
						DECLARE @remainingQuantity int;
						SELECT @remainingQuantity = CurrentQuantity FROM Bins WHERE BinID = @binToReplace;

						-- also get the nenw bin capacity using the partID
						DECLARE @currentPartCapacity int = (SELECT BinCapacity FROM Parts 
							WHERE PartID = @currentPartID);
					
					BEGIN TRY

						SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
						BEGIN TRAN
							-- remove old bin, insert new bin, assign capacity, and add remaining part count,
							-- then delete the task
							INSERT INTO Bins (PartID, CurrentQuantity) VALUES
								(@currentPartID, @currentPartCapacity + @remainingQuantity);

								DECLARE @newBinID int = SCOPE_IDENTITY();
								IF @currentPartID = 'Housing' 
									UPDATE AssemblyStations SET HousingBin = @newBinID
										WHERE StationID = @currentStationID
								IF @currentPartID = 'Harness'
									UPDATE AssemblyStations SET HarnessBin = @newBinID
										WHERE StationID = @currentStationID
								IF @currentPartID = 'Reflector'
									UPDATE AssemblyStations SET ReflectorBin = @newBinID
										WHERE StationID = @currentStationID
								IF @currentPartID = 'Bulb'
									UPDATE AssemblyStations SET BulbBin = @newBinID
										WHERE StationID = @currentStationID
								IF @currentPartID = 'Bezel'
									UPDATE AssemblyStations SET BezelBin = @newBinID
										WHERE StationID = @currentStationID
								IF @currentPartID = 'Lens'
									UPDATE AssemblyStations SET LensBin = @newBinID
										WHERE StationID = @currentStationID

							DELETE FROM Bins WHERE BinID = @binToReplace;
							DELETE FROM RunnerTasks WHERE TaskID = @currentTaskID;
						COMMIT TRAN
					END TRY
					BEGIN CATCH
						IF @@TRANCOUNT > 0
						BEGIN
							ROLLBACK TRAN;
							SET @returnCode = -2;
						END
					END CATCH

					SET @activeTaskCount = @activeTaskCount - 1
				END
			END

			-- activate all the pending tasks
			UPDATE RunnerTasks SET TaskInProgress = 1 WHERE TaskInProgress = 0;
			SET @returnCode = 0;
		END
		ELSE
		BEGIN
			SET @returnCode = -1;
		END
	END TRY
	-- if we catch an error here, it's not something we want to handle, so stop execution and throw it back
	BEGIN CATCH
		SET @returnCode = -3;
		THROW;
	END CATCH
	RETURN @returnCode;
END
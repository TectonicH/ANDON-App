-- FILE : PROG3070TermProject_FunctionScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This file contains the functions created for the Manufacturing Term Project.
--			     The functions aid the simulation by generating the time to wait for the runner and lamp assembly to finish
--			      based on several configuration key/value pairs

-- FUNCTION : GetAsssemblyTime
-- DESCRIPTION : This function takes a LampID and calculates the actual assembly time using 
--				 the base assembly time and time scale from configuration, the worker level assembly time, 
--				 and assembly range percent associated with the worker making the lamp. 
--				 It will calculate the average assembly time by multiplying the base assembly time with the time
--				 scale and skill level assembly time rate. It will then calculate a random assembly time 
--				 between the average time minus the range percent and the average time plus the range percent

USE PROG3070_TermProjectDB;

GO

DROP FUNCTION IF EXISTS GetAssemblyTime;
DROP FUNCTION IF EXISTS GetRunnerTime;

GO

CREATE FUNCTION GetAssemblyTime
(@lampID int)
RETURNS int
BEGIN
	DECLARE @result int;

	DECLARE @timeScale decimal(8,4);
	SELECT @timeScale = CAST(ConfigValue AS decimal) FROM [Configurations] WHERE ConfigKey = 'timeScale';

	DECLARE @baseAssemblyTime int;
	SELECT @baseAssemblyTime = CAST(ConfigValue AS int) FROM [Configurations] WHERE ConfigKey = 'baseAssemblyTimeInSeconds';

	DECLARE @baseAssemblyMultiplier  decimal(8,4);;
	DECLARE @assemblyRange decimal(8,4);
	SELECT @baseAssemblyMultiplier = AssemblyTime, @assemblyRange = AssemblyRangePercent FROM WorkerLevels
		INNER JOIN Workers ON Workers.SkillLevel = WorkerLevels.SkillLevel
		INNER JOIN FogLamps ON FogLamps.WorkerID = Workers.WorkerID AND lampID = @LampID;

	IF @baseAssemblyTime IS NULL OR @timeScale IS NULL 
		SET @result = -1
	ELSE
	BEGIN
		IF @baseAssemblyMultiplier IS NULL OR @assemblyRange IS NULL
			SET @result = -2
		ELSE
		BEGIN
			DECLARE @randResult decimal(8, 4);
			SELECT @randResult = RandResult FROM GetRandValueView;

			DECLARE @averageAssemblyTime int = CAST(ROUND(@baseAssemblyTime * @timeScale * @baseAssemblyMultiplier, 0) AS int);
			DECLARE @assemblyLowRange int = @averageAssemblyTime - CAST(ROUND(@averageAssemblyTime * @assemblyRange, 0) AS int);
			DECLARE @assemblyHighRange int = @averageAssemblyTime + CAST(ROUND(@averageAssemblyTime * @assemblyRange, 0) AS int);
			
			SET @result = FLOOR(@randResult * (@assemblyHighRange - @assemblyLowRange + 1)) + @assemblyLowRange;
		END
	END

	RETURN @result;
END

GO


-- FUNCTION : GetRunnerTime
-- DESCRIPTION : This function takes no parameters and calculates the actual runner time 
--               using the timeScale and baseRunnerTimeInSeconds configuration setting.
--				 If either config keys are missing, an error result of -1 is returned.
CREATE FUNCTION GetRunnerTime ()
RETURNS int
BEGIN
	DECLARE @result int;

	DECLARE @timeScale decimal(8,4);
	SELECT @timeScale = CAST(ConfigValue AS decimal(8,4)) FROM [Configurations] WHERE ConfigKey = 'timeScale';

	DECLARE @baseRunnerTime int;
	SELECT @baseRunnerTime = CAST(ConfigValue AS int) FROM [Configurations] WHERE ConfigKey = 'baseRunnerTimeInSeconds';

	IF @timeScale IS NULL OR @baseRunnerTime IS NULL
		SET @result = -1;
	ELSE SET @result = CAST(ROUND(@baseRunnerTime * @timeScale, 0) AS int);

	RETURN @result;
END

GO
DROP FUNCTION IF EXISTS AssemblyStationProgress
GO

CREATE FUNCTION AssemblyStationProgress
	(@stationID int)
	RETURNS @assemblyStationProgress TABLE(
		StationID int, 
		LampInProgress int,
		LampsCompletedSuccessfully int,
		DefectiveAssembledLamps int,
		HarnessPartCount int,
		HousingPartCount int,
		ReflectorPartCount int,
		BulbPartCount int,
		BezelPartCount int,
		LensPartCount int,
		IsActive bit,
		RunnerTasksBeingCompleted int,
		RunnerTasksPending int)

AS
BEGIN
	DECLARE @progressLamp int = (SELECT count(LampID) FROM FogLamps WHERE StationID = @stationID AND [Status] = 'InProgress');
	DECLARE @lampsPassed int = (SELECT count(LampID) FROM FogLamps WHERE StationID = @stationID AND [Status] = 'Completed');
	DECLARE @lampsFailed int = (SELECT count(LampID) FROM FogLamps WHERE StationID = @stationID AND [Status] = 'Defective');
	DECLARE @harness int = (SELECT CurrentQuantity FROM Bins INNER JOIN AssemblyStations ON BinID = HarnessBin AND StationID = @stationID);
	DECLARE @housing int = (SELECT CurrentQuantity FROM Bins INNER JOIN AssemblyStations ON BinID = HousingBin AND StationID = @stationID);
	DECLARE @reflector int = (SELECT CurrentQuantity FROM Bins INNER JOIN AssemblyStations ON BinID = ReflectorBin AND StationID = @stationID);
	DECLARE @bulb int = (SELECT CurrentQuantity FROM Bins INNER JOIN AssemblyStations ON BinID = BulbBin AND StationID = @stationID);
	DECLARE @bezel int = (SELECT CurrentQuantity FROM Bins INNER JOIN AssemblyStations ON BinID = BezelBin AND StationID = @stationID);
	DECLARE @lens int = (SELECT CurrentQuantity FROM Bins INNER JOIN AssemblyStations ON BinID = LensBin AND StationID = @stationID);
	DECLARE @active bit = (SELECT ISActive From AssemblyStations WHERE StationID = @stationID)
	DECLARE @activeRunners int = (SELECT count(TaskID) FROM RunnerTasks WHERE StationID = @stationID AND TaskInProgress = 1);
	DECLARE @inActiveRunners int = (SELECT count(TaskID) FROM RunnerTasks WHERE StationID = @stationID AND TaskInProgress = 0);

	INSERT INTO @assemblyStationProgress (
		StationID, 
		LampInProgress, 
		LampsCompletedSuccessfully, 
		DefectiveAssembledLamps, 
		HarnessPartCount, 
		HousingPartCount, 
		ReflectorPartCount, 
		BulbPartCount, 
		BezelPartCount, 
		LensPartCount,
		IsActive,
		RunnerTasksBeingCompleted,
		RunnerTasksPending) 
	VALUES (
		@stationID,
		@progressLamp,
		@lampsPassed,
		@lampsFailed,
		@harness,
		@housing,
		@reflector,
		@bulb,
		@bezel,
		@lens,
		@active,
		@activeRunners,
		@inActiveRunners);

	RETURN;
END
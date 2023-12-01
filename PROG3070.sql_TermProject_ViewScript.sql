-- FILE : PROG3070TermProject_ViewScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This database script creates the views for the term project database

USE PROG3070_TermProjectDB;

GO
DROP VIEW IF EXISTS GetRandValueView;
GO

-- This view exists because we cannot call rand() in user defined functions, but we need to for the GetAssemblyTime function.
CREATE VIEW GetRandValueView
AS
SELECT rand() AS RandResult;

GO
DROP VIEW IF EXISTS AssemblyLineLampView
GO

-- VIEW : AssemblyLineLampView
-- DESCRIPTION : This displays useful information about the overall assembly line with regards to lamp counts and orders
CREATE VIEW AssemblyLineLampView
AS 

SELECT
	max(ConfigValue) as 'TotalOrders', 
	count(allLamps.LampID) as 'TotalLampsInSystem',
	count(inProgLamps.LampID) as 'TotalLampsInProgress', 
	count(totalNonProgressLamps.LampID) as 'TotalLampsProduced', 
	count(completedLamps.LampID) as 'TotalSuccessfulLampsYield'
	 FROM FogLamps allLamps
		LEFT JOIN RunnerTasks allRunners ON allRunners.TaskID > 0
		LEFT JOIN FogLamps totalNonProgressLamps ON totalNonProgressLamps.[Status] <> 'InProgress' AND allLamps.LampID = totalNonProgressLamps.LampID
		LEFT JOIN FogLamps inProgLamps ON inProgLamps.[Status] = 'InProgress'  AND allLamps.LampID = inProgLamps.LampID
		LEFT JOIN FogLamps completedLamps ON completedLamps.[Status] = 'Completed'  AND allLamps.LampID = completedLamps.LampID
		LEFT JOIN [Configurations] ON [Configurations].ConfigKey = 'TotalOrderCount';

GO
DROP VIEW IF EXISTS AssemblyLineTaskView
GO

-- VIEW : AssemblyLineLampView
-- DESCRIPTION : This displays useful information about the overall assembly line with regards to runners
CREATE VIEW AssemblyLineTaskView
AS
	SELECT  
		count(allRunners.TaskID) as 'TotalRunnerTasksInSystem',
		count(activeRunners.TaskID) as 'TotalActiveRunnerTasks',
		count(inactiveRunners.TaskID) as 'TotalPendingRunnerTasks'
		FROM RunnerTasks allRunners
			LEFT JOIN RunnerTasks activeRunners ON activeRunners.TaskInProgress = 1 AND activeRunners.TaskID = allRunners.TaskID
			LEFT JOIN RunnerTasks inactiveRunners ON inactiveRunners.TaskInProgress = 0 AND inactiveRunners.TaskID = allRunners.TaskID;

GO
DROP VIEW IF EXISTS RunnerStationTaskBreakdownView
GO

-- VIEW : RunnerStationTaskBreakdwonView
-- DESCRIPTION : This view shows all of the runner tasks joined so that we can see the bins they are replacing
CREATE VIEW RunnerStationTaskBreakdownView
AS
	SELECT RunnerTasks.PartID, BinID, RunnerTasks.StationID, TaskInProgress
		FROM Bins
		INNER JOIN RunnerTasks ON RunnerTasks.PartID = Bins.PartID
		INNER JOIN AssemblyStations ON AssemblyStations.StationID = RunnerTasks.StationID
		WHERE BinID IN (HarnessBin, HousingBin, ReflectorBin, BulbBin, BezelBin, LensBin)
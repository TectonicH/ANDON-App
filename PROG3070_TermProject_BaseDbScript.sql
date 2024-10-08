-- FILE : PROG3070_TermProject_BaseDbScript.sql
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This script contains the creation of the term project database, its tables, and its indexes  
--			     but does not include the functions, views, triggers, or stored procedures.
--				 This script also inserts rows in tables that only hold constant data that will not change throughout the simulation.


-- set up a try catch so we don't keep executing if we can't delete the database
BEGIN TRY
	-- drop and recreate database if it exists so that this script can also reset the database
	DROP DATABASE IF EXISTS PROG3070_TermProjectDB; 
	CREATE DATABASE PROG3070_TermProjectDB;
	USE PROG3070_TermProjectDB;

	--
	-- create database tables
	--

	-- Stores all configuration settings set for the simulation
	-- Configurations will be set at the beginning of the simulation by an initializer script
	-- and will not be changed for the duration of the simulation.
	CREATE TABLE [Configurations] (
		ConfigKey nvarchar(50),
		ConfigValue nvarchar(50) NOT NULL
		PRIMARY KEY (ConfigKey)
	);

	-- Stores the possible states of a fog lamp. 
	-- This table will be filled with static values that will not be affected by the running of the simulation
	CREATE TABLE LampStatuses (
		[Status] nchar(10),
		PRIMARY KEY ([Status])
	);

	-- Stores all of the possible part names as unique ids.
	-- This table will be filled with static values that will not be affected by the running of the simulation
	CREATE TABLE Parts (
		PartID NCHAR(9),
		BinCapacity int NOT NULL,
		PRIMARY KEY (PartID)
	);

	-- Stores information about specific part bins that have been created.
	-- Bins will be deleted from the database when they are replaced with new bins that will be added
	CREATE TABLE Bins (
		BinID int IDENTITY(1,1),
		PartID nchar(9) NOT NULL,
		CurrentQuantity int NOT NULL,
		PRIMARY KEY (BinID),
		FOREIGN KEY (PartID) REFERENCES Parts(PartID)
	);

	-- Stores all of the possible skill levels for a worker, including their defect rate, 
	-- the average time it should take to assemble a part, and how much their assembly time may randomly vary above and below the assembly time.
	-- This table will be filled with static values that will not be affected by the running of the simulation
	CREATE TABLE WorkerLevels (
		SkillLevel nchar(12),
		DefectRate decimal(6, 4) NOT NULL,
		AssemblyTime decimal(4, 2) NOT NULL,
		AssemblyRangePercent decimal(6, 4) NOT NULL,
		PRIMARY KEY (SkillLevel)
	);


	-- Stores Information on individual workers
	-- Each worker is assigned to an assembly station and has a given skill level from the WorkerLevels table
	-- Workers will be created in the simulation along with the stations they work at
	CREATE TABLE Workers (
		WorkerID int IDENTITY(1,1),
		SkillLevel nchar(12) NOT NULL,
		PRIMARY KEY (WorkerID),
		FOREIGN KEY (SkillLevel) REFERENCES WorkerLevels(SkillLevel)
	);

	-- Stores information about a single assembly station capable of creating one fog lamp at a time and managed by one asssembly worker.
	-- The number of assembly stations to be created will be determined by the config table and are created at the beginning of the simulation.
	-- No Assembly Stations should be deleted during the simulation, however its bins will be updated as they run low and are replaced,
	-- and the isActive flag may be updated if the station is disabled for any reason.
	CREATE TABLE AssemblyStations (
		StationID int IDENTITY(1,1),
		IsActive bit NOT NULL,
		CurrentWorkerID int,
		HarnessBin int,
		ReflectorBin int,
		BulbBin int,
		BezelBin int,
		HousingBin int,
		LensBin int,
		PRIMARY KEY (StationID),
		FOREIGN KEY (CurrentWorkerID) REFERENCES Workers(WorkerID),
		FOREIGN KEY (HarnessBin) REFERENCES Bins(BinID),
		FOREIGN KEY (HousingBin) REFERENCES Bins(BinID),
		FOREIGN KEY (ReflectorBin) REFERENCES Bins(BinID),
		FOREIGN KEY (BulbBin) REFERENCES Bins(BinID),
		FOREIGN KEY (BezelBin) REFERENCES Bins(BinID),
		FOREIGN KEY (LensBin) REFERENCES Bins(BinID)
	);

	-- Stores jobs for the runner
	-- The TaskInProgress bit determines if the task is in the process of being completed, 
	-- or if it is waiting for the runner to start it. Runner tasks are created when the parts of a bin reach a specific count
	-- as a queued task, and will be activated by the runner simulator. When tasks are completed, they are deleted from the table
	CREATE TABLE RunnerTasks (
		TaskID int IDENTITY(1,1),
		PartID nchar(9) NOT NULL,
		StationID int NOT NULL,
		TaskInProgress bit NOT NULL,
		PRIMARY KEY (TaskID),
		FOREIGN KEY (PartID) REFERENCES Parts(PartID),
		FOREIGN KEY (StationID) REFERENCES AssemblyStations(StationID)
	);


	-- Stores information about a single fog lamp
	-- The worker and station are set by the assembly station in charge of assembling the lamp
	-- The status is set by the LampStatuses table and indicates if the lamp is being assembled, 
	-- has been assembled successfully, or has been assembled but has failed testing
	-- FogLamps will not be deleted so that they can be used to track overall metrics for each work station
	CREATE TABLE FogLamps (
		LampID int IDENTITY(1,1),
		StationID int NOT NULL,
		WorkerID int NOT NULL,
		[Status] nchar(10) NOT NULL,
		AssemblyTimeInSeconds int,
		PRIMARY KEY (LampID),
		FOREIGN KEY (StationID) REFERENCES AssemblyStations(StationID), 
		FOREIGN KEY (WorkerID) REFERENCES Workers(WorkerID),
		FOREIGN KEY ([Status]) REFERENCES LampStatuses([Status])
	);

	--
	-- change database configuration as needed
	--

	ALTER DATABASE PROG3070_TermProjectDB
		SET ALLOW_SNAPSHOT_ISOLATION ON
	--
	-- insert constant/static table data
	--

	INSERT INTO Parts(PartID, BinCapacity) VALUES
		('Harness', 55),
		('Housing', 24),
		('Reflector', 35),
		('Bulb', 60),
		('Bezel', 75),
		('Lens', 40);

	INSERT INTO LampStatuses([Status]) VALUES
		('InProgress'),
		('Completed'),
		('Defective');

	INSERT INTO WorkerLevels(SkillLevel, DefectRate, AssemblyTime, AssemblyRangePercent) VALUES
		('Beginner', 0.0085, 1.5, 0.1),
		('Intermediate', 0.005, 1.0, 0.1),
		('Expert', 0.0015, 0.85, 0.1);

	INSERT INTO [Configurations](ConfigKey, ConfigValue) VALUES
		('TimeScale', '0.2'),
		('BaseRunnerTimeInSeconds', '300'),
		('BaseAssemblyTimeInSeconds', '60'),
		('MinPartCountForNotification', '5'),
		('TotalOrderCount', '100');

	--
	-- create indexes
	--

	CREATE NONCLUSTERED INDEX idxFogLampStationID 
		ON FogLamps(StationID);

	CREATE NONCLUSTERED INDEX idxFogLampStatus
		ON FogLamps([Status]);

	CREATE NONCLUSTERED INDEX idxBinPartID
		ON Bins(PartID);

	CREATE NONCLUSTERED INDEX idxStationsIsActive
		ON AssemblyStations(IsActive);

	CREATE NONCLUSTERED INDEX idxTaskInProgress
		ON RunnerTasks(TaskInProgress);

	CREATE NONCLUSTERED INDEX idxWorkerSkillLevel
		ON Workers(SkillLevel);

END TRY
BEGIN CATCH

	SELECT ERROR_MESSAGE() AS 'Error Message';

END CATCH


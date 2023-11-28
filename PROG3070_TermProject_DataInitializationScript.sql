-- FILE : PROG3070TermProject_DataInitializationScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This script inserts all of the initial data required by the simulation 
--			     that is subject to potential change in the simulation. This includes the workers, assembly stations and bins
--               but does not include constant data tables such as Parts, LampStatuses, and WorkerLevels which are defined in the table script.

USE PROG3070_TermProjectDB;

GO

-- get the bin capacity for each part, which already exists and insert the bins

DECLARE @harnessCapacity int;
DECLARE @housingCapacity int;
DECLARE @reflectorCapacity int;
DECLARE @bulbCapacity int;
DECLARE @bezelCapacity int;
DECLARE @lensCapacity int;

SELECT @harnessCapacity = BinCapacity FROM Parts Where PartID = 'Harness';
SELECT @housingCapacity = BinCapacity FROM Parts Where PartID = 'Housing';
SELECT @reflectorCapacity = BinCapacity FROM Parts Where PartID = 'Reflector';
SELECT @bulbCapacity = BinCapacity FROM Parts Where PartID = 'Bulb';
SELECT @bezelCapacity = BinCapacity FROM Parts Where PartID = 'Bezel';
SELECT @lensCapacity = BinCapacity FROM Parts Where PartID = 'Lens';

INSERT INTO Bins (PartID, CurrentQuantity) VALUES
	('Harness', @harnessCapacity),
	('Harness', @harnessCapacity),
	('Harness', @harnessCapacity),
	('Housing', @housingCapacity),
	('Housing', @housingCapacity),
	('Housing', @housingCapacity),
	('Reflector', @reflectorCapacity),
	('Reflector', @reflectorCapacity),
	('Reflector', @reflectorCapacity),
	('Bulb', @bulbCapacity),
	('Bulb', @bulbCapacity),
	('Bulb', @bulbCapacity),
	('Bezel', @bezelCapacity),
	('Bezel', @bezelCapacity),
	('Bezel', @bezelCapacity),
	('Lens', @lensCapacity),
	('Lens', @lensCapacity),
	('Lens', @lensCapacity);

-- create a worker for each station, using each of the skill level constants already stored
INSERT INTO Workers (SkillLevel) VALUES
	('Beginner'),
	('Intermediate'),
	('Expert');

-- insert new assembly stations with bins and workers attached
-- we use IDENTIY(1,1) to handle the ids for bin and workers, so we can assign the right ids based on knowing the insert order
INSERT INTO AssemblyStations (IsActive, CurrentWorkerID, HarnessBin, ReflectorBin, BulbBin, BezelBin, HousingBin, LensBin)
	VALUES 
		(1, 1, 1, 7, 10, 13, 4, 16),
		(1, 2, 2, 8, 11, 14, 5, 17),
		(1, 3, 3, 9, 12, 15, 6, 18);
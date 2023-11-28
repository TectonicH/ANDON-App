-- FILE : PROG3070TermProject_procedureScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This script contains the stored procedures for the term project database.
--				 These procedures simulate the main employee workflow with workers assembling lamps and runners refreshing bins

CREATE PROCEDURE BeginAssembly
@stationID int,
@lampID int OUTPUT
AS
BEGIN
	-- TODO implement functionality
	SET @lampID = 1;
END;

GO

CREATE PROCEDURE FinishAssembly
@lampID int
AS
BEGIN
	-- TODO implement functionality, currently using a dummy statement
	SET @lampID = 0;
END;

GO

CREATE PROCEDURE RefreshRunnerLoop
AS
BEGIN
	-- TODO implement functionality, currently using a dummy
	DECLARE @dummyValue int = 0;
END;
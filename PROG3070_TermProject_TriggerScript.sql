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

CREATE TRIGGER BinCountTrigger
ON Bins AFTER UPDATE
AS
BEGIN
	-- TODO implement trigger
	DECLARE @minimumValue int = 5;

END
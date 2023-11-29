-- FILE : PROG3070TermProject_ViewScript
-- PROJECT : PROG3070 Term Project : Manufacturing Scenario
-- PROGRAMMERS : Elizabeth deVries and Kristian Biviens
-- SUBMISSION DATE : November 29 2023
-- DESCRIPTION : This database script creates the views for the term project database

USE PROG3070_TermProjectDB;

GO

-- This view exists because we cannot call rand() in user defined functions, but we need to for the GetAssemblyTime function.
CREATE VIEW GetRandValueView
AS
SELECT rand() AS RandResult;
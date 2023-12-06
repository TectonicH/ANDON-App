To Create the Database and run the programs

	1) Run the 'PROG3070_TermProject_BaseDbScript.sql' script on your sql server
	2) Run the 'PROG3070.sql_TermProject_ViewScript.sql' script on your sql server. 
		This must be done before the functions script is run because the function script relies on a specific view existing.
	3) Run all other database scripts:
		'PROG3070_TermProject_ProcedureScript.sql'
		'PROG3070_TermProject_TriggerScript.sql'
		'PROG3070TermProject_FunctionScript.sql'
		'PROG3070_TermProject_DataInitializationScript.sql'
	4) query the database for the assembly station 'StationIDs' to note what the station IDs are, which we will need for the workstation sim. 
		If this is the first time the database has been set up, the station IDs will be 1, 2, or 3, however they are based on the IDENTITY function, so if the database is reset
		or the data is dropped and re-added, the station ids may be different.
	5) Go to the app.config files for RunnerProgram, Workstation Sim Program, and AssemblyLineKanbanDisplay, and make sure they exist in the same directory level as the executables. 
		Open the app.config files for each and insert your connection string in the connectionString property inside the add tag that is nested within the connectionStrings tag.
	6) Launch the RunnerProgram from the command line. This program operates a runner that works on all active stations.
	7) Launch the Workstation Sim program from three different command lines. Provide the three different station IDs noted earlier when prompted
	8) Run the RunnerStationStatus, Andon Station Display, and AssemblyLineKanbanDisplay executables.

To run the config tool:

	1) Replace server name (laptop-mfjmrjal) with your server
	2) Right click on edmx file and hit 'run custom tool'
	3) Build the project. 

	If the prior steps don't work: 

	1) Double click on edmx page
	2) Right click anywhere in the view, then hit update model from database, then make sure configuration table is in the 'refresh' page then hit finish 
		(then save the file to trigger the tool to run)
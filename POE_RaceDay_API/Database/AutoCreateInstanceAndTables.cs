
using System;
using System.Data.SqlClient;
using System.Diagnostics;
namespace POE_RaceDay_API.Database
{
   // a Class that generates the Instance, Database and Tables Automatically
        public class AutoCreateInstanceAndTables
        {
        //devlaring the instance and database names globally
        private readonly string instanceName = "POERaceday_API";
            private readonly string databaseName = "RaceDay_Database";

            private string connectionStringToInstance =>
                $@"Server=(localdb)\{instanceName};Integrated Security=true;";

            private string connectionStringToDatabase =>
                $@"Server=(localdb)\{instanceName};Database={databaseName};Integrated Security=true;";

            // INITIALIZING THE SYSTEM

            public void InitializeSystem()
            {
                try
                {
                    CreateLocalDbInstance();
                    StartLocalDbInstance();
                    CreateDatabase();
                    CreateTables();

                    Console.WriteLine(
                        "RaceDay database and tables verified successfully!");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Database initialization failed: {ex.Message}");

                    throw;
                }
            }

            // CHECK LOCALDB INSTANCE

            private bool CheckInstanceExists()
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sqllocaldb.exe",
                    Arguments = $"info \"{instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo)
                    ?? throw new Exception(
                        "Could not start sqllocaldb.exe. Ensure SQL Server LocalDB is installed.");

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0 &&
                       !string.IsNullOrWhiteSpace(output);
            }

            // CREATE LOCALDB INSTANCE

            private void CreateLocalDbInstance()
            {
                if (CheckInstanceExists())
                {
                    Console.WriteLine(
                        $"LocalDB instance '{instanceName}' already exists.");

                    return;
                }

                RunLocalDbCommand("create");

                Console.WriteLine(
                    $"LocalDB instance '{instanceName}' created.");
            }

            // START LOCALDB INSTANCE

            private void StartLocalDbInstance()
            {
                RunLocalDbCommand("start");

                Console.WriteLine(
                    $"LocalDB instance '{instanceName}' started.");
            }

            private void RunLocalDbCommand(string action)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sqllocaldb.exe",
                    Arguments = $"{action} \"{instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo)
                    ?? throw new Exception(
                        "Could not start sqllocaldb.exe.");

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception(
                        $"LocalDB {action} failed: {error} {output}");
                }
            }

            // CREATE DATABASE

            private void CreateDatabase()
            {
                using var connection =
                    new SqlConnection(connectionStringToInstance);

                connection.Open();

                string sql = @"
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @CreateSql NVARCHAR(MAX);

    SET @CreateSql =
        N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);

    EXEC sp_executesql @CreateSql;
END;";

                using var command = new SqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@DatabaseName", databaseName);

                command.ExecuteNonQuery();

                Console.WriteLine(
                    $"Database '{databaseName}' verified.");
            }

            // CREATE TABLES

            private void CreateTables()
            {
                using var connection =
                    new SqlConnection(connectionStringToDatabase);

                connection.Open();

                // USERS TABLE

                string usersSql = @"
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserID INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Users PRIMARY KEY,

        FullName NVARCHAR(150) NOT NULL,

        Email NVARCHAR(254) NOT NULL,

        PasswordHash NVARCHAR(255) NOT NULL,

        Role NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Users_Role
            DEFAULT 'Participant',

        PhoneNumber NVARCHAR(20) NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Users_CreatedAt
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT UQ_Users_Email UNIQUE (Email),

        CONSTRAINT CK_Users_Role
            CHECK (Role IN ('Participant', 'Organiser'))
    );
END;";

                ExecuteSql(connection, usersSql);

                Console.WriteLine("Users table verified.");

                // EVENTS TABLE

                string eventsSql = @"
IF OBJECT_ID('dbo.Events', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events
    (
        EventID INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Events PRIMARY KEY,

        EventName NVARCHAR(150) NOT NULL,

        Description NVARCHAR(1000) NULL,

        EventDate DATETIME2 NOT NULL,

        Location NVARCHAR(250) NOT NULL,

        OrganiserID INT NOT NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Events_CreatedAt
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Events_Users
            FOREIGN KEY (OrganiserID)
            REFERENCES dbo.Users(UserID)
    );
END;";

                ExecuteSql(connection, eventsSql);

                Console.WriteLine("Events table verified.");

                // CATEGORIES TABLE

                string categoriesSql = @"
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryID INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Categories PRIMARY KEY,

        CategoryName NVARCHAR(100) NOT NULL,

        Distance DECIMAL(8,2) NOT NULL,

        EntryFee DECIMAL(10,2) NOT NULL
            CONSTRAINT DF_Categories_EntryFee
            DEFAULT 0,

        EventID INT NOT NULL,

        CONSTRAINT FK_Categories_Events
            FOREIGN KEY (EventID)
            REFERENCES dbo.Events(EventID),

        CONSTRAINT CK_Categories_Distance
            CHECK (Distance > 0),

        CONSTRAINT CK_Categories_EntryFee
            CHECK (EntryFee >= 0)
    );
END;";

                ExecuteSql(connection, categoriesSql);

                Console.WriteLine("Categories table verified.");

                // ENROLMENTS TABLE

                string enrolmentsSql = @"
IF OBJECT_ID('dbo.Enrolments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Enrolments
    (
        EnrolmentID INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Enrolments PRIMARY KEY,

        ParticipantID INT NOT NULL,

        CategoryID INT NOT NULL,

        EnrolmentDate DATETIME2 NOT NULL
            CONSTRAINT DF_Enrolments_Date
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Enrolments_Users
            FOREIGN KEY (ParticipantID)
            REFERENCES dbo.Users(UserID),

        CONSTRAINT FK_Enrolments_Categories
            FOREIGN KEY (CategoryID)
            REFERENCES dbo.Categories(CategoryID),

        CONSTRAINT UQ_Enrolments_Participant_Category
            UNIQUE (ParticipantID, CategoryID)
    );
END;";

                ExecuteSql(connection, enrolmentsSql);

                Console.WriteLine("Enrolments table verified.");

                // RESULTS TABLE

                string resultsSql = @"
IF OBJECT_ID('dbo.Results', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Results
    (
        ResultID INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Results PRIMARY KEY,

        EnrolmentID INT NOT NULL,

        FinishTime TIME(0) NOT NULL,

        Position INT NOT NULL,

        RecordedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Results_RecordedAt
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Results_Enrolments
            FOREIGN KEY (EnrolmentID)
            REFERENCES dbo.Enrolments(EnrolmentID),

        CONSTRAINT UQ_Results_Enrolment
            UNIQUE (EnrolmentID),

        CONSTRAINT CK_Results_Position
            CHECK (Position > 0)
    );
END;";

                ExecuteSql(connection, resultsSql);

                Console.WriteLine("Results table verified.");

                Console.WriteLine(
                    "All RaceDay tables verified successfully!");
            }

            // EXECUTE SQL

            private static void ExecuteSql(
                SqlConnection connection,
                string sql)
            {
                using var command = new SqlCommand(sql, connection);

                command.ExecuteNonQuery();
            }
        }
    }
//I AM DONEEE // danko
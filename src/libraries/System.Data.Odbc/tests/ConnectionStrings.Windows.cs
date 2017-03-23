namespace System.Data.Odbc.Tests
{
    public static class ConnectionStrings
    {
        // Requires http://www.ch-werner.de/sqliteodbc/
        public const string WorkingConnection =
            "Driver=SQLite3 ODBC Driver;" +
            "Database=smoketests.sqlite;";
    }
}

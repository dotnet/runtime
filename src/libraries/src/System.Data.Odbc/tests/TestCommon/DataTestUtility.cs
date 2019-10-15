// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Data.Odbc.Tests
{
    public static class DataTestUtility
    {
        public static readonly string OdbcConnStr = null;

        static DataTestUtility()
        {
            OdbcConnStr = Environment.GetEnvironmentVariable("TEST_ODBC_CONN_STR");
        }

        public static bool AreConnStringsSetup()
        {
            return !string.IsNullOrEmpty(OdbcConnStr);
        }

        // the name length will be no more then (16 + prefix.Length + escapeLeft.Length + escapeRight.Length)
        // some providers does not support names (Oracle supports up to 30)
        public static string GetUniqueName(string prefix, string escapeLeft, string escapeRight)
        {
            string uniqueName = string.Format("{0}{1}_{2}_{3}{4}",
                escapeLeft,
                prefix,
                DateTime.Now.Ticks.ToString("X", CultureInfo.InvariantCulture), // up to 8 characters
                Guid.NewGuid().ToString().Substring(0, 6), // take the first 6 characters only
                escapeRight);
            return uniqueName;
        }

        public static void RunNonQuery(string connectionString, string sql)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                using (OdbcCommand command = new OdbcCommand(sql, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

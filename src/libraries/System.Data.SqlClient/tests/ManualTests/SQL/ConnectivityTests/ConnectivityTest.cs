// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ConnectivityParametersTest
    {
        private const string COL_PROGRAM_NAME = "ProgramName";
        private const string COL_HOSTNAME = "HostName";

        [CheckConnStrSetupFact]
        public static void EnvironmentHostNameTest()
        {
            SqlConnectionStringBuilder builder = (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { Pooling = true });
            builder.ApplicationName = "HostNameTest";

            using (SqlConnection sqlConnection = new SqlConnection(builder.ConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand("sp_who2", sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int programNameOrdinal = reader.GetOrdinal(COL_PROGRAM_NAME);
                            string programName = reader.GetString(programNameOrdinal);
                            
                            if (programName != null && programName.Trim().Equals(builder.ApplicationName))
                            {
                                // Get the hostname
                                int hostnameOrdinal = reader.GetOrdinal(COL_HOSTNAME);
                                string hostnameFromServer = reader.GetString(hostnameOrdinal);
                                string expectedMachineName = Environment.MachineName.ToUpper();
                                string hostNameFromServer = hostnameFromServer.Trim().ToUpper();
                                Assert.Matches(expectedMachineName, hostNameFromServer);
                                return;
                            }
                        }
                    }
                }
            }
            Assert.True(false, "No non-empty hostname found for the application");
        }
    }
}

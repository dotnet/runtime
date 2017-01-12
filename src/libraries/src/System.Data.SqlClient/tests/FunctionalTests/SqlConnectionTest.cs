// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlConnectionBasicTests
    {

        [Fact]
        [ActiveIssue(14017)]
        public void ConnectionTest()
        {
            Exception e = null;

            using (TestTdsServer server = TestTdsServer.StartTestServer())
            {
                try
                {
                    SqlConnection connection = new SqlConnection(server.ConnectionString);
                    connection.Open();
                }
                catch (Exception ce)
                {
                    e = ce;
                }
            }
            Assert.Null(e);
        }

    }

}

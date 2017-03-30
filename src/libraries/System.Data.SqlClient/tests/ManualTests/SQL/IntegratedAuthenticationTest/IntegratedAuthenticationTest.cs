// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class IntegratedAuthenticationTest
    {
        [CheckConnStrSetupFact]
        public static void IntegratedAuthenticationTestWithConnectionPooling()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            builder.IntegratedSecurity = true;
            builder.Pooling = true;
            TryOpenConnectionWithIntegratedAuthentication(builder.ConnectionString);
        }

        [CheckConnStrSetupFact]
        public static void IntegratedAuthenticationTestWithOutConnectionPooling()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            builder.IntegratedSecurity = true;
            builder.Pooling = false;
            TryOpenConnectionWithIntegratedAuthentication(builder.ConnectionString);
        }

        public static void TryOpenConnectionWithIntegratedAuthentication(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
            }
        }
    }
}

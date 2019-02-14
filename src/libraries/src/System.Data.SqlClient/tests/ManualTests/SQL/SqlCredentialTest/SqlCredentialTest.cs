// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlClient.ManualTesting.Tests;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlCredentialTest
    {

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        public static void CreateSqlConnectionWithCredential()
        {
            var user = "u" + Guid.NewGuid().ToString().Replace("-", "");
            var passStr = "Pax561O$T5K#jD";

            try
            {
                createTestUser(user, passStr);

                var csb = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
                csb.Remove("User ID");
                csb.Remove("Password");
                csb.IntegratedSecurity = false;

                var password = new SecureString();
                passStr.ToCharArray().ToList().ForEach(x => password.AppendChar(x));
                password.MakeReadOnly();

                using (var conn = new SqlConnection(csb.ConnectionString, new SqlCredential(user, password)))
                using (var cmd = new SqlCommand("SELECT 1;", conn))
                {
                    conn.Open();
                    Assert.Equal(1, cmd.ExecuteScalar());
                }
            }
            finally
            {
                dropTestUser(user);
            }
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        public static void SqlConnectionChangePasswordPlaintext()
        {
            var user = "u" + Guid.NewGuid().ToString().Replace("-", "");
            var pass = "!21Ja3Ims7LI&n";
            var newPass = "fmVCNf@24Dg*8j";

            try
            {
                createTestUser(user, pass);

                var csb = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
                csb.UserID = user;
                csb.Password = pass;
                csb.IntegratedSecurity = false;

                // Change password and try opening connection.
                SqlConnection.ChangePassword(csb.ConnectionString, newPass);
                csb.Password = newPass;

                using (var conn = new SqlConnection(csb.ConnectionString))
                using (var cmd = new SqlCommand("SELECT 1;", conn))
                {
                    conn.Open();
                    Assert.Equal(1, cmd.ExecuteScalar());
                }
            }
            finally
            {
                dropTestUser(user);
            }
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        public static void SqlConnectionChangePasswordSecureString()
        {
            var user = "u" + Guid.NewGuid().ToString().Replace("-", "");
            var passStr = "tcM0qB^izt%3u7";
            var newPassStr = "JSG2e(Vp0WCXE&";

            try
            {
                createTestUser(user, passStr);

                var csb = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
                csb.Remove("User ID");
                csb.Remove("Password");
                csb.IntegratedSecurity = false;

                var password = new SecureString();
                passStr.ToCharArray().ToList().ForEach(x => password.AppendChar(x));
                password.MakeReadOnly();

                var newPassword = new SecureString();
                newPassStr.ToCharArray().ToList().ForEach(x => newPassword.AppendChar(x));
                newPassword.MakeReadOnly();

                // Change password and try opening connection.
                SqlConnection.ChangePassword(csb.ConnectionString, new SqlCredential(user, password), newPassword);

                using (var conn = new SqlConnection(csb.ConnectionString, new SqlCredential(user, newPassword)))
                using (var cmd = new SqlCommand("SELECT 1;", conn))
                {
                    conn.Open();
                    Assert.Equal(1, cmd.ExecuteScalar());
                }
            }
            finally
            {
                dropTestUser(user);
            }
        }

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        public static void OldCredentialsShouldFail()
        {
            String user = "u" + Guid.NewGuid().ToString().Replace("-", "");
            String passStr = "Pax561O$T5K#jD";

            try
            {
                createTestUser(user, passStr);

                SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
                sqlConnectionStringBuilder.Remove("User ID");
                sqlConnectionStringBuilder.Remove("Password");
                sqlConnectionStringBuilder.IntegratedSecurity = false;

                SecureString password = new SecureString();
                passStr.ToCharArray().ToList().ForEach(x => password.AppendChar(x));
                password.MakeReadOnly();
                SqlCredential credential = new SqlCredential(user, password);

                using (SqlConnection conn1 = new SqlConnection(sqlConnectionStringBuilder.ConnectionString, credential))
                using (SqlConnection conn2 = new SqlConnection(sqlConnectionStringBuilder.ConnectionString, credential))
                using (SqlConnection conn3 = new SqlConnection(sqlConnectionStringBuilder.ConnectionString, credential))
                using (SqlConnection conn4 = new SqlConnection(sqlConnectionStringBuilder.ConnectionString, credential))
                {
                    conn1.Open();
                    conn2.Open();
                    conn3.Open();
                    conn4.Open();

                    SecureString newPassword = new SecureString();
                    "newPassword".ToCharArray().ToList().ForEach(x => newPassword.AppendChar(x));
                    newPassword.MakeReadOnly();
                    SqlConnection.ChangePassword(sqlConnectionStringBuilder.ConnectionString, credential, newPassword);
                    using (SqlConnection conn5 = new SqlConnection(sqlConnectionStringBuilder.ConnectionString, new SqlCredential(user, password)))
                    {
                        Assert.Throws<SqlException>(() => conn5.Open());
                    }
                }
            }
            finally
            {
                dropTestUser(user);
            }
        }

        private static void createTestUser(string username, string password)
        {
            // Creates a test user with read permissions.
            string createUserCmd = $"CREATE LOGIN {username} WITH PASSWORD = '{password}', CHECK_POLICY=OFF;"
                                    + $"EXEC sp_adduser '{username}', '{username}', 'db_datareader';";

            using (var conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (var cmd = new SqlCommand(createUserCmd, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static void dropTestUser(string username)
        {
            // Removes a created test user.
            string dropUserCmd = $"IF EXISTS (SELECT * FROM sys.schemas WHERE name = '{username}') BEGIN DROP SCHEMA {username} END;"
                                + $"IF EXISTS (SELECT * FROM sys.database_principals WHERE type = 'S' AND name = '{username}') BEGIN DROP USER {username} END;"
                                + $"DROP LOGIN {username}";

            // Pool must be cleared to prevent DROP LOGIN failure.
            SqlConnection.ClearAllPools();

            using (var conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (var cmd = new SqlCommand(dropUserCmd, conn))
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}

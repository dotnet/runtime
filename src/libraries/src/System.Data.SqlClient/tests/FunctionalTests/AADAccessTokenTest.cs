// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class AADAccessTokenTest
    {
        SqlConnectionStringBuilder _builder;

        [Fact]
        public void InvalidCombinationOfAccessToken()
        {
            _builder = new SqlConnectionStringBuilder
            {
                ["Data Source"] = "sample.database.windows.net",
                ["Integrated Security"] = true
            };
            InvalidCombinationCheck(null);

            _builder = new SqlConnectionStringBuilder
            {
                ["UID"] = "test"
            };
            InvalidCombinationCheck(null);

            _builder = new SqlConnectionStringBuilder
            {
                ["PWD"] = "test"
            };
            InvalidCombinationCheck(null);

            _builder = new SqlConnectionStringBuilder
            {
                ["Data Source"] = "sample.database.windows.net",
            };
            Security.SecureString password = new Security.SecureString();
            password.MakeReadOnly();
            SqlCredential credential = new SqlCredential("userID", password);
            InvalidCombinationCheck(credential);
        }

        private void InvalidCombinationCheck(SqlCredential credential)
        {
            using (SqlConnection connection = new SqlConnection(_builder.ConnectionString, credential))
            {
                Assert.Throws<InvalidOperationException>(() => connection.AccessToken = "SampleAccessToken");
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class PoolBlockPeriodTest
    {
        private static readonly string _sampleAzureEndpoint = "nonexistance.database.windows.net";
        private static readonly string _sampleNonAzureEndpoint = "nonexistanceserver";
        private static readonly string _policyKeyword = "PoolBlockingPeriod";

        [Theory]
        [InlineData("Azure with Default Policy must Disable blocking (.database.windows.net)", new object[] { "nonexistance.database.windows.net" })]
        [InlineData("Azure with Default Policy must Disable blocking (.database.chinacloudapi.cn)", new object[] { "nonexistance.database.chinacloudapi.cn" })]
        [InlineData("Azure with Default Policy must Disable blocking (.database.usgovcloudapi.net)", new object[] { "nonexistance.database.usgovcloudapi.net" })]
        [InlineData("Azure with Default Policy must Disable blocking (.database.cloudapi.de)", new object[] { "nonexistance.database.cloudapi.de" })]
        [InlineData("Azure with Default Policy must Disable blocking (MIXED CASES) (.database.WINDOWS.net)", new object[] { "nonexistance.database.WINDOWS.net" })]
        [InlineData("Azure with Default Policy must Disable blocking (PORT) (.database.WINDOWS.net,1234)", new object[] { "nonexistance.database.WINDOWS.net,1234" })]
        [InlineData("Azure with Default Policy must Disable blocking (INSTANCE NAME) (.database.WINDOWS.net,1234\\INSTANCE_NAME)", new object[] { "nonexistance.database.WINDOWS.net,1234\\INSTANCE_NAME" })]
        [InlineData("Azure with Auto Policy must Disable Blocking", new object[] { "nonexistance.database.windows.net", PoolBlockingPeriod.Auto })]
        [InlineData("Azure with Always Policy must Enable Blocking", new object[] { "nonexistance.database.windows.net", PoolBlockingPeriod.AlwaysBlock })]
        [InlineData("Azure with Never Policy must Disable Blocking", new object[] { "nonexistance.database.windows.net", PoolBlockingPeriod.NeverBlock })]
        public void TestAzureBlockingPeriod(string description, object[] Params)
        {
            SqlConnection.ClearAllPools();
            string serverName = Params[0] as string;
            PoolBlockingPeriod? policy = null;
            if (Params.Length > 1)
            {
                policy = (PoolBlockingPeriod)Params[1];
            }

            string connString = CreateConnectionString(serverName, policy);
            PoolBlockingPeriodAzureTest(connString, policy);
        }

        [Theory]
        [InlineData("NonAzure with Default Policy must Enable blocking", new object[] { "nonexistanceserver" })]
        [InlineData("NonAzure with Auto Policy must Enable Blocking", new object[] { "nonexistanceserver", PoolBlockingPeriod.Auto })]
        [InlineData("NonAzure with Always Policy must Enable Blocking", new object[] { "nonexistanceserver", PoolBlockingPeriod.AlwaysBlock })]
        [InlineData("NonAzure with Never Policy must Disable Blocking", new object[] { "nonexistanceserver", PoolBlockingPeriod.NeverBlock })]
        [InlineData("NonAzure (which contains azure endpoint - nonexistance.WINDOWS.net) with Default Policy must Enable Blocking", new object[] { "nonexistance.windows.net" })]
        [InlineData("NonAzure (which contains azure endpoint - nonexistance.database.WINDOWS.net.else) with Default Policy must Enable Blocking", new object[] { "nonexistance.database.windows.net.else" })]
        public void TestNonAzureBlockingPeriod(string description, object[] Params)
        {
            SqlConnection.ClearAllPools();
            string serverName = Params[0] as string;
            PoolBlockingPeriod? policy = null;

            if (Params.Length > 1)
            {
                policy = (PoolBlockingPeriod)Params[1];
            }

            string connString = CreateConnectionString(serverName, policy);
            PoolBlockingPeriodNonAzureTest(connString, policy);
        }

        [Theory]
        [InlineData("Test policy with Auto (lowercase)", new object[] { "auto" })]
        [InlineData("Test policy with Auto (miXedcase)", new object[] { "auTo" })]
        [InlineData("Test policy with Auto (Pascalcase)", new object[] { "Auto" })]
        public void TestSetPolicyWithAutoVariations(string description, object[] Params)
        {
            SqlConnection.ClearAllPools();
            string policyString = Params[0] as string;

            string connString = CreateConnectionString(_sampleAzureEndpoint, null) + $";{_policyKeyword}={policyString}";
            PoolBlockingPeriodAzureTest(connString, PoolBlockingPeriod.Auto);
        }

        [Theory]
        [InlineData("Test policy with Always (lowercase)", new object[] { "alwaysblock" })]
        [InlineData("Test policy with Always (miXedcase)", new object[] { "aLwAysBlock" })]
        [InlineData("Test policy with Always (Pascalcase)", new object[] { "AlwaysBlock" })]
        public void TestSetPolicyWithAlwaysVariations(string description, object[] Params)
        {
            SqlConnection.ClearAllPools();
            string policyString = Params[0] as string;

            string connString = CreateConnectionString(_sampleAzureEndpoint, null) + $";{_policyKeyword}={policyString}";
            PoolBlockingPeriodAzureTest(connString, PoolBlockingPeriod.AlwaysBlock);
        }

        [Theory]
        [InlineData("Test policy with Never (lowercase)", new object[] { "neverblock" })]
        [InlineData("Test policy with Never (miXedcase)", new object[] { "neVeRblock" })]
        [InlineData("Test policy with Never (Pascalcase)", new object[] { "NeverBlock" })]
        public void TestSetPolicyWithNeverVariations(string description, object[] Params)
        {
            SqlConnection.ClearAllPools();
            string policyString = Params[0] as string;

            string connString = CreateConnectionString(_sampleNonAzureEndpoint, null) + $";{_policyKeyword}={policyString}";
            PoolBlockingPeriodNonAzureTest(connString, PoolBlockingPeriod.NeverBlock);
        }

        public void PoolBlockingPeriodNonAzureTest(string connStr, PoolBlockingPeriod? policy)
        {
            SqlConnection.ClearAllPools();
            Guid previousConnectionId = Guid.Empty;
            int count = 0;

            while (count < 2)
            {
                using (SqlConnection sqlConnection = new SqlConnection(connStr))
                {
                    try
                    {
                        sqlConnection.Open();
                        throw new Exception("Connection Open must expect an exception!");
                    }
                    catch (SqlException e)
                    {
                        // if it is the first time the exception is happening (previousConnectionId == Guid.Empty) skip the check
                        if (previousConnectionId != Guid.Empty)
                        {
                            switch (policy)
                            {
                                case PoolBlockingPeriod.Auto:
                                case PoolBlockingPeriod.AlwaysBlock:
                                    if (e.ClientConnectionId != previousConnectionId)
                                    {
                                        throw new Exception($"Connection Open with Policy '{policy}' expect an exception with same connection id!");
                                    }
                                    break;
                                case PoolBlockingPeriod.NeverBlock:
                                    if (e.ClientConnectionId == previousConnectionId)
                                    {
                                        throw new Exception($"Connection Open with Policy '{policy}' expect an exception with different connection id!");
                                    }
                                    break;
                            }
                        }
                        previousConnectionId = e.ClientConnectionId;
                    }
                }
                count++;
            }
        }

        public void PoolBlockingPeriodAzureTest(string connStr, PoolBlockingPeriod? policy)
        {
            SqlConnection.ClearAllPools();
            Guid previousConnectionId = Guid.Empty;
            int count = 0;

            while (count < 2)
            {
                using (SqlConnection sqlConnection = new SqlConnection(connStr))
                {
                    try
                    {
                        sqlConnection.Open();
                        throw new Exception("Connection Open must expect an exception!");
                    }
                    catch (SqlException e)
                    {
                        // if it is the first time the exception is happening (previousConnectionId == Guid.Empty) skip the check
                        if (previousConnectionId != Guid.Empty)
                        {
                            switch (policy)
                            {
                                case PoolBlockingPeriod.AlwaysBlock:
                                    if (e.ClientConnectionId != previousConnectionId)
                                    {
                                        throw new Exception($"Connection Open with Policy '{policy}' expect an exception with same connection id!");
                                    }
                                    break;
                                case PoolBlockingPeriod.Auto:
                                case PoolBlockingPeriod.NeverBlock:
                                    if (e.ClientConnectionId == previousConnectionId)
                                    {
                                        throw new Exception($"Connection Open with Policy '{policy}' expect an exception with different connection id!");
                                    }
                                    break;
                            }
                        }
                        previousConnectionId = e.ClientConnectionId;
                    }
                }
                count++;
            }
        }

        public string CreateConnectionString(string serverName, PoolBlockingPeriod? policy)
        {
            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder();
            connBuilder.DataSource = serverName;
            connBuilder.UserID = "user";
            connBuilder.Password = "password";
            connBuilder.InitialCatalog = "test";
            connBuilder.PersistSecurityInfo = true;
            if (policy != null)
            {
                connBuilder.PoolBlockingPeriod = policy.Value;
            }
            return connBuilder.ToString();
        }
    }
}

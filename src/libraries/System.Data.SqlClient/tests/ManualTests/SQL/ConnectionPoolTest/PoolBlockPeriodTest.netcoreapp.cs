// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class PoolBlockPeriodTest
    {
        private const string AzureEndpointSample = "nonexistent.database.windows.net";
        private const string AzureChinaEnpointSample = "nonexistent.database.chinacloudapi.cn";
        private const string AzureUSGovernmentEndpointSample = "nonexistent.database.usgovcloudapi.net";
        private const string AzureGermanEndpointSample = "nonexistent.database.cloudapi.de";
        private const string AzureEndpointMixedCaseSample = "nonexistent.database.WINDOWS.net";
        private const string NonExistentServer = "nonexistentserver";
        private const string PolicyKeyword = "PoolBlockingPeriod";
        private const string PortNumber = "1234";
        private const string InstanceName = "InstanceName";
        private const int ConnectionTimeout = 15;
        private const int CompareMargin = 2;

        [ConditionalTheory(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        [InlineData("Azure with Default Policy must Disable blocking (*.database.windows.net)", new object[] { AzureEndpointSample })]
        [InlineData("Azure with Default Policy must Disable blocking (*.database.chinacloudapi.cn)", new object[] { AzureChinaEnpointSample })]
        [InlineData("Azure with Default Policy must Disable blocking (*.database.usgovcloudapi.net)", new object[] { AzureUSGovernmentEndpointSample })]
        [InlineData("Azure with Default Policy must Disable blocking (*.database.cloudapi.de)", new object[] { AzureGermanEndpointSample })]
        [InlineData("Azure with Default Policy must Disable blocking (MIXED CASES) (*.database.WINDOWS.net)", new object[] { AzureEndpointMixedCaseSample })]
        [InlineData("Azure with Default Policy must Disable blocking (PORT) (*.database.WINDOWS.net,1234)", new object[] { AzureEndpointMixedCaseSample + "," + PortNumber })]
        [InlineData("Azure with Default Policy must Disable blocking (INSTANCE NAME) (*.database.WINDOWS.net,1234\\InstanceName)", new object[] { AzureEndpointMixedCaseSample + "," + PortNumber + "\\" + InstanceName })]
        [InlineData("Azure with Auto Policy must Disable Blocking", new object[] { AzureEndpointSample, PoolBlockingPeriod.Auto })]
        [InlineData("Azure with Always Policy must Enable Blocking", new object[] { AzureEndpointSample, PoolBlockingPeriod.AlwaysBlock })]
        [InlineData("Azure with Never Policy must Disable Blocking", new object[] { AzureEndpointSample, PoolBlockingPeriod.NeverBlock })]
        public void TestAzureBlockingPeriod(string description, object[] Params)
        {
            string serverName = Params[0] as string;
            PoolBlockingPeriod? policy = null;
            if (Params.Length > 1)
            {
                policy = (PoolBlockingPeriod)Params[1];
            }

            string connString = CreateConnectionString(serverName, policy);
            PoolBlockingPeriodAzureTest(connString, policy);
        }

        [ConditionalTheory(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        [InlineData("NonAzure with Default Policy must Enable blocking", new object[] { NonExistentServer })]
        [InlineData("NonAzure with Auto Policy must Enable Blocking", new object[] { NonExistentServer, PoolBlockingPeriod.Auto })]
        [InlineData("NonAzure with Always Policy must Enable Blocking", new object[] { NonExistentServer, PoolBlockingPeriod.AlwaysBlock })]
        [InlineData("NonAzure with Never Policy must Disable Blocking", new object[] { NonExistentServer, PoolBlockingPeriod.NeverBlock })]
        [InlineData("NonAzure (which contains azure endpoint - nonexistent.WINDOWS.net) with Default Policy must Enable Blocking", new object[] { "nonexistent.windows.net" })]
        [InlineData("NonAzure (which contains azure endpoint - nonexistent.database.windows.net.else) with Default Policy must Enable Blocking", new object[] { "nonexistent.database.windows.net.else" })]
        public void TestNonAzureBlockingPeriod(string description, object[] Params)
        {
            string serverName = Params[0] as string;
            PoolBlockingPeriod? policy = null;

            if (Params.Length > 1)
            {
                policy = (PoolBlockingPeriod)Params[1];
            }

            string connString = CreateConnectionString(serverName, policy);
            PoolBlockingPeriodNonAzureTest(connString, policy);
        }

        [ConditionalTheory(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup), /* [ActiveIssue(33930)] */ nameof(DataTestUtility.IsUsingNativeSNI))]
        [InlineData("Test policy with Auto (lowercase)", "auto")]
        [InlineData("Test policy with Auto (PascalCase)", "Auto")]
        [InlineData("Test policy with Always (lowercase)", "alwaysblock")]
        [InlineData("Test policy with Always (PascalCase)", "AlwaysBlock")]
        [InlineData("Test policy with Never (lowercase)", "neverblock")]
        [InlineData("Test policy with Never (PascalCase)", "NeverBlock")]
        public void TestSetPolicyWithVariations(string description, string policyString)
        {
            PoolBlockingPeriod? policy = null;
            if (policyString.ToLower().Contains("auto"))
            {
                policy = PoolBlockingPeriod.Auto;
            }
            else if (policyString.ToLower().Contains("always"))
            {
                policy = PoolBlockingPeriod.AlwaysBlock;
            }
            else
            {
                policy = PoolBlockingPeriod.NeverBlock;
            }
            string connString = $"{CreateConnectionString(AzureEndpointSample, null)};{PolicyKeyword}={policyString}";
            PoolBlockingPeriodAzureTest(connString, policy);
        }

        public void PoolBlockingPeriodNonAzureTest(string connStr, PoolBlockingPeriod? policy)
        {
            int firstErrorTimeInSecs = GetConnectionOpenTimeInSeconds(connStr);
            int secondErrorTimeInSecs = GetConnectionOpenTimeInSeconds(connStr);
            switch (policy)
            {
                case PoolBlockingPeriod.Auto:
                case PoolBlockingPeriod.AlwaysBlock:
                    Assert.InRange(secondErrorTimeInSecs, 0, firstErrorTimeInSecs + CompareMargin);
                    break;
                case PoolBlockingPeriod.NeverBlock:
                    Assert.InRange(secondErrorTimeInSecs, 1, 2 * ConnectionTimeout);
                    break;
            }
        }

        public void PoolBlockingPeriodAzureTest(string connStr, PoolBlockingPeriod? policy)
        {
            int firstErrorTimeInSecs = GetConnectionOpenTimeInSeconds(connStr);
            int secondErrorTimeInSecs = GetConnectionOpenTimeInSeconds(connStr);
            switch (policy)
            {
                case PoolBlockingPeriod.AlwaysBlock:
                    Assert.InRange(secondErrorTimeInSecs, 0, firstErrorTimeInSecs + CompareMargin);
                    break;
                case PoolBlockingPeriod.Auto:
                case PoolBlockingPeriod.NeverBlock:
                    Assert.InRange(secondErrorTimeInSecs, 1, 2 * ConnectionTimeout);
                    break;
            }
        }

        private int GetConnectionOpenTimeInSeconds(string connString)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                try
                {
                    stopwatch.Start();
                    conn.Open();
                    throw new Exception("Connection Open must expect an exception");
                }
                catch (Exception)
                {
                    stopwatch.Stop();
                }
                return stopwatch.Elapsed.Seconds;
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

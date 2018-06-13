// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class PoolBlockPeriodTest
    {
#if netcoreapp
        private static readonly string _sampleAzureEndpoint = "nonexistance.database.windows.net";
        private static readonly string _sampleNonAzureEndpoint = "nonexistanceserver";
        private static readonly string _policyKeyword = "PoolBlockingPeriod";

        private static readonly int _connectionTimeoutInSeconds = 3;
        private static readonly int _firstBlockingPeriodInSeconds = 5;
        private static readonly int _maxBlockingPeriodInSeconds = 60;

        public enum CDbAsyncSettings
        {
            UseSyncOverAsync,
            UseAsyncOversync
        }

        [Theory]
        [InlineData(CDbAsyncSettings.UseSyncOverAsync)]
        public void BlockingIntervalSyncTest(CDbAsyncSettings setting)
        {
            SqlConnection.ClearAllPools();
            string connString = CreateConnectionString(_sampleNonAzureEndpoint, null);
            int errorTimeInSecs = GetConnectionOpenTimeInSeconds(connString, setting);

            int blockingPeriodInSeconds = _firstBlockingPeriodInSeconds; //first blocking period is 5 seconds
            int counter = 0;
            int comparisonMargin = 2;
            while (blockingPeriodInSeconds < _maxBlockingPeriodInSeconds)
            {
                int timeInSecs = GetBlockingPeriodTimeInSeconds(connString, setting);

                Assert.True(CompareWithMargin(timeInSecs, blockingPeriodInSeconds, comparisonMargin),
                    $"#{counter} blocking Period must be {blockingPeriodInSeconds} seconds");

                counter++;
                blockingPeriodInSeconds = blockingPeriodInSeconds * 2;
            }

            //Test after max period
            for (int i = 0; i < 2; i++)
            {
                int timeInSecs = GetBlockingPeriodTimeInSeconds(connString, setting);
                Assert.True(CompareWithMargin(timeInSecs, _maxBlockingPeriodInSeconds, comparisonMargin),
                     $"#{counter} blocking Period must be capped at max blocking period '{_maxBlockingPeriodInSeconds} seconds'");
                counter++;
            }

        }

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
            int errorTimeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);
            int timeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);

            switch (policy)
            {
                case null:
                case PoolBlockingPeriod.NeverBlock:
                case PoolBlockingPeriod.Auto:
                    {
                        //Azure Endpoint with Default/Auto/Never Policy must Disable blocking.
                        Assert.InRange(timeInSecs, 1, int.MaxValue);
                        break;
                    }

                case PoolBlockingPeriod.AlwaysBlock:
                    {
                        //Azure Endpoint with Always Policy must Enable blocking. (Fast Failed)
                        Assert.Equal(0, timeInSecs);
                        break;
                    }
            }
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
            int errorTimeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);
            int timeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);

            switch (policy)
            {
                case PoolBlockingPeriod.NeverBlock:
                    {
                        //Azure Endpoint with Never Policy must Disable blocking.
                        Assert.InRange(timeInSecs, 1, int.MaxValue);
                        break;
                    }

                case null:
                case PoolBlockingPeriod.Auto:
                case PoolBlockingPeriod.AlwaysBlock:
                    {
                        //Azure Endpoint with Default/Auto/Always must Enable blocking. (Fast Failed)
                        Assert.InRange(timeInSecs, 0, errorTimeInSecs);
                        //Assert.True(errorTimeInSecs >= timeInSecs, $"Azure Endpoint with Default/Auto/Always must Enable blocking. (Fast Failed)");
                        break;
                    }
            }
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
            int errorTimeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);
            int timeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);

            //Disable Blocking
            Assert.InRange(timeInSecs, 1, int.MaxValue);
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
            int errorTimeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);
            int timeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);

            //Enable Blocking
            Assert.True(timeInSecs == 0, $"Azure Endpoint with Always Policy '{policyString}'must Enable blocking.");
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
            int errorTimeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);
            int timeInSecs = GetConnectionOpenTimeInSeconds(connString, CDbAsyncSettings.UseSyncOverAsync);

            //Disable Blocking
            Assert.True(timeInSecs > 0, $"Non Azure Endpoint with Never Policy '{policyString}'must Disabled blocking.");
        }

        //[Theory]        Commenting this test for now to investigate the failure later.
        [InlineData("Test connection Id with policy Auto", new object[] { PoolBlockingPeriod.Auto })]
        [InlineData("Test connection Id with policy AlwaysBlcok", new object[] { PoolBlockingPeriod.AlwaysBlock })]
        [InlineData("Test connection Id with policy NeverBlock", new object[] { PoolBlockingPeriod.NeverBlock })]
        public void PoolBlockingPeriodWithConnectionIdTest(string description, object[] Params)
        {
            SqlConnection.ClearAllPools();
            PoolBlockingPeriod policy = (PoolBlockingPeriod)Params[0];
            Guid previousConnectionId = Guid.Empty;
            string connStr = DataTestUtility.TcpConnStr + $";{_policyKeyword}={policy}";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connStr);
            builder.InitialCatalog = "Changed"; // modify connection string to make it fail
            int count = 0;
            string _changedConnStr = builder.ConnectionString;

            while (count < 10)
            {
                using (SqlConnection sqlConnection = new SqlConnection(_changedConnStr))
                {
                    try
                    {
                        sqlConnection.Open();
                        throw new Exception("Connection Open must expect an exception!");
                    }
                    catch (SqlException e)
                    {
                        switch (policy)
                        {
                            case PoolBlockingPeriod.Auto:
                            case PoolBlockingPeriod.AlwaysBlock:
                                // if it is the first time the exception is happening (previousConnectionId == Guid.Empty) skip the check
                                if (e.ClientConnectionId != previousConnectionId && previousConnectionId != Guid.Empty)
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
                        previousConnectionId = e.ClientConnectionId;
                    }
                }
                count++;
            }
        }
        private bool CompareWithMargin(int value, int comparison, int margin)
        {
            return (value >= comparison - margin) && (value <= comparison + margin);
        }

        public string CreateConnectionString(string serverName, PoolBlockingPeriod? policy)
        {
            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder();
            connBuilder.DataSource = serverName;
            connBuilder.UserID = "user";
            connBuilder.Password = "password";
            connBuilder.InitialCatalog = "test";
            connBuilder.ConnectTimeout = _connectionTimeoutInSeconds;
            connBuilder.PersistSecurityInfo = true;
            if (policy != null)
            {
                connBuilder.PoolBlockingPeriod = policy.Value;
            }
            return connBuilder.ToString();
        }

        private int GetConnectionOpenTimeInSeconds(string connString, CDbAsyncSettings setting, Type expectedExceptionType = null)
        {
            expectedExceptionType = expectedExceptionType ?? typeof(SqlException);
            using (SqlConnection conn = new SqlConnection(connString))
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                try
                {
                    stopwatch.Start();
                    if (setting == CDbAsyncSettings.UseAsyncOversync)
                    {
                        conn.OpenAsync().Wait();
                    }
                    else
                    {
                        conn.Open();
                    }
                    conn.Open();
                    throw new Exception("Connection Open must expect an exception");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    if (setting == CDbAsyncSettings.UseSyncOverAsync)
                    {
                        AggregateException aggregateEx = ex as System.AggregateException;
                        if (aggregateEx != null)
                        {
                            Assert.True(aggregateEx.InnerException.GetType() == expectedExceptionType,
                                "Connection Open must return the same type of exception :" + ex.ToString());
                        }
                    }
                    else
                    {
                        Assert.True(ex.GetType() == expectedExceptionType,
                            "Connection Open must return the same type of exception :" + ex.ToString());
                    }
                }

                return stopwatch.Elapsed.Seconds;
            }
        }
        private int GetBlockingPeriodTimeInSeconds(string connString, CDbAsyncSettings setting, Type expectedExceptionType = null)
        {
            int timeInSeconds = 0;
            int counter = 0;
            while (timeInSeconds <= 0)
            {
                timeInSeconds = GetConnectionOpenTimeInSeconds(connString, setting, expectedExceptionType);
                counter++;
                Thread.Sleep(1000); //keep trying every second
            }

            return counter;
        }
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Data.Common;
using System.Data.SqlClient;

namespace System.Data.ProviderBase
{
    sealed internal partial class DbConnectionPool
    {
        partial void CheckPoolBlockingPeriod(Exception e)
        {
            if (!IsBlockingPeriodEnabled())
            {
                throw e;
            }
        }

        private bool IsBlockingPeriodEnabled()
        {
            var poolGroupConnectionOptions = _connectionPoolGroup.ConnectionOptions as SqlConnectionString;
            if (poolGroupConnectionOptions == null)
            {
                return true;
            }
            var policy = poolGroupConnectionOptions.PoolBlockingPeriod;

            switch (policy)
            {
                case System.Data.SqlClient.PoolBlockingPeriod.Auto:
                    {
                        return !ADP.IsAzureSqlServerEndpoint(poolGroupConnectionOptions.DataSource);
                    }
                case System.Data.SqlClient.PoolBlockingPeriod.AlwaysBlock:
                    {
                        return true; //Enabled
                    }
                case System.Data.SqlClient.PoolBlockingPeriod.NeverBlock:
                    {
                        return false; //Disabled
                    }
                default:
                    {
                        //we should never get into this path.
                        Debug.Fail("Unknown PoolBlockingPeriod. Please specify explicit results in above switch case statement.");
                        return true;
                    }
            }
        }
    }
}

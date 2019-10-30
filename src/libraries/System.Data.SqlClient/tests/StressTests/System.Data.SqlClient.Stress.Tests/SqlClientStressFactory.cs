// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Data.SqlClient;
using Microsoft.Test.Data.SqlClient;

namespace Stress.Data.SqlClient
{
    public class SqlClientStressFactory : DataStressFactory
    {
        // scenarios
        internal enum SqlClientScenario
        {
            Sql
        }

        private SqlServerDataSource _source;
        private SqlClientScenario _scenario;

        private MultiSubnetFailoverSetup _multiSubnetSetupHelper;

        internal SqlClientStressFactory()
            : base(SqlClientFactory.Instance)
        {
        }

        internal void Initialize(ref string scenario, ref DataSource source)
        {
            // Ignore all asserts from known issues
            var defaultTraceListener = Trace.Listeners["Default"] as DefaultTraceListener;
            if (defaultTraceListener != null)
            {
                var newTraceListener = new FilteredDefaultTraceListener(defaultTraceListener);
                Trace.Listeners.Remove(defaultTraceListener);
                Trace.Listeners.Add(newTraceListener);
            }

            // scenario <=> SqlClientScenario
            if (string.IsNullOrEmpty(scenario))
            {
                _scenario = SqlClientScenario.Sql;
            }
            else
            {
                _scenario = (SqlClientScenario)Enum.Parse(typeof(SqlClientScenario), scenario, true);
            }
            scenario = _scenario.ToString();

            // initialize the source information
            // SNAC/WDAC is using SqlServer sources; JET is using Access
            switch (_scenario)
            {
                case SqlClientScenario.Sql:
                    if (source == null)
                        source = DataStressSettings.Instance.GetDefaultSourceByType(DataSourceType.SqlServer);
                    else if (source.Type != DataSourceType.SqlServer)
                        throw new ArgumentException(string.Format("Given source type is wrong: required {0}, received {1}", DataSourceType.SqlServer, source.Type));
                    break;

                default:
                    throw new ArgumentException("Wrong scenario \"" + scenario + "\"");
            }

            _source = (SqlServerDataSource)source;

            // Only try to add Multisubnet Failover host entries when the settings allow it in the source.
            if (!_source.DisableMultiSubnetFailoverSetup)
            {
                _multiSubnetSetupHelper = new MultiSubnetFailoverSetup(_source);
                _multiSubnetSetupHelper.InitializeFakeHostsForMultiSubnetFailover();
            }
        }



        internal void Terminate()
        {
            if (_multiSubnetSetupHelper != null)
            {
                _multiSubnetSetupHelper.Terminate();
            }
        }

        public sealed override string GetParameterName(string pName)
        {
            return "@" + pName;
        }

        public override bool PrimaryKeyValueIsRequired
        {
            get { return false; }
        }


        public override string CreateBaseConnectionString(Random rnd, ConnectionStringOptions options)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            switch (_scenario)
            {
                case SqlClientScenario.Sql:
                    builder.DataSource = _source.DataSource;
                    builder.InitialCatalog = _source.Database;
                    break;

                default:
                    throw new InvalidOperationException("missing case for " + _scenario);
            }

            // Randomize between Windows Authentication and SQL Authentication
            // Note that having 2 options here doubles the number of connection pools
            bool integratedSecurity = false;
            if (_source.SupportsWindowsAuthentication)
            {
                if (string.IsNullOrEmpty(_source.User)) // if sql login is not provided
                    integratedSecurity = true;
                else
                    integratedSecurity = (rnd != null) ? (rnd.Next(2) == 0) : true;
            }

            if (integratedSecurity)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = _source.User;
                builder.Password = _source.Password;
            }

            if (CurrentPoolingStressMode == PoolingStressMode.RandomizeConnectionStrings && rnd != null)
            {
                // Randomize connection string

                // Randomize packetsize
                // Note that having 2 options here doubles the number of connection pools
                if (rnd.NextBool())
                {
                    builder.PacketSize = 8192;
                }
                else
                {
                    builder.PacketSize = 512;
                }

                // If test case allows randomization and doesn't disallow MultiSubnetFailover, then enable MultiSubnetFailover 20% of the time
                // Note that having 2 options here doubles the number of connection pools

                if (!_source.DisableMultiSubnetFailoverSetup &&
                    !options.HasFlag(ConnectionStringOptions.DisableMultiSubnetFailover) &&
                    rnd != null &&
                    rnd.Next(5) == 0)
                {
                    string msfHostName;
                    if (integratedSecurity)
                    {
                        msfHostName = _multiSubnetSetupHelper.MultiSubnetFailoverHostNameForIntegratedSecurity;
                    }
                    else
                    {
                        msfHostName = _multiSubnetSetupHelper.GetMultiSubnetFailoverHostName(rnd);
                    }
                    string serverName;

                    // replace with build which has host name with multiple IP addresses
                    builder = NetUtils.GetMultiSubnetFailoverConnectionString(builder.ConnectionString, msfHostName, out serverName);
                }

                // Randomize between using Named Pipes and TCP providers
                // Note that having 2 options here doubles the number of connection pools
                if (rnd != null)
                {
                    if (rnd.Next(2) == 0)
                    {
                        builder.DataSource = "tcp:" + builder.DataSource;
                    }
                    else if (!_source.DisableNamedPipes)
                    {
                        // Named Pipes
                        if (builder.DataSource.Equals("(local)"))
                            builder.DataSource = "np:" + builder.DataSource;
                        else
                            builder.DataSource = @"np:\\" + builder.DataSource.Split(',')[0] + @"\pipe\sql\query";
                    }
                }

                // Set MARS if it is requested by the test case
                if (options.HasFlag(ConnectionStringOptions.EnableMars))
                {
                    builder.MultipleActiveResultSets = true;
                }

                // Disable connection resiliency, which is on by default, 20% of the time.
                if (rnd != null && rnd.NextBool(.2))
                {
                    builder.ConnectRetryCount = 0;
                }
            }
            else
            {
                // Minimal randomization of connection string

                // Enable MARS for all scenarios
                builder.MultipleActiveResultSets = true;
            }

            builder.MaxPoolSize = 1000;

            return builder.ToString();
        }

        protected override int GetNumDifferentApplicationNames()
        {
            // Return only 1 because the randomization in the base connection string above will give us more pools, so we don't need
            // to also have many different application names. Getting connections from many different pools is not interesting to test
            // because it reduces the amount of multithreadedness within each pool.
            return 1;
        }
    }
}

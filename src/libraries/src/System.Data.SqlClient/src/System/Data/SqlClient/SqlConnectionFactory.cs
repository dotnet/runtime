// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Specialized;
using System.Configuration;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.IO;

namespace System.Data.SqlClient
{
    sealed internal class SqlConnectionFactory : DbConnectionFactory
    {

        private const string _metaDataXml = "MetaDataXml";

        private SqlConnectionFactory() : base() { }

        public static readonly SqlConnectionFactory SingletonInstance = new SqlConnectionFactory();

        override public DbProviderFactory ProviderFactory
        {
            get
            {
                return SqlClientFactory.Instance;
            }
        }

        override protected DbConnectionInternal CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningConnection)
        {
            return CreateConnection(options, poolKey, poolGroupProviderInfo, pool, owningConnection, userOptions: null);
        }

        override protected DbConnectionInternal CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningConnection, DbConnectionOptions userOptions)
        {
            SqlConnectionString opt = (SqlConnectionString)options;
            SqlConnectionPoolKey key = (SqlConnectionPoolKey)poolKey;
            SqlInternalConnection result = null;
            SessionData recoverySessionData = null;

            SqlConnection sqlOwningConnection = (SqlConnection)owningConnection;
            bool applyTransientFaultHandling = sqlOwningConnection != null ? sqlOwningConnection._applyTransientFaultHandling : false;

            SqlConnectionString userOpt = null;
            if (userOptions != null)
            {
                userOpt = (SqlConnectionString)userOptions;
            }
            else if (sqlOwningConnection != null)
            {
                userOpt = (SqlConnectionString)(sqlOwningConnection.UserConnectionOptions);
            }

            if (sqlOwningConnection != null)
            {
                recoverySessionData = sqlOwningConnection._recoverySessionData;
            }

            bool redirectedUserInstance = false;
            DbConnectionPoolIdentity identity = null;

            // Pass DbConnectionPoolIdentity to SqlInternalConnectionTds if using integrated security.
            // Used by notifications.
            if (opt.IntegratedSecurity)
            {
                if (pool != null)
                {
                    identity = pool.Identity;
                }
                else
                {
                    identity = DbConnectionPoolIdentity.GetCurrent();
                }
            }

            // FOLLOWING IF BLOCK IS ENTIRELY FOR SSE USER INSTANCES
            // If "user instance=true" is in the connection string, we're using SSE user instances
            if (opt.UserInstance)
            {
                // opt.DataSource is used to create the SSE connection
                redirectedUserInstance = true;
                string instanceName;

                if ((null == pool) ||
                     (null != pool && pool.Count <= 0))
                { // Non-pooled or pooled and no connections in the pool.
                    SqlInternalConnectionTds sseConnection = null;
                    try
                    {
                        // We throw an exception in case of a failure
                        // NOTE: Cloning connection option opt to set 'UserInstance=True' and 'Enlist=False'
                        //       This first connection is established to SqlExpress to get the instance name 
                        //       of the UserInstance.
                        SqlConnectionString sseopt = new SqlConnectionString(opt, opt.DataSource, userInstance: true, setEnlistValue: false);
                        sseConnection = new SqlInternalConnectionTds(identity, sseopt, key.Credential, null, "", null, false, applyTransientFaultHandling: applyTransientFaultHandling);
                        // NOTE: Retrieve <UserInstanceName> here. This user instance name will be used below to connect to the Sql Express User Instance.
                        instanceName = sseConnection.InstanceName;

                        if (!instanceName.StartsWith("\\\\.\\", StringComparison.Ordinal))
                        {
                            throw SQL.NonLocalSSEInstance();
                        }

                        if (null != pool)
                        { // Pooled connection - cache result
                            SqlConnectionPoolProviderInfo providerInfo = (SqlConnectionPoolProviderInfo)pool.ProviderInfo;
                            // No lock since we are already in creation mutex
                            providerInfo.InstanceName = instanceName;
                        }
                    }
                    finally
                    {
                        if (null != sseConnection)
                        {
                            sseConnection.Dispose();
                        }
                    }
                }
                else
                { // Cached info from pool.
                    SqlConnectionPoolProviderInfo providerInfo = (SqlConnectionPoolProviderInfo)pool.ProviderInfo;
                    // No lock since we are already in creation mutex
                    instanceName = providerInfo.InstanceName;
                }

                // NOTE: Here connection option opt is cloned to set 'instanceName=<UserInstanceName>' that was
                //       retrieved from the previous SSE connection. For this UserInstance connection 'Enlist=True'.
                // options immutable - stored in global hash - don't modify
                opt = new SqlConnectionString(opt, instanceName, userInstance: false, setEnlistValue: null);
                poolGroupProviderInfo = null; // null so we do not pass to constructor below...
            }
            result = new SqlInternalConnectionTds(identity, opt, key.Credential, poolGroupProviderInfo, "", null, redirectedUserInstance, userOpt, recoverySessionData, applyTransientFaultHandling: applyTransientFaultHandling, key.AccessToken);
            return result;
        }

        protected override DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous)
        {
            Debug.Assert(!string.IsNullOrEmpty(connectionString), "empty connectionString");
            SqlConnectionString result = new SqlConnectionString(connectionString);
            return result;
        }

        override internal DbConnectionPoolProviderInfo CreateConnectionPoolProviderInfo(DbConnectionOptions connectionOptions)
        {
            DbConnectionPoolProviderInfo providerInfo = null;

            if (((SqlConnectionString)connectionOptions).UserInstance)
            {
                providerInfo = new SqlConnectionPoolProviderInfo();
            }

            return providerInfo;
        }

        override protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions(DbConnectionOptions connectionOptions)
        {
            SqlConnectionString opt = (SqlConnectionString)connectionOptions;

            DbConnectionPoolGroupOptions poolingOptions = null;

            if (opt.Pooling)
            {    // never pool context connections.
                int connectionTimeout = opt.ConnectTimeout;

                if ((0 < connectionTimeout) && (connectionTimeout < int.MaxValue / 1000))
                    connectionTimeout *= 1000;
                else if (connectionTimeout >= int.MaxValue / 1000)
                    connectionTimeout = int.MaxValue;

                poolingOptions = new DbConnectionPoolGroupOptions(
                                                    opt.IntegratedSecurity,
                                                    opt.MinPoolSize,
                                                    opt.MaxPoolSize,
                                                    connectionTimeout,
                                                    opt.LoadBalanceTimeout,
                                                    opt.Enlist);
            }
            return poolingOptions;
        }


        override internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo(DbConnectionOptions connectionOptions)
        {
            return new SqlConnectionPoolGroupProviderInfo((SqlConnectionString)connectionOptions);
        }


        internal static SqlConnectionString FindSqlConnectionOptions(SqlConnectionPoolKey key)
        {
            SqlConnectionString connectionOptions = (SqlConnectionString)SingletonInstance.FindConnectionOptions(key);
            if (null == connectionOptions)
            {
                connectionOptions = new SqlConnectionString(key.ConnectionString);
            }
            if (connectionOptions.IsEmpty)
            {
                throw ADP.NoConnectionString();
            }
            return connectionOptions;
        }


        override internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection)
        {
            SqlConnection c = (connection as SqlConnection);
            if (null != c)
            {
                return c.PoolGroup;
            }
            return null;
        }

        override internal DbConnectionInternal GetInnerConnection(DbConnection connection)
        {
            SqlConnection c = (connection as SqlConnection);
            if (null != c)
            {
                return c.InnerConnection;
            }
            return null;
        }


        override internal void PermissionDemand(DbConnection outerConnection)
        {
            SqlConnection c = (outerConnection as SqlConnection);
            if (null != c)
            {
                c.PermissionDemand();
            }
        }

        override internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup)
        {
            SqlConnection c = (outerConnection as SqlConnection);
            if (null != c)
            {
                c.PoolGroup = poolGroup;
            }
        }

        override internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to)
        {
            SqlConnection c = (owningObject as SqlConnection);
            if (null != c)
            {
                c.SetInnerConnectionEvent(to);
            }
        }

        override internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from)
        {
            SqlConnection c = (owningObject as SqlConnection);
            if (null != c)
            {
                return c.SetInnerConnectionFrom(to, from);
            }
            return false;
        }

        override internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to)
        {
            SqlConnection c = (owningObject as SqlConnection);
            if (null != c)
            {
                c.SetInnerConnectionTo(to);
            }
        }

        protected override DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory)
        {
            Debug.Assert(internalConnection != null, "internalConnection may not be null.");
            
            Stream xmlStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("System.Data.SqlClient.SqlMetaData.xml");
            cacheMetaDataFactory = true;
            
            Debug.Assert(xmlStream != null, nameof(xmlStream) + " may not be null.");

            return new SqlMetaDataFactory(xmlStream,
                                          internalConnection.ServerVersion,
                                          internalConnection.ServerVersion);
        }
    }
}


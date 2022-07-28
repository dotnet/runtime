// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Data.Odbc
{
    internal sealed class OdbcConnectionFactory : DbConnectionFactory
    {
        private OdbcConnectionFactory() : base() { }
        // At this time, the ODBC Provider doesn't have any connection pool counters
        // because we'd only confuse people with "non-pooled" connections that are
        // actually being pooled by the native pooler.

        public static readonly OdbcConnectionFactory SingletonInstance = new OdbcConnectionFactory();

        public override DbProviderFactory ProviderFactory
        {
            get
            {
                return OdbcFactory.Instance;
            }
        }

        protected override DbConnectionInternal CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool? pool, DbConnection? owningObject)
        {
            // TODO: owningObject may actually be null (see DbConnectionPool.CreateObject), in which case this will throw...
            DbConnectionInternal result = new OdbcConnectionOpen((owningObject as OdbcConnection)!, (options as OdbcConnectionString)!);
            return result;
        }

        protected override DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions? previous)
        {
            Debug.Assert(!string.IsNullOrEmpty(connectionString), "empty connectionString");
            OdbcConnectionString result = new OdbcConnectionString(connectionString, (null != previous));
            return result;
        }

        protected override DbConnectionPoolGroupOptions? CreateConnectionPoolGroupOptions(DbConnectionOptions connectionOptions)
        {
            // At this time, the ODBC provider only supports native pooling so we
            // simply return NULL to indicate that.
            return null;
        }

        internal override DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo(DbConnectionOptions connectionOptions)
        {
            return new OdbcConnectionPoolGroupProviderInfo();
        }

        protected override DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection)
        {
            Debug.Assert(internalConnection != null, "internalConnection may not be null.");

            OdbcConnection odbcOuterConnection = ((OdbcConnectionOpen)internalConnection).OuterConnection;
            Debug.Assert(odbcOuterConnection != null, "outer connection may not be null.");

            // get the DBMS Name
            odbcOuterConnection.GetInfoStringUnhandled(ODBC32.SQL_INFO.DRIVER_NAME);

            Stream? XMLStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("System.Data.Odbc.OdbcMetaData.xml");

            Debug.Assert(XMLStream != null, "XMLstream may not be null.");

            string versionString = odbcOuterConnection.GetInfoStringUnhandled(ODBC32.SQL_INFO.DBMS_VER)!;

            return new OdbcMetaDataFactory(XMLStream,
                                            versionString,
                                            versionString,
                                            odbcOuterConnection);
        }

        internal override DbConnectionPoolGroup? GetConnectionPoolGroup(DbConnection? connection)
        {
            OdbcConnection? c = (connection as OdbcConnection);
            if (null != c)
            {
                return c.PoolGroup;
            }
            return null;
        }

        internal override DbConnectionInternal? GetInnerConnection(DbConnection connection)
        {
            OdbcConnection? c = (connection as OdbcConnection);
            if (null != c)
            {
                return c.InnerConnection;
            }
            return null;
        }

        internal override void PermissionDemand(DbConnection outerConnection)
        {
            (outerConnection as OdbcConnection)?.PermissionDemand();
        }

        internal override void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup)
        {
            OdbcConnection? c = (outerConnection as OdbcConnection);
            if (null != c)
            {
                c.PoolGroup = poolGroup;
            }
        }

        internal override void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to)
        {
            (owningObject as OdbcConnection)?.SetInnerConnectionEvent(to);
        }

        internal override bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from)
        {
            OdbcConnection? c = (owningObject as OdbcConnection);
            if (null != c)
            {
                return c.SetInnerConnectionFrom(to, from);
            }
            return false;
        }

        internal override void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to)
        {
            (owningObject as OdbcConnection)?.SetInnerConnectionTo(to);
        }
    }
}

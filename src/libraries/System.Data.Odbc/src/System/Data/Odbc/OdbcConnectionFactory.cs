// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace System.Data.Odbc
{
    internal sealed class OdbcConnectionFactory : DbConnectionFactory
    {
        private OdbcConnectionFactory() : base() { }
        // At this time, the ODBC Provider doesn't have any connection pool counters
        // because we'd only confuse people with "non-pooled" connections that are
        // actually being pooled by the native pooler.

        private const string _MetaData = ":MetaDataXml";
        private const string _defaultMetaDataXml = "defaultMetaDataXml";

        public static readonly OdbcConnectionFactory SingletonInstance = new OdbcConnectionFactory();

        override public DbProviderFactory ProviderFactory
        {
            get
            {
                return OdbcFactory.Instance;
            }
        }

        override protected DbConnectionInternal CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningObject)
        {
            DbConnectionInternal result = new OdbcConnectionOpen(owningObject as OdbcConnection, options as OdbcConnectionString);
            return result;
        }

        override protected DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous)
        {
            Debug.Assert(!ADP.IsEmpty(connectionString), "empty connectionString");
            OdbcConnectionString result = new OdbcConnectionString(connectionString, (null != previous));
            return result;
        }

        override protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions(DbConnectionOptions connectionOptions)
        {
            // At this time, the ODBC provider only supports native pooling so we
            // simply return NULL to indicate that.
            return null;
        }

        override internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo(DbConnectionOptions connectionOptions)
        {
            return new OdbcConnectionPoolGroupProviderInfo();
        }

        // SxS (VSDD 545786): metadata files are opened from <.NetRuntimeFolder>\CONFIG\<metadatafilename.xml>
        // this operation is safe in SxS because the file is opened in read-only mode and each NDP runtime accesses its own copy of the metadata
        // under the runtime folder.
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        override protected DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory)
        {
            Debug.Assert(internalConnection != null, "internalConnection may not be null.");
            cacheMetaDataFactory = false;

            OdbcConnection odbcOuterConnection = ((OdbcConnectionOpen)internalConnection).OuterConnection;
            Debug.Assert(odbcOuterConnection != null, "outer connection may not be null.");

            NameValueCollection settings = (NameValueCollection)PrivilegedConfigurationManager.GetSection("system.data.odbc");
            Stream XMLStream = null;

            // get the DBMS Name
            object driverName = null;
            string stringValue = odbcOuterConnection.GetInfoStringUnhandled(ODBC32.SQL_INFO.DRIVER_NAME);
            if (stringValue != null)
            {
                driverName = stringValue;
            }

            if (settings != null)
            {
                string[] values = null;
                string metaDataXML = null;
                // first try to get the provider specific xml

                // if driver name is not supported we can't build the settings key needed to
                // get the provider specific XML path
                if (driverName != null)
                {
                    metaDataXML = ((string)driverName) + _MetaData;
                    values = settings.GetValues(metaDataXML);
                }

                // if we did not find provider specific xml see if there is new default xml
                if (values == null)
                {
                    metaDataXML = _defaultMetaDataXml;
                    values = settings.GetValues(metaDataXML);
                }

                // If there is an XML file get it
                if (values != null)
                {
                    XMLStream = ADP.GetXmlStreamFromValues(values, metaDataXML);
                }
            }

            // use the embedded xml if the user did not over ride it
            if (XMLStream == null)
            {
                XMLStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("System.Data.Odbc.OdbcMetaData.xml");
                cacheMetaDataFactory = true;
            }

            Debug.Assert(XMLStream != null, "XMLstream may not be null.");

            String versionString = odbcOuterConnection.GetInfoStringUnhandled(ODBC32.SQL_INFO.DBMS_VER);

            return new OdbcMetaDataFactory(XMLStream,
                                            versionString,
                                            versionString,
                                            odbcOuterConnection);
        }

        override internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection)
        {
            OdbcConnection c = (connection as OdbcConnection);
            if (null != c)
            {
                return c.PoolGroup;
            }
            return null;
        }

        override internal DbConnectionInternal GetInnerConnection(DbConnection connection)
        {
            OdbcConnection c = (connection as OdbcConnection);
            if (null != c)
            {
                return c.InnerConnection;
            }
            return null;
        }

        override protected int GetObjectId(DbConnection connection)
        {
            OdbcConnection c = (connection as OdbcConnection);
            if (null != c)
            {
                return c.ObjectID;
            }
            return 0;
        }

        override internal void PermissionDemand(DbConnection outerConnection)
        {
            OdbcConnection c = (outerConnection as OdbcConnection);
            if (null != c)
            {
                c.PermissionDemand();
            }
        }

        override internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup)
        {
            OdbcConnection c = (outerConnection as OdbcConnection);
            if (null != c)
            {
                c.PoolGroup = poolGroup;
            }
        }

        override internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to)
        {
            OdbcConnection c = (owningObject as OdbcConnection);
            if (null != c)
            {
                c.SetInnerConnectionEvent(to);
            }
        }

        override internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from)
        {
            OdbcConnection c = (owningObject as OdbcConnection);
            if (null != c)
            {
                return c.SetInnerConnectionFrom(to, from);
            }
            return false;
        }

        override internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to)
        {
            OdbcConnection c = (owningObject as OdbcConnection);
            if (null != c)
            {
                c.SetInnerConnectionTo(to);
            }
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Data.Common;
using System.Configuration;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace System.Data.OleDb
{
    sealed internal class OleDbConnectionFactory : DbConnectionFactory
    {
        private OleDbConnectionFactory() : base() { }
        // At this time, the OleDb Managed Provider doesn't have any connection pool
        // counters because we'd only confuse people with "non-pooled" connections
        // that are actually being pooled by the native pooler.

        private const string _metaDataXml = ":MetaDataXml";
        private const string _defaultMetaDataXml = "defaultMetaDataXml";

        public static readonly OleDbConnectionFactory SingletonInstance = new OleDbConnectionFactory();

        override public DbProviderFactory ProviderFactory
        {
            get
            {
                return OleDbFactory.Instance;
            }
        }

        override protected DbConnectionInternal CreateConnection(DbConnectionOptions options, DbConnectionPoolKey poolKey, object poolGroupProviderInfo, DbConnectionPool pool, DbConnection owningObject)
        {
            DbConnectionInternal result = new OleDbConnectionInternal((OleDbConnectionString)options, (OleDbConnection)owningObject);
            return result;
        }

        protected override DbConnectionOptions CreateConnectionOptions(string connectionString, DbConnectionOptions previous)
        {
            Debug.Assert(!ADP.IsEmpty(connectionString), "null connectionString");
            OleDbConnectionString result = new OleDbConnectionString(connectionString, (null != previous));
            return result;
        }

        override protected DbMetaDataFactory CreateMetaDataFactory(DbConnectionInternal internalConnection, out bool cacheMetaDataFactory)
        {
            Debug.Assert(internalConnection != null, "internalConnection may not be null.");
            cacheMetaDataFactory = false;

            OleDbConnectionInternal oleDbInternalConnection = (OleDbConnectionInternal)internalConnection;
            OleDbConnection oleDbOuterConnection = oleDbInternalConnection.Connection;
            Debug.Assert(oleDbOuterConnection != null, "outer connection may not be null.");

            NameValueCollection settings = (NameValueCollection)ConfigurationManager.GetSection("system.data.oledb");
            Stream XMLStream = null;
            String providerFileName = oleDbOuterConnection.GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_PROVIDERFILENAME) as string;

            if (settings != null)
            {
                string[] values = null;
                string metaDataXML = null;
                // first try to get the provider specific xml

                // if providerfilename is not supported we can't build the settings key needed to
                // get the provider specific XML path
                if (providerFileName != null)
                {
                    metaDataXML = providerFileName + _metaDataXml;
                    values = settings.GetValues(metaDataXML);
                }

                // if we did not find provider specific xml see if there is new default xml
                if (values == null)
                {
                    metaDataXML = _defaultMetaDataXml;
                    values = settings.GetValues(metaDataXML);
                }

                // If there is new XML get it
                if (values != null)
                {
                    XMLStream = ADP.GetXmlStreamFromValues(values, metaDataXML);
                }
            }

            // if the xml was not obtained from machine.config use the embedded XML resource
            if (XMLStream == null)
            {
                XMLStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("System.Data.OleDb.OleDbMetaData.xml");
                cacheMetaDataFactory = true;
            }

            Debug.Assert(XMLStream != null, "XMLstream may not be null.");

            // using the ServerVersion as the NormalizedServerVersion. Doing this for two reasons
            //  1) The Spec for DBPROP_DBMSVER normalizes the ServerVersion
            //  2) for OLE DB its the only game in town
            return new OleDbMetaDataFactory(XMLStream,
                                             oleDbInternalConnection.ServerVersion,
                                             oleDbInternalConnection.ServerVersion,
                                             oleDbInternalConnection.GetSchemaRowsetInformation());
        }

        override protected DbConnectionPoolGroupOptions CreateConnectionPoolGroupOptions(DbConnectionOptions connectionOptions)
        {
            return null;
        }

        override internal DbConnectionPoolGroupProviderInfo CreateConnectionPoolGroupProviderInfo(DbConnectionOptions connectionOptions)
        {
            return new OleDbConnectionPoolGroupProviderInfo();
        }

        override internal DbConnectionPoolGroup GetConnectionPoolGroup(DbConnection connection)
        {
            OleDbConnection c = (connection as OleDbConnection);
            if (null != c)
            {
                return c.PoolGroup;
            }
            return null;
        }

        override internal void PermissionDemand(DbConnection outerConnection)
        {
            OleDbConnection c = (outerConnection as OleDbConnection);
            if (null != c)
            {
                c.PermissionDemand();
            }
        }

        override internal void SetConnectionPoolGroup(DbConnection outerConnection, DbConnectionPoolGroup poolGroup)
        {
            OleDbConnection c = (outerConnection as OleDbConnection);
            if (null != c)
            {
                c.PoolGroup = poolGroup;
            }
        }

        override internal void SetInnerConnectionEvent(DbConnection owningObject, DbConnectionInternal to)
        {
            OleDbConnection c = (owningObject as OleDbConnection);
            if (null != c)
            {
                c.SetInnerConnectionEvent(to);
            }
        }

        override internal bool SetInnerConnectionFrom(DbConnection owningObject, DbConnectionInternal to, DbConnectionInternal from)
        {
            OleDbConnection c = (owningObject as OleDbConnection);
            if (null != c)
            {
                return c.SetInnerConnectionFrom(to, from);
            }
            return false;
        }

        override internal void SetInnerConnectionTo(DbConnection owningObject, DbConnectionInternal to)
        {
            OleDbConnection c = (owningObject as OleDbConnection);
            if (null != c)
            {
                c.SetInnerConnectionTo(to);
            }
        }

    }
}


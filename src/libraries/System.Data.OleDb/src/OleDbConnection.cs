// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.OleDb
{
    using SysTx = Transactions;

    // wraps the OLEDB IDBInitialize interface which represents a connection
    // Notes about connection pooling
    // 1. Connection pooling isn't supported on Win95
    // 2. Only happens if we use the IDataInitialize or IDBPromptInitialize interfaces
    //    it won't happen if you directly create the provider and set its properties
    // 3. First call on IDBInitialize must be Initialize, can't QI for any other interfaces before that
    [DefaultEvent("InfoMessage")]
    public sealed partial class OleDbConnection : DbConnection, ICloneable, IDbConnection
    {
        static private readonly object EventInfoMessage = new object();

        public OleDbConnection(string connectionString) : this()
        {
            ConnectionString = connectionString;
        }

        private OleDbConnection(OleDbConnection connection) : this()
        { // Clone
            CopyFrom(connection);
        }

        [
        DefaultValue(""),
#pragma warning disable 618 // ignore obsolete warning about RecommendedAsConfigurable to use SettingsBindableAttribute
        RecommendedAsConfigurable(true),
#pragma warning restore 618
        SettingsBindable(true),
        RefreshProperties(RefreshProperties.All),
        ]
        override public string ConnectionString
        {
            get
            {
                return ConnectionString_Get();
            }
            set
            {
                ConnectionString_Set(value);
            }
        }

        private OleDbConnectionString OleDbConnectionStringValue
        {
            get { return (OleDbConnectionString)ConnectionOptions; }
        }

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        override public int ConnectionTimeout
        {
            get
            {
                object value = null;
                if (IsOpen)
                {
                    value = GetDataSourceValue(OleDbPropertySetGuid.DBInit, ODB.DBPROP_INIT_TIMEOUT);
                }
                else
                {
                    OleDbConnectionString constr = this.OleDbConnectionStringValue;
                    value = (null != constr) ? constr.ConnectTimeout : ADP.DefaultConnectionTimeout;
                }
                if (null != value)
                {
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                else
                {
                    return ADP.DefaultConnectionTimeout;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        override public string Database
        {
            get
            {
                OleDbConnectionString constr = (OleDbConnectionString)UserConnectionOptions;
                object value = (null != constr) ? constr.InitialCatalog : string.Empty;
                if ((null != value) && !((string)value).StartsWith(DbConnectionOptions.DataDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    OleDbConnectionInternal connection = GetOpenConnection();
                    if (null != connection)
                    {
                        if (connection.HasSession)
                        {
                            value = GetDataSourceValue(OleDbPropertySetGuid.DataSource, ODB.DBPROP_CURRENTCATALOG);
                        }
                        else
                        {
                            value = GetDataSourceValue(OleDbPropertySetGuid.DBInit, ODB.DBPROP_INIT_CATALOG);
                        }
                    }
                    else
                    {
                        constr = this.OleDbConnectionStringValue;
                        value = (null != constr) ? constr.InitialCatalog : string.Empty;
                    }
                }
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        [
        Browsable(true)
        ]
        override public string DataSource
        {
            get
            {
                OleDbConnectionString constr = (OleDbConnectionString)UserConnectionOptions;
                object value = (null != constr) ? constr.DataSource : string.Empty;
                if ((null != value) && !((string)value).StartsWith(DbConnectionOptions.DataDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    if (IsOpen)
                    {
                        value = GetDataSourceValue(OleDbPropertySetGuid.DBInit, ODB.DBPROP_INIT_DATASOURCE);
                        if ((null == value) || ((value is string) && (0 == (value as string).Length)))
                        {
                            value = GetDataSourceValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_DATASOURCENAME);
                        }
                    }
                    else
                    {
                        constr = this.OleDbConnectionStringValue;
                        value = (null != constr) ? constr.DataSource : string.Empty;
                    }
                }
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        internal bool IsOpen
        {
            get { return (null != GetOpenConnection()); }
        }

        internal OleDbTransaction LocalTransaction
        {
            set
            {
                OleDbConnectionInternal openConnection = GetOpenConnection();

                if (null != openConnection)
                {
                    openConnection.LocalTransaction = value;
                }
            }
        }

        [
        Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public String Provider
        {
            get
            {
                OleDbConnectionString constr = this.OleDbConnectionStringValue;
                string value = ((null != constr) ? constr.ConvertValueToString(ODB.Provider, null) : null);
                return ((null != value) ? value : string.Empty);
            }
        }

        internal OleDbConnectionPoolGroupProviderInfo ProviderInfo
        {
            get
            {
                return (OleDbConnectionPoolGroupProviderInfo)PoolGroup.ProviderInfo;
            }
        }

        override public string ServerVersion
        {
            get
            {
                return InnerConnection.ServerVersion;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        // ResDescriptionAttribute(SR.DbConnection_State),
        ]
        override public ConnectionState State
        {
            get
            {
                return InnerConnection.State;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void ResetState()
        {
            if (IsOpen)
            {
                object value = GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_CONNECTIONSTATUS);
                if (value is Int32)
                {
                    int connectionStatus = (int)value;
                    switch (connectionStatus)
                    {
                        case ODB.DBPROPVAL_CS_UNINITIALIZED: // provider closed on us
                        case ODB.DBPROPVAL_CS_COMMUNICATIONFAILURE: // broken connection
                            GetOpenConnection().DoomThisConnection();
                            NotifyWeakReference(OleDbReferenceCollection.Canceling);
                            Close();
                            break;

                        case ODB.DBPROPVAL_CS_INITIALIZED: // everything is okay
                            break;

                        default: // have to assume everything is okay
                            Debug.Assert(false, "Unknown 'Connection Status' value " + connectionStatus.ToString("G", CultureInfo.InvariantCulture));
                            break;
                    }
                }
            }
        }

        public event OleDbInfoMessageEventHandler InfoMessage
        {
            add
            {
                Events.AddHandler(EventInfoMessage, value);
            }
            remove
            {
                Events.RemoveHandler(EventInfoMessage, value);
            }
        }

        internal UnsafeNativeMethods.ICommandText ICommandText()
        {
            Debug.Assert(null != GetOpenConnection(), "ICommandText closed");
            return GetOpenConnection().ICommandText();
        }

        private IDBPropertiesWrapper IDBProperties()
        {
            Debug.Assert(null != GetOpenConnection(), "IDBProperties closed");
            return GetOpenConnection().IDBProperties();
        }

        internal IOpenRowsetWrapper IOpenRowset()
        {
            Debug.Assert(null != GetOpenConnection(), "IOpenRowset closed");
            return GetOpenConnection().IOpenRowset();
        }

        internal int SqlSupport()
        {
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString SqlSupport");
            return this.OleDbConnectionStringValue.GetSqlSupport(this);
        }

        internal bool SupportMultipleResults()
        {
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString SupportMultipleResults");
            return this.OleDbConnectionStringValue.GetSupportMultipleResults(this);
        }

        internal bool SupportIRow(OleDbCommand cmd)
        {
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString SupportIRow");
            return this.OleDbConnectionStringValue.GetSupportIRow(this, cmd);
        }

        internal int QuotedIdentifierCase()
        {
            Debug.Assert(null != this.OleDbConnectionStringValue, "no OleDbConnectionString QuotedIdentifierCase");

            int quotedIdentifierCase;
            object value = GetDataSourcePropertyValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_QUOTEDIDENTIFIERCASE);
            if (value is Int32)
            {// not OleDbPropertyStatus
                quotedIdentifierCase = (int)value;
            }
            else
            {
                quotedIdentifierCase = -1;
            }
            return quotedIdentifierCase;
        }

        internal bool ForceNewConnection { get { return false; } set {; } }

        new public OleDbTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.Unspecified);
        }

        new public OleDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return (OleDbTransaction)InnerConnection.BeginTransaction(isolationLevel);
        }

        override public void ChangeDatabase(string value)
        {
            CheckStateOpen(ADP.ChangeDatabase);
            if ((null == value) || (0 == value.Trim().Length))
            {
                throw ADP.EmptyDatabaseName();
            }
            SetDataSourcePropertyValue(OleDbPropertySetGuid.DataSource, ODB.DBPROP_CURRENTCATALOG, ODB.Current_Catalog, true, value);
        }

        internal void CheckStateOpen(string method)
        {
            ConnectionState state = State;
            if (ConnectionState.Open != state)
            {
                throw ADP.OpenConnectionRequired(method, state);
            }
        }

        object ICloneable.Clone()
        {
            OleDbConnection clone = new OleDbConnection(this);
            return clone;
        }

        override public void Close()
        {
            InnerConnection.CloseConnection(this, ConnectionFactory);
            // does not require GC.KeepAlive(this) because of OnStateChange
        }

        new public OleDbCommand CreateCommand()
        {
            return new OleDbCommand("", this);
        }

        private void DisposeMe(bool disposing)
        {
            if (disposing)
            { // release mananged objects
                if (DesignMode)
                {
                    // release the object pool in design-mode so that
                    // native MDAC can be properly released during shutdown
                    OleDbConnection.ReleaseObjectPool();
                }
            }
        }

        // suppress this message - we cannot use SafeHandle here.
        [SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
        override protected DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            DbTransaction transaction = InnerConnection.BeginTransaction(isolationLevel);

            // InnerConnection doesn't maintain a ref on the outer connection (this) and 
            //   subsequently leaves open the possibility that the outer connection could be GC'ed before the DbTransaction
            //   is fully hooked up (leaving a DbTransaction with a null connection property). Ensure that this is reachable
            //   until the completion of BeginTransaction with KeepAlive
            GC.KeepAlive(this);

            return transaction;
        }

        internal object GetDataSourcePropertyValue(Guid propertySet, int propertyID)
        {
            OleDbConnectionInternal connection = GetOpenConnection();
            return connection.GetDataSourcePropertyValue(propertySet, propertyID);
        }

        internal object GetDataSourceValue(Guid propertySet, int propertyID)
        {
            object value = GetDataSourcePropertyValue(propertySet, propertyID);
            if ((value is OleDbPropertyStatus) || Convert.IsDBNull(value))
            {
                value = null;
            }
            return value;
        }

        private OleDbConnectionInternal GetOpenConnection()
        {
            DbConnectionInternal innerConnection = InnerConnection;
            return (innerConnection as OleDbConnectionInternal);
        }

        internal void GetLiteralQuotes(string method, out string quotePrefix, out string quoteSuffix)
        {
            CheckStateOpen(method);
            OleDbConnectionPoolGroupProviderInfo info = ProviderInfo;
            if (info.HasQuoteFix)
            {
                quotePrefix = info.QuotePrefix;
                quoteSuffix = info.QuoteSuffix;
            }
            else
            {
                OleDbConnectionInternal connection = GetOpenConnection();
                quotePrefix = connection.GetLiteralInfo(ODB.DBLITERAL_QUOTE_PREFIX);
                quoteSuffix = connection.GetLiteralInfo(ODB.DBLITERAL_QUOTE_SUFFIX);
                if (null == quotePrefix)
                {
                    quotePrefix = "";
                }
                if (null == quoteSuffix)
                {
                    quoteSuffix = quotePrefix;
                }
                info.SetQuoteFix(quotePrefix, quoteSuffix);
            }
        }

        public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions)
        {
            CheckStateOpen(ADP.GetOleDbSchemaTable);
            OleDbConnectionInternal connection = GetOpenConnection();

            if (OleDbSchemaGuid.DbInfoLiterals == schema)
            {
                if ((null == restrictions) || (0 == restrictions.Length))
                {
                    return connection.BuildInfoLiterals();
                }
                throw ODB.InvalidRestrictionsDbInfoLiteral("restrictions");
            }
            else if (OleDbSchemaGuid.SchemaGuids == schema)
            {
                if ((null == restrictions) || (0 == restrictions.Length))
                {
                    return connection.BuildSchemaGuids();
                }
                throw ODB.InvalidRestrictionsSchemaGuids("restrictions");
            }
            else if (OleDbSchemaGuid.DbInfoKeywords == schema)
            {
                if ((null == restrictions) || (0 == restrictions.Length))
                {
                    return connection.BuildInfoKeywords();
                }
                throw ODB.InvalidRestrictionsDbInfoKeywords("restrictions");
            }

            if (connection.SupportSchemaRowset(schema))
            {
                return connection.GetSchemaRowset(schema, restrictions);
            }
            else
            {
                using (IDBSchemaRowsetWrapper wrapper = connection.IDBSchemaRowset())
                {
                    if (null == wrapper.Value)
                    {
                        throw ODB.SchemaRowsetsNotSupported(Provider);
                    }
                }
                throw ODB.NotSupportedSchemaTable(schema, this);
            }
        }

        internal DataTable GetSchemaRowset(Guid schema, object[] restrictions)
        {
            Debug.Assert(null != GetOpenConnection(), "GetSchemaRowset closed");
            return GetOpenConnection().GetSchemaRowset(schema, restrictions);
        }

        internal bool HasLiveReader(OleDbCommand cmd)
        {
            bool result = false;
            OleDbConnectionInternal openConnection = GetOpenConnection();

            if (null != openConnection)
            {
                result = openConnection.HasLiveReader(cmd);
            }
            return result;
        }

        internal void OnInfoMessage(UnsafeNativeMethods.IErrorInfo errorInfo, OleDbHResult errorCode)
        {
            OleDbInfoMessageEventHandler handler = (OleDbInfoMessageEventHandler)Events[EventInfoMessage];
            if (null != handler)
            {
                try
                {
                    OleDbException exception = OleDbException.CreateException(errorInfo, errorCode, null);
                    OleDbInfoMessageEventArgs e = new OleDbInfoMessageEventArgs(exception);
                    handler(this, e);
                }
                catch (Exception e)
                { // eat the exception
                    // UNDONE - should not be catching all exceptions!!!
                    if (!ADP.IsCatchableOrSecurityExceptionType(e))
                    {
                        throw;
                    }

                    ADP.TraceExceptionWithoutRethrow(e);
                }
            }
#if DEBUG
            else
            {
                OleDbException exception = OleDbException.CreateException(errorInfo, errorCode, null);
            }
#endif
        }

        override public void Open()
        {
            InnerConnection.OpenConnection(this, ConnectionFactory);

            // need to manually enlist in some cases, because
            // native OLE DB doesn't know about SysTx transactions.
            if ((0 != (ODB.DBPROPVAL_OS_TXNENLISTMENT & ((OleDbConnectionString)(this.ConnectionOptions)).OleDbServices))
                        && ADP.NeedManualEnlistment())
            {
                GetOpenConnection().EnlistTransactionInternal(SysTx.Transaction.Current);
            }
        }

        internal void SetDataSourcePropertyValue(Guid propertySet, int propertyID, string description, bool required, object value)
        {
            CheckStateOpen(ADP.SetProperties);
            OleDbHResult hr;
            using (IDBPropertiesWrapper idbProperties = IDBProperties())
            {
                using (DBPropSet propSet = DBPropSet.CreateProperty(propertySet, propertyID, required, value))
                {
                    hr = idbProperties.Value.SetProperties(propSet.PropertySetCount, propSet);

                    if (hr < 0)
                    {
                        Exception e = OleDbConnection.ProcessResults(hr, null, this);
                        if (OleDbHResult.DB_E_ERRORSOCCURRED == hr)
                        {
                            StringBuilder builder = new StringBuilder();
                            Debug.Assert(1 == propSet.PropertySetCount, "too many PropertySets");

                            tagDBPROP[] dbprops = propSet.GetPropertySet(0, out propertySet);
                            Debug.Assert(1 == dbprops.Length, "too many Properties");

                            ODB.PropsetSetFailure(builder, description, dbprops[0].dwStatus);

                            e = ODB.PropsetSetFailure(builder.ToString(), e);
                        }
                        if (null != e)
                        {
                            throw e;
                        }
                    }
                    else
                    {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                }
            }
        }

        internal bool SupportSchemaRowset(Guid schema)
        {
            return GetOpenConnection().SupportSchemaRowset(schema);
        }

        internal OleDbTransaction ValidateTransaction(OleDbTransaction transaction, string method)
        {
            return GetOpenConnection().ValidateTransaction(transaction, method);
        }

        static internal Exception ProcessResults(OleDbHResult hresult, OleDbConnection connection, object src)
        {
            if ((0 <= (int)hresult) && ((null == connection) || (null == connection.Events[EventInfoMessage])))
            {
                SafeNativeMethods.Wrapper.ClearErrorInfo();
                return null;
            }

            // ErrorInfo object is to be checked regardless the hresult returned by the function called
            Exception e = null;
            UnsafeNativeMethods.IErrorInfo errorInfo = null;
            OleDbHResult hr = UnsafeNativeMethods.GetErrorInfo(0, out errorInfo);  // 0 - IErrorInfo exists, 1 - no IErrorInfo
            if ((OleDbHResult.S_OK == hr) && (null != errorInfo))
            {
                if (hresult < 0)
                {
                    // UNDONE: if authentication failed - throw a unique exception object type
                    //if (/*OLEDB_Error.DB_SEC_E_AUTH_FAILED*/unchecked((int)0x80040E4D) == hr) {
                    //}
                    //else if (/*OLEDB_Error.DB_E_CANCELED*/unchecked((int)0x80040E4E) == hr) {
                    //}
                    // else {
                    e = OleDbException.CreateException(errorInfo, hresult, null);
                    //}

                    if (OleDbHResult.DB_E_OBJECTOPEN == hresult)
                    {
                        e = ADP.OpenReaderExists(e);
                    }

                    ResetState(connection);
                }
                else if (null != connection)
                {
                    connection.OnInfoMessage(errorInfo, hresult);
                }
                else
                {
                }
                Marshal.ReleaseComObject(errorInfo);
            }
            else if (0 < hresult)
            {
                // @devnote: OnInfoMessage with no ErrorInfo
            }
            else if ((int)hresult < 0)
            {
                e = ODB.NoErrorInformation((null != connection) ? connection.Provider : null, hresult, null); // OleDbException

                ResetState(connection);
            }
            if (null != e)
            {
                ADP.TraceExceptionAsReturnValue(e);
            }
            return e;
        }

        // @devnote: should be multithread safe
        static public void ReleaseObjectPool()
        {
            OleDbConnectionString.ReleaseObjectPool();
            OleDbConnectionInternal.ReleaseObjectPool();
            OleDbConnectionFactory.SingletonInstance.ClearAllPools();
        }

        static private void ResetState(OleDbConnection connection)
        {
            if (null != connection)
            {
                connection.ResetState();
            }
        }
    }
}

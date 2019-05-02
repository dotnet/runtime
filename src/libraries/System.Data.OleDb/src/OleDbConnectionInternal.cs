// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Data.OleDb
{
    using SysTx = Transactions;

    sealed internal class OleDbConnectionInternal : DbConnectionInternal, IDisposable
    {
        static private volatile OleDbServicesWrapper idataInitialize;
        static private object dataInitializeLock = new object();

        internal readonly OleDbConnectionString ConnectionString; // parsed connection string attributes

        // A SafeHandle is used instead of a RCW because we need to fake the CLR into not marshalling

        // OLE DB Services is marked apartment thread, but it actually supports/requires free-threading.
        // However the CLR doesn't know this and attempts to marshal the interfaces back to their original context.
        // But the OLE DB doesn't marshal very well if at all.  Our workaround is based on the fact
        // OLE DB is free-threaded and allows the workaround.

        // Creating DataSource/Session would requiring marshalling DataLins to its original context
        // and has a severe performance impact (when working with transactions), hence our workaround to not Marshal.

        // Creating a Command would requiring marshalling Session to its original context and
        // actually doesn't work correctly, without our workaround you must execute the command in
        // the same context of the connection open.  This doesn't work for pooled objects that contain
        // an open OleDbConnection.

        // We don't do extra work at this time to allow the DataReader to be used in a different context
        // from which the command was executed in. IRowset.GetNextRows will throw InvalidCastException

        // In V1.0, we worked around the performance impact of creating a DataSource/Session using
        // WrapIUnknownWithComObject which creates a new RCW without searching for existing RCW
        // effectively faking out the CLR into thinking the call is in the correct context.
        // We also would use Marshal.ReleaseComObject to force the release of the 'temporary' RCW.

        // In V1.1, we worked around the CreateCommand issue with the same WrapIUnknownWithComObject trick.

        // In V2.0, the performance of using WrapIUnknownWithComObject & ReleaseComObject severly degraded.
        // Using a SafeHandle (for lifetime control) and a delegate to call the apporiate COM method
        // offered much better performance.

        // the "Data Source object".
        private readonly DataSourceWrapper _datasrcwrp;

        // the "Session object".
        private readonly SessionWrapper _sessionwrp;

        private WeakReference weakTransaction;

        // When set to true the current connection is enlisted in a transaction that must be
        // un-enlisted during Deactivate.
        private bool _unEnlistDuringDeactivate;

        internal OleDbConnectionInternal(OleDbConnectionString constr, OleDbConnection connection) : base()
        {
            Debug.Assert((null != constr) && !constr.IsEmpty, "empty connectionstring");
            ConnectionString = constr;

            if (constr.PossiblePrompt && !System.Environment.UserInteractive)
            {
                throw ODB.PossiblePromptNotUserInteractive();
            }

            try
            {
                // this is the native DataLinks object which pools the native datasource/session
                OleDbServicesWrapper wrapper = OleDbConnectionInternal.GetObjectPool();
                _datasrcwrp = new DataSourceWrapper();

                // DataLinks wrapper will call IDataInitialize::GetDataSource to create the DataSource
                // uses constr.ActualConnectionString, no InfoMessageEvent checking
                wrapper.GetDataSource(constr, ref _datasrcwrp);
                Debug.Assert(!_datasrcwrp.IsInvalid, "bad DataSource");

                // initialization is delayed because of OleDbConnectionStringBuilder only wants
                // pre-Initialize IDBPropertyInfo & IDBProperties on the data source
                if (null != connection)
                {
                    _sessionwrp = new SessionWrapper();

                    // From the DataSource object, will call IDBInitialize.Initialize & IDBCreateSession.CreateSession
                    // We always need both called so we use a single call for a single DangerousAddRef/DangerousRelease pair.
                    OleDbHResult hr = _datasrcwrp.InitializeAndCreateSession(constr, ref _sessionwrp);

                    // process the HResult here instead of from the SafeHandle because the possibility
                    // of an InfoMessageEvent.
                    if ((0 <= hr) && !_sessionwrp.IsInvalid)
                    { // process infonessage events
                        OleDbConnection.ProcessResults(hr, connection, connection);
                    }
                    else
                    {
                        Exception e = OleDbConnection.ProcessResults(hr, null, null);
                        Debug.Assert(null != e, "CreateSessionError");
                        throw e;
                    }
                    Debug.Assert(!_sessionwrp.IsInvalid, "bad Session");
                }
            }
            catch
            {
                if (null != _sessionwrp)
                {
                    _sessionwrp.Dispose();
                    _sessionwrp = null;
                }
                if (null != _datasrcwrp)
                {
                    _datasrcwrp.Dispose();
                    _datasrcwrp = null;
                }
                throw;
            }
        }

        internal OleDbConnection Connection
        {
            get
            {
                return (OleDbConnection)Owner;
            }
        }

        internal bool HasSession
        {
            get
            {
                return (null != _sessionwrp);
            }
        }

        internal OleDbTransaction LocalTransaction
        {
            get
            {
                OleDbTransaction result = null;
                if (null != weakTransaction)
                {
                    result = ((OleDbTransaction)weakTransaction.Target);
                }
                return result;
            }
            set
            {
                weakTransaction = null;

                if (null != value)
                {
                    weakTransaction = new WeakReference((OleDbTransaction)value);
                }
            }
        }

        private string Provider
        {
            get { return ConnectionString.Provider; }
        }

        override public string ServerVersion
        {
            // consider making a method, not a property
            get
            {
                object value = GetDataSourceValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_DBMSVER);
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        // grouping the native OLE DB casts togther by required interfaces and optional interfaces, connection then session
        // want these to be methods, not properties otherwise they appear in VS7 managed debugger which attempts to evaluate them

        // required interface, safe cast
        internal IDBPropertiesWrapper IDBProperties()
        {
            Debug.Assert(null != _datasrcwrp, "IDBProperties: null datasource");
            return _datasrcwrp.IDBProperties(this);
        }

        // required interface, safe cast
        internal IOpenRowsetWrapper IOpenRowset()
        {
            Debug.Assert(null != _datasrcwrp, "IOpenRowset: null datasource");
            Debug.Assert(null != _sessionwrp, "IOpenRowset: null session");
            return _sessionwrp.IOpenRowset(this);
        }

        // optional interface, unsafe cast
        private IDBInfoWrapper IDBInfo()
        {
            Debug.Assert(null != _datasrcwrp, "IDBInfo: null datasource");
            return _datasrcwrp.IDBInfo(this);
        }

        // optional interface, unsafe cast
        internal IDBSchemaRowsetWrapper IDBSchemaRowset()
        {
            Debug.Assert(null != _datasrcwrp, "IDBSchemaRowset: null datasource");
            Debug.Assert(null != _sessionwrp, "IDBSchemaRowset: null session");
            return _sessionwrp.IDBSchemaRowset(this);
        }

        // optional interface, unsafe cast
        internal ITransactionJoinWrapper ITransactionJoin()
        {
            Debug.Assert(null != _datasrcwrp, "ITransactionJoin: null datasource");
            Debug.Assert(null != _sessionwrp, "ITransactionJoin: null session");
            return _sessionwrp.ITransactionJoin(this);
        }

        // optional interface, unsafe cast
        internal UnsafeNativeMethods.ICommandText ICommandText()
        {
            Debug.Assert(null != _datasrcwrp, "IDBCreateCommand: null datasource");
            Debug.Assert(null != _sessionwrp, "IDBCreateCommand: null session");

            object icommandText = null;
            OleDbHResult hr = _sessionwrp.CreateCommand(ref icommandText);

            Debug.Assert((0 <= hr) || (null == icommandText), "CreateICommandText: error with ICommandText");
            if (hr < 0)
            {
                if (OleDbHResult.E_NOINTERFACE != hr)
                {
                    ProcessResults(hr);
                }
                else
                {
                    SafeNativeMethods.Wrapper.ClearErrorInfo();
                }
            }
            return (UnsafeNativeMethods.ICommandText)icommandText;
        }

        override protected void Activate(SysTx.Transaction transaction)
        {
            throw ADP.NotSupported();
        }

        override public DbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            OleDbConnection outerConnection = Connection;
            if (null != LocalTransaction)
            {
                throw ADP.ParallelTransactionsNotSupported(outerConnection);
            }

            object unknown = null;
            OleDbTransaction transaction;
            try
            {
                transaction = new OleDbTransaction(outerConnection, null, isolationLevel);
                Debug.Assert(null != _datasrcwrp, "ITransactionLocal: null datasource");
                Debug.Assert(null != _sessionwrp, "ITransactionLocal: null session");
                unknown = _sessionwrp.ComWrapper();
                UnsafeNativeMethods.ITransactionLocal value = (unknown as UnsafeNativeMethods.ITransactionLocal);
                if (null == value)
                {
                    throw ODB.TransactionsNotSupported(Provider, (Exception)null);
                }
                transaction.BeginInternal(value);
            }
            finally
            {
                if (null != unknown)
                {
                    Marshal.ReleaseComObject(unknown);
                }
            }
            LocalTransaction = transaction;
            return transaction;
        }

        override protected DbReferenceCollection CreateReferenceCollection()
        {
            return new OleDbReferenceCollection();
        }

        override protected void Deactivate()
        { // used by both managed and native pooling
            NotifyWeakReference(OleDbReferenceCollection.Closing);

            if (_unEnlistDuringDeactivate)
            {
                // Un-enlist transaction as OLEDB connection pool is unaware of managed transactions.
                EnlistTransactionInternal(null);
            }
            OleDbTransaction transaction = LocalTransaction;
            if (null != transaction)
            {
                LocalTransaction = null;
                // required to rollback any transactions on this connection
                // before releasing the back to the oledb connection pool
                transaction.Dispose();
            }
        }

        public override void Dispose()
        {
            Debug.Assert(null == LocalTransaction, "why was Deactivate not called first");
            if (null != _sessionwrp)
            {
                _sessionwrp.Dispose();
            }
            if (null != _datasrcwrp)
            {
                _datasrcwrp.Dispose();
            }
            base.Dispose();
        }

        override public void EnlistTransaction(SysTx.Transaction transaction)
        {
            OleDbConnection outerConnection = Connection;
            if (null != LocalTransaction)
            {
                throw ADP.LocalTransactionPresent();
            }
            EnlistTransactionInternal(transaction);
        }

        internal void EnlistTransactionInternal(SysTx.Transaction transaction)
        {
            SysTx.IDtcTransaction oleTxTransaction = ADP.GetOletxTransaction(transaction);

            using (ITransactionJoinWrapper transactionJoin = ITransactionJoin())
            {
                if (null == transactionJoin.Value)
                {
                    throw ODB.TransactionsNotSupported(Provider, (Exception)null);
                }
                transactionJoin.Value.JoinTransaction(oleTxTransaction, (int)IsolationLevel.Unspecified, 0, IntPtr.Zero);
                _unEnlistDuringDeactivate = (null != transaction);
            }
            EnlistedTransaction = transaction;
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

        internal object GetDataSourcePropertyValue(Guid propertySet, int propertyID)
        {
            OleDbHResult hr;
            tagDBPROP[] dbprops;
            using (IDBPropertiesWrapper idbProperties = IDBProperties())
            {
                using (PropertyIDSet propidset = new PropertyIDSet(propertySet, propertyID))
                {
                    using (DBPropSet propset = new DBPropSet(idbProperties.Value, propidset, out hr))
                    {
                        if (hr < 0)
                        {
                            // OLEDB Data Reader masks provider specific errors by raising "Internal Data Provider error 30."
                            // DBPropSet c-tor will register the exception and it will be raised at GetPropertySet call in case of failure
                            SafeNativeMethods.Wrapper.ClearErrorInfo();
                        }
                        dbprops = propset.GetPropertySet(0, out propertySet);
                    }
                }
            }
            if (OleDbPropertyStatus.Ok == dbprops[0].dwStatus)
            {
                return dbprops[0].vValue;
            }
            return dbprops[0].dwStatus;
        }

        internal DataTable BuildInfoLiterals()
        {
            using (IDBInfoWrapper wrapper = IDBInfo())
            {
                UnsafeNativeMethods.IDBInfo dbInfo = wrapper.Value;
                if (null == dbInfo)
                {
                    return null;
                }

                DataTable table = new DataTable("DbInfoLiterals");
                table.Locale = CultureInfo.InvariantCulture;
                DataColumn literalName = new DataColumn("LiteralName", typeof(String));
                DataColumn literalValue = new DataColumn("LiteralValue", typeof(String));
                DataColumn invalidChars = new DataColumn("InvalidChars", typeof(String));
                DataColumn invalidStart = new DataColumn("InvalidStartingChars", typeof(String));
                DataColumn literal = new DataColumn("Literal", typeof(Int32));
                DataColumn maxlen = new DataColumn("Maxlen", typeof(Int32));

                table.Columns.Add(literalName);
                table.Columns.Add(literalValue);
                table.Columns.Add(invalidChars);
                table.Columns.Add(invalidStart);
                table.Columns.Add(literal);
                table.Columns.Add(maxlen);

                OleDbHResult hr;
                int literalCount = 0;
                IntPtr literalInfo = ADP.PtrZero;
                using (DualCoTaskMem handle = new DualCoTaskMem(dbInfo, null, out literalCount, out literalInfo, out hr))
                {
                    // All literals were either invalid or unsupported. The provider allocates memory for *prgLiteralInfo and sets the value of the fSupported element in all of the structures to FALSE. The consumer frees this memory when it no longer needs the information.
                    if (OleDbHResult.DB_E_ERRORSOCCURRED != hr)
                    {
                        long offset = literalInfo.ToInt64();
                        tagDBLITERALINFO tag = new tagDBLITERALINFO();
                        for (int i = 0; i < literalCount; ++i, offset += ODB.SizeOf_tagDBLITERALINFO)
                        {
                            Marshal.PtrToStructure((IntPtr)offset, tag);

                            DataRow row = table.NewRow();
                            row[literalName] = ((OleDbLiteral)tag.it).ToString();
                            row[literalValue] = tag.pwszLiteralValue;
                            row[invalidChars] = tag.pwszInvalidChars;
                            row[invalidStart] = tag.pwszInvalidStartingChars;
                            row[literal] = tag.it;
                            row[maxlen] = tag.cchMaxLen;

                            table.Rows.Add(row);
                            row.AcceptChanges();
                        }
                        if (hr < 0)
                        { // ignore infomsg
                            ProcessResults(hr);
                        }
                    }
                    else
                    {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                }
                return table;
            }
        }

        internal DataTable BuildInfoKeywords()
        {
            DataTable table = new DataTable(ODB.DbInfoKeywords);
            table.Locale = CultureInfo.InvariantCulture;
            DataColumn keyword = new DataColumn(ODB.Keyword, typeof(String));
            table.Columns.Add(keyword);

            if (!AddInfoKeywordsToTable(table, keyword))
            {
                table = null;
            }

            return table;
        }

        internal bool AddInfoKeywordsToTable(DataTable table, DataColumn keyword)
        {
            using (IDBInfoWrapper wrapper = IDBInfo())
            {
                UnsafeNativeMethods.IDBInfo dbInfo = wrapper.Value;
                if (null == dbInfo)
                {
                    return false;
                }

                OleDbHResult hr;
                string keywords;
                hr = dbInfo.GetKeywords(out keywords);

                if (hr < 0)
                { // ignore infomsg
                    ProcessResults(hr);
                }

                if (null != keywords)
                {
                    string[] values = keywords.Split(new char[1] { ',' });
                    for (int i = 0; i < values.Length; ++i)
                    {
                        DataRow row = table.NewRow();
                        row[keyword] = values[i];

                        table.Rows.Add(row);
                        row.AcceptChanges();
                    }
                }
                return true;
            }
        }

        internal DataTable BuildSchemaGuids()
        {
            DataTable table = new DataTable(ODB.SchemaGuids);
            table.Locale = CultureInfo.InvariantCulture;

            DataColumn schemaGuid = new DataColumn(ODB.Schema, typeof(Guid));
            DataColumn restrictionSupport = new DataColumn(ODB.RestrictionSupport, typeof(Int32));

            table.Columns.Add(schemaGuid);
            table.Columns.Add(restrictionSupport);

            SchemaSupport[] supportedSchemas = GetSchemaRowsetInformation();

            if (null != supportedSchemas)
            {
                object[] values = new object[2];
                table.BeginLoadData();
                for (int i = 0; i < supportedSchemas.Length; ++i)
                {
                    values[0] = supportedSchemas[i]._schemaRowset;
                    values[1] = supportedSchemas[i]._restrictions;
                    table.LoadDataRow(values, LoadOption.OverwriteChanges);
                }
                table.EndLoadData();
            }
            return table;
        }

        internal string GetLiteralInfo(int literal)
        {
            using (IDBInfoWrapper wrapper = IDBInfo())
            {
                UnsafeNativeMethods.IDBInfo dbInfo = wrapper.Value;
                if (null == dbInfo)
                {
                    return null;
                }
                string literalValue = null;
                IntPtr literalInfo = ADP.PtrZero;
                int literalCount = 0;
                OleDbHResult hr;

                using (DualCoTaskMem handle = new DualCoTaskMem(dbInfo, new int[1] { literal }, out literalCount, out literalInfo, out hr))
                {
                    // All literals were either invalid or unsupported. The provider allocates memory for *prgLiteralInfo and sets the value of the fSupported element in all of the structures to FALSE. The consumer frees this memory when it no longer needs the information.
                    if (OleDbHResult.DB_E_ERRORSOCCURRED != hr)
                    {
                        if ((1 == literalCount) && Marshal.ReadInt32(literalInfo, ODB.OffsetOf_tagDBLITERALINFO_it) == literal)
                        {
                            literalValue = Marshal.PtrToStringUni(Marshal.ReadIntPtr(literalInfo, 0));
                        }
                        if (hr < 0)
                        { // ignore infomsg
                            ProcessResults(hr);
                        }
                    }
                    else
                    {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                }
                return literalValue;
            }
        }

        internal SchemaSupport[] GetSchemaRowsetInformation()
        {
            OleDbConnectionString constr = ConnectionString;
            SchemaSupport[] supportedSchemas = constr.SchemaSupport;
            if (null != supportedSchemas)
            {
                return supportedSchemas;
            }
            using (IDBSchemaRowsetWrapper wrapper = IDBSchemaRowset())
            {
                UnsafeNativeMethods.IDBSchemaRowset dbSchemaRowset = wrapper.Value;
                if (null == dbSchemaRowset)
                {
                    return null; // IDBSchemaRowset not supported
                }

                OleDbHResult hr;
                int schemaCount = 0;
                IntPtr schemaGuids = ADP.PtrZero;
                IntPtr schemaRestrictions = ADP.PtrZero;

                using (DualCoTaskMem safehandle = new DualCoTaskMem(dbSchemaRowset, out schemaCount, out schemaGuids, out schemaRestrictions, out hr))
                {
                    dbSchemaRowset = null;
                    if (hr < 0)
                    { // ignore infomsg
                        ProcessResults(hr);
                    }

                    supportedSchemas = new SchemaSupport[schemaCount];
                    if (ADP.PtrZero != schemaGuids)
                    {
                        for (int i = 0, offset = 0; i < supportedSchemas.Length; ++i, offset += ODB.SizeOf_Guid)
                        {
                            IntPtr ptr = ADP.IntPtrOffset(schemaGuids, i * ODB.SizeOf_Guid);
                            supportedSchemas[i]._schemaRowset = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
                        }
                    }
                    if (ADP.PtrZero != schemaRestrictions)
                    {
                        for (int i = 0; i < supportedSchemas.Length; ++i)
                        {
                            supportedSchemas[i]._restrictions = Marshal.ReadInt32(schemaRestrictions, i * 4);
                        }
                    }
                }
                constr.SchemaSupport = supportedSchemas;
                return supportedSchemas;
            }
        }

        internal DataTable GetSchemaRowset(Guid schema, object[] restrictions)
        {
            if (null == restrictions)
            {
                restrictions = new object[0];
            }
            DataTable dataTable = null;
            using (IDBSchemaRowsetWrapper wrapper = IDBSchemaRowset())
            {
                UnsafeNativeMethods.IDBSchemaRowset dbSchemaRowset = wrapper.Value;
                if (null == dbSchemaRowset)
                {
                    throw ODB.SchemaRowsetsNotSupported(Provider);
                }

                UnsafeNativeMethods.IRowset rowset = null;
                OleDbHResult hr;
                hr = dbSchemaRowset.GetRowset(ADP.PtrZero, ref schema, restrictions.Length, restrictions, ref ODB.IID_IRowset, 0, ADP.PtrZero, out rowset);

                if (hr < 0)
                { // ignore infomsg
                    ProcessResults(hr);
                }

                if (null != rowset)
                {
                    using (OleDbDataReader dataReader = new OleDbDataReader(Connection, null, 0, CommandBehavior.Default))
                    {
                        dataReader.InitializeIRowset(rowset, ChapterHandle.DB_NULL_HCHAPTER, IntPtr.Zero);
                        dataReader.BuildMetaInfo();
                        dataReader.HasRowsRead();

                        dataTable = new DataTable();
                        dataTable.Locale = CultureInfo.InvariantCulture;
                        dataTable.TableName = OleDbSchemaGuid.GetTextFromValue(schema);
                        OleDbDataAdapter.FillDataTable(dataReader, dataTable);
                    }
                }
                return dataTable;
            }
        }

        // returns true if there is an active data reader on the specified command
        internal bool HasLiveReader(OleDbCommand cmd)
        {
            OleDbDataReader reader = null;

            if (null != ReferenceCollection)
            {
                reader = ReferenceCollection.FindItem<OleDbDataReader>(OleDbReferenceCollection.DataReaderTag, (dataReader) => cmd == dataReader.Command);
            }

            return (reader != null);
        }

        private void ProcessResults(OleDbHResult hr)
        {
            OleDbConnection connection = Connection; // get value from weakref only once
            Exception e = OleDbConnection.ProcessResults(hr, connection, connection);
            if (null != e)
            { throw e; }
        }

        internal bool SupportSchemaRowset(Guid schema)
        {
            SchemaSupport[] schemaSupport = GetSchemaRowsetInformation();
            if (null != schemaSupport)
            {
                for (int i = 0; i < schemaSupport.Length; ++i)
                {
                    if (schema == schemaSupport[i]._schemaRowset)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static private object CreateInstanceDataLinks()
        {
            Type datalink = Type.GetTypeFromCLSID(ODB.CLSID_DataLinks, true);
            return Activator.CreateInstance(datalink, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, null, CultureInfo.InvariantCulture, null);
        }

        // @devnote: should be multithread safe access to OleDbConnection.idataInitialize,
        // though last one wins for setting variable.  It may be different objects, but
        // OLE DB will ensure I'll work with just the single pool
        static private OleDbServicesWrapper GetObjectPool()
        {
            OleDbServicesWrapper wrapper = OleDbConnectionInternal.idataInitialize;
            if (null == wrapper)
            {
                lock (dataInitializeLock)
                {
                    wrapper = OleDbConnectionInternal.idataInitialize;
                    if (null == wrapper)
                    {
                        VersionCheck();

                        object datalinks;
                        try
                        {
                            datalinks = CreateInstanceDataLinks();
                        }
                        catch (Exception e)
                        {
                            // UNDONE - should not be catching all exceptions!!!
                            if (!ADP.IsCatchableExceptionType(e))
                            {
                                throw;
                            }

                            throw ODB.MDACNotAvailable(e);
                        }
                        if (null == datalinks)
                        {
                            throw ODB.MDACNotAvailable(null);
                        }
                        wrapper = new OleDbServicesWrapper(datalinks);
                        OleDbConnectionInternal.idataInitialize = wrapper;
                    }
                }
            }
            Debug.Assert(null != wrapper, "GetObjectPool: null dataInitialize");
            return wrapper;
        }

        static private void VersionCheck()
        {
            // $REVIEW: do we still need this?
            // if ApartmentUnknown, then CoInitialize may not have been called yet
            if (ApartmentState.Unknown == Thread.CurrentThread.GetApartmentState())
            {
                SetMTAApartmentState();
            }

            ADP.CheckVersionMDAC(false);
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static private void SetMTAApartmentState()
        {
            // we are defaulting to a multithread apartment state
            Thread.CurrentThread.SetApartmentState(ApartmentState.MTA);
        }

        // @devnote: should be multithread safe
        static public void ReleaseObjectPool()
        {
            OleDbConnectionInternal.idataInitialize = null;
        }

        internal OleDbTransaction ValidateTransaction(OleDbTransaction transaction, string method)
        {
            if (null != this.weakTransaction)
            {
                OleDbTransaction head = (OleDbTransaction)this.weakTransaction.Target;
                if ((null != head) && this.weakTransaction.IsAlive)
                {
                    head = OleDbTransaction.TransactionUpdate(head);

                    // either we are wrong or finalize was called and object still alive
                    Debug.Assert(null != head, "unexcpted Transaction state");
                }
                // else transaction has finalized on user

                if (null != head)
                {
                    if (null == transaction)
                    {
                        // valid transaction exists and cmd doesn't have it
                        throw ADP.TransactionRequired(method);
                    }
                    else
                    {
                        OleDbTransaction tail = OleDbTransaction.TransactionLast(head);
                        if (tail != transaction)
                        {
                            if (tail.Connection != transaction.Connection)
                            {
                                throw ADP.TransactionConnectionMismatch();
                            }
                            // else cmd has incorrect transaction
                            throw ADP.TransactionCompleted();
                        }
                        // else cmd has correct transaction
                        return transaction;
                    }
                }
                else
                { // cleanup for Finalized transaction
                    this.weakTransaction = null;
                }
            }
            else if ((null != transaction) && (null != transaction.Connection))
            {
                throw ADP.TransactionConnectionMismatch();
            }
            // else no transaction and cmd is correct

            // if transactionObject is from this connection but zombied
            // and no transactions currently exists - then ignore the bogus object
            return null;
        }

        internal Dictionary<string, OleDbPropertyInfo> GetPropertyInfo(Guid[] propertySets)
        {
            bool isopen = HasSession;
            OleDbConnectionString constr = ConnectionString;
            Dictionary<string, OleDbPropertyInfo> properties = null;

            if (null == propertySets)
            {
                propertySets = new Guid[0];
            }
            using (PropertyIDSet propidset = new PropertyIDSet(propertySets))
            {
                using (IDBPropertiesWrapper idbProperties = IDBProperties())
                {
                    using (PropertyInfoSet infoset = new PropertyInfoSet(idbProperties.Value, propidset))
                    {
                        properties = infoset.GetValues();
                    }
                }
            }
            return properties;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    internal sealed class OleDbConnectionInternal : DbConnectionInternal, IDisposable
    {
        private static volatile OleDbServicesWrapper? idataInitialize;
        private static readonly object dataInitializeLock = new object();

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
        private readonly DataSourceWrapper? _datasrcwrp;

        // the "Session object".
        private readonly SessionWrapper? _sessionwrp;

        private WeakReference? weakTransaction;

        // When set to true the current connection is enlisted in a transaction that must be
        // un-enlisted during Deactivate.
        private bool _unEnlistDuringDeactivate;

        internal OleDbConnectionInternal(OleDbConnectionString constr, OleDbConnection? connection) : base()
        {
            Debug.Assert((constr != null) && !constr.IsEmpty, "empty connectionstring");
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
                if (connection != null)
                {
                    _sessionwrp = new SessionWrapper();

                    // From the DataSource object, will call IDBInitialize.Initialize & IDBCreateSession.CreateSession
                    // We always need both called so we use a single call for a single DangerousAddRef/DangerousRelease pair.
                    OleDbHResult hr = _datasrcwrp.InitializeAndCreateSession(constr, ref _sessionwrp);

                    // process the HResult here instead of from the SafeHandle because the possibility
                    // of an InfoMessageEvent.
                    if ((hr >= 0) && !_sessionwrp.IsInvalid)
                    { // process infonessage events
                        OleDbConnection.ProcessResults(hr, connection, connection);
                    }
                    else
                    {
                        Exception? e = OleDbConnection.ProcessResults(hr, null, null);
                        Debug.Assert(e != null, "CreateSessionError");
                        throw e;
                    }
                    Debug.Assert(!_sessionwrp.IsInvalid, "bad Session");
                }
            }
            catch
            {
                if (_sessionwrp != null)
                {
                    _sessionwrp.Dispose();
                    _sessionwrp = null;
                }
                if (_datasrcwrp != null)
                {
                    _datasrcwrp.Dispose();
                    _datasrcwrp = null;
                }
                throw;
            }
        }

        internal OleDbConnection? Connection
        {
            get
            {
                return (OleDbConnection?)Owner;
            }
        }

        internal bool HasSession
        {
            get
            {
                return (_sessionwrp != null);
            }
        }

        internal OleDbTransaction? LocalTransaction
        {
            get
            {
                OleDbTransaction? result = null;
                if (weakTransaction != null)
                {
                    result = ((OleDbTransaction?)weakTransaction.Target);
                }
                return result;
            }
            set
            {
                weakTransaction = null;

                if (value != null)
                {
                    weakTransaction = new WeakReference((OleDbTransaction)value);
                }
            }
        }

        private string Provider
        {
            get { return ConnectionString.Provider; }
        }

        public override string ServerVersion
        {
            // consider making a method, not a property
            get
            {
                object value = GetDataSourceValue(OleDbPropertySetGuid.DataSourceInfo, ODB.DBPROP_DBMSVER)!;
                return Convert.ToString(value, CultureInfo.InvariantCulture)!;
            }
        }

        // grouping the native OLE DB casts togther by required interfaces and optional interfaces, connection then session
        // want these to be methods, not properties otherwise they appear in VS7 managed debugger which attempts to evaluate them

        // required interface, safe cast
        internal IDBPropertiesWrapper IDBProperties()
        {
            Debug.Assert(_datasrcwrp != null, "IDBProperties: null datasource");
            return _datasrcwrp.IDBProperties(this);
        }

        // required interface, safe cast
        internal IOpenRowsetWrapper IOpenRowset()
        {
            Debug.Assert(_datasrcwrp != null, "IOpenRowset: null datasource");
            Debug.Assert(_sessionwrp != null, "IOpenRowset: null session");
            return _sessionwrp.IOpenRowset(this);
        }

        // optional interface, unsafe cast
        private IDBInfoWrapper IDBInfo()
        {
            Debug.Assert(_datasrcwrp != null, "IDBInfo: null datasource");
            return _datasrcwrp.IDBInfo(this);
        }

        // optional interface, unsafe cast
        internal IDBSchemaRowsetWrapper IDBSchemaRowset()
        {
            Debug.Assert(_datasrcwrp != null, "IDBSchemaRowset: null datasource");
            Debug.Assert(_sessionwrp != null, "IDBSchemaRowset: null session");
            return _sessionwrp.IDBSchemaRowset(this);
        }

        // optional interface, unsafe cast
        internal ITransactionJoinWrapper ITransactionJoin()
        {
            Debug.Assert(_datasrcwrp != null, "ITransactionJoin: null datasource");
            Debug.Assert(_sessionwrp != null, "ITransactionJoin: null session");
            return _sessionwrp.ITransactionJoin(this);
        }

        // optional interface, unsafe cast
        internal UnsafeNativeMethods.ICommandText? ICommandText()
        {
            Debug.Assert(_datasrcwrp != null, "IDBCreateCommand: null datasource");
            Debug.Assert(_sessionwrp != null, "IDBCreateCommand: null session");

            object? icommandText = null;
            OleDbHResult hr = _sessionwrp.CreateCommand(ref icommandText);

            Debug.Assert((hr >= 0) || (icommandText == null), "CreateICommandText: error with ICommandText");
            if (hr < 0)
            {
                if (hr != OleDbHResult.E_NOINTERFACE)
                {
                    ProcessResults(hr);
                }
                else
                {
                    SafeNativeMethods.Wrapper.ClearErrorInfo();
                }
            }
            return (UnsafeNativeMethods.ICommandText?)icommandText;
        }

        protected override void Activate(SysTx.Transaction? transaction)
        {
            throw ADP.NotSupported();
        }

        public override DbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            OleDbConnection outerConnection = Connection!;
            if (LocalTransaction != null)
            {
                throw ADP.ParallelTransactionsNotSupported(outerConnection);
            }

            object? unknown = null;
            OleDbTransaction transaction;
            try
            {
                transaction = new OleDbTransaction(outerConnection, null, isolationLevel);
                Debug.Assert(_datasrcwrp != null, "ITransactionLocal: null datasource");
                Debug.Assert(_sessionwrp != null, "ITransactionLocal: null session");
                unknown = _sessionwrp.ComWrapper();
                UnsafeNativeMethods.ITransactionLocal? value = (unknown as UnsafeNativeMethods.ITransactionLocal);
                if (value == null)
                {
                    throw ODB.TransactionsNotSupported(Provider, null);
                }
                transaction.BeginInternal(value);
            }
            finally
            {
                if (unknown != null)
                {
                    Marshal.ReleaseComObject(unknown);
                }
            }
            LocalTransaction = transaction;
            return transaction;
        }

        protected override DbReferenceCollection CreateReferenceCollection()
        {
            return new OleDbReferenceCollection();
        }

        protected override void Deactivate()
        { // used by both managed and native pooling
            NotifyWeakReference(OleDbReferenceCollection.Closing);

            if (_unEnlistDuringDeactivate)
            {
                // Un-enlist transaction as OLEDB connection pool is unaware of managed transactions.
                EnlistTransactionInternal(null);
            }
            OleDbTransaction? transaction = LocalTransaction;
            if (transaction != null)
            {
                LocalTransaction = null;
                // required to rollback any transactions on this connection
                // before releasing the back to the oledb connection pool
                transaction.Dispose();
            }
        }

        public override void Dispose()
        {
            Debug.Assert(LocalTransaction == null, "why was Deactivate not called first");
            if (_sessionwrp != null)
            {
                _sessionwrp.Dispose();
            }
            if (_datasrcwrp != null)
            {
                _datasrcwrp.Dispose();
            }
            base.Dispose();
        }

        public override void EnlistTransaction(SysTx.Transaction? transaction)
        {
            if (LocalTransaction != null)
            {
                throw ADP.LocalTransactionPresent();
            }
            EnlistTransactionInternal(transaction);
        }

        internal void EnlistTransactionInternal(SysTx.Transaction? transaction)
        {
            SysTx.IDtcTransaction? oleTxTransaction = ADP.GetOletxTransaction(transaction);

            using (ITransactionJoinWrapper transactionJoin = ITransactionJoin())
            {
                if (transactionJoin.Value == null)
                {
                    throw ODB.TransactionsNotSupported(Provider, null);
                }
                transactionJoin.Value.JoinTransaction(oleTxTransaction, (int)IsolationLevel.Unspecified, 0, IntPtr.Zero);
                _unEnlistDuringDeactivate = (transaction != null);
            }
            EnlistedTransaction = transaction;
        }

        internal object? GetDataSourceValue(Guid propertySet, int propertyID)
        {
            object? value = GetDataSourcePropertyValue(propertySet, propertyID);
            if ((value is OleDbPropertyStatus) || Convert.IsDBNull(value))
            {
                value = null;
            }
            return value;
        }

        internal object? GetDataSourcePropertyValue(Guid propertySet, int propertyID)
        {
            OleDbHResult hr;
            ItagDBPROP[] dbprops;
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
            if (dbprops[0].dwStatus == OleDbPropertyStatus.Ok)
            {
                return dbprops[0].vValue;
            }
            return dbprops[0].dwStatus;
        }

        internal DataTable? BuildInfoLiterals()
        {
            using (IDBInfoWrapper wrapper = IDBInfo())
            {
                UnsafeNativeMethods.IDBInfo dbInfo = wrapper.Value;
                // TODO-NULLABLE: check may not be necessary (and thus method may return non-nullable)
                if (dbInfo == null)
                {
                    return null;
                }

                DataTable table = new DataTable("DbInfoLiterals");
                table.Locale = CultureInfo.InvariantCulture;
                DataColumn literalName = new DataColumn("LiteralName", typeof(string));
                DataColumn literalValue = new DataColumn("LiteralValue", typeof(string));
                DataColumn invalidChars = new DataColumn("InvalidChars", typeof(string));
                DataColumn invalidStart = new DataColumn("InvalidStartingChars", typeof(string));
                DataColumn literal = new DataColumn("Literal", typeof(int));
                DataColumn maxlen = new DataColumn("Maxlen", typeof(int));

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
                    if (hr != OleDbHResult.DB_E_ERRORSOCCURRED)
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

        internal DataTable? BuildInfoKeywords()
        {
            DataTable? table = new DataTable(ODB.DbInfoKeywords);
            table.Locale = CultureInfo.InvariantCulture;
            DataColumn keyword = new DataColumn(ODB.Keyword, typeof(string));
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
                if (dbInfo == null)
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

                if (keywords != null)
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
            DataColumn restrictionSupport = new DataColumn(ODB.RestrictionSupport, typeof(int));

            table.Columns.Add(schemaGuid);
            table.Columns.Add(restrictionSupport);

            SchemaSupport[]? supportedSchemas = GetSchemaRowsetInformation();

            if (supportedSchemas != null)
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

        internal string? GetLiteralInfo(int literal)
        {
            using (IDBInfoWrapper wrapper = IDBInfo())
            {
                UnsafeNativeMethods.IDBInfo dbInfo = wrapper.Value;
                if (dbInfo == null)
                {
                    return null;
                }
                string? literalValue = null;
                IntPtr literalInfo = ADP.PtrZero;
                int literalCount = 0;
                OleDbHResult hr;

                using (DualCoTaskMem handle = new DualCoTaskMem(dbInfo, new int[1] { literal }, out literalCount, out literalInfo, out hr))
                {
                    // All literals were either invalid or unsupported. The provider allocates memory for *prgLiteralInfo and sets the value of the fSupported element in all of the structures to FALSE. The consumer frees this memory when it no longer needs the information.
                    if (hr != OleDbHResult.DB_E_ERRORSOCCURRED)
                    {
                        if ((literalCount == 1) && Marshal.ReadInt32(literalInfo, ODB.OffsetOf_tagDBLITERALINFO_it) == literal)
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

        internal SchemaSupport[]? GetSchemaRowsetInformation()
        {
            OleDbConnectionString constr = ConnectionString;
            SchemaSupport[]? supportedSchemas = constr.SchemaSupport;
            if (supportedSchemas != null)
            {
                return supportedSchemas;
            }
            using (IDBSchemaRowsetWrapper wrapper = IDBSchemaRowset())
            {
                UnsafeNativeMethods.IDBSchemaRowset? dbSchemaRowset = wrapper.Value;
                if (dbSchemaRowset == null)
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
                    if (schemaGuids != ADP.PtrZero)
                    {
                        for (int i = 0, offset = 0; i < supportedSchemas.Length; ++i, offset += ODB.SizeOf_Guid)
                        {
                            IntPtr ptr = ADP.IntPtrOffset(schemaGuids, i * ODB.SizeOf_Guid);
                            supportedSchemas[i]._schemaRowset = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid))!;
                        }
                    }
                    if (schemaRestrictions != ADP.PtrZero)
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

        internal DataTable? GetSchemaRowset(Guid schema, object?[]? restrictions)
        {
            if (restrictions == null)
            {
                restrictions = Array.Empty<object>();
            }
            DataTable? dataTable = null;
            using (IDBSchemaRowsetWrapper wrapper = IDBSchemaRowset())
            {
                UnsafeNativeMethods.IDBSchemaRowset dbSchemaRowset = wrapper.Value;
                if (dbSchemaRowset == null)
                {
                    throw ODB.SchemaRowsetsNotSupported(Provider);
                }

                UnsafeNativeMethods.IRowset? rowset = null;
                OleDbHResult hr;
                hr = dbSchemaRowset.GetRowset(ADP.PtrZero, ref schema, restrictions.Length, restrictions, ref ODB.IID_IRowset, 0, ADP.PtrZero, out rowset);

                if (hr < 0)
                { // ignore infomsg
                    ProcessResults(hr);
                }

                if (rowset != null)
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
            OleDbDataReader? reader = null;

            if (ReferenceCollection != null)
            {
                reader = ReferenceCollection.FindItem<OleDbDataReader>(OleDbReferenceCollection.DataReaderTag, (dataReader) => cmd == dataReader.Command);
            }

            return (reader != null);
        }

        private void ProcessResults(OleDbHResult hr)
        {
            OleDbConnection? connection = Connection; // get value from weakref only once
            Exception? e = OleDbConnection.ProcessResults(hr, connection, connection);
            if (e != null)
            { throw e; }
        }

        internal bool SupportSchemaRowset(Guid schema)
        {
            SchemaSupport[]? schemaSupport = GetSchemaRowsetInformation();
            if (schemaSupport != null)
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

        private static object CreateInstanceDataLinks()
        {
            Type datalink = Type.GetTypeFromCLSID(ODB.CLSID_DataLinks, true)!;
            return Activator.CreateInstance(datalink, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, null, CultureInfo.InvariantCulture, null)!;
        }

        // @devnote: should be multithread safe access to OleDbConnection.idataInitialize,
        // though last one wins for setting variable.  It may be different objects, but
        // OLE DB will ensure I'll work with just the single pool
        private static OleDbServicesWrapper GetObjectPool()
        {
            OleDbServicesWrapper? wrapper = OleDbConnectionInternal.idataInitialize;
            if (wrapper == null)
            {
                lock (dataInitializeLock)
                {
                    wrapper = OleDbConnectionInternal.idataInitialize;
                    if (wrapper == null)
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
                        if (datalinks == null)
                        {
                            throw ODB.MDACNotAvailable(null);
                        }
                        wrapper = new OleDbServicesWrapper(datalinks);
                        OleDbConnectionInternal.idataInitialize = wrapper;
                    }
                }
            }
            Debug.Assert(wrapper != null, "GetObjectPool: null dataInitialize");
            return wrapper;
        }

        private static void VersionCheck()
        {
            // $REVIEW: do we still need this?
            // if ApartmentUnknown, then CoInitialize may not have been called yet
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.Unknown)
            {
                SetMTAApartmentState();
            }

            ADP.CheckVersionMDAC(false);
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void SetMTAApartmentState()
        {
            // we are defaulting to a multithread apartment state
            Thread.CurrentThread.SetApartmentState(ApartmentState.MTA);
        }

        // @devnote: should be multithread safe
        public static void ReleaseObjectPool()
        {
            OleDbConnectionInternal.idataInitialize = null;
        }

        internal OleDbTransaction? ValidateTransaction(OleDbTransaction? transaction, string method)
        {
            if (this.weakTransaction != null)
            {
                OleDbTransaction? head = (OleDbTransaction?)this.weakTransaction.Target;
                if ((head != null) && this.weakTransaction.IsAlive)
                {
                    head = OleDbTransaction.TransactionUpdate(head);

                    // either we are wrong or finalize was called and object still alive
                    Debug.Assert(head != null, "unexcpted Transaction state");
                }
                // else transaction has finalized on user

                if (head != null)
                {
                    if (transaction == null)
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
            else if ((transaction != null) && (transaction.Connection != null))
            {
                throw ADP.TransactionConnectionMismatch();
            }
            // else no transaction and cmd is correct

            // if transactionObject is from this connection but zombied
            // and no transactions currently exists - then ignore the bogus object
            return null;
        }

        internal Dictionary<string, OleDbPropertyInfo>? GetPropertyInfo(Guid[] propertySets)
        {
            Dictionary<string, OleDbPropertyInfo>? properties = null;

            if (propertySets == null)
            {
                propertySets = Array.Empty<Guid>();
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

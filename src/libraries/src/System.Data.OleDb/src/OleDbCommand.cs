// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Data.OleDb
{
    public sealed class OleDbCommand : DbCommand, ICloneable, IDbCommand
    {
        // command data
        private string _commandText;
        private CommandType _commandType;
        private int _commandTimeout = ADP.DefaultCommandTimeout;
        private UpdateRowSource _updatedRowSource = UpdateRowSource.Both;
        private bool _designTimeInvisible;
        private OleDbConnection _connection;
        private OleDbTransaction _transaction;

        private OleDbParameterCollection _parameters;

        // native information
        private UnsafeNativeMethods.ICommandText _icommandText;

        // if executing with a different CommandBehavior.KeyInfo behavior
        // original ICommandText must be released and a new ICommandText generated
        private CommandBehavior commandBehavior;

        private Bindings _dbBindings;

        internal bool canceling;
        private bool _isPrepared;
        private bool _executeQuery;
        private bool _trackingForClose;
        private bool _hasDataReader;

        private IntPtr _recordsAffected;
        private int _changeID;
        private int _lastChangeID;

        public OleDbCommand() : base()
        {
            GC.SuppressFinalize(this);
        }

        public OleDbCommand(string cmdText) : this()
        {
            CommandText = cmdText;
        }

        public OleDbCommand(string cmdText, OleDbConnection connection) : this()
        {
            CommandText = cmdText;
            Connection = connection;
        }

        public OleDbCommand(string cmdText, OleDbConnection connection, OleDbTransaction transaction) : this()
        {
            CommandText = cmdText;
            Connection = connection;
            Transaction = transaction;
        }

        private OleDbCommand(OleDbCommand from) : this()
        { // Clone
            CommandText = from.CommandText;
            CommandTimeout = from.CommandTimeout;
            CommandType = from.CommandType;
            Connection = from.Connection;
            DesignTimeVisible = from.DesignTimeVisible;
            UpdatedRowSource = from.UpdatedRowSource;
            Transaction = from.Transaction;

            OleDbParameterCollection parameters = Parameters;
            foreach (object parameter in from.Parameters)
            {
                parameters.Add((parameter is ICloneable) ? (parameter as ICloneable).Clone() : parameter);
            }
        }

        private Bindings ParameterBindings
        {
            get
            {
                return _dbBindings;
            }
            set
            {
                Bindings bindings = _dbBindings;
                _dbBindings = value;
                if ((null != bindings) && (value != bindings))
                {
                    bindings.Dispose();
                }
            }
        }

        [DefaultValue("")]
        [RefreshProperties(RefreshProperties.All)]
        override public string CommandText
        {
            get
            {
                string value = _commandText;
                return ((null != value) ? value : string.Empty);
            }
            set
            {
                if (0 != ADP.SrcCompare(_commandText, value))
                {
                    PropertyChanging();
                    _commandText = value;
                }
            }
        }

        override public int CommandTimeout
        { // V1.2.3300, XXXCommand V1.0.5000
            get
            {
                return _commandTimeout;
            }
            set
            {
                if (value < 0)
                {
                    throw ADP.InvalidCommandTimeout(value);
                }
                if (value != _commandTimeout)
                {
                    PropertyChanging();
                    _commandTimeout = value;
                }
            }
        }

        public void ResetCommandTimeout()
        { // V1.2.3300
            if (ADP.DefaultCommandTimeout != _commandTimeout)
            {
                PropertyChanging();
                _commandTimeout = ADP.DefaultCommandTimeout;
            }
        }

        [DefaultValue(System.Data.CommandType.Text)]
        [RefreshProperties(RefreshProperties.All)]
        override public CommandType CommandType
        {
            get
            {
                CommandType cmdType = _commandType;
                return ((0 != cmdType) ? cmdType : CommandType.Text);
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case CommandType.Text:
                    case CommandType.StoredProcedure:
                    case CommandType.TableDirect:
                        PropertyChanging();
                        _commandType = value;
                        break;
                    default:
                        throw ADP.InvalidCommandType(value);
                }
            }
        }

        [DefaultValue(null)]
        new public OleDbConnection Connection
        {
            get
            {
                return _connection;
            }
            set
            {
                OleDbConnection connection = _connection;
                if (value != connection)
                {
                    PropertyChanging();
                    ResetConnection();

                    _connection = value;

                    if (null != value)
                    {
                        _transaction = OleDbTransaction.TransactionUpdate(_transaction);
                    }
                }
            }
        }

        private void ResetConnection()
        {
            OleDbConnection connection = _connection;
            if (null != connection)
            {
                PropertyChanging();
                CloseInternal();
                if (_trackingForClose)
                {
                    connection.RemoveWeakReference(this);
                    _trackingForClose = false;
                }
            }
            _connection = null;
        }

        override protected DbConnection DbConnection
        { // V1.2.3300
            get
            {
                return Connection;
            }
            set
            {
                Connection = (OleDbConnection)value;
            }
        }

        override protected DbParameterCollection DbParameterCollection
        { // V1.2.3300
            get
            {
                return Parameters;
            }
        }

        override protected DbTransaction DbTransaction
        { // V1.2.3300
            get
            {
                return Transaction;
            }
            set
            {
                Transaction = (OleDbTransaction)value;
            }
        }

        // @devnote: By default, the cmd object is visible on the design surface (i.e. VS7 Server Tray)
        // to limit the number of components that clutter the design surface,
        // when the DataAdapter design wizard generates the insert/update/delete commands it will
        // set the DesignTimeVisible property to false so that cmds won't appear as individual objects
        [
        DefaultValue(true),
        DesignOnly(true),
        Browsable(false),
        EditorBrowsable(EditorBrowsableState.Never),
        ]
        public override bool DesignTimeVisible
        { // V1.2.3300, XXXCommand V1.0.5000
            get
            {
                return !_designTimeInvisible;
            }
            set
            {
                _designTimeInvisible = !value;
                TypeDescriptor.Refresh(this);
            }
        }

        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content)
        ]
        new public OleDbParameterCollection Parameters
        {
            get
            {
                OleDbParameterCollection value = _parameters;
                if (null == value)
                {
                    // delay the creation of the OleDbParameterCollection
                    // until user actually uses the Parameters property
                    value = new OleDbParameterCollection();
                    _parameters = value;
                }
                return value;
            }
        }

        private bool HasParameters()
        {
            OleDbParameterCollection value = _parameters;
            return (null != value) && (0 < value.Count);
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        new public OleDbTransaction Transaction
        {
            get
            {
                // find the last non-zombied local transaction object, but not transactions
                // that may have been started after the current local transaction
                OleDbTransaction transaction = _transaction;
                while ((null != transaction) && (null == transaction.Connection))
                {
                    transaction = transaction.Parent;
                    _transaction = transaction;
                }
                return transaction;
            }
            set
            {
                _transaction = value;
            }
        }

        [
        DefaultValue(System.Data.UpdateRowSource.Both)
        ]
        override public UpdateRowSource UpdatedRowSource
        { // V1.2.3300, XXXCommand V1.0.5000
            get
            {
                return _updatedRowSource;
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case UpdateRowSource.None:
                    case UpdateRowSource.OutputParameters:
                    case UpdateRowSource.FirstReturnedRecord:
                    case UpdateRowSource.Both:
                        _updatedRowSource = value;
                        break;
                    default:
                        throw ADP.InvalidUpdateRowSource(value);
                }
            }
        }

        // required interface, safe cast
        private UnsafeNativeMethods.IAccessor IAccessor()
        {
            Debug.Assert(null != _icommandText, "IAccessor: null ICommandText");
            return (UnsafeNativeMethods.IAccessor)_icommandText;
        }

        // required interface, safe cast
        internal UnsafeNativeMethods.ICommandProperties ICommandProperties()
        {
            Debug.Assert(null != _icommandText, "ICommandProperties: null ICommandText");
            return (UnsafeNativeMethods.ICommandProperties)_icommandText;
        }

        // optional interface, unsafe cast
        private UnsafeNativeMethods.ICommandPrepare ICommandPrepare()
        {
            Debug.Assert(null != _icommandText, "ICommandPrepare: null ICommandText");
            return (_icommandText as UnsafeNativeMethods.ICommandPrepare);
        }

        // optional interface, unsafe cast
        private UnsafeNativeMethods.ICommandWithParameters ICommandWithParameters()
        {
            Debug.Assert(null != _icommandText, "ICommandWithParameters: null ICommandText");
            UnsafeNativeMethods.ICommandWithParameters value = (_icommandText as UnsafeNativeMethods.ICommandWithParameters);
            if (null == value)
            {
                throw ODB.NoProviderSupportForParameters(_connection.Provider, (Exception)null);
            }
            return value;
        }

        private void CreateAccessor()
        {
            Debug.Assert(System.Data.CommandType.Text == CommandType || System.Data.CommandType.StoredProcedure == CommandType, "CreateAccessor: incorrect CommandType");
            Debug.Assert(null == _dbBindings, "CreateAccessor: already has dbBindings");
            Debug.Assert(HasParameters(), "CreateAccessor: unexpected, no parameter collection");

            // do this first in-case the command doesn't support parameters
            UnsafeNativeMethods.ICommandWithParameters commandWithParameters = ICommandWithParameters();

            OleDbParameterCollection collection = _parameters;
            OleDbParameter[] parameters = new OleDbParameter[collection.Count];
            collection.CopyTo(parameters, 0);

            // _dbBindings is used as a switch during ExecuteCommand, so don't set it until everything okay
            Bindings bindings = new Bindings(parameters, collection.ChangeID);

            for (int i = 0; i < parameters.Length; ++i)
            {
                bindings.ForceRebind |= parameters[i].BindParameter(i, bindings);
            }

            bindings.AllocateForAccessor(null, 0, 0);

            ApplyParameterBindings(commandWithParameters, bindings.BindInfo);

            UnsafeNativeMethods.IAccessor iaccessor = IAccessor();
            OleDbHResult hr = bindings.CreateAccessor(iaccessor, ODB.DBACCESSOR_PARAMETERDATA);
            if (hr < 0)
            {
                ProcessResults(hr);
            }
            _dbBindings = bindings;
        }

        private void ApplyParameterBindings(UnsafeNativeMethods.ICommandWithParameters commandWithParameters, tagDBPARAMBINDINFO[] bindInfo)
        {
            IntPtr[] ordinals = new IntPtr[bindInfo.Length];
            for (int i = 0; i < ordinals.Length; ++i)
            {
                ordinals[i] = (IntPtr)(i + 1);
            }
            OleDbHResult hr = commandWithParameters.SetParameterInfo((IntPtr)bindInfo.Length, ordinals, bindInfo);

            if (hr < 0)
            {
                ProcessResults(hr);
            }
        }

        override public void Cancel()
        {
            unchecked
            { _changeID++; }

            UnsafeNativeMethods.ICommandText icmdtxt = _icommandText;
            if (null != icmdtxt)
            {
                OleDbHResult hr = OleDbHResult.S_OK;

                lock (icmdtxt)
                {
                    // lock the object to avoid race conditions between using the object and releasing the object
                    // after we acquire the lock, if the class has moved on don't actually call Cancel
                    if (icmdtxt == _icommandText)
                    {
                        hr = icmdtxt.Cancel();
                    }
                }
                if (OleDbHResult.DB_E_CANTCANCEL != hr)
                {
                    // if the provider can't cancel the command - don't cancel the DataReader
                    this.canceling = true;
                }

                // since cancel is allowed to occur at anytime we can't check the connection status
                // since if it returns as closed then the connection will close causing the reader to close
                // and that would introduce the possilbility of one thread reading and one thread closing at the same time
                ProcessResultsNoReset(hr);
            }
            else
            {
                this.canceling = true;
            }
        }

        public OleDbCommand Clone()
        {
            OleDbCommand clone = new OleDbCommand(this);
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        // Connection.Close & Connection.Dispose(true) notification
        internal void CloseCommandFromConnection(bool canceling)
        {
            this.canceling = canceling;
            CloseInternal();
            _trackingForClose = false;
            _transaction = null;
            //GC.SuppressFinalize(this);
        }

        internal void CloseInternal()
        {
            Debug.Assert(null != _connection, "no connection, CloseInternal");
            CloseInternalParameters();
            CloseInternalCommand();
        }

        // may be called from either
        //      OleDbDataReader.Close/Dispose
        //      via OleDbCommand.Dispose or OleDbConnection.Close
        internal void CloseFromDataReader(Bindings bindings)
        {
            if (null != bindings)
            {
                if (canceling)
                {
                    bindings.Dispose();
                    Debug.Assert(_dbBindings == bindings, "bindings with two owners");
                }
                else
                {
                    bindings.ApplyOutputParameters();
                    ParameterBindings = bindings;
                }
            }
            _hasDataReader = false;
        }

        private void CloseInternalCommand()
        {
            unchecked
            { _changeID++; }
            this.commandBehavior = CommandBehavior.Default;
            _isPrepared = false;

            UnsafeNativeMethods.ICommandText ict = Interlocked.Exchange<UnsafeNativeMethods.ICommandText>(ref _icommandText, null);
            if (null != ict)
            {
                lock (ict)
                {
                    // lock the object to avoid race conditions between using the object and releasing the object
                    Marshal.ReleaseComObject(ict);
                }
            }
        }
        private void CloseInternalParameters()
        {
            Debug.Assert(null != _connection, "no connection, CloseInternalParameters");
            Bindings bindings = _dbBindings;
            _dbBindings = null;
            if (null != bindings)
            {
                bindings.Dispose();
            }
        }

        new public OleDbParameter CreateParameter()
        {
            return new OleDbParameter();
        }

        override protected DbParameter CreateDbParameter()
        {
            return CreateParameter();
        }

        override protected void Dispose(bool disposing)
        {
            if (disposing)
            { // release mananged objects
                // the DataReader takes ownership of the parameter Bindings
                // this way they don't get destroyed when user calls OleDbCommand.Dispose
                // when there is an open DataReader

                unchecked
                { _changeID++; }

                // in V1.0, V1.1 the Connection,Parameters,CommandText,Transaction where reset
                ResetConnection();
                _transaction = null;
                _parameters = null;
                CommandText = null;
            }
            // release unmanaged objects
            base.Dispose(disposing); // notify base classes
        }

        new public OleDbDataReader ExecuteReader()
        {
            return ExecuteReader(CommandBehavior.Default);
        }

        IDataReader IDbCommand.ExecuteReader()
        {
            return ExecuteReader(CommandBehavior.Default);
        }

        new public OleDbDataReader ExecuteReader(CommandBehavior behavior)
        {
            _executeQuery = true;
            return ExecuteReaderInternal(behavior, ADP.ExecuteReader);
        }

        IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReader(behavior);
        }

        override protected DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteReader(behavior);
        }

        private OleDbDataReader ExecuteReaderInternal(CommandBehavior behavior, string method)
        {
            OleDbDataReader dataReader = null;
            OleDbException nextResultsFailure = null;
            int state = ODB.InternalStateClosed;
            try
            {
                ValidateConnectionAndTransaction(method);

                if (0 != (CommandBehavior.SingleRow & behavior))
                {
                    // CommandBehavior.SingleRow implies CommandBehavior.SingleResult
                    behavior |= CommandBehavior.SingleResult;
                }

                object executeResult;
                int resultType;

                switch (CommandType)
                {
                    case 0: // uninitialized CommandType.Text
                    case CommandType.Text:
                    case CommandType.StoredProcedure:
                        resultType = ExecuteCommand(behavior, out executeResult);
                        break;

                    case CommandType.TableDirect:
                        resultType = ExecuteTableDirect(behavior, out executeResult);
                        break;

                    default:
                        throw ADP.InvalidCommandType(CommandType);
                }

                if (_executeQuery)
                {
                    try
                    {
                        dataReader = new OleDbDataReader(_connection, this, 0, this.commandBehavior);

                        switch (resultType)
                        {
                            case ODB.ExecutedIMultipleResults:
                                dataReader.InitializeIMultipleResults(executeResult);
                                dataReader.NextResult();
                                break;
                            case ODB.ExecutedIRowset:
                                dataReader.InitializeIRowset(executeResult, ChapterHandle.DB_NULL_HCHAPTER, _recordsAffected);
                                dataReader.BuildMetaInfo();
                                dataReader.HasRowsRead();
                                break;
                            case ODB.ExecutedIRow:
                                dataReader.InitializeIRow(executeResult, _recordsAffected);
                                dataReader.BuildMetaInfo();
                                break;
                            case ODB.PrepareICommandText:
                                if (!_isPrepared)
                                {
                                    PrepareCommandText(2);
                                }
                                OleDbDataReader.GenerateSchemaTable(dataReader, _icommandText, behavior);
                                break;
                            default:
                                Debug.Assert(false, "ExecuteReaderInternal: unknown result type");
                                break;
                        }
                        executeResult = null;
                        _hasDataReader = true;
                        _connection.AddWeakReference(dataReader, OleDbReferenceCollection.DataReaderTag);

                        // command stays in the executing state until the connection
                        // has a datareader to track for it being closed
                        state = ODB.InternalStateOpen;
                    }
                    finally
                    {
                        if (ODB.InternalStateOpen != state)
                        {
                            this.canceling = true;
                            if (null != dataReader)
                            {
                                ((IDisposable)dataReader).Dispose();
                                dataReader = null;
                            }
                        }
                    }
                    Debug.Assert(null != dataReader, "ExecuteReader should never return a null DataReader");
                }
                else
                { // optimized code path for ExecuteNonQuery to not create a OleDbDataReader object
                    try
                    {
                        if (ODB.ExecutedIMultipleResults == resultType)
                        {
                            UnsafeNativeMethods.IMultipleResults multipleResults = (UnsafeNativeMethods.IMultipleResults)executeResult;

                            // may cause a Connection.ResetState which closes connection
                            nextResultsFailure = OleDbDataReader.NextResults(multipleResults, _connection, this, out _recordsAffected);
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (null != executeResult)
                            {
                                Marshal.ReleaseComObject(executeResult);
                                executeResult = null;
                            }
                            CloseFromDataReader(ParameterBindings);
                        }
                        catch (Exception e)
                        {
                            // UNDONE - should not be catching all exceptions!!!
                            if (!ADP.IsCatchableExceptionType(e))
                            {
                                throw;
                            }
                            if (null != nextResultsFailure)
                            {
                                nextResultsFailure = new OleDbException(nextResultsFailure, e);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
            }
            finally
            { // finally clear executing state
                try
                {
                    if ((null == dataReader) && (ODB.InternalStateOpen != state))
                    {
                        ParameterCleanup();
                    }
                }
                catch (Exception e)
                {
                    // UNDONE - should not be catching all exceptions!!!
                    if (!ADP.IsCatchableExceptionType(e))
                    {
                        throw;
                    }
                    if (null != nextResultsFailure)
                    {
                        nextResultsFailure = new OleDbException(nextResultsFailure, e);
                    }
                    else
                    {
                        throw;
                    }
                }
                if (null != nextResultsFailure)
                {
                    throw nextResultsFailure;
                }
            }
            return dataReader;
        }

        private int ExecuteCommand(CommandBehavior behavior, out object executeResult)
        {
            if (InitializeCommand(behavior, false))
            {
                if (0 != (CommandBehavior.SchemaOnly & this.commandBehavior))
                {
                    executeResult = null;
                    return ODB.PrepareICommandText;
                }
                return ExecuteCommandText(out executeResult);
            }
            return ExecuteTableDirect(behavior, out executeResult);
        }

        // dbindings handle can't be freed until the output parameters
        // have been filled in which occurs after the last rowset is released
        // dbbindings.FreeDataHandle occurs in Cloe
        private int ExecuteCommandText(out object executeResult)
        {
            int retcode;
            tagDBPARAMS dbParams = null;
            RowBinding rowbinding = null;
            Bindings bindings = ParameterBindings;
            bool mustRelease = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (null != bindings)
                { // parameters may be suppressed
                    rowbinding = bindings.RowBinding();

                    rowbinding.DangerousAddRef(ref mustRelease);

                    // bindings can't be released until after last rowset is released
                    // that is when output parameters are populated
                    // initialize the input parameters to the input databuffer
                    bindings.ApplyInputParameters();

                    dbParams = new tagDBPARAMS();
                    dbParams.pData = rowbinding.DangerousGetDataPtr();
                    dbParams.cParamSets = 1;
                    dbParams.hAccessor = rowbinding.DangerousGetAccessorHandle();
                }
                if ((0 == (CommandBehavior.SingleResult & this.commandBehavior)) && _connection.SupportMultipleResults())
                {
                    retcode = ExecuteCommandTextForMultpleResults(dbParams, out executeResult);
                }
                else if (0 == (CommandBehavior.SingleRow & this.commandBehavior) || !_executeQuery)
                {
                    retcode = ExecuteCommandTextForSingleResult(dbParams, out executeResult);
                }
                else
                {
                    retcode = ExecuteCommandTextForSingleRow(dbParams, out executeResult);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    rowbinding.DangerousRelease();
                }
            }
            return retcode;
        }

        private int ExecuteCommandTextForMultpleResults(tagDBPARAMS dbParams, out object executeResult)
        {
            Debug.Assert(0 == (CommandBehavior.SingleRow & this.commandBehavior), "SingleRow implies SingleResult");
            OleDbHResult hr;
            hr = _icommandText.Execute(ADP.PtrZero, ref ODB.IID_IMultipleResults, dbParams, out _recordsAffected, out executeResult);

            if (OleDbHResult.E_NOINTERFACE != hr)
            {
                ExecuteCommandTextErrorHandling(hr);
                return ODB.ExecutedIMultipleResults;
            }
            SafeNativeMethods.Wrapper.ClearErrorInfo();
            return ExecuteCommandTextForSingleResult(dbParams, out executeResult);
        }

        private int ExecuteCommandTextForSingleResult(tagDBPARAMS dbParams, out object executeResult)
        {
            OleDbHResult hr;

            // (Microsoft.Jet.OLEDB.4.0 returns 0 for recordsAffected instead of -1)
            if (_executeQuery)
            {
                hr = _icommandText.Execute(ADP.PtrZero, ref ODB.IID_IRowset, dbParams, out _recordsAffected, out executeResult);
            }
            else
            {
                hr = _icommandText.Execute(ADP.PtrZero, ref ODB.IID_NULL, dbParams, out _recordsAffected, out executeResult);
            }
            ExecuteCommandTextErrorHandling(hr);
            return ODB.ExecutedIRowset;
        }

        private int ExecuteCommandTextForSingleRow(tagDBPARAMS dbParams, out object executeResult)
        {
            Debug.Assert(_executeQuery, "ExecuteNonQuery should always use ExecuteCommandTextForSingleResult");

            if (_connection.SupportIRow(this))
            {
                OleDbHResult hr;
                hr = _icommandText.Execute(ADP.PtrZero, ref ODB.IID_IRow, dbParams, out _recordsAffected, out executeResult);

                if (OleDbHResult.DB_E_NOTFOUND == hr)
                {
                    SafeNativeMethods.Wrapper.ClearErrorInfo();
                    return ODB.ExecutedIRow;
                }
                else if (OleDbHResult.E_NOINTERFACE != hr)
                {
                    ExecuteCommandTextErrorHandling(hr);
                    return ODB.ExecutedIRow;
                }
            }
            SafeNativeMethods.Wrapper.ClearErrorInfo();
            return ExecuteCommandTextForSingleResult(dbParams, out executeResult);
        }

        private void ExecuteCommandTextErrorHandling(OleDbHResult hr)
        {
            Exception e = OleDbConnection.ProcessResults(hr, _connection, this);
            if (null != e)
            {
                e = ExecuteCommandTextSpecialErrorHandling(hr, e);
                throw e;
            }
        }

        private Exception ExecuteCommandTextSpecialErrorHandling(OleDbHResult hr, Exception e)
        {
            if (((OleDbHResult.DB_E_ERRORSOCCURRED == hr) || (OleDbHResult.DB_E_BADBINDINFO == hr)) && (null != _dbBindings))
            {
                //
                // this code exist to try for a better user error message by post-morten detection
                // of invalid parameter types being passed to a provider that doesn't understand
                // the user specified parameter OleDbType

                Debug.Assert(null != e, "missing inner exception");

                StringBuilder builder = new StringBuilder();
                ParameterBindings.ParameterStatus(builder);
                e = ODB.CommandParameterStatus(builder.ToString(), e);
            }
            return e;
        }

        override public int ExecuteNonQuery()
        {
            _executeQuery = false;
            ExecuteReaderInternal(CommandBehavior.Default, ADP.ExecuteNonQuery);
            return ADP.IntPtrToInt32(_recordsAffected);
        }

        override public object ExecuteScalar()
        {
            object value = null;
            _executeQuery = true;
            using (OleDbDataReader reader = ExecuteReaderInternal(CommandBehavior.Default, ADP.ExecuteScalar))
            {
                if (reader.Read() && (0 < reader.FieldCount))
                {
                    value = reader.GetValue(0);
                }
            }
            return value;
        }

        private int ExecuteTableDirect(CommandBehavior behavior, out object executeResult)
        {
            this.commandBehavior = behavior;
            executeResult = null;

            OleDbHResult hr = OleDbHResult.S_OK;

            StringMemHandle sptr = null;
            bool mustReleaseStringHandle = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                sptr = new StringMemHandle(ExpandCommandText());

                sptr.DangerousAddRef(ref mustReleaseStringHandle);

                if (mustReleaseStringHandle)
                {
                    tagDBID tableID = new tagDBID();
                    tableID.uGuid = Guid.Empty;
                    tableID.eKind = ODB.DBKIND_NAME;
                    tableID.ulPropid = sptr.DangerousGetHandle();

                    using (IOpenRowsetWrapper iopenRowset = _connection.IOpenRowset())
                    {
                        using (DBPropSet propSet = CommandPropertySets())
                        {
                            if (null != propSet)
                            {
                                bool mustRelease = false;
                                RuntimeHelpers.PrepareConstrainedRegions();
                                try
                                {
                                    propSet.DangerousAddRef(ref mustRelease);
                                    hr = iopenRowset.Value.OpenRowset(ADP.PtrZero, tableID, ADP.PtrZero, ref ODB.IID_IRowset, propSet.PropertySetCount, propSet.DangerousGetHandle(), out executeResult);
                                }
                                finally
                                {
                                    if (mustRelease)
                                    {
                                        propSet.DangerousRelease();
                                    }
                                }

                                if (OleDbHResult.DB_E_ERRORSOCCURRED == hr)
                                {
                                    hr = iopenRowset.Value.OpenRowset(ADP.PtrZero, tableID, ADP.PtrZero, ref ODB.IID_IRowset, 0, IntPtr.Zero, out executeResult);
                                }
                            }
                            else
                            {
                                hr = iopenRowset.Value.OpenRowset(ADP.PtrZero, tableID, ADP.PtrZero, ref ODB.IID_IRowset, 0, IntPtr.Zero, out executeResult);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (mustReleaseStringHandle)
                {
                    sptr.DangerousRelease();
                }
            }
            ProcessResults(hr);
            _recordsAffected = ADP.RecordsUnaffected;
            return ODB.ExecutedIRowset;
        }

        private string ExpandCommandText()
        {
            string cmdtxt = CommandText;
            if (ADP.IsEmpty(cmdtxt))
            {
                return string.Empty;
            }
            CommandType cmdtype = CommandType;
            switch (cmdtype)
            {
                case System.Data.CommandType.Text:
                    // do nothing, already expanded by user
                    return cmdtxt;

                case System.Data.CommandType.StoredProcedure:
                    // { ? = CALL SPROC (? ?) }, { ? = CALL SPROC }, { CALL SPRC (? ?) }, { CALL SPROC }
                    return ExpandStoredProcedureToText(cmdtxt);

                case System.Data.CommandType.TableDirect:
                    // @devnote: Provider=Jolt4.0 doesn't like quoted table names, SQOLEDB requires them
                    // Providers should not require table names to be quoted and should guarantee that
                    // unquoted table names correctly open the specified table, even if the table name
                    // contains special characters, as long as the table can be unambiguously identified
                    // without quoting.
                    return cmdtxt;

                default:
                    throw ADP.InvalidCommandType(cmdtype);
            }
        }

        private string ExpandOdbcMaximumToText(string sproctext, int parameterCount)
        {
            StringBuilder builder = new StringBuilder();
            if ((0 < parameterCount) && (ParameterDirection.ReturnValue == Parameters[0].Direction))
            {
                parameterCount--;
                builder.Append("{ ? = CALL ");
            }
            else
            {
                builder.Append("{ CALL ");
            }
            builder.Append(sproctext);

            switch (parameterCount)
            {
                case 0:
                    builder.Append(" }");
                    break;
                case 1:
                    builder.Append("( ? ) }");
                    break;
                default:
                    builder.Append("( ?, ?");
                    for (int i = 2; i < parameterCount; ++i)
                    {
                        builder.Append(", ?");
                    }
                    builder.Append(" ) }");
                    break;
            }
            return builder.ToString();
        }

        private string ExpandOdbcMinimumToText(string sproctext, int parameterCount)
        {
            //if ((0 < parameterCount) && (ParameterDirection.ReturnValue == Parameters[0].Direction)) {
            //    Debug.Assert("doesn't support ReturnValue parameters");
            //}
            StringBuilder builder = new StringBuilder();
            builder.Append("exec ");
            builder.Append(sproctext);
            if (0 < parameterCount)
            {
                builder.Append(" ?");
                for (int i = 1; i < parameterCount; ++i)
                {
                    builder.Append(", ?");
                }
            }
            return builder.ToString();
        }

        private string ExpandStoredProcedureToText(string sproctext)
        {
            Debug.Assert(null != _connection, "ExpandStoredProcedureToText: null Connection");

            int parameterCount = (null != _parameters) ? _parameters.Count : 0;
            if (0 == (ODB.DBPROPVAL_SQL_ODBC_MINIMUM & _connection.SqlSupport()))
            {
                return ExpandOdbcMinimumToText(sproctext, parameterCount);
            }
            return ExpandOdbcMaximumToText(sproctext, parameterCount);
        }

        private void ParameterCleanup()
        {
            Bindings bindings = ParameterBindings;
            if (null != bindings)
            {
                bindings.CleanupBindings();
            }
        }

        private bool InitializeCommand(CommandBehavior behavior, bool throwifnotsupported)
        {
            Debug.Assert(null != _connection, "InitializeCommand: null OleDbConnection");

            int changeid = _changeID;
            if ((0 != (CommandBehavior.KeyInfo & (this.commandBehavior ^ behavior))) || (_lastChangeID != changeid))
            {
                CloseInternalParameters(); // could optimize out
                CloseInternalCommand();
            }
            this.commandBehavior = behavior;
            changeid = _changeID;

            if (!PropertiesOnCommand(false))
            {
                return false;
            }

            if ((null != _dbBindings) && _dbBindings.AreParameterBindingsInvalid(_parameters))
            {
                CloseInternalParameters();
            }

            // if we already having bindings - don't create the accessor
            // if _parameters is null - no parameters exist - don't create the collection
            // do we actually have parameters since the collection exists
            if ((null == _dbBindings) && HasParameters())
            {
                // if we setup the parameters before setting cmdtxt then named parameters can happen
                CreateAccessor();
            }

            if (_lastChangeID != changeid)
            {
                OleDbHResult hr;

                String commandText = ExpandCommandText();

                hr = _icommandText.SetCommandText(ref ODB.DBGUID_DEFAULT, commandText);

                if (hr < 0)
                {
                    ProcessResults(hr);
                }
            }

            _lastChangeID = changeid;

            return true;
        }

        private void PropertyChanging()
        {
            unchecked
            { _changeID++; }
        }

        override public void Prepare()
        {
            if (CommandType.TableDirect != CommandType)
            {
                ValidateConnectionAndTransaction(ADP.Prepare);

                _isPrepared = false;
                if (CommandType.TableDirect != CommandType)
                {
                    InitializeCommand(0, true);
                    PrepareCommandText(1);
                }
            }
        }

        private void PrepareCommandText(int expectedExecutionCount)
        {
            OleDbParameterCollection parameters = _parameters;
            if (null != parameters)
            {
                foreach (OleDbParameter parameter in parameters)
                {
                    if (parameter.IsParameterComputed())
                    {
                        // @devnote: use IsParameterComputed which is called in the normal case
                        // only to call Prepare to throw the specialized error message
                        // reducing the overall number of methods to actually jit
                        parameter.Prepare(this);
                    }
                }
            }
            UnsafeNativeMethods.ICommandPrepare icommandPrepare = ICommandPrepare();
            if (null != icommandPrepare)
            {
                OleDbHResult hr;
                hr = icommandPrepare.Prepare(expectedExecutionCount);

                ProcessResults(hr);

            }
            // don't recompute bindings on prepared statements
            _isPrepared = true;
        }

        private void ProcessResults(OleDbHResult hr)
        {
            Exception e = OleDbConnection.ProcessResults(hr, _connection, this);
            if (null != e)
            { throw e; }
        }

        private void ProcessResultsNoReset(OleDbHResult hr)
        {
            Exception e = OleDbConnection.ProcessResults(hr, null, this);
            if (null != e)
            { throw e; }
        }

        internal object GetPropertyValue(Guid propertySet, int propertyID)
        {
            if (null != _icommandText)
            {
                OleDbHResult hr;
                tagDBPROP[] dbprops;
                UnsafeNativeMethods.ICommandProperties icommandProperties = ICommandProperties();

                using (PropertyIDSet propidset = new PropertyIDSet(propertySet, propertyID))
                {
                    using (DBPropSet propset = new DBPropSet(icommandProperties, propidset, out hr))
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
                if (OleDbPropertyStatus.Ok == dbprops[0].dwStatus)
                {
                    return dbprops[0].vValue;
                }
                return dbprops[0].dwStatus;
            }
            return OleDbPropertyStatus.NotSupported;
        }

        private bool PropertiesOnCommand(bool throwNotSupported)
        {
            if (null != _icommandText)
            {
                return true;
            }
            Debug.Assert(!_isPrepared, "null command isPrepared");

            OleDbConnection connection = _connection;
            if (null == connection)
            {
                connection.CheckStateOpen(ODB.Properties);
            }
            if (!_trackingForClose)
            {
                _trackingForClose = true;
                connection.AddWeakReference(this, OleDbReferenceCollection.CommandTag);
            }
            _icommandText = connection.ICommandText();

            if (null == _icommandText)
            {
                if (throwNotSupported || HasParameters())
                {
                    throw ODB.CommandTextNotSupported(connection.Provider, null);
                }
                return false;
            }

            using (DBPropSet propSet = CommandPropertySets())
            {
                if (null != propSet)
                {
                    UnsafeNativeMethods.ICommandProperties icommandProperties = ICommandProperties();
                    OleDbHResult hr = icommandProperties.SetProperties(propSet.PropertySetCount, propSet);

                    if (hr < 0)
                    {
                        SafeNativeMethods.Wrapper.ClearErrorInfo();
                    }
                }
            }
            return true;
        }

        private DBPropSet CommandPropertySets()
        {
            DBPropSet propSet = null;

            bool keyInfo = (0 != (CommandBehavior.KeyInfo & this.commandBehavior));

            // always set the CommandTimeout value?
            int count = (_executeQuery ? (keyInfo ? 4 : 2) : 1);

            if (0 < count)
            {
                propSet = new DBPropSet(1);

                tagDBPROP[] dbprops = new tagDBPROP[count];

                dbprops[0] = new tagDBPROP(ODB.DBPROP_COMMANDTIMEOUT, false, CommandTimeout);

                if (_executeQuery)
                {
                    // 'Microsoft.Jet.OLEDB.4.0' default is DBPROPVAL_AO_SEQUENTIAL
                    dbprops[1] = new tagDBPROP(ODB.DBPROP_ACCESSORDER, false, ODB.DBPROPVAL_AO_RANDOM);

                    if (keyInfo)
                    {
                        // 'Unique Rows' property required for SQLOLEDB to retrieve things like 'BaseTableName'
                        dbprops[2] = new tagDBPROP(ODB.DBPROP_UNIQUEROWS, false, keyInfo);

                        // otherwise 'Microsoft.Jet.OLEDB.4.0' doesn't support IColumnsRowset
                        dbprops[3] = new tagDBPROP(ODB.DBPROP_IColumnsRowset, false, true);
                    }
                }
                propSet.SetPropertySet(0, OleDbPropertySetGuid.Rowset, dbprops);
            }
            return propSet;
        }

        internal Bindings TakeBindingOwnerShip()
        {
            Bindings bindings = _dbBindings;
            _dbBindings = null;
            return bindings;
        }

        private void ValidateConnection(string method)
        {
            if (null == _connection)
            {
                throw ADP.ConnectionRequired(method);
            }
            _connection.CheckStateOpen(method);

            // user attempting to execute the command while the first dataReader hasn't returned
            // use the connection reference collection to see if the dataReader referencing this
            // command has been garbage collected or not.
            if (_hasDataReader)
            {
                if (_connection.HasLiveReader(this))
                {
                    throw ADP.OpenReaderExists();
                }
                _hasDataReader = false;
            }
        }

        private void ValidateConnectionAndTransaction(string method)
        {
            ValidateConnection(method);
            _transaction = _connection.ValidateTransaction(Transaction, method);
            this.canceling = false;
        }
    }
}

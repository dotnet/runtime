// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Collections.Generic;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    public abstract class DbDataAdapter : DataAdapter, IDbDataAdapter, ICloneable
    {
        public const string DefaultSourceTableName = "Table";

        internal static readonly object s_parameterValueNonNullValue = 0;
        internal static readonly object s_parameterValueNullValue = 1;

        private IDbCommand? _deleteCommand, _insertCommand, _selectCommand, _updateCommand;

        private CommandBehavior _fillCommandBehavior;

        private struct BatchCommandInfo
        {
            internal int _commandIdentifier;     // whatever AddToBatch returns, so we can reference the command later in GetBatchedParameter
            internal int _parameterCount;        // number of parameters on the command, so we know how many to loop over when processing output parameters
            internal DataRow _row;                   // the row that the command is intended to update
            internal StatementType _statementType;         // the statement type of the command, needed for accept changes
            internal UpdateRowSource _updatedRowSource;      // the UpdatedRowSource value from the command, to know whether we need to look for output parameters or not
            internal int? _recordsAffected;
            internal Exception? _errors;
        }

        protected DbDataAdapter() : base()
        {
        }

        protected DbDataAdapter(DbDataAdapter adapter) : base(adapter)
        {
            CloneFrom(adapter);
        }

        private IDbDataAdapter _IDbDataAdapter
        {
            get
            {
                return this;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public DbCommand? DeleteCommand
        {
            get
            {
                return (DbCommand?)(_IDbDataAdapter.DeleteCommand);
            }
            set
            {
                _IDbDataAdapter.DeleteCommand = value;
            }
        }

        IDbCommand? IDbDataAdapter.DeleteCommand
        {
            get
            {
                return _deleteCommand;
            }
            set
            {
                _deleteCommand = value;
            }
        }

        protected internal CommandBehavior FillCommandBehavior
        {
            get
            {
                return (_fillCommandBehavior | CommandBehavior.SequentialAccess);
            }
            set
            {
                _fillCommandBehavior = (value | CommandBehavior.SequentialAccess);
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public DbCommand? InsertCommand
        {
            get
            {
                return (DbCommand?)(_IDbDataAdapter.InsertCommand);
            }
            set
            {
                _IDbDataAdapter.InsertCommand = value;
            }
        }

        IDbCommand? IDbDataAdapter.InsertCommand
        {
            get
            {
                return _insertCommand;
            }
            set
            {
                _insertCommand = value;
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public DbCommand? SelectCommand
        {
            get
            {
                return (DbCommand?)(_IDbDataAdapter.SelectCommand);
            }
            set
            {
                _IDbDataAdapter.SelectCommand = value;
            }
        }

        IDbCommand? IDbDataAdapter.SelectCommand
        {
            get
            {
                return _selectCommand;
            }
            set
            {
                _selectCommand = value;
            }
        }

        [DefaultValue(1)]
        public virtual int UpdateBatchSize
        {
            get
            {
                return 1;
            }
            set
            {
                if (value != 1)
                {
                    throw ADP.NotSupported();
                }
            }
        }

        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        ]
        public DbCommand? UpdateCommand
        {
            get
            {
                return (DbCommand?)(_IDbDataAdapter.UpdateCommand);
            }
            set
            {
                _IDbDataAdapter.UpdateCommand = value;
            }
        }

        IDbCommand? IDbDataAdapter.UpdateCommand
        {
            get
            {
                return _updateCommand;
            }
            set
            {
                _updateCommand = value;
            }
        }

        private System.Data.MissingMappingAction UpdateMappingAction
        {
            get
            {
                if (MissingMappingAction == System.Data.MissingMappingAction.Passthrough)
                {
                    return System.Data.MissingMappingAction.Passthrough;
                }
                return System.Data.MissingMappingAction.Error;
            }
        }

        private System.Data.MissingSchemaAction UpdateSchemaAction
        {
            get
            {
                System.Data.MissingSchemaAction action = MissingSchemaAction;
                if ((action == System.Data.MissingSchemaAction.Add) || (action == System.Data.MissingSchemaAction.AddWithKey))
                {
                    return System.Data.MissingSchemaAction.Ignore;
                }
                return System.Data.MissingSchemaAction.Error;
            }
        }

        protected virtual int AddToBatch(IDbCommand command)
        {
            // Called to add a single command to the batch of commands that need
            // to be executed as a batch, when batch updates are requested.  It
            // must return an identifier that can be used to identify the command
            // to GetBatchedParameter later.

            throw ADP.NotSupported();
        }

        protected virtual void ClearBatch()
        {
            // Called when batch updates are requested to clear out the contents
            // of the batch, whether or not it's been executed.

            throw ADP.NotSupported();
        }

        object ICloneable.Clone()
        {
#pragma warning disable 618 // ignore obsolete warning about CloneInternals
            DbDataAdapter clone = (DbDataAdapter)CloneInternals();
#pragma warning restore 618
            clone.CloneFrom(this);
            return clone;
        }

        private void CloneFrom(DbDataAdapter from)
        {
            IDbDataAdapter pfrom = from._IDbDataAdapter;
            _IDbDataAdapter.SelectCommand = CloneCommand(pfrom.SelectCommand);
            _IDbDataAdapter.InsertCommand = CloneCommand(pfrom.InsertCommand);
            _IDbDataAdapter.UpdateCommand = CloneCommand(pfrom.UpdateCommand);
            _IDbDataAdapter.DeleteCommand = CloneCommand(pfrom.DeleteCommand);
        }

        private IDbCommand? CloneCommand(IDbCommand? command)
        {
            return (IDbCommand?)((command is ICloneable) ? ((ICloneable)command).Clone() : null);
        }

        protected virtual RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new RowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected virtual RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
        {
            return new RowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // release mananged objects
                IDbDataAdapter pthis = this; // must cast to interface to obtain correct value
                pthis.SelectCommand = null;
                pthis.InsertCommand = null;
                pthis.UpdateCommand = null;
                pthis.DeleteCommand = null;
            }
            // release unmanaged objects

            base.Dispose(disposing); // notify base classes
        }

        protected virtual int ExecuteBatch()
        {
            // Called to execute the batched update command, returns the number
            // of rows affected, just as ExecuteNonQuery would.

            throw ADP.NotSupported();
        }

        public DataTable? FillSchema(DataTable dataTable, SchemaType schemaType)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataTable, schemaType={1}", ObjectID, schemaType);
            try
            {
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return FillSchema(dataTable, schemaType, selectCmd!, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public override DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataSet, schemaType={1}", ObjectID, schemaType);
            try
            {
                IDbCommand? command = _IDbDataAdapter.SelectCommand;
                if (DesignMode && ((command == null) || (command.Connection == null) || string.IsNullOrEmpty(command.CommandText)))
                {
                    return Array.Empty<DataTable>(); // design-time support
                }
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return FillSchema(dataSet, schemaType, command!, DbDataAdapter.DefaultSourceTableName, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, string srcTable)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataSet, schemaType={1}, srcTable={2}", ObjectID, (int)schemaType, srcTable);
            try
            {
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return FillSchema(dataSet, schemaType, selectCmd!, srcTable, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, IDbCommand command, string srcTable, CommandBehavior behavior)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataSet, schemaType, command, srcTable, behavior={1}", ObjectID, behavior);
            try
            {
                if (dataSet == null)
                {
                    throw ADP.ArgumentNull(nameof(dataSet));
                }
                if ((schemaType != SchemaType.Source) && (schemaType != SchemaType.Mapped))
                {
                    throw ADP.InvalidSchemaType(schemaType);
                }
                if (string.IsNullOrEmpty(srcTable))
                {
                    throw ADP.FillSchemaRequiresSourceTableName(nameof(srcTable));
                }
                if (command == null)
                {
                    throw ADP.MissingSelectCommand(ADP.FillSchema);
                }
                // Never returns null if dataSet is non-null
                return (DataTable[])FillSchemaInternal(dataSet, null, schemaType, command, srcTable, behavior)!;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual DataTable? FillSchema(DataTable dataTable, SchemaType schemaType, IDbCommand command, CommandBehavior behavior)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataTable, schemaType, command, behavior={1}", ObjectID, behavior);
            try
            {
                if (dataTable == null)
                {
                    throw ADP.ArgumentNull(nameof(dataTable));
                }
                if ((schemaType != SchemaType.Source) && (schemaType != SchemaType.Mapped))
                {
                    throw ADP.InvalidSchemaType(schemaType);
                }
                if (command == null)
                {
                    throw ADP.MissingSelectCommand(ADP.FillSchema);
                }
                string srcTableName = dataTable.TableName;
                int index = IndexOfDataSetTable(srcTableName);
                if (index != -1)
                {
                    srcTableName = TableMappings[index].SourceTable;
                }
                return (DataTable?)FillSchemaInternal(null, dataTable, schemaType, command, srcTableName, behavior | CommandBehavior.SingleResult);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private object? FillSchemaInternal(DataSet? dataset, DataTable? datatable, SchemaType schemaType, IDbCommand command, string srcTable, CommandBehavior behavior)
        {
            object? dataTables = null;
            bool restoreNullConnection = (command.Connection == null);
            try
            {
                IDbConnection activeConnection = DbDataAdapter.GetConnection3(this, command, ADP.FillSchema);
                ConnectionState originalState = ConnectionState.Open;

                try
                {
                    QuietOpen(activeConnection, out originalState);
                    using (IDataReader dataReader = command.ExecuteReader(behavior | CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                    {
                        if (datatable != null)
                        { // delegate to next set of protected FillSchema methods
                            dataTables = FillSchema(datatable, schemaType, dataReader);
                        }
                        else
                        {
                            dataTables = FillSchema(dataset!, schemaType, srcTable, dataReader);
                        }
                    }
                }
                finally
                {
                    QuietClose(activeConnection, originalState);
                }
            }
            finally
            {
                if (restoreNullConnection)
                {
                    command.Transaction = null;
                    command.Connection = null;
                }
            }
            return dataTables;
        }

        public override int Fill(DataSet dataSet)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet", ObjectID);
            try
            {
                // delegate to Fill4
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return Fill(dataSet, 0, 0, DbDataAdapter.DefaultSourceTableName, selectCmd!, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public int Fill(DataSet dataSet, string srcTable)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet, srcTable='{1}'", ObjectID, srcTable);
            try
            {
                // delegate to Fill4
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return Fill(dataSet, 0, 0, srcTable, selectCmd!, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public int Fill(DataSet dataSet, int startRecord, int maxRecords, string srcTable)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet, startRecord={1}, maxRecords={2}, srcTable='{3}'", ObjectID, startRecord, maxRecords, srcTable);
            try
            {
                // delegate to Fill4
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return Fill(dataSet, startRecord, maxRecords, srcTable, selectCmd!, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual int Fill(DataSet dataSet, int startRecord, int maxRecords, string srcTable, IDbCommand command, CommandBehavior behavior)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet, startRecord, maxRecords, srcTable, command, behavior={1}", ObjectID, behavior);
            try
            {
                if (dataSet == null)
                {
                    throw ADP.FillRequires(nameof(dataSet));
                }
                if (startRecord < 0)
                {
                    throw ADP.InvalidStartRecord(nameof(startRecord), startRecord);
                }
                if (maxRecords < 0)
                {
                    throw ADP.InvalidMaxRecords(nameof(maxRecords), maxRecords);
                }
                if (string.IsNullOrEmpty(srcTable))
                {
                    throw ADP.FillRequiresSourceTableName(nameof(srcTable));
                }
                if (command == null)
                {
                    throw ADP.MissingSelectCommand(ADP.Fill);
                }
                return FillInternal(dataSet, null, startRecord, maxRecords, srcTable, command, behavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public int Fill(DataTable dataTable)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataTable", ObjectID);
            try
            {
                // delegate to Fill8
                DataTable[] dataTables = new DataTable[1] { dataTable };
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return Fill(dataTables, 0, 0, selectCmd!, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public int Fill(int startRecord, int maxRecords, params DataTable[] dataTables)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, startRecord={1}, maxRecords={2}, dataTable[]", ObjectID, startRecord, maxRecords);
            try
            {
                // delegate to Fill8
                IDbCommand? selectCmd = _IDbDataAdapter.SelectCommand;
                CommandBehavior cmdBehavior = FillCommandBehavior;
                return Fill(dataTables, startRecord, maxRecords, selectCmd!, cmdBehavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual int Fill(DataTable dataTable, IDbCommand command, CommandBehavior behavior)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataTable, command, behavior={1}", ObjectID, behavior);
            try
            {
                // delegate to Fill8
                DataTable[] dataTables = new DataTable[1] { dataTable };
                return Fill(dataTables, 0, 0, command, behavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual int Fill(DataTable[] dataTables, int startRecord, int maxRecords, IDbCommand command, CommandBehavior behavior)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataTables[], startRecord, maxRecords, command, behavior={1}", ObjectID, behavior);
            try
            {
                if ((dataTables == null) || (dataTables.Length == 0) || (dataTables[0] == null))
                {
                    throw ADP.FillRequires("dataTable");
                }
                if (startRecord < 0)
                {
                    throw ADP.InvalidStartRecord(nameof(startRecord), startRecord);
                }
                if (maxRecords < 0)
                {
                    throw ADP.InvalidMaxRecords(nameof(maxRecords), maxRecords);
                }
                if ((dataTables.Length > 1) && ((startRecord != 0) || (maxRecords != 0)))
                {
                    throw ADP.OnlyOneTableForStartRecordOrMaxRecords();
                }
                if (command == null)
                {
                    throw ADP.MissingSelectCommand(ADP.Fill);
                }
                if (dataTables.Length == 1)
                {
                    behavior |= CommandBehavior.SingleResult;
                }
                return FillInternal(null, dataTables, startRecord, maxRecords, null, command, behavior);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private int FillInternal(DataSet? dataset, DataTable[]? datatables, int startRecord, int maxRecords, string? srcTable, IDbCommand command, CommandBehavior behavior)
        {
            int rowsAddedToDataSet = 0;
            bool restoreNullConnection = (command.Connection == null);
            try
            {
                IDbConnection activeConnection = DbDataAdapter.GetConnection3(this, command, ADP.Fill);
                ConnectionState originalState = ConnectionState.Open;

                // the default is MissingSchemaAction.Add, the user must explicitly
                // set MisingSchemaAction.AddWithKey to get key information back in the dataset
                if (MissingSchemaAction == Data.MissingSchemaAction.AddWithKey)
                {
                    behavior |= CommandBehavior.KeyInfo;
                }

                try
                {
                    QuietOpen(activeConnection, out originalState);
                    behavior |= CommandBehavior.SequentialAccess;

                    IDataReader? dataReader = null;
                    try
                    {
                        dataReader = command.ExecuteReader(behavior);

                        if (datatables != null)
                        { // delegate to next set of protected Fill methods
                            rowsAddedToDataSet = Fill(datatables, dataReader, startRecord, maxRecords);
                        }
                        else
                        {
                            rowsAddedToDataSet = Fill(dataset!, srcTable!, dataReader, startRecord, maxRecords);
                        }
                    }
                    finally
                    {
                        if (dataReader != null)
                        {
                            dataReader.Dispose();
                        }
                    }
                }
                finally
                {
                    QuietClose(activeConnection, originalState);
                }
            }
            finally
            {
                if (restoreNullConnection)
                {
                    command.Transaction = null;
                    command.Connection = null;
                }
            }
            return rowsAddedToDataSet;
        }

        protected virtual IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex)
        {
            // Called to retrieve a parameter from a specific bached command, the
            // first argument is the value that was returned by AddToBatch when it
            // was called for the command.

            throw ADP.NotSupported();
        }

        protected virtual bool GetBatchedRecordsAffected(int commandIdentifier, out int recordsAffected, out Exception? error)
        {
            // Called to retrieve the records affected from a specific batched command,
            // first argument is the value that was returned by AddToBatch when it
            // was called for the command.

            // default implementation always returns 1, derived classes override for otherwise
            // otherwise DbConcurrencyException will only be thrown if sum of all records in batch is 0

            // return 0 to cause Update to throw DbConcurrencyException
            recordsAffected = 1;
            error = null;
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public override IDataParameter[] GetFillParameters()
        {
            IDataParameter[]? value = null;
            IDbCommand? select = _IDbDataAdapter.SelectCommand;
            if (select != null)
            {
                IDataParameterCollection parameters = select.Parameters;
                if (parameters != null)
                {
                    value = new IDataParameter[parameters.Count];
                    parameters.CopyTo(value, 0);
                }
            }
            if (value == null)
            {
                value = Array.Empty<IDataParameter>();
            }
            return value;
        }

        internal DataTableMapping GetTableMapping(DataTable dataTable)
        {
            DataTableMapping? tableMapping = null;
            int index = IndexOfDataSetTable(dataTable.TableName);
            if (index != -1)
            {
                tableMapping = TableMappings[index];
            }
            if (tableMapping == null)
            {
                if (MissingMappingAction == System.Data.MissingMappingAction.Error)
                {
                    throw ADP.MissingTableMappingDestination(dataTable.TableName);
                }
                tableMapping = new DataTableMapping(dataTable.TableName, dataTable.TableName);
            }
            return tableMapping;
        }

        protected virtual void InitializeBatching()
        {
            // Called when batch updates are requested to prepare for processing
            // of a batch of commands.

            throw ADP.NotSupported();
        }

        protected virtual void OnRowUpdated(RowUpdatedEventArgs value)
        {
        }

        protected virtual void OnRowUpdating(RowUpdatingEventArgs value)
        {
        }

        private void ParameterInput(IDataParameterCollection parameters, StatementType typeIndex, DataRow row, DataTableMapping mappings)
        {
            Data.MissingMappingAction missingMapping = UpdateMappingAction;
            Data.MissingSchemaAction missingSchema = UpdateSchemaAction;

            foreach (IDataParameter parameter in parameters)
            {
                if ((parameter != null) && ((ParameterDirection.Input & parameter.Direction) != 0))
                {
                    string columnName = parameter.SourceColumn;
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        DataColumn? dataColumn = mappings.GetDataColumn(columnName, null, row.Table, missingMapping, missingSchema);
                        if (dataColumn != null)
                        {
                            DataRowVersion version = DbDataAdapter.GetParameterSourceVersion(typeIndex, parameter);
                            parameter.Value = row[dataColumn, version];
                        }
                        else
                        {
                            parameter.Value = null;
                        }

                        DbParameter? dbparameter = (parameter as DbParameter);
                        if ((dbparameter != null) && dbparameter.SourceColumnNullMapping)
                        {
                            Debug.Assert(parameter.DbType == DbType.Int32, "unexpected DbType");
                            parameter.Value = ADP.IsNull(parameter.Value) ? s_parameterValueNullValue : s_parameterValueNonNullValue;
                        }
                    }
                }
            }
        }

        private void ParameterOutput(IDataParameter parameter, DataRow row, DataTableMapping mappings, MissingMappingAction missingMapping, MissingSchemaAction missingSchema)
        {
            if ((ParameterDirection.Output & parameter.Direction) != 0)
            {
                object? value = parameter.Value;
                if (value != null)
                {
                    // null means default, meaning we leave the current DataRow value alone
                    string columnName = parameter.SourceColumn;
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        DataColumn? dataColumn = mappings.GetDataColumn(columnName, null, row.Table, missingMapping, missingSchema);
                        if (dataColumn != null)
                        {
                            if (dataColumn.ReadOnly)
                            {
                                try
                                {
                                    dataColumn.ReadOnly = false;
                                    row[dataColumn] = value;
                                }
                                finally
                                {
                                    dataColumn.ReadOnly = true;
                                }
                            }
                            else
                            {
                                row[dataColumn] = value;
                            }
                        }
                    }
                }
            }
        }

        private void ParameterOutput(IDataParameterCollection parameters, DataRow row, DataTableMapping mappings)
        {
            Data.MissingMappingAction missingMapping = UpdateMappingAction;
            Data.MissingSchemaAction missingSchema = UpdateSchemaAction;

            foreach (IDataParameter parameter in parameters)
            {
                if (parameter != null)
                {
                    ParameterOutput(parameter, row, mappings, missingMapping, missingSchema);
                }
            }
        }

        protected virtual void TerminateBatching()
        {
            // Called when batch updates are requested to cleanup after a batch
            // update has been completed.

            throw ADP.NotSupported();
        }

        public override int Update(DataSet dataSet)
        {
            return Update(dataSet, DbDataAdapter.DefaultSourceTableName);
        }

        public int Update(DataRow[] dataRows)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataRows[]", ObjectID);
            try
            {
                int rowsAffected = 0;
                if (dataRows == null)
                {
                    throw ADP.ArgumentNull(nameof(dataRows));
                }
                else if (dataRows.Length != 0)
                {
                    DataTable? dataTable = null;
                    for (int i = 0; i < dataRows.Length; ++i)
                    {
                        if ((dataRows[i] != null) && (dataTable != dataRows[i].Table))
                        {
                            if (dataTable != null)
                            {
                                throw ADP.UpdateMismatchRowTable(i);
                            }
                            dataTable = dataRows[i].Table;
                        }
                    }
                    if (dataTable != null)
                    {
                        DataTableMapping tableMapping = GetTableMapping(dataTable);
                        rowsAffected = Update(dataRows, tableMapping);
                    }
                }
                return rowsAffected;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public int Update(DataTable dataTable)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataTable", ObjectID);
            try
            {
                if (dataTable == null)
                {
                    throw ADP.UpdateRequiresDataTable(nameof(dataTable));
                }

                DataTableMapping? tableMapping = null;
                int index = IndexOfDataSetTable(dataTable.TableName);
                if (index != -1)
                {
                    tableMapping = TableMappings[index];
                }
                if (tableMapping == null)
                {
                    if (MissingMappingAction == System.Data.MissingMappingAction.Error)
                    {
                        throw ADP.MissingTableMappingDestination(dataTable.TableName);
                    }
                    tableMapping = new DataTableMapping(DbDataAdapter.DefaultSourceTableName, dataTable.TableName);
                }
                return UpdateFromDataTable(dataTable, tableMapping);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        public int Update(DataSet dataSet, string srcTable)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataSet, srcTable='{1}'", ObjectID, srcTable);
            try
            {
                if (dataSet == null)
                {
                    throw ADP.UpdateRequiresNonNullDataSet(nameof(dataSet));
                }
                if (string.IsNullOrEmpty(srcTable))
                {
                    throw ADP.UpdateRequiresSourceTableName(nameof(srcTable));
                }

                int rowsAffected = 0;

                DataTableMapping? tableMapping = GetTableMappingBySchemaAction(srcTable, srcTable, UpdateMappingAction);
                Debug.Assert(tableMapping != null, "null TableMapping when MissingMappingAction.Error");

                // the ad-hoc scenario of no dataTable just returns
                // ad-hoc scenario is defined as MissingSchemaAction.Add or MissingSchemaAction.Ignore
                System.Data.MissingSchemaAction schemaAction = UpdateSchemaAction;
                DataTable? dataTable = tableMapping.GetDataTableBySchemaAction(dataSet, schemaAction);
                if (dataTable != null)
                {
                    rowsAffected = UpdateFromDataTable(dataTable, tableMapping);
                }
                else if (!HasTableMappings() || (TableMappings.IndexOf(tableMapping) == -1))
                {
                    //throw error since the user didn't explicitly map this tableName to Ignore.
                    throw ADP.UpdateRequiresSourceTable(srcTable);
                }
                return rowsAffected;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual int Update(DataRow[] dataRows, DataTableMapping tableMapping)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataRows[], tableMapping", ObjectID);
            try
            {
                Debug.Assert((dataRows != null) && (dataRows.Length > 0), "Update: bad dataRows");
                Debug.Assert(tableMapping != null, "Update: bad DataTableMapping");

                // If records were affected, increment row count by one - that is number of rows affected in dataset.
                int cumulativeDataRowsAffected = 0;

                IDbConnection?[] connections = new IDbConnection[5]; // one for each statementtype
                ConnectionState[] connectionStates = new ConnectionState[5]; // closed by default (== 0)

                bool useSelectConnectionState = false;
                IDbCommand? tmpcmd = _IDbDataAdapter.SelectCommand;
                if (tmpcmd != null)
                {
                    connections[0] = tmpcmd.Connection;
                    if (connections[0] != null)
                    {
                        connectionStates[0] = connections[0]!.State;
                        useSelectConnectionState = true;
                    }
                }

                int maxBatchCommands = Math.Min(UpdateBatchSize, dataRows.Length);

                if (maxBatchCommands < 1)
                {  // batch size of zero indicates one batch, no matter how large...
                    maxBatchCommands = dataRows.Length;
                }

                BatchCommandInfo[] batchCommands = new BatchCommandInfo[maxBatchCommands];
                DataRow[] rowBatch = new DataRow[maxBatchCommands];
                int commandCount = 0;

                // the outer try/finally is for closing any connections we may have opened
                try
                {
                    try
                    {
                        if (maxBatchCommands != 1)
                        {
                            InitializeBatching();
                        }
                        StatementType statementType = StatementType.Select;
                        IDbCommand? dataCommand = null;

                        // for each row which is either insert, update, or delete
                        foreach (DataRow dataRow in dataRows)
                        {
                            if (dataRow == null)
                            {
                                continue; // foreach DataRow
                            }
                            bool isCommandFromRowUpdating = false;

                            // obtain the appropriate command
                            switch (dataRow.RowState)
                            {
                                case DataRowState.Detached:
                                case DataRowState.Unchanged:
                                    continue; // foreach DataRow
                                case DataRowState.Added:
                                    statementType = StatementType.Insert;
                                    dataCommand = _IDbDataAdapter.InsertCommand;
                                    break;
                                case DataRowState.Deleted:
                                    statementType = StatementType.Delete;
                                    dataCommand = _IDbDataAdapter.DeleteCommand;
                                    break;
                                case DataRowState.Modified:
                                    statementType = StatementType.Update;
                                    dataCommand = _IDbDataAdapter.UpdateCommand;
                                    break;
                                default:
                                    Debug.Fail("InvalidDataRowState");
                                    throw ADP.InvalidDataRowState(dataRow.RowState); // out of Update without completing batch
                            }

                            // setup the event to be raised
                            // TODO: the event may be raised with a null command, but only if the update, but only if
                            // the update attempt fails (because no command was configured). We should not emit the
                            // event in this case.
                            RowUpdatingEventArgs? rowUpdatingEvent = CreateRowUpdatingEvent(dataRow, dataCommand, statementType, tableMapping);

                            // this try/catch for any exceptions during the parameter initialization
                            try
                            {
                                dataRow.RowError = null;
                                if (dataCommand != null)
                                {
                                    // prepare the parameters for the user who then can modify them during OnRowUpdating
                                    ParameterInput(dataCommand.Parameters, statementType, dataRow, tableMapping);
                                }
                            }
                            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                            {
                                ADP.TraceExceptionForCapture(e);
                                rowUpdatingEvent.Errors = e;
                                rowUpdatingEvent.Status = UpdateStatus.ErrorsOccurred;
                            }

                            OnRowUpdating(rowUpdatingEvent); // user may throw out of Update without completing batch

                            IDbCommand? tmpCommand = rowUpdatingEvent.Command;
                            isCommandFromRowUpdating = (dataCommand != tmpCommand);
                            dataCommand = tmpCommand;
                            tmpCommand = null;

                            // handle the status from RowUpdating event
                            UpdateStatus rowUpdatingStatus = rowUpdatingEvent.Status;
                            if (rowUpdatingStatus != UpdateStatus.Continue)
                            {
                                if (rowUpdatingStatus == UpdateStatus.ErrorsOccurred)
                                {
                                    UpdatingRowStatusErrors(rowUpdatingEvent, dataRow);
                                    continue; // foreach DataRow
                                }
                                else if (rowUpdatingStatus == UpdateStatus.SkipCurrentRow)
                                {
                                    if (dataRow.RowState == DataRowState.Unchanged)
                                    {
                                        cumulativeDataRowsAffected++;
                                    }
                                    continue; // foreach DataRow
                                }
                                else if (rowUpdatingStatus == UpdateStatus.SkipAllRemainingRows)
                                {
                                    if (dataRow.RowState == DataRowState.Unchanged)
                                    {
                                        cumulativeDataRowsAffected++;
                                    }
                                    break; // execute existing batch and return
                                }
                                else
                                {
                                    throw ADP.InvalidUpdateStatus(rowUpdatingStatus);  // out of Update
                                }
                            }
                            // else onward to Append/ExecuteNonQuery/ExecuteReader

                            rowUpdatingEvent = null;
                            RowUpdatedEventArgs? rowUpdatedEvent = null;

                            if (maxBatchCommands == 1)
                            {
                                if (dataCommand != null)
                                {
                                    batchCommands[0]._commandIdentifier = 0;
                                    batchCommands[0]._parameterCount = dataCommand.Parameters.Count;
                                    batchCommands[0]._statementType = statementType;
                                    batchCommands[0]._updatedRowSource = dataCommand.UpdatedRowSource;
                                }
                                batchCommands[0]._row = dataRow;
                                rowBatch[0] = dataRow; // not doing a batch update, just simplifying code...
                                commandCount = 1;
                            }
                            else
                            {
                                Exception? errors = null;

                                try
                                {
                                    if (dataCommand != null)
                                    {
                                        if ((UpdateRowSource.FirstReturnedRecord & dataCommand.UpdatedRowSource) == 0)
                                        {
                                            // append the command to the commandset. If an exception
                                            // occurs, then the user must append and continue

                                            batchCommands[commandCount]._commandIdentifier = AddToBatch(dataCommand);
                                            batchCommands[commandCount]._parameterCount = dataCommand.Parameters.Count;
                                            batchCommands[commandCount]._row = dataRow;
                                            batchCommands[commandCount]._statementType = statementType;
                                            batchCommands[commandCount]._updatedRowSource = dataCommand.UpdatedRowSource;

                                            rowBatch[commandCount] = dataRow;
                                            commandCount++;

                                            if (commandCount < maxBatchCommands)
                                            {
                                                continue; // foreach DataRow
                                            }
                                            // else onward execute the batch
                                        }
                                        else
                                        {
                                            // do not allow the expectation that returned results will be used
                                            errors = ADP.ResultsNotAllowedDuringBatch();
                                        }
                                    }
                                    else
                                    {
                                        // null Command will force RowUpdatedEvent with ErrorsOccurred without completing batch
                                        errors = ADP.UpdateRequiresCommand(statementType, isCommandFromRowUpdating);
                                    }
                                }
                                catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                                {
                                    // try/catch for RowUpdatedEventArgs
                                    ADP.TraceExceptionForCapture(e);
                                    errors = e;
                                }

                                if (errors != null)
                                {
                                    // TODO: See above comment on dataCommand being null
                                    rowUpdatedEvent = CreateRowUpdatedEvent(dataRow, dataCommand, StatementType.Batch, tableMapping);
                                    rowUpdatedEvent.Errors = errors;
                                    rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;

                                    OnRowUpdated(rowUpdatedEvent); // user may throw out of Update
                                    if (errors != rowUpdatedEvent.Errors)
                                    { // user set the error msg and we will use it
                                        for (int i = 0; i < batchCommands.Length; ++i)
                                        {
                                            batchCommands[i]._errors = null;
                                        }
                                    }

                                    cumulativeDataRowsAffected += UpdatedRowStatus(rowUpdatedEvent, batchCommands, commandCount);
                                    if (rowUpdatedEvent.Status == UpdateStatus.SkipAllRemainingRows)
                                    {
                                        break;
                                    }
                                    continue; // foreach datarow
                                }
                            }

                            // TODO: See above comment on dataCommand being null
                            rowUpdatedEvent = CreateRowUpdatedEvent(dataRow, dataCommand, statementType, tableMapping);

                            // this try/catch for any exceptions during the execution, population, output parameters
                            try
                            {
                                if (maxBatchCommands != 1)
                                {
                                    IDbConnection connection = DbDataAdapter.GetConnection1(this);

                                    ConnectionState state = UpdateConnectionOpen(connection, StatementType.Batch, connections, connectionStates, useSelectConnectionState);
                                    rowUpdatedEvent.AdapterInit(rowBatch);

                                    if (state == ConnectionState.Open)
                                    {
                                        UpdateBatchExecute(batchCommands, commandCount, rowUpdatedEvent);
                                    }
                                    else
                                    {
                                        // null Connection will force RowUpdatedEvent with ErrorsOccurred without completing batch
                                        rowUpdatedEvent.Errors = ADP.UpdateOpenConnectionRequired(StatementType.Batch, false, state);
                                        rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                                    }
                                }
                                else if (dataCommand != null)
                                {
                                    IDbConnection connection = DbDataAdapter.GetConnection4(this, dataCommand, statementType, isCommandFromRowUpdating);
                                    ConnectionState state = UpdateConnectionOpen(connection, statementType, connections, connectionStates, useSelectConnectionState);
                                    if (state == ConnectionState.Open)
                                    {
                                        UpdateRowExecute(rowUpdatedEvent, dataCommand, statementType);
                                        batchCommands[0]._recordsAffected = rowUpdatedEvent.RecordsAffected;
                                        batchCommands[0]._errors = null;
                                    }
                                    else
                                    {
                                        // null Connection will force RowUpdatedEvent with ErrorsOccurred without completing batch
                                        rowUpdatedEvent.Errors = ADP.UpdateOpenConnectionRequired(statementType, isCommandFromRowUpdating, state);
                                        rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                                    }
                                }
                                else
                                {
                                    // null Command will force RowUpdatedEvent with ErrorsOccurred without completing batch
                                    rowUpdatedEvent.Errors = ADP.UpdateRequiresCommand(statementType, isCommandFromRowUpdating);
                                    rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                                }
                            }
                            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                            {
                                // try/catch for RowUpdatedEventArgs
                                ADP.TraceExceptionForCapture(e);
                                rowUpdatedEvent.Errors = e;
                                rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                            }

                            bool clearBatchOnSkipAll = (rowUpdatedEvent.Status == UpdateStatus.ErrorsOccurred);

                            {
                                Exception? errors = rowUpdatedEvent.Errors;
                                OnRowUpdated(rowUpdatedEvent); // user may throw out of Update
                                // NOTE: the contents of rowBatch are now tainted...
                                if (errors != rowUpdatedEvent.Errors)
                                { // user set the error msg and we will use it
                                    for (int i = 0; i < batchCommands.Length; ++i)
                                    {
                                        batchCommands[i]._errors = null;
                                    }
                                }
                            }

                            cumulativeDataRowsAffected += UpdatedRowStatus(rowUpdatedEvent, batchCommands, commandCount);

                            if (rowUpdatedEvent.Status == UpdateStatus.SkipAllRemainingRows)
                            {
                                if (clearBatchOnSkipAll && maxBatchCommands != 1)
                                {
                                    ClearBatch();
                                    commandCount = 0;
                                }
                                break; // from update
                            }

                            if (maxBatchCommands != 1)
                            {
                                ClearBatch();
                                commandCount = 0;
                            }
                            for (int i = 0; i < batchCommands.Length; ++i)
                            {
                                batchCommands[i] = default(BatchCommandInfo);
                            }
                            commandCount = 0;
                        } // foreach DataRow

                        // must handle the last batch
                        if (maxBatchCommands != 1 && commandCount > 0)
                        {
                            // TODO: See above comment on dataCommand being null
                            // TODO: DataRow is null because we call AdapterInit below, which populates rows
                            RowUpdatedEventArgs rowUpdatedEvent = CreateRowUpdatedEvent(null!, dataCommand, statementType, tableMapping);

                            try
                            {
                                IDbConnection connection = DbDataAdapter.GetConnection1(this);

                                ConnectionState state = UpdateConnectionOpen(connection, StatementType.Batch, connections, connectionStates, useSelectConnectionState);

                                DataRow[] finalRowBatch = rowBatch;

                                if (commandCount < rowBatch.Length)
                                {
                                    finalRowBatch = new DataRow[commandCount];
                                    Array.Copy(rowBatch, finalRowBatch, commandCount);
                                }
                                rowUpdatedEvent.AdapterInit(finalRowBatch);

                                if (state == ConnectionState.Open)
                                {
                                    UpdateBatchExecute(batchCommands, commandCount, rowUpdatedEvent);
                                }
                                else
                                {
                                    // null Connection will force RowUpdatedEvent with ErrorsOccurred without completing batch
                                    rowUpdatedEvent.Errors = ADP.UpdateOpenConnectionRequired(StatementType.Batch, false, state);
                                    rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                                }
                            }
                            catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                            {
                                // try/catch for RowUpdatedEventArgs
                                ADP.TraceExceptionForCapture(e);
                                rowUpdatedEvent.Errors = e;
                                rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                            }
                            Exception? errors = rowUpdatedEvent.Errors;
                            OnRowUpdated(rowUpdatedEvent); // user may throw out of Update
                            // NOTE: the contents of rowBatch are now tainted...
                            if (errors != rowUpdatedEvent.Errors)
                            { // user set the error msg and we will use it
                                for (int i = 0; i < batchCommands.Length; ++i)
                                {
                                    batchCommands[i]._errors = null;
                                }
                            }

                            cumulativeDataRowsAffected += UpdatedRowStatus(rowUpdatedEvent, batchCommands, commandCount);
                        }
                    }
                    finally
                    {
                        if (maxBatchCommands != 1)
                        {
                            TerminateBatching();
                        }
                    }
                }
                finally
                { // try/finally for connection cleanup
                    for (int i = 0; i < connections.Length; ++i)
                    {
                        QuietClose(connections[i], connectionStates[i]);
                    }
                }
                return cumulativeDataRowsAffected;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        private void UpdateBatchExecute(BatchCommandInfo[] batchCommands, int commandCount, RowUpdatedEventArgs rowUpdatedEvent)
        {
            Debug.Assert(rowUpdatedEvent.Rows != null);
            try
            {
                // the batch execution may succeed, partially succeed and throw an exception (or not), or totally fail
                int recordsAffected = ExecuteBatch();
                rowUpdatedEvent.AdapterInit(recordsAffected);
            }
            catch (DbException e)
            {
                // an exception was thrown be but some part of the batch may have been succesfull
                ADP.TraceExceptionForCapture(e);
                rowUpdatedEvent.Errors = e;
                rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
            }
            Data.MissingMappingAction missingMapping = UpdateMappingAction;
            Data.MissingSchemaAction missingSchema = UpdateSchemaAction;

            int checkRecordsAffected = 0;
            bool hasConcurrencyViolation = false;
            List<DataRow>? rows = null;

            // walk through the batch to build the sum of recordsAffected
            //      determine possible indivdual messages per datarow
            //      determine possible concurrency violations per datarow
            //      map output parameters to the datarow
            for (int bc = 0; bc < commandCount; ++bc)
            {
                BatchCommandInfo batchCommand = batchCommands[bc];
                StatementType statementType = batchCommand._statementType;

                // default implementation always returns 1, derived classes must override
                // otherwise DbConcurrencyException will only be thrown if sum of all records in batch is 0
                int rowAffected;
                if (GetBatchedRecordsAffected(batchCommand._commandIdentifier, out rowAffected, out batchCommands[bc]._errors))
                {
                    batchCommands[bc]._recordsAffected = rowAffected;
                }

                if ((batchCommands[bc]._errors == null) && batchCommands[bc]._recordsAffected.HasValue)
                {
                    // determine possible concurrency violations per datarow
                    if ((statementType == StatementType.Update) || (statementType == StatementType.Delete))
                    {
                        checkRecordsAffected++;
                        if (rowAffected == 0)
                        {
                            if (rows == null)
                            {
                                rows = new List<DataRow>();
                            }
                            batchCommands[bc]._errors = ADP.UpdateConcurrencyViolation(batchCommands[bc]._statementType, 0, 1, new DataRow[] { rowUpdatedEvent.Rows[bc] });
                            hasConcurrencyViolation = true;
                            rows.Add(rowUpdatedEvent.Rows[bc]);
                        }
                    }

                    // map output parameters to the datarow
                    if (((statementType == StatementType.Insert) || (statementType == StatementType.Update))
                        && ((UpdateRowSource.OutputParameters & batchCommand._updatedRowSource) != 0) && (rowAffected != 0))
                    {
                        if (statementType == StatementType.Insert)
                        {
                            // AcceptChanges for 'added' rows so backend generated keys that are returned
                            // propagte into the datatable correctly.
                            rowUpdatedEvent.Rows[bc].AcceptChanges();
                        }

                        for (int i = 0; i < batchCommand._parameterCount; ++i)
                        {
                            IDataParameter parameter = GetBatchedParameter(batchCommand._commandIdentifier, i);
                            ParameterOutput(parameter, batchCommand._row, rowUpdatedEvent.TableMapping, missingMapping, missingSchema);
                        }
                    }
                }
            }

            if (rowUpdatedEvent.Errors == null)
            {
                // Only error if RecordsAffect == 0, not -1.  A value of -1 means no count was received from server,
                // do not error in that situation (means 'set nocount on' was executed on server).
                if (rowUpdatedEvent.Status == UpdateStatus.Continue)
                {
                    if ((checkRecordsAffected > 0) && ((rowUpdatedEvent.RecordsAffected == 0) || hasConcurrencyViolation))
                    {
                        // bug50526, an exception if no records affected and attempted an Update/Delete
                        Debug.Assert(rowUpdatedEvent.Errors == null, "Continue - but contains an exception");
                        DataRow[] rowsInError = (rows != null) ? rows.ToArray() : rowUpdatedEvent.Rows!;
                        rowUpdatedEvent.Errors = ADP.UpdateConcurrencyViolation(StatementType.Batch, commandCount - rowsInError.Length, commandCount, rowsInError);
                        rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                    }
                }
            }
        }

        private ConnectionState UpdateConnectionOpen(IDbConnection connection, StatementType statementType, IDbConnection?[] connections, ConnectionState[] connectionStates, bool useSelectConnectionState)
        {
            Debug.Assert(connection != null, "unexpected null connection");
            int index = (int)statementType;
            if (connection != connections[index])
            {
                // if the user has changed the connection on the command object
                // and we had opened that connection, close that connection
                QuietClose(connections[index], connectionStates[index]);

                connections[index] = connection;
                connectionStates[index] = ConnectionState.Closed; // required, open may throw

                QuietOpen(connection, out connectionStates[index]);
                if (useSelectConnectionState && (connections[0] == connection))
                {
                    connectionStates[index] = connections[0]!.State;
                }
            }
            return connection.State;
        }

        private int UpdateFromDataTable(DataTable dataTable, DataTableMapping tableMapping)
        {
            int rowsAffected = 0;
            DataRow[] dataRows = ADP.SelectAdapterRows(dataTable, false);
            if ((dataRows != null) && (dataRows.Length > 0))
            {
                rowsAffected = Update(dataRows, tableMapping);
            }
            return rowsAffected;
        }

        private void UpdateRowExecute(RowUpdatedEventArgs rowUpdatedEvent, IDbCommand dataCommand, StatementType cmdIndex)
        {
            Debug.Assert(rowUpdatedEvent != null, "null rowUpdatedEvent");
            Debug.Assert(dataCommand != null, "null dataCommand");
            Debug.Assert(rowUpdatedEvent.Command == dataCommand, "dataCommand differs from rowUpdatedEvent");

            bool insertAcceptChanges = true;
            UpdateRowSource updatedRowSource = dataCommand.UpdatedRowSource;
            if ((cmdIndex == StatementType.Delete) || ((UpdateRowSource.FirstReturnedRecord & updatedRowSource) == 0))
            {
                int recordsAffected = dataCommand.ExecuteNonQuery();
                rowUpdatedEvent.AdapterInit(recordsAffected);
            }
            else if ((cmdIndex == StatementType.Insert) || (cmdIndex == StatementType.Update))
            {
                // we only care about the first row of the first result
                using (IDataReader dataReader = dataCommand.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    DataReaderContainer readerHandler = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
                    try
                    {
                        bool getData = false;
                        do
                        {
                            // advance to the first row returning result set
                            // determined by actually having columns in the result set
                            if (readerHandler.FieldCount > 0)
                            {
                                getData = true;
                                break;
                            }
                        } while (dataReader.NextResult());

                        if (getData && (dataReader.RecordsAffected != 0))
                        {
                            SchemaMapping mapping = new SchemaMapping(this, null, rowUpdatedEvent.Row.Table, readerHandler, false, SchemaType.Mapped, rowUpdatedEvent.TableMapping.SourceTable, true, null, null);

                            if ((mapping.DataTable != null) && (mapping.DataValues != null))
                            {
                                if (dataReader.Read())
                                {
                                    if ((cmdIndex == StatementType.Insert) && insertAcceptChanges)
                                    {
                                        rowUpdatedEvent.Row.AcceptChanges();
                                        insertAcceptChanges = false;
                                    }
                                    mapping.ApplyToDataRow(rowUpdatedEvent.Row);
                                }
                            }
                        }
                    }
                    finally
                    {
                        // using Close which can optimize its { while(dataReader.NextResult()); } loop
                        dataReader.Close();

                        // RecordsAffected is available after Close, but don't trust it after Dispose
                        int recordsAffected = dataReader.RecordsAffected;
                        rowUpdatedEvent.AdapterInit(recordsAffected);
                    }
                }
            }
            else
            {
                // StatementType.Select, StatementType.Batch
                Debug.Fail("unexpected StatementType");
            }

            // map the parameter results to the dataSet
            if (((cmdIndex == StatementType.Insert) || (cmdIndex == StatementType.Update))
                && ((UpdateRowSource.OutputParameters & updatedRowSource) != 0) && (rowUpdatedEvent.RecordsAffected != 0))
            {
                if ((cmdIndex == StatementType.Insert) && insertAcceptChanges)
                {
                    rowUpdatedEvent.Row.AcceptChanges();
                }
                ParameterOutput(dataCommand.Parameters, rowUpdatedEvent.Row, rowUpdatedEvent.TableMapping);
            }

            // Only error if RecordsAffect == 0, not -1.  A value of -1 means no count was received from server,
            // do not error in that situation (means 'set nocount on' was executed on server).
            switch (rowUpdatedEvent.Status)
            {
                case UpdateStatus.Continue:
                    switch (cmdIndex)
                    {
                        case StatementType.Update:
                        case StatementType.Delete:
                            if (rowUpdatedEvent.RecordsAffected == 0)
                            {
                                Debug.Assert(rowUpdatedEvent.Errors == null, "Continue - but contains an exception");
                                rowUpdatedEvent.Errors = ADP.UpdateConcurrencyViolation(cmdIndex, rowUpdatedEvent.RecordsAffected, 1, new DataRow[] { rowUpdatedEvent.Row });
                                rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
                            }
                            break;
                    }
                    break;
            }
        }

        private int UpdatedRowStatus(RowUpdatedEventArgs rowUpdatedEvent, BatchCommandInfo[] batchCommands, int commandCount)
        {
            Debug.Assert(rowUpdatedEvent != null, "null rowUpdatedEvent");
            int cumulativeDataRowsAffected = 0;
            switch (rowUpdatedEvent.Status)
            {
                case UpdateStatus.Continue:
                    cumulativeDataRowsAffected = UpdatedRowStatusContinue(rowUpdatedEvent, batchCommands, commandCount);
                    break; // return to foreach DataRow
                case UpdateStatus.ErrorsOccurred:
                    cumulativeDataRowsAffected = UpdatedRowStatusErrors(rowUpdatedEvent, batchCommands, commandCount);
                    break; // no datarow affected if ErrorsOccurred
                case UpdateStatus.SkipCurrentRow:
                case UpdateStatus.SkipAllRemainingRows: // cancel the Update method
                    cumulativeDataRowsAffected = UpdatedRowStatusSkip(batchCommands, commandCount);
                    break; // foreach DataRow without accepting changes on this row (but user may haved accepted chagnes for us)
                default:
                    throw ADP.InvalidUpdateStatus(rowUpdatedEvent.Status);
            } // switch RowUpdatedEventArgs.Status
            return cumulativeDataRowsAffected;
        }

        private int UpdatedRowStatusContinue(RowUpdatedEventArgs rowUpdatedEvent, BatchCommandInfo[] batchCommands, int commandCount)
        {
            Debug.Assert(batchCommands != null, "null batchCommands?");
            int cumulativeDataRowsAffected = 0;
            // 1. We delay accepting the changes until after we fire RowUpdatedEvent
            //    so the user has a chance to call RejectChanges for any given reason
            // 2. If the DataSource return 0 records affected, its an indication that
            //    the command didn't take so we don't want to automatically
            //    AcceptChanges.
            // With 'set nocount on' the count will be -1, accept changes in that case too.
            // 3.  Don't accept changes if no rows were affected, the user needs
            //     to know that there is a concurrency violation

            // Only accept changes if the row is not already accepted, ie detached.
            bool acdu = AcceptChangesDuringUpdate;
            for (int i = 0; i < commandCount; i++)
            {
                var batchCommand = batchCommands[i];
                DataRow row = batchCommand._row;
                if ((batchCommand._errors == null) && batchCommand._recordsAffected != null && (batchCommand._recordsAffected.Value != 0))
                {
                    Debug.Assert(row != null, "null dataRow?");
                    if (acdu)
                    {
                        if (((DataRowState.Added | DataRowState.Deleted | DataRowState.Modified) & row.RowState) != 0)
                        {
                            row.AcceptChanges();
                        }
                    }
                    cumulativeDataRowsAffected++;
                }
            }
            return cumulativeDataRowsAffected;
        }

        private int UpdatedRowStatusErrors(RowUpdatedEventArgs rowUpdatedEvent, BatchCommandInfo[] batchCommands, int commandCount)
        {
            Debug.Assert(batchCommands != null, "null batchCommands?");
            Exception? errors = rowUpdatedEvent.Errors;
            if (errors == null)
            {
                // user changed status to ErrorsOccurred without supplying an exception message
                errors = ADP.RowUpdatedErrors();
                rowUpdatedEvent.Errors = errors;
            }

            int affected = 0;
            bool done = false;
            string message = errors.Message;

            for (int i = 0; i < commandCount; i++)
            {
                DataRow row = batchCommands[i]._row;
                Debug.Assert(row != null, "null dataRow?");

                if (batchCommands[i]._errors is Exception commandErrors)
                { // will exist if 0 == RecordsAffected
                    string rowMsg = commandErrors.Message;
                    if (string.IsNullOrEmpty(rowMsg))
                    {
                        rowMsg = message;
                    }
                    row.RowError += rowMsg;
                    done = true;
                }
            }
            if (!done)
            { // all rows are in 'error'
                for (int i = 0; i < commandCount; i++)
                {
                    DataRow row = batchCommands[i]._row;
                    // its possible a DBConcurrencyException exists and all rows have records affected
                    // via not overriding GetBatchedRecordsAffected or user setting the exception
                    row.RowError += message;
                }
            }
            else
            {
                affected = UpdatedRowStatusContinue(rowUpdatedEvent, batchCommands, commandCount);
            }
            if (!ContinueUpdateOnError)
            {
                throw errors; // out of Update
            }
            return affected; // return the count of successful rows within the batch failure
        }

        private int UpdatedRowStatusSkip(BatchCommandInfo[] batchCommands, int commandCount)
        {
            Debug.Assert(batchCommands != null, "null batchCommands?");

            int cumulativeDataRowsAffected = 0;

            for (int i = 0; i < commandCount; i++)
            {
                DataRow row = batchCommands[i]._row;
                Debug.Assert(row != null, "null dataRow?");
                if (((DataRowState.Detached | DataRowState.Unchanged) & row.RowState) != 0)
                {
                    cumulativeDataRowsAffected++;
                }
            }
            return cumulativeDataRowsAffected;
        }

        private void UpdatingRowStatusErrors(RowUpdatingEventArgs rowUpdatedEvent, DataRow dataRow)
        {
            Debug.Assert(dataRow != null, "null dataRow");
            Exception? errors = rowUpdatedEvent.Errors;

            if (errors == null)
            {
                // user changed status to ErrorsOccurred without supplying an exception message
                errors = ADP.RowUpdatingErrors();
                rowUpdatedEvent.Errors = errors;
            }
            string message = errors.Message;
            dataRow.RowError += message;

            if (!ContinueUpdateOnError)
            {
                throw errors; // out of Update
            }
        }

        private static IDbConnection GetConnection1(DbDataAdapter adapter)
        {
            IDbCommand? command = adapter._IDbDataAdapter.SelectCommand;
            if (command == null)
            {
                command = adapter._IDbDataAdapter.InsertCommand;
                if (command == null)
                {
                    command = adapter._IDbDataAdapter.UpdateCommand;
                    if (command == null)
                    {
                        command = adapter._IDbDataAdapter.DeleteCommand;
                    }
                }
            }
            IDbConnection? connection = null;
            if (command != null)
            {
                connection = command.Connection;
            }
            if (connection == null)
            {
                throw ADP.UpdateConnectionRequired(StatementType.Batch, false);
            }
            return connection;
        }

        private static IDbConnection GetConnection3(DbDataAdapter adapter, IDbCommand command, string method)
        {
            Debug.Assert(command != null, "GetConnection3: null command");
            Debug.Assert(!string.IsNullOrEmpty(method), "missing method name");
            IDbConnection? connection = command.Connection;
            if (connection == null)
            {
                throw ADP.ConnectionRequired_Res(method);
            }
            return connection;
        }

        private static IDbConnection GetConnection4(DbDataAdapter adapter, IDbCommand command, StatementType statementType, bool isCommandFromRowUpdating)
        {
            Debug.Assert(command != null, "GetConnection4: null command");
            IDbConnection? connection = command.Connection;
            if (connection == null)
            {
                throw ADP.UpdateConnectionRequired(statementType, isCommandFromRowUpdating);
            }
            return connection;
        }
        private static DataRowVersion GetParameterSourceVersion(StatementType statementType, IDataParameter parameter)
        {
            switch (statementType)
            {
                case StatementType.Insert: return DataRowVersion.Current;  // ignores parameter.SourceVersion
                case StatementType.Update: return parameter.SourceVersion;
                case StatementType.Delete: return DataRowVersion.Original; // ignores parameter.SourceVersion
                case StatementType.Select:
                case StatementType.Batch:
                    throw ADP.UnwantedStatementType(statementType);
                default:
                    throw ADP.InvalidStatementType(statementType);
            }
        }

        private static void QuietClose(IDbConnection? connection, ConnectionState originalState)
        {
            // close the connection if:
            // * it was closed on first use and adapter has opened it, AND
            // * provider's implementation did not ask to keep this connection open
            if ((connection != null) && (originalState == ConnectionState.Closed))
            {
                // we don't have to check the current connection state because
                // it is supposed to be safe to call Close multiple times
                connection.Close();
            }
        }

        // QuietOpen needs to appear in the try {} finally { QuietClose } block
        // otherwise a possibility exists that an exception may be thrown
        // where we would Open the connection and not close it
        private static void QuietOpen(IDbConnection connection, out ConnectionState originalState)
        {
            Debug.Assert(connection != null, "QuietOpen: null connection");
            originalState = connection.State;
            if (originalState == ConnectionState.Closed)
            {
                connection.Open();
            }
        }
    }
}

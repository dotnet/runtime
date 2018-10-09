// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.Common
{
    public class DataAdapter : Component, IDataAdapter
    {
        private static readonly object s_eventFillError = new object();

        private bool _acceptChangesDuringUpdate = true;
        private bool _acceptChangesDuringUpdateAfterInsert = true;
        private bool _continueUpdateOnError = false;
        private bool _hasFillErrorHandler = false;
        private bool _returnProviderSpecificTypes = false;

        private bool _acceptChangesDuringFill = true;
        private LoadOption _fillLoadOption;

        private MissingMappingAction _missingMappingAction = System.Data.MissingMappingAction.Passthrough;
        private MissingSchemaAction _missingSchemaAction = System.Data.MissingSchemaAction.Add;
        private DataTableMappingCollection _tableMappings;

        private static int s_objectTypeCount; // Bid counter
        internal readonly int _objectID = System.Threading.Interlocked.Increment(ref s_objectTypeCount);

#if DEBUG
        // if true, we are asserting that the caller has provided a select command
        // which should not return an empty result set
        private bool _debugHookNonEmptySelectCommand = false;
#endif

        [Conditional("DEBUG")]
        private void AssertReaderHandleFieldCount(DataReaderContainer readerHandler)
        {
#if DEBUG
            Debug.Assert(!_debugHookNonEmptySelectCommand || readerHandler.FieldCount > 0, "Scenario expects non-empty results but no fields reported by reader");
#endif
        }

        [Conditional("DEBUG")]
        private void AssertSchemaMapping(SchemaMapping mapping)
        {
#if DEBUG
            if (_debugHookNonEmptySelectCommand)
            {
                Debug.Assert(mapping != null && mapping.DataValues != null && mapping.DataTable != null, "Debug hook specifies that non-empty results are not expected");
            }
#endif
        }

        protected DataAdapter() : base()
        {
            GC.SuppressFinalize(this);
        }

        protected DataAdapter(DataAdapter from) : base()
        {
            CloneFrom(from);
        }

        [DefaultValue(true)]
        public bool AcceptChangesDuringFill
        {
            get
            {
                return _acceptChangesDuringFill;
            }
            set
            {
                _acceptChangesDuringFill = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeAcceptChangesDuringFill()
        {
            return (0 == _fillLoadOption);
        }

        [DefaultValue(true)]
        public bool AcceptChangesDuringUpdate
        {
            get { return _acceptChangesDuringUpdate; }
            set { _acceptChangesDuringUpdate = value; }
        }

        [DefaultValue(false)]
        public bool ContinueUpdateOnError
        {
            get { return _continueUpdateOnError; }
            set { _continueUpdateOnError = value; }
        }

        [RefreshProperties(RefreshProperties.All)]
        public LoadOption FillLoadOption
        {
            get
            {
                LoadOption fillLoadOption = _fillLoadOption;
                return ((0 != fillLoadOption) ? _fillLoadOption : LoadOption.OverwriteChanges);
            }
            set
            {
                switch (value)
                {
                    case 0: // to allow simple resetting
                    case LoadOption.OverwriteChanges:
                    case LoadOption.PreserveChanges:
                    case LoadOption.Upsert:
                        _fillLoadOption = value;
                        break;
                    default:
                        throw ADP.InvalidLoadOption(value);
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ResetFillLoadOption()
        {
            _fillLoadOption = 0;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeFillLoadOption() => 0 != _fillLoadOption;

        [DefaultValue(System.Data.MissingMappingAction.Passthrough)]
        public MissingMappingAction MissingMappingAction
        {
            get { return _missingMappingAction; }
            set
            {
                switch (value)
                {
                    case MissingMappingAction.Passthrough:
                    case MissingMappingAction.Ignore:
                    case MissingMappingAction.Error:
                        _missingMappingAction = value;
                        break;
                    default:
                        throw ADP.InvalidMissingMappingAction(value);
                }
            }
        }

        [DefaultValue(Data.MissingSchemaAction.Add)]
        public MissingSchemaAction MissingSchemaAction
        {
            get { return _missingSchemaAction; }
            set
            {
                switch (value)
                {
                    case MissingSchemaAction.Add:
                    case MissingSchemaAction.Ignore:
                    case MissingSchemaAction.Error:
                    case MissingSchemaAction.AddWithKey:
                        _missingSchemaAction = value;
                        break;
                    default:
                        throw ADP.InvalidMissingSchemaAction(value);
                }
            }
        }

        internal int ObjectID => _objectID;

        [DefaultValue(false)]
        public virtual bool ReturnProviderSpecificTypes
        {
            get { return _returnProviderSpecificTypes; }
            set { _returnProviderSpecificTypes = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public DataTableMappingCollection TableMappings
        {
            get
            {
                DataTableMappingCollection mappings = _tableMappings;
                if (null == mappings)
                {
                    mappings = CreateTableMappings();
                    if (null == mappings)
                    {
                        mappings = new DataTableMappingCollection();
                    }
                    _tableMappings = mappings;
                }
                return mappings; // constructed by base class
            }
        }

        ITableMappingCollection IDataAdapter.TableMappings => TableMappings;

        protected virtual bool ShouldSerializeTableMappings() => true;

        protected bool HasTableMappings() => ((null != _tableMappings) && (0 < TableMappings.Count));

        public event FillErrorEventHandler FillError
        {
            add
            {
                _hasFillErrorHandler = true;
                Events.AddHandler(s_eventFillError, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventFillError, value);
            }
        }

        [Obsolete("CloneInternals() has been deprecated.  Use the DataAdapter(DataAdapter from) constructor.  https://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual DataAdapter CloneInternals()
        {
            DataAdapter clone = (DataAdapter)Activator.CreateInstance(GetType(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, null, CultureInfo.InvariantCulture, null);
            clone.CloneFrom(this);
            return clone;
        }

        private void CloneFrom(DataAdapter from)
        {
            _acceptChangesDuringUpdate = from._acceptChangesDuringUpdate;
            _acceptChangesDuringUpdateAfterInsert = from._acceptChangesDuringUpdateAfterInsert;
            _continueUpdateOnError = from._continueUpdateOnError;
            _returnProviderSpecificTypes = from._returnProviderSpecificTypes;
            _acceptChangesDuringFill = from._acceptChangesDuringFill;
            _fillLoadOption = from._fillLoadOption;
            _missingMappingAction = from._missingMappingAction;
            _missingSchemaAction = from._missingSchemaAction;

            if ((null != from._tableMappings) && (0 < from.TableMappings.Count))
            {
                DataTableMappingCollection parameters = TableMappings;
                foreach (object parameter in from.TableMappings)
                {
                    parameters.Add((parameter is ICloneable) ? ((ICloneable)parameter).Clone() : parameter);
                }
            }
        }

        protected virtual DataTableMappingCollection CreateTableMappings()
        {
            DataCommonEventSource.Log.Trace("<comm.DataAdapter.CreateTableMappings|API> {0}", ObjectID);
            return new DataTableMappingCollection();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // release mananged objects
                _tableMappings = null;
            }
            // release unmanaged objects

            base.Dispose(disposing); // notify base classes
        }

        public virtual DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType)
        {
            throw ADP.NotSupported();
        }

        protected virtual DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, string srcTable, IDataReader dataReader)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.FillSchema|API> {0}, dataSet, schemaType={1}, srcTable, dataReader", ObjectID, schemaType);
            try
            {
                if (null == dataSet)
                {
                    throw ADP.ArgumentNull(nameof(dataSet));
                }
                if ((SchemaType.Source != schemaType) && (SchemaType.Mapped != schemaType))
                {
                    throw ADP.InvalidSchemaType(schemaType);
                }
                if (string.IsNullOrEmpty(srcTable))
                {
                    throw ADP.FillSchemaRequiresSourceTableName(nameof(srcTable));
                }
                if ((null == dataReader) || dataReader.IsClosed)
                {
                    throw ADP.FillRequires(nameof(dataReader));
                }
                // user must Close/Dispose of the dataReader
                object value = FillSchemaFromReader(dataSet, null, schemaType, srcTable, dataReader);
                return (DataTable[])value;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual DataTable FillSchema(DataTable dataTable, SchemaType schemaType, IDataReader dataReader)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.FillSchema|API> {0}, dataTable, schemaType, dataReader", ObjectID);
            try
            {
                if (null == dataTable)
                {
                    throw ADP.ArgumentNull(nameof(dataTable));
                }
                if ((SchemaType.Source != schemaType) && (SchemaType.Mapped != schemaType))
                {
                    throw ADP.InvalidSchemaType(schemaType);
                }
                if ((null == dataReader) || dataReader.IsClosed)
                {
                    throw ADP.FillRequires(nameof(dataReader));
                }
                // user must Close/Dispose of the dataReader
                // user will have to call NextResult to access remaining results
                object value = FillSchemaFromReader(null, dataTable, schemaType, null, dataReader);
                return (DataTable)value;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal object FillSchemaFromReader(DataSet dataset, DataTable datatable, SchemaType schemaType, string srcTable, IDataReader dataReader)
        {
            DataTable[] dataTables = null;
            int schemaCount = 0;
            do
            {
                DataReaderContainer readerHandler = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);

                AssertReaderHandleFieldCount(readerHandler);
                if (0 >= readerHandler.FieldCount)
                {
                    continue;
                }
                string tmp = null;
                if (null != dataset)
                {
                    tmp = DataAdapter.GetSourceTableName(srcTable, schemaCount);
                    schemaCount++; // don't increment if no SchemaTable ( a non-row returning result )
                }

                SchemaMapping mapping = new SchemaMapping(this, dataset, datatable, readerHandler, true, schemaType, tmp, false, null, null);

                if (null != datatable)
                {
                    // do not read remaining results in single DataTable case
                    return mapping.DataTable;
                }
                else if (null != mapping.DataTable)
                {
                    if (null == dataTables)
                    {
                        dataTables = new DataTable[1] { mapping.DataTable };
                    }
                    else
                    {
                        dataTables = DataAdapter.AddDataTableToArray(dataTables, mapping.DataTable);
                    }
                }
            } while (dataReader.NextResult()); // FillSchema does not capture errors for FillError event

            object value = dataTables;
            if ((null == value) && (null == datatable))
            {
                value = Array.Empty<DataTable>();
            }
            return value; // null if datatable had no results
        }

        public virtual int Fill(DataSet dataSet)
        {
            throw ADP.NotSupported();
        }

        protected virtual int Fill(DataSet dataSet, string srcTable, IDataReader dataReader, int startRecord, int maxRecords)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.Fill|API> {0}, dataSet, srcTable, dataReader, startRecord, maxRecords", ObjectID);
            try
            {
                if (null == dataSet)
                {
                    throw ADP.FillRequires(nameof(dataSet));
                }
                if (string.IsNullOrEmpty(srcTable))
                {
                    throw ADP.FillRequiresSourceTableName(nameof(srcTable));
                }
                if (null == dataReader)
                {
                    throw ADP.FillRequires(nameof(dataReader));
                }
                if (startRecord < 0)
                {
                    throw ADP.InvalidStartRecord(nameof(startRecord), startRecord);
                }
                if (maxRecords < 0)
                {
                    throw ADP.InvalidMaxRecords(nameof(maxRecords), maxRecords);
                }
                if (dataReader.IsClosed)
                {
                    return 0;
                }
                // user must Close/Dispose of the dataReader
                DataReaderContainer readerHandler = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
                return FillFromReader(dataSet, null, srcTable, readerHandler, startRecord, maxRecords, null, null);
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        protected virtual int Fill(DataTable dataTable, IDataReader dataReader)
        {
            DataTable[] dataTables = new DataTable[] { dataTable };
            return Fill(dataTables, dataReader, 0, 0);
        }

        protected virtual int Fill(DataTable[] dataTables, IDataReader dataReader, int startRecord, int maxRecords)
        {
            long logScopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.Fill|API> {0}, dataTables[], dataReader, startRecord, maxRecords", ObjectID);
            try
            {
                ADP.CheckArgumentLength(dataTables, nameof(dataTables));
                if ((null == dataTables) || (0 == dataTables.Length) || (null == dataTables[0]))
                {
                    throw ADP.FillRequires("dataTable");
                }
                if (null == dataReader)
                {
                    throw ADP.FillRequires(nameof(dataReader));
                }
                if ((1 < dataTables.Length) && ((0 != startRecord) || (0 != maxRecords)))
                {
                    throw ADP.NotSupported(); // FillChildren is not supported with FillPage
                }

                int result = 0;
                bool enforceContraints = false;
                DataSet commonDataSet = dataTables[0].DataSet;
                try
                {
                    if (null != commonDataSet)
                    {
                        enforceContraints = commonDataSet.EnforceConstraints;
                        commonDataSet.EnforceConstraints = false;
                    }
                    for (int i = 0; i < dataTables.Length; ++i)
                    {
                        Debug.Assert(null != dataTables[i], "null DataTable Fill");

                        if (dataReader.IsClosed)
                        {
#if DEBUG
                            Debug.Assert(!_debugHookNonEmptySelectCommand, "Debug hook asserts data reader should be open");
#endif
                            break;
                        }
                        DataReaderContainer readerHandler = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
                        AssertReaderHandleFieldCount(readerHandler);
                        if (readerHandler.FieldCount <= 0)
                        {
                            if (i == 0)
                            {
                                bool lastFillNextResult;
                                do
                                {
                                    lastFillNextResult = FillNextResult(readerHandler);
                                }
                                while (lastFillNextResult && readerHandler.FieldCount <= 0);
                                if (!lastFillNextResult)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if ((0 < i) && !FillNextResult(readerHandler))
                        {
                            break;
                        }
                        // user must Close/Dispose of the dataReader
                        // user will have to call NextResult to access remaining results
                        int count = FillFromReader(null, dataTables[i], null, readerHandler, startRecord, maxRecords, null, null);
                        if (0 == i)
                        {
                            result = count;
                        }
                    }
                }
                catch (ConstraintException)
                {
                    enforceContraints = false;
                    throw;
                }
                finally
                {
                    if (enforceContraints)
                    {
                        commonDataSet.EnforceConstraints = true;
                    }
                }
                return result;
            }
            finally
            {
                DataCommonEventSource.Log.ExitScope(logScopeId);
            }
        }

        internal int FillFromReader(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int startRecord, int maxRecords, DataColumn parentChapterColumn, object parentChapterValue)
        {
            int rowsAddedToDataSet = 0;
            int schemaCount = 0;
            do
            {
                AssertReaderHandleFieldCount(dataReader);
                if (0 >= dataReader.FieldCount)
                {
                    continue; // loop to next result
                }

                SchemaMapping mapping = FillMapping(dataset, datatable, srcTable, dataReader, schemaCount, parentChapterColumn, parentChapterValue);
                schemaCount++; // don't increment if no SchemaTable ( a non-row returning result )

                AssertSchemaMapping(mapping);

                if (null == mapping)
                {
                    continue; // loop to next result
                }
                if (null == mapping.DataValues)
                {
                    continue; // loop to next result
                }
                if (null == mapping.DataTable)
                {
                    continue; // loop to next result
                }
                mapping.DataTable.BeginLoadData();
                try
                {
                    // startRecord and maxRecords only apply to the first resultset
                    if ((1 == schemaCount) && ((0 < startRecord) || (0 < maxRecords)))
                    {
                        rowsAddedToDataSet = FillLoadDataRowChunk(mapping, startRecord, maxRecords);
                    }
                    else
                    {
                        int count = FillLoadDataRow(mapping);

                        if (1 == schemaCount)
                        {
                            // only return LoadDataRow count for first resultset
                            // not secondary or chaptered results
                            rowsAddedToDataSet = count;
                        }
                    }
                }
                finally
                {
                    mapping.DataTable.EndLoadData();
                }
                if (null != datatable)
                {
                    break; // do not read remaining results in single DataTable case
                }
            } while (FillNextResult(dataReader));

            return rowsAddedToDataSet;
        }

        private int FillLoadDataRowChunk(SchemaMapping mapping, int startRecord, int maxRecords)
        {
            DataReaderContainer dataReader = mapping.DataReader;

            while (0 < startRecord)
            {
                if (!dataReader.Read())
                {
                    // there are no more rows on first resultset
                    return 0;
                }
                --startRecord;
            }

            int rowsAddedToDataSet = 0;
            if (0 < maxRecords)
            {
                while ((rowsAddedToDataSet < maxRecords) && dataReader.Read())
                {
                    if (_hasFillErrorHandler)
                    {
                        try
                        {
                            mapping.LoadDataRowWithClear();
                            rowsAddedToDataSet++;
                        }
                        catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                        {
                            ADP.TraceExceptionForCapture(e);
                            OnFillErrorHandler(e, mapping.DataTable, mapping.DataValues);
                        }
                    }
                    else
                    {
                        mapping.LoadDataRow();
                        rowsAddedToDataSet++;
                    }
                }
                // skip remaining rows of the first resultset
            }
            else
            {
                rowsAddedToDataSet = FillLoadDataRow(mapping);
            }
            return rowsAddedToDataSet;
        }

        private int FillLoadDataRow(SchemaMapping mapping)
        {
            int rowsAddedToDataSet = 0;
            DataReaderContainer dataReader = mapping.DataReader;
            if (_hasFillErrorHandler)
            {
                while (dataReader.Read())
                { // read remaining rows of first and subsequent resultsets
                    try
                    {
                        // only try-catch if a FillErrorEventHandler is registered so that
                        // in the default case we get the full callstack from users
                        mapping.LoadDataRowWithClear();
                        rowsAddedToDataSet++;
                    }
                    catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                    {
                        ADP.TraceExceptionForCapture(e);
                        OnFillErrorHandler(e, mapping.DataTable, mapping.DataValues);
                    }
                }
            }
            else
            {
                while (dataReader.Read())
                {
                    // read remaining rows of first and subsequent resultset
                    mapping.LoadDataRow();
                    rowsAddedToDataSet++;
                }
            }
            return rowsAddedToDataSet;
        }

        private SchemaMapping FillMappingInternal(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int schemaCount, DataColumn parentChapterColumn, object parentChapterValue)
        {
            bool withKeyInfo = (Data.MissingSchemaAction.AddWithKey == MissingSchemaAction);
            string tmp = null;
            if (null != dataset)
            {
                tmp = DataAdapter.GetSourceTableName(srcTable, schemaCount);
            }
            return new SchemaMapping(this, dataset, datatable, dataReader, withKeyInfo, SchemaType.Mapped, tmp, true, parentChapterColumn, parentChapterValue);
        }

        private SchemaMapping FillMapping(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int schemaCount, DataColumn parentChapterColumn, object parentChapterValue)
        {
            SchemaMapping mapping = null;
            if (_hasFillErrorHandler)
            {
                try
                {
                    // only try-catch if a FillErrorEventHandler is registered so that
                    // in the default case we get the full callstack from users
                    mapping = FillMappingInternal(dataset, datatable, srcTable, dataReader, schemaCount, parentChapterColumn, parentChapterValue);
                }
                catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                {
                    ADP.TraceExceptionForCapture(e);
                    OnFillErrorHandler(e, null, null);
                }
            }
            else
            {
                mapping = FillMappingInternal(dataset, datatable, srcTable, dataReader, schemaCount, parentChapterColumn, parentChapterValue);
            }
            return mapping;
        }

        private bool FillNextResult(DataReaderContainer dataReader)
        {
            bool result = true;
            if (_hasFillErrorHandler)
            {
                try
                {
                    // only try-catch if a FillErrorEventHandler is registered so that
                    // in the default case we get the full callstack from users
                    result = dataReader.NextResult();
                }
                catch (Exception e) when (ADP.IsCatchableExceptionType(e))
                {
                    ADP.TraceExceptionForCapture(e);
                    OnFillErrorHandler(e, null, null);
                }
            }
            else
            {
                result = dataReader.NextResult();
            }
            return result;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public virtual IDataParameter[] GetFillParameters() => Array.Empty<IDataParameter>();

        internal DataTableMapping GetTableMappingBySchemaAction(string sourceTableName, string dataSetTableName, MissingMappingAction mappingAction)
        {
            return DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMappings, sourceTableName, dataSetTableName, mappingAction);
        }

        internal int IndexOfDataSetTable(string dataSetTable)
        {
            if (null != _tableMappings)
            {
                return TableMappings.IndexOfDataSetTable(dataSetTable);
            }
            return -1;
        }

        protected virtual void OnFillError(FillErrorEventArgs value)
        {
            ((FillErrorEventHandler)Events[s_eventFillError])?.Invoke(this, value);
        }

        private void OnFillErrorHandler(Exception e, DataTable dataTable, object[] dataValues)
        {
            FillErrorEventArgs fillErrorEvent = new FillErrorEventArgs(dataTable, dataValues);
            fillErrorEvent.Errors = e;
            OnFillError(fillErrorEvent);

            if (!fillErrorEvent.Continue)
            {
                if (null != fillErrorEvent.Errors)
                {
                    throw fillErrorEvent.Errors;
                }
                throw e;
            }
        }

        public virtual int Update(DataSet dataSet)
        {
            throw ADP.NotSupported();
        }

        // used by FillSchema which returns an array of datatables added to the dataset
        private static DataTable[] AddDataTableToArray(DataTable[] tables, DataTable newTable)
        {
            for (int i = 0; i < tables.Length; ++i)
            { // search for duplicates
                if (tables[i] == newTable)
                {
                    return tables; // duplicate found
                }
            }
            DataTable[] newTables = new DataTable[tables.Length + 1]; // add unique data table
            for (int i = 0; i < tables.Length; ++i)
            {
                newTables[i] = tables[i];
            }
            newTables[tables.Length] = newTable;
            return newTables;
        }

        // dynamically generate source table names
        private static string GetSourceTableName(string srcTable, int index)
        {
            //if ((null != srcTable) && (0 <= index) && (index < srcTable.Length)) {
            if (0 == index)
            {
                return srcTable; //[index];
            }
            return srcTable + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    internal sealed class LoadAdapter : DataAdapter
    {
        internal LoadAdapter() { }

        internal int FillFromReader(DataTable[] dataTables, IDataReader dataReader, int startRecord, int maxRecords)
        {
            return Fill(dataTables, dataReader, startRecord, maxRecords);
        }
    }
}

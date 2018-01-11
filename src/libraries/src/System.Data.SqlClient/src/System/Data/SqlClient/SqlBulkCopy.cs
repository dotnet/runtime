// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Data.SqlClient
{
    // This internal class helps us to associate the metadata from the target
    // with the ColumnOrdinals from the source.
    internal sealed class _ColumnMapping
    {
        internal int _sourceColumnOrdinal;
        internal _SqlMetaData _metadata;

        internal _ColumnMapping(int columnId, _SqlMetaData metadata)
        {
            _sourceColumnOrdinal = columnId;
            _metadata = metadata;
        }
    }

    internal sealed class Row
    {
        private object[] _dataFields;

        internal Row(int rowCount)
        {
            _dataFields = new object[rowCount];
        }

        internal object[] DataFields
        {
            get
            {
                return _dataFields;
            }
        }

        internal object this[int index]
        {
            get
            {
                return _dataFields[index];
            }
        }
    }

    // The controlling class for one result (metadata + rows)
    internal sealed class Result
    {
        private readonly _SqlMetaDataSet _metadata;
        private readonly List<Row> _rowset;

        internal Result(_SqlMetaDataSet metadata)
        {
            _metadata = metadata;
            _rowset = new List<Row>();
        }

        internal int Count => _rowset.Count;

        internal _SqlMetaDataSet MetaData => _metadata;

        internal Row this[int index] => _rowset[index];

        internal void AddRow(Row row)
        {
            _rowset.Add(row);
        }
    }

    // A wrapper object for metadata and rowsets returned by our initial queries
    internal sealed class BulkCopySimpleResultSet
    {
        private readonly List<Result> _results;        // The list of results
        private Result _resultSet;                     // The current result
        private int[] _indexmap;                       // Associates columnids with indexes in the rowarray

        internal BulkCopySimpleResultSet()
        {
            _results = new List<Result>();
        }

        // Indexer
        internal Result this[int idx] => _results[idx];

        // Callback function for the tdsparser
        // (note that setting the metadata adds a resultset)
        internal void SetMetaData(_SqlMetaDataSet metadata)
        {
            _resultSet = new Result(metadata);
            _results.Add(_resultSet);

            _indexmap = new int[_resultSet.MetaData.Length];
            for (int i = 0; i < _indexmap.Length; i++)
            {
                _indexmap[i] = i;
            }
        }

        // Callback function for the tdsparser.
        // This will create an indexmap for the active resultset.
        internal int[] CreateIndexMap() => _indexmap;

        // Callback function for the tdsparser.
        // This will return an array of rows to store the rowdata.
        internal object[] CreateRowBuffer()
        {
            Row row = new Row(_resultSet.MetaData.Length);
            _resultSet.AddRow(row);
            return row.DataFields;
        }
    }

    public sealed class SqlBulkCopy : IDisposable
    {
        private enum ValueSourceType
        {
            Unspecified = 0,
            IDataReader,
            DataTable,
            RowArray,
            DbDataReader
        }

        // Enum for specifying SqlDataReader.Get method used
        private enum ValueMethod : byte
        {
            GetValue,
            SqlTypeSqlDecimal,
            SqlTypeSqlDouble,
            SqlTypeSqlSingle,
            DataFeedStream,
            DataFeedText,
            DataFeedXml
        }

        // Used to hold column metadata for SqlDataReader case
        private readonly struct SourceColumnMetadata
        {
            public SourceColumnMetadata(ValueMethod method, bool isSqlType, bool isDataFeed)
            {
                Method = method;
                IsSqlType = isSqlType;
                IsDataFeed = isDataFeed;
            }

            public readonly ValueMethod Method;
            public readonly bool IsSqlType;
            public readonly bool IsDataFeed;
        }

        // The initial query will return three tables.
        // Transaction count has only one value in one column and one row
        // MetaData has n columns but no rows
        // Collation has 4 columns and n rows

        private const int MetaDataResultId = 1;

        private const int CollationResultId = 2;
        private const int CollationId = 3;

        private const int MAX_LENGTH = 0x7FFFFFFF;

        private const int DefaultCommandTimeout = 30;

        private bool _enableStreaming = false;
        private int _batchSize;
        private bool _ownConnection;
        private SqlBulkCopyOptions _copyOptions;
        private int _timeout = DefaultCommandTimeout;
        private string _destinationTableName;
        private int _rowsCopied;
        private int _notifyAfter;
        private int _rowsUntilNotification;
        private bool _insideRowsCopiedEvent;

        private object _rowSource;
        private SqlDataReader _SqlDataReaderRowSource;
        private DbDataReader _DbDataReaderRowSource;
        private DataTable _dataTableSource;

        private SqlBulkCopyColumnMappingCollection _columnMappings;
        private SqlBulkCopyColumnMappingCollection _localColumnMappings;

        private SqlConnection _connection;
        private SqlTransaction _internalTransaction;
        private SqlTransaction _externalTransaction;

        private ValueSourceType _rowSourceType = ValueSourceType.Unspecified;
        private DataRow _currentRow;
        private int _currentRowLength;
        private DataRowState _rowStateToSkip;
        private IEnumerator _rowEnumerator;

        private TdsParser _parser;
        private TdsParserStateObject _stateObj;
        private List<_ColumnMapping> _sortedColumnMappings;

        private SqlRowsCopiedEventHandler _rowsCopiedEventHandler;

        // Newly added member variables for Async modification, m = member variable to bcp.
        private int _savedBatchSize = 0; // Save the batchsize so that changes are not affected unexpectedly.
        private bool _hasMoreRowToCopy = false;
        private bool _isAsyncBulkCopy = false;
        private bool _isBulkCopyingInProgress = false;
        private SqlInternalConnectionTds.SyncAsyncLock _parserLock = null;

        private SourceColumnMetadata[] _currentRowMetadata;

#if DEBUG
        internal static bool _setAlwaysTaskOnWrite = false; //when set and in DEBUG mode, TdsParser::WriteBulkCopyValue will always return a task
        internal static bool SetAlwaysTaskOnWrite
        {
            set
            {
                _setAlwaysTaskOnWrite = value;
            }
            get
            {
                return _setAlwaysTaskOnWrite;
            }
        }
#endif

        public SqlBulkCopy(SqlConnection connection)
        {
            if (connection == null)
            {
                throw ADP.ArgumentNull(nameof(connection));
            }
            _connection = connection;
            _columnMappings = new SqlBulkCopyColumnMappingCollection();
        }

        public SqlBulkCopy(SqlConnection connection, SqlBulkCopyOptions copyOptions, SqlTransaction externalTransaction)
            : this(connection)
        {
            _copyOptions = copyOptions;
            if (externalTransaction != null && IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction))
            {
                throw SQL.BulkLoadConflictingTransactionOption();
            }

            if (!IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction))
            {
                _externalTransaction = externalTransaction;
            }
        }

        public SqlBulkCopy(string connectionString)
        {
            if (connectionString == null)
            {
                throw ADP.ArgumentNull(nameof(connectionString));
            }
            _connection = new SqlConnection(connectionString);
            _columnMappings = new SqlBulkCopyColumnMappingCollection();
            _ownConnection = true;
        }

        public SqlBulkCopy(string connectionString, SqlBulkCopyOptions copyOptions)
            : this(connectionString)
        {
            _copyOptions = copyOptions;
        }

        public int BatchSize
        {
            get
            {
                return _batchSize;
            }
            set
            {
                if (value >= 0)
                {
                    _batchSize = value;
                }
                else
                {
                    throw ADP.ArgumentOutOfRange(nameof(BatchSize));
                }
            }
        }

        public int BulkCopyTimeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                if (value < 0)
                {
                    throw SQL.BulkLoadInvalidTimeout(value);
                }
                _timeout = value;
            }
        }

        public bool EnableStreaming
        {
            get
            {
                return _enableStreaming;
            }
            set
            {
                _enableStreaming = value;
            }
        }

        public SqlBulkCopyColumnMappingCollection ColumnMappings
        {
            get
            {
                return _columnMappings;
            }
        }

        public string DestinationTableName
        {
            get
            {
                return _destinationTableName;
            }
            set
            {
                if (value == null)
                {
                    throw ADP.ArgumentNull(nameof(DestinationTableName));
                }
                else if (value.Length == 0)
                {
                    throw ADP.ArgumentOutOfRange(nameof(DestinationTableName));
                }
                _destinationTableName = value;
            }
        }

        public int NotifyAfter
        {
            get
            {
                return _notifyAfter;
            }
            set
            {
                if (value >= 0)
                {
                    _notifyAfter = value;
                }
                else
                {
                    throw ADP.ArgumentOutOfRange(nameof(NotifyAfter));
                }
            }
        }

        public event SqlRowsCopiedEventHandler SqlRowsCopied
        {
            add
            {
                _rowsCopiedEventHandler += value;
            }
            remove
            {
                _rowsCopiedEventHandler -= value;
            }
        }

        internal SqlStatistics Statistics
        {
            get
            {
                if (null != _connection)
                {
                    if (_connection.StatisticsEnabled)
                    {
                        return _connection.Statistics;
                    }
                }
                return null;
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool IsCopyOption(SqlBulkCopyOptions copyOption) => ((_copyOptions & copyOption) == copyOption);

        //Creates the initial query string, but does not execute it.
        private string CreateInitialQuery()
        {
            string[] parts;
            try
            {
                parts = MultipartIdentifier.ParseMultipartIdentifier(this.DestinationTableName, "[\"", "]\"", SR.SQL_BulkCopyDestinationTableName, true);
            }
            catch (Exception e)
            {
                throw SQL.BulkLoadInvalidDestinationTable(this.DestinationTableName, e);
            }
            if (string.IsNullOrEmpty(parts[MultipartIdentifier.TableIndex]))
            {
                throw SQL.BulkLoadInvalidDestinationTable(this.DestinationTableName, null);
            }
            string TDSCommand;


            TDSCommand = "select @@trancount; SET FMTONLY ON select * from " + this.DestinationTableName + " SET FMTONLY OFF ";

            string TableCollationsStoredProc;
            if (_connection.IsKatmaiOrNewer)
            {
                TableCollationsStoredProc = "sp_tablecollations_100";
            }
            else
            {
                TableCollationsStoredProc = "sp_tablecollations_90";
            }

            string TableName = parts[MultipartIdentifier.TableIndex];
            bool isTempTable = TableName.Length > 0 && '#' == TableName[0];
            if (!string.IsNullOrEmpty(TableName))
            {
                // Escape table name to be put inside TSQL literal block (within N'').
                TableName = SqlServerEscapeHelper.EscapeStringAsLiteral(TableName);
                // Escape the table name
                TableName = SqlServerEscapeHelper.EscapeIdentifier(TableName);
            }

            string SchemaName = parts[MultipartIdentifier.SchemaIndex];
            if (!string.IsNullOrEmpty(SchemaName))
            {
                // Escape schema name to be put inside TSQL literal block (within N'').
                SchemaName = SqlServerEscapeHelper.EscapeStringAsLiteral(SchemaName);
                // Escape the schema name
                SchemaName = SqlServerEscapeHelper.EscapeIdentifier(SchemaName);
            }

            string CatalogName = parts[MultipartIdentifier.CatalogIndex];
            if (isTempTable && string.IsNullOrEmpty(CatalogName))
            {
                TDSCommand += String.Format((IFormatProvider)null, "exec tempdb..{0} N'{1}.{2}'",
                    TableCollationsStoredProc,
                    SchemaName,
                    TableName
                );
            }
            else
            {
                // Escape the catalog name
                if (!string.IsNullOrEmpty(CatalogName))
                {
                    CatalogName = SqlServerEscapeHelper.EscapeIdentifier(CatalogName);
                }
                TDSCommand += String.Format((IFormatProvider)null, "exec {0}..{1} N'{2}.{3}'",
                    CatalogName,
                    TableCollationsStoredProc,
                    SchemaName,
                    TableName
                );
            }
            return TDSCommand;
        }

        // Creates and then executes initial query to get information about the targettable
        // When __isAsyncBulkCopy == false (i.e. it is Sync copy): out result contains the resulset. Returns null.
        // When __isAsyncBulkCopy == true (i.e. it is Async copy): This still uses the _parser.Run method synchronously and return Task<BulkCopySimpleResultSet>.
        // We need to have a _parser.RunAsync to make it real async.
        private Task<BulkCopySimpleResultSet> CreateAndExecuteInitialQueryAsync(out BulkCopySimpleResultSet result)
        {
            string TDSCommand = CreateInitialQuery();

            Task executeTask = _parser.TdsExecuteSQLBatch(TDSCommand, this.BulkCopyTimeout, null, _stateObj, sync: !_isAsyncBulkCopy, callerHasConnectionLock: true);

            if (executeTask == null)
            {
                result = new BulkCopySimpleResultSet();
                RunParser(result);
                return null;
            }
            else
            {
                Debug.Assert(_isAsyncBulkCopy, "Execution pended when not doing async bulk copy");
                result = null;
                return executeTask.ContinueWith<BulkCopySimpleResultSet>(t =>
                {
                    Debug.Assert(!t.IsCanceled, "Execution task was canceled");
                    if (t.IsFaulted)
                    {
                        throw t.Exception.InnerException;
                    }
                    else
                    {
                        var internalResult = new BulkCopySimpleResultSet();
                        RunParserReliably(internalResult);
                        return internalResult;
                    }
                }, TaskScheduler.Default);
            }
        }

        // Matches associated columns with metadata from initial query.
        // Builds and executes the update bulk command.
        private string AnalyzeTargetAndCreateUpdateBulkCommand(BulkCopySimpleResultSet internalResults)
        {
            Debug.Assert(internalResults != null, "Where are the results from the initial query?");

            StringBuilder updateBulkCommandText = new StringBuilder();

            if (0 == internalResults[CollationResultId].Count)
            {
                throw SQL.BulkLoadNoCollation();
            }

            updateBulkCommandText.AppendFormat("insert bulk {0} (", this.DestinationTableName);
            int nmatched = 0;  // Number of columns that match and are accepted
            int nrejected = 0; // Number of columns that match but were rejected
            bool rejectColumn; // True if a column is rejected because of an excluded type

            bool isInTransaction;

            isInTransaction = _connection.HasLocalTransaction;
            // Throw if there is a transaction but no flag is set
            if (isInTransaction && null == _externalTransaction && null == _internalTransaction && (_connection.Parser != null && _connection.Parser.CurrentTransaction != null && _connection.Parser.CurrentTransaction.IsLocal))
            {
                throw SQL.BulkLoadExistingTransaction();
            }

            // Loop over the metadata for each column
            _SqlMetaDataSet metaDataSet = internalResults[MetaDataResultId].MetaData;
            _sortedColumnMappings = new List<_ColumnMapping>(metaDataSet.Length);
            for (int i = 0; i < metaDataSet.Length; i++)
            {
                _SqlMetaData metadata = metaDataSet[i];
                rejectColumn = false;

                // Check for excluded types
                if ((metadata.type == SqlDbType.Timestamp)
                    || ((metadata.isIdentity) && !IsCopyOption(SqlBulkCopyOptions.KeepIdentity)))
                {
                    // Remove metadata for excluded columns
                    metaDataSet[i] = null;
                    rejectColumn = true;
                    // We still need to find a matching column association
                }

                // Find out if this column is associated
                int assocId;
                for (assocId = 0; assocId < _localColumnMappings.Count; assocId++)
                {
                    if ((_localColumnMappings[assocId]._destinationColumnOrdinal == metadata.ordinal) ||
                        (UnquotedName(_localColumnMappings[assocId]._destinationColumnName) == metadata.column))
                    {
                        if (rejectColumn)
                        {
                            nrejected++; // Count matched columns only
                            break;
                        }

                        _sortedColumnMappings.Add(new _ColumnMapping(_localColumnMappings[assocId]._internalSourceColumnOrdinal, metadata));
                        nmatched++;

                        if (nmatched > 1)
                        {
                            updateBulkCommandText.Append(", "); // A leading comma for all but the first one
                        }

                        // Some datatypes need special handling ...
                        if (metadata.type == SqlDbType.Variant)
                        {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, "sql_variant");
                        }
                        else if (metadata.type == SqlDbType.Udt)
                        {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, "varbinary");
                        }
                        else
                        {
                            AppendColumnNameAndTypeName(updateBulkCommandText, metadata.column, metadata.type.ToString());
                        }

                        switch (metadata.metaType.NullableType)
                        {
                            case TdsEnums.SQLNUMERICN:
                            case TdsEnums.SQLDECIMALN:
                                // Decimal and numeric need to include precision and scale
                                updateBulkCommandText.AppendFormat((IFormatProvider)null, "({0},{1})", metadata.precision, metadata.scale);
                                break;
                            case TdsEnums.SQLUDT:
                                {
                                    if (metadata.IsLargeUdt)
                                    {
                                        updateBulkCommandText.Append("(max)");
                                    }
                                    else
                                    {
                                        int size = metadata.length;
                                        updateBulkCommandText.AppendFormat((IFormatProvider)null, "({0})", size);
                                    }
                                    break;
                                }
                            case TdsEnums.SQLTIME:
                            case TdsEnums.SQLDATETIME2:
                            case TdsEnums.SQLDATETIMEOFFSET:
                                // date, dateime2, and datetimeoffset need to include scale
                                updateBulkCommandText.AppendFormat((IFormatProvider)null, "({0})", metadata.scale);
                                break;
                            default:
                                {
                                    // For non-long non-fixed types we need to add the Size
                                    if (!metadata.metaType.IsFixed && !metadata.metaType.IsLong)
                                    {
                                        int size = metadata.length;
                                        switch (metadata.metaType.NullableType)
                                        {
                                            case TdsEnums.SQLNCHAR:
                                            case TdsEnums.SQLNVARCHAR:
                                            case TdsEnums.SQLNTEXT:
                                                size /= 2;
                                                break;
                                            default:
                                                break;
                                        }
                                        updateBulkCommandText.AppendFormat((IFormatProvider)null, "({0})", size);
                                    }
                                    else if (metadata.metaType.IsPlp && metadata.metaType.SqlDbType != SqlDbType.Xml)
                                    {
                                        // Partial length column prefix (max)
                                        updateBulkCommandText.Append("(max)");
                                    }
                                    break;
                                }
                        }

                        // Get collation for column i
                        Result rowset = internalResults[CollationResultId];
                        object rowvalue = rowset[i][CollationId];

                        bool shouldSendCollation;
                        switch (metadata.type)
                        {
                            case SqlDbType.Char:
                            case SqlDbType.NChar:
                            case SqlDbType.VarChar:
                            case SqlDbType.NVarChar:
                            case SqlDbType.Text:
                            case SqlDbType.NText:
                                shouldSendCollation = true;
                                break;

                            default:
                                shouldSendCollation = false;
                                break;
                        }

                        if (rowvalue != null && shouldSendCollation)
                        {
                            Debug.Assert(rowvalue is SqlString);
                            SqlString collation_name = (SqlString)rowvalue;
                            if (!collation_name.IsNull)
                            {
                                updateBulkCommandText.Append(" COLLATE " + collation_name.Value);
                                // Compare collations only if the collation value was set on the metadata
                                if (null != _SqlDataReaderRowSource && metadata.collation != null)
                                {
                                    // On SqlDataReader we can verify the sourcecolumn collation!
                                    int sourceColumnId = _localColumnMappings[assocId]._internalSourceColumnOrdinal;
                                    int destinationLcid = metadata.collation.LCID;
                                    int sourceLcid = _SqlDataReaderRowSource.GetLocaleId(sourceColumnId);
                                    if (sourceLcid != destinationLcid)
                                    {
                                        throw SQL.BulkLoadLcidMismatch(sourceLcid, _SqlDataReaderRowSource.GetName(sourceColumnId), destinationLcid, metadata.column);
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                if (assocId == _localColumnMappings.Count)
                {
                    // Remove metadata for unmatched columns
                    metaDataSet[i] = null;
                }
            }

            // All columnmappings should have matched up
            if (nmatched + nrejected != _localColumnMappings.Count)
            {
                throw (SQL.BulkLoadNonMatchingColumnMapping());
            }

            updateBulkCommandText.Append(")");

            if ((_copyOptions & (
                    SqlBulkCopyOptions.KeepNulls
                    | SqlBulkCopyOptions.TableLock
                    | SqlBulkCopyOptions.CheckConstraints
                    | SqlBulkCopyOptions.FireTriggers)) != SqlBulkCopyOptions.Default)
            {
                bool addSeparator = false; // Insert a comma character if multiple options in list
                updateBulkCommandText.Append(" with (");
                if (IsCopyOption(SqlBulkCopyOptions.KeepNulls))
                {
                    updateBulkCommandText.Append("KEEP_NULLS");
                    addSeparator = true;
                }
                if (IsCopyOption(SqlBulkCopyOptions.TableLock))
                {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "TABLOCK");
                    addSeparator = true;
                }
                if (IsCopyOption(SqlBulkCopyOptions.CheckConstraints))
                {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "CHECK_CONSTRAINTS");
                    addSeparator = true;
                }
                if (IsCopyOption(SqlBulkCopyOptions.FireTriggers))
                {
                    updateBulkCommandText.Append((addSeparator ? ", " : "") + "FIRE_TRIGGERS");
                    addSeparator = true;
                }
                updateBulkCommandText.Append(")");
            }
            return (updateBulkCommandText.ToString());
        }

        private Task SubmitUpdateBulkCommand(string TDSCommand)
        {
            Task executeTask = _parser.TdsExecuteSQLBatch(TDSCommand, this.BulkCopyTimeout, null, _stateObj, sync: !_isAsyncBulkCopy, callerHasConnectionLock: true);

            if (executeTask == null)
            {
                RunParser();
                return null;
            }
            else
            {
                Debug.Assert(_isAsyncBulkCopy, "Execution pended when not doing async bulk copy");
                return executeTask.ContinueWith(t =>
                {
                    Debug.Assert(!t.IsCanceled, "Execution task was canceled");
                    if (t.IsFaulted)
                    {
                        throw t.Exception.InnerException;
                    }
                    else
                    {
                        RunParserReliably();
                    }
                }, TaskScheduler.Default);
            }
        }

        // Starts writing the Bulkcopy data stream
        private void WriteMetaData(BulkCopySimpleResultSet internalResults)
        {
            _stateObj.SetTimeoutSeconds(this.BulkCopyTimeout);

            _SqlMetaDataSet metadataCollection = internalResults[MetaDataResultId].MetaData;
            _stateObj._outputMessageType = TdsEnums.MT_BULK;
            _parser.WriteBulkCopyMetaData(metadataCollection, _sortedColumnMappings.Count, _stateObj);
        }

        // Terminates the bulk copy operation.
        // Must be called at the end of the bulk copy session.
        public void Close()
        {
            if (_insideRowsCopiedEvent)
            {
                throw SQL.InvalidOperationInsideEvent();
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose dependent objects
                _columnMappings = null;
                _parser = null;
                try
                {
                    // Just in case there is a lingering transaction (which there shouldn't be)
                    try
                    {
                        Debug.Assert(_internalTransaction == null, "Internal transaction exists during dispose");
                        if (null != _internalTransaction)
                        {
                            _internalTransaction.Rollback();
                            _internalTransaction.Dispose();
                            _internalTransaction = null;
                        }
                    }
                    catch (Exception e)
                    {
                        if (!ADP.IsCatchableExceptionType(e))
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    if (_connection != null)
                    {
                        if (_ownConnection)
                        {
                            _connection.Dispose();
                        }
                        _connection = null;
                    }
                }
            }
        }

        // Unified method to read a value from the current row
        private object GetValueFromSourceRow(int destRowIndex, out bool isSqlType, out bool isDataFeed, out bool isNull)
        {
            _SqlMetaData metadata = _sortedColumnMappings[destRowIndex]._metadata;
            int sourceOrdinal = _sortedColumnMappings[destRowIndex]._sourceColumnOrdinal;

            switch (_rowSourceType)
            {
                case ValueSourceType.IDataReader:
                case ValueSourceType.DbDataReader:
                    // Handle data feeds (common for both DbDataReader and SqlDataReader)
                    if (_currentRowMetadata[destRowIndex].IsDataFeed)
                    {
                        if (_DbDataReaderRowSource.IsDBNull(sourceOrdinal))
                        {
                            isSqlType = false;
                            isDataFeed = false;
                            isNull = true;
                            return DBNull.Value;
                        }
                        else
                        {
                            isSqlType = false;
                            isDataFeed = true;
                            isNull = false;
                            switch (_currentRowMetadata[destRowIndex].Method)
                            {
                                case ValueMethod.DataFeedStream:
                                    return new StreamDataFeed(_DbDataReaderRowSource.GetStream(sourceOrdinal));
                                case ValueMethod.DataFeedText:
                                    return new TextDataFeed(_DbDataReaderRowSource.GetTextReader(sourceOrdinal));
                                case ValueMethod.DataFeedXml:
                                    // Only SqlDataReader supports an XmlReader
                                    // There is no GetXmlReader on DbDataReader, however if GetValue returns XmlReader we will read it as stream if it is assigned to XML field
                                    Debug.Assert(_SqlDataReaderRowSource != null, "Should not be reading row as an XmlReader if bulk copy source is not a SqlDataReader");
                                    return new XmlDataFeed(_SqlDataReaderRowSource.GetXmlReader(sourceOrdinal));
                                default:
                                    Debug.Fail(string.Format("Current column is marked as being a DataFeed, but no DataFeed compatible method was provided. Method: {0}", _currentRowMetadata[destRowIndex].Method));
                                    isDataFeed = false;
                                    object columnValue = _DbDataReaderRowSource.GetValue(sourceOrdinal);
                                    ADP.IsNullOrSqlType(columnValue, out isNull, out isSqlType);
                                    return columnValue;
                            }
                        }
                    }
                    // SqlDataReader-specific logic
                    else if (null != _SqlDataReaderRowSource)
                    {
                        if (_currentRowMetadata[destRowIndex].IsSqlType)
                        {
                            INullable value;
                            isSqlType = true;
                            isDataFeed = false;
                            switch (_currentRowMetadata[destRowIndex].Method)
                            {
                                case ValueMethod.SqlTypeSqlDecimal:
                                    value = _SqlDataReaderRowSource.GetSqlDecimal(sourceOrdinal);
                                    break;
                                case ValueMethod.SqlTypeSqlDouble:
                                    value = new SqlDecimal(_SqlDataReaderRowSource.GetSqlDouble(sourceOrdinal).Value);
                                    break;
                                case ValueMethod.SqlTypeSqlSingle:
                                    value = new SqlDecimal(_SqlDataReaderRowSource.GetSqlSingle(sourceOrdinal).Value);
                                    break;
                                default:
                                    Debug.Fail(string.Format("Current column is marked as being a SqlType, but no SqlType compatible method was provided. Method: {0}", _currentRowMetadata[destRowIndex].Method));
                                    value = (INullable)_SqlDataReaderRowSource.GetSqlValue(sourceOrdinal);
                                    break;
                            }

                            isNull = value.IsNull;
                            return value;
                        }
                        else
                        {
                            isSqlType = false;
                            isDataFeed = false;

                            object value = _SqlDataReaderRowSource.GetValue(sourceOrdinal);
                            isNull = ((value == null) || (value == DBNull.Value));
                            if ((!isNull) && (metadata.type == SqlDbType.Udt))
                            {
                                var columnAsINullable = value as INullable;
                                isNull = (columnAsINullable != null) && columnAsINullable.IsNull;
                            }
#if DEBUG
                            else if (!isNull)
                            {
                                Debug.Assert(!(value is INullable) || !((INullable)value).IsNull, "IsDBNull returned false, but GetValue returned a null INullable");
                            }
#endif
                            return value;
                        }
                    }
                    else
                    {
                        isDataFeed = false;

                        IDataReader rowSourceAsIDataReader = (IDataReader)_rowSource;

                        // Only use IsDbNull when streaming is enabled and only for non-SqlDataReader
                        if ((_enableStreaming) && (_SqlDataReaderRowSource == null) && (rowSourceAsIDataReader.IsDBNull(sourceOrdinal)))
                        {
                            isSqlType = false;
                            isNull = true;
                            return DBNull.Value;
                        }
                        else
                        {
                            object columnValue = rowSourceAsIDataReader.GetValue(sourceOrdinal);
                            ADP.IsNullOrSqlType(columnValue, out isNull, out isSqlType);
                            return columnValue;
                        }
                    }
                case ValueSourceType.DataTable:
                case ValueSourceType.RowArray:
                    {
                        Debug.Assert(_currentRow != null, "uninitialized _currentRow");
                        Debug.Assert(sourceOrdinal < _currentRowLength, "inconsistency of length of rows from rowsource!");

                        isDataFeed = false;
                        object currentRowValue = _currentRow[sourceOrdinal];
                        ADP.IsNullOrSqlType(currentRowValue, out isNull, out isSqlType);

                        // If this row is not null, and there are special storage types for this row, then handle the special storage types
                        if ((!isNull) && (_currentRowMetadata[destRowIndex].IsSqlType))
                        {
                            switch (_currentRowMetadata[destRowIndex].Method)
                            {
                                case ValueMethod.SqlTypeSqlSingle:
                                    {
                                        if (isSqlType)
                                        {
                                            return new SqlDecimal(((SqlSingle)currentRowValue).Value);
                                        }
                                        else
                                        {
                                            float f = (float)currentRowValue;
                                            if (!float.IsNaN(f))
                                            {
                                                isSqlType = true;
                                                return new SqlDecimal(f);
                                            }
                                            break;
                                        }
                                    }
                                case ValueMethod.SqlTypeSqlDouble:
                                    {
                                        if (isSqlType)
                                        {
                                            return new SqlDecimal(((SqlDouble)currentRowValue).Value);
                                        }
                                        else
                                        {
                                            double d = (double)currentRowValue;
                                            if (!double.IsNaN(d))
                                            {
                                                isSqlType = true;
                                                return new SqlDecimal(d);
                                            }
                                            break;
                                        }
                                    }
                                case ValueMethod.SqlTypeSqlDecimal:
                                    {
                                        if (isSqlType)
                                        {
                                            return (SqlDecimal)currentRowValue;
                                        }
                                        else
                                        {
                                            isSqlType = true;
                                            return new SqlDecimal((Decimal)currentRowValue);
                                        }
                                    }
                                default:
                                    {
                                        Debug.Fail(string.Format("Current column is marked as being a SqlType, but no SqlType compatible method was provided. Method: {0}", _currentRowMetadata[destRowIndex].Method));
                                        break;
                                    }
                            }
                        }

                        // If we are here then either the value is null, there was no special storage type for this column or the special storage type wasn't handled (e.g. if the currentRowValue is NaN)
                        return currentRowValue;
                    }
                default:
                    {
                        Debug.Fail("ValueSourcType unspecified");
                        throw ADP.NotSupported();
                    }
            }
        }

        // Unified method to read a row from the current rowsource.
        // When _isAsyncBulkCopy == true (i.e. async copy): returns Task<bool> when IDataReader is a DbDataReader, Null for others.
        // When _isAsyncBulkCopy == false (i.e. sync copy): returns null. Uses ReadFromRowSource to get the boolean value.
        // "more" -- should be used by the caller only when the return value is null.
        private Task ReadFromRowSourceAsync(CancellationToken cts)
        {
            if (_isAsyncBulkCopy && _DbDataReaderRowSource != null)
            {
                // This will call ReadAsync for DbDataReader (for SqlDataReader it will be truly async read; for non-SqlDataReader it may block.)
                return _DbDataReaderRowSource.ReadAsync(cts).ContinueWith((t) =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        _hasMoreRowToCopy = t.Result;
                    }
                    return t;
                }, TaskScheduler.Default).Unwrap();
            }
            else
            { // This will call Read for DataRows, DataTable and IDataReader (this includes all IDataReader except DbDataReader)
                _hasMoreRowToCopy = false;
                try
                {
                    _hasMoreRowToCopy = ReadFromRowSource(); // Synchronous calls for DataRows and DataTable won't block. For IDataReader, it may block.
                }
                catch (Exception ex)
                {
                    if (_isAsyncBulkCopy)
                    {
                        return Task.FromException<bool>(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                return null;
            }
        }

        private bool ReadFromRowSource()
        {
            switch (_rowSourceType)
            {
                case ValueSourceType.DbDataReader:
                case ValueSourceType.IDataReader:
                    return ((IDataReader)_rowSource).Read();

                // Treatment for RowArray case is same as for DataTable, prevent code duplicate
                case ValueSourceType.RowArray:
                case ValueSourceType.DataTable:
                    Debug.Assert(_rowEnumerator != null, "uninitialized _rowEnumerator");
                    Debug.Assert((_rowStateToSkip & DataRowState.Deleted) != 0, "Deleted is a permitted rowstate?");

                    // Repeat until we get a row that is not deleted or there are no more rows
                    do
                    {
                        if (!_rowEnumerator.MoveNext())
                        {
                            return false;
                        }
                        _currentRow = (DataRow)_rowEnumerator.Current;
                    } while ((_currentRow.RowState & _rowStateToSkip) != 0); // Repeat if there is an unexpected rowstate

                    _currentRowLength = _currentRow.ItemArray.Length;
                    return true;

                default:
                    Debug.Fail("ValueSourcType unspecified");
                    throw ADP.NotSupported();
            }
        }

        private SourceColumnMetadata GetColumnMetadata(int ordinal)
        {
            int sourceOrdinal = _sortedColumnMappings[ordinal]._sourceColumnOrdinal;
            _SqlMetaData metadata = _sortedColumnMappings[ordinal]._metadata;

            // Handle special Sql data types for SqlDataReader and DataTables
            ValueMethod method;
            bool isSqlType;
            bool isDataFeed;
            if (((_SqlDataReaderRowSource != null) || (_dataTableSource != null)) && ((metadata.metaType.NullableType == TdsEnums.SQLDECIMALN) || (metadata.metaType.NullableType == TdsEnums.SQLNUMERICN)))
            {
                isDataFeed = false;

                Type t;
                switch (_rowSourceType)
                {
                    case ValueSourceType.DbDataReader:
                    case ValueSourceType.IDataReader:
                        t = _SqlDataReaderRowSource.GetFieldType(sourceOrdinal);
                        break;
                    case ValueSourceType.DataTable:
                    case ValueSourceType.RowArray:
                        t = _dataTableSource.Columns[sourceOrdinal].DataType;
                        break;
                    default:
                        t = null;
                        Debug.Fail(string.Format("Unknown value source: {0}", _rowSourceType));
                        break;
                }

                if (typeof(SqlDecimal) == t || typeof(Decimal) == t)
                {
                    isSqlType = true;
                    method = ValueMethod.SqlTypeSqlDecimal;  // Source Type Decimal
                }
                else if (typeof(SqlDouble) == t || typeof(double) == t)
                {
                    isSqlType = true;
                    method = ValueMethod.SqlTypeSqlDouble;  // Source Type SqlDouble
                }
                else if (typeof(SqlSingle) == t || typeof(float) == t)
                {
                    isSqlType = true;
                    method = ValueMethod.SqlTypeSqlSingle;  // Source Type SqlSingle
                }
                else
                {
                    isSqlType = false;
                    method = ValueMethod.GetValue;
                }
            }
            // Check for data streams
            else if ((_enableStreaming) && (metadata.length == MAX_LENGTH))
            {
                isSqlType = false;

                if (_SqlDataReaderRowSource != null)
                {
                    // MetaData property is not set for SMI, but since streaming is disabled we do not need it
                    MetaType mtSource = _SqlDataReaderRowSource.MetaData[sourceOrdinal].metaType;

                    // There is no memory gain for non-sequential access for binary
                    if ((metadata.type == SqlDbType.VarBinary) && (mtSource.IsBinType) && (mtSource.SqlDbType != SqlDbType.Timestamp) && _SqlDataReaderRowSource.IsCommandBehavior(CommandBehavior.SequentialAccess))
                    {
                        isDataFeed = true;
                        method = ValueMethod.DataFeedStream;
                    }
                    // For text and XML there is memory gain from streaming on destination side even if reader is non-sequential
                    else if (((metadata.type == SqlDbType.VarChar) || (metadata.type == SqlDbType.NVarChar)) && (mtSource.IsCharType) && (mtSource.SqlDbType != SqlDbType.Xml))
                    {
                        isDataFeed = true;
                        method = ValueMethod.DataFeedText;
                    }
                    else if ((metadata.type == SqlDbType.Xml) && (mtSource.SqlDbType == SqlDbType.Xml))
                    {
                        isDataFeed = true;
                        method = ValueMethod.DataFeedXml;
                    }
                    else
                    {
                        isDataFeed = false;
                        method = ValueMethod.GetValue;
                    }
                }
                else if (_DbDataReaderRowSource != null)
                {
                    if (metadata.type == SqlDbType.VarBinary)
                    {
                        isDataFeed = true;
                        method = ValueMethod.DataFeedStream;
                    }
                    else if ((metadata.type == SqlDbType.VarChar) || (metadata.type == SqlDbType.NVarChar))
                    {
                        isDataFeed = true;
                        method = ValueMethod.DataFeedText;
                    }
                    else
                    {
                        isDataFeed = false;
                        method = ValueMethod.GetValue;
                    }
                }
                else
                {
                    isDataFeed = false;
                    method = ValueMethod.GetValue;
                }
            }
            else
            {
                isSqlType = false;
                isDataFeed = false;
                method = ValueMethod.GetValue;
            }

            return new SourceColumnMetadata(method, isSqlType, isDataFeed);
        }

        private void CreateOrValidateConnection(string method)
        {
            if (null == _connection)
            {
                throw ADP.ConnectionRequired(method);
            }

            if (_ownConnection && _connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            // Close any non-MARS dead readers, if applicable, and then throw if still busy.
            _connection.ValidateConnectionForExecute(method, null);

            // If we have a transaction, check to ensure that the active
            // connection property matches the connection associated with
            // the transaction.
            if (null != _externalTransaction && _connection != _externalTransaction.Connection)
            {
                throw ADP.TransactionConnectionMismatch();
            }
        }

        // Runs the _parser until it is done and ensures that ThreadHasParserLockForClose is correctly set and unset
        // Ensure that you only call this inside of a Reliability Section
        private void RunParser(BulkCopySimpleResultSet bulkCopyHandler = null)
        {
            // In case of error while reading, we should let the connection know that we already own the _parserLock
            SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();

            internalConnection.ThreadHasParserLockForClose = true;
            try
            {
                _parser.Run(RunBehavior.UntilDone, null, null, bulkCopyHandler, _stateObj);
            }
            finally
            {
                internalConnection.ThreadHasParserLockForClose = false;
            }
        }

        // Runs the _parser until it is done and ensures that ThreadHasParserLockForClose is correctly set and unset
        // This takes care of setting up the Reliability Section, and will doom the connect if there is a catastrophic (OOM, StackOverflow, ThreadAbort) error
        private void RunParserReliably(BulkCopySimpleResultSet bulkCopyHandler = null)
        {
            // In case of error while reading, we should let the connection know that we already own the _parserLock
            SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();
            internalConnection.ThreadHasParserLockForClose = true;
            try
            {
                _parser.Run(RunBehavior.UntilDone, null, null, bulkCopyHandler, _stateObj);
            }
            finally
            {
                internalConnection.ThreadHasParserLockForClose = false;
            }
        }

        private void CommitTransaction()
        {
            if (null != _internalTransaction)
            {
                SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();
                internalConnection.ThreadHasParserLockForClose = true; // In case of error, let the connection know that we have the lock
                try
                {
                    _internalTransaction.Commit();
                    _internalTransaction.Dispose();
                    _internalTransaction = null;
                }
                finally
                {
                    internalConnection.ThreadHasParserLockForClose = false;
                }
            }
        }

        private void AbortTransaction()
        {
            if (_internalTransaction != null)
            {
                if (!_internalTransaction.IsZombied)
                {
                    SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();
                    internalConnection.ThreadHasParserLockForClose = true; // In case of error, let the connection know that we have the lock
                    try
                    {
                        _internalTransaction.Rollback();
                    }
                    finally
                    {
                        internalConnection.ThreadHasParserLockForClose = false;
                    }
                }
                _internalTransaction.Dispose();
                _internalTransaction = null;
            }
        }

        // Appends columnname in square brackets, a space, and the typename to the query.
        // Putting the name in quotes also requires doubling existing ']' so that they are not mistaken for
        // the closing quote.
        // example: abc will become [abc] but abc[] will become [abc[]]]
        private void AppendColumnNameAndTypeName(StringBuilder query, string columnName, string typeName)
        {
            SqlServerEscapeHelper.EscapeIdentifier(query, columnName);
            query.Append(" ");
            query.Append(typeName);
        }

        private string UnquotedName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name[0] == '[')
            {
                int l = name.Length;
                Debug.Assert(name[l - 1] == ']', "Name starts with [ but does not end with ]");
                name = name.Substring(1, l - 2);
            }
            return name;
        }

        private object ValidateBulkCopyVariant(object value)
        {
            // From the spec:
            // "The only acceptable types are ..."
            // GUID, BIGVARBINARY, BIGBINARY, BIGVARCHAR, BIGCHAR, NVARCHAR, NCHAR, BIT, INT1, INT2, INT4, INT8,
            // MONEY4, MONEY, DECIMALN, NUMERICN, FTL4, FLT8, DATETIME4 and DATETIME
            MetaType metatype = MetaType.GetMetaTypeFromValue(value);
            switch (metatype.TDSType)
            {
                case TdsEnums.SQLFLT4:
                case TdsEnums.SQLFLT8:
                case TdsEnums.SQLINT8:
                case TdsEnums.SQLINT4:
                case TdsEnums.SQLINT2:
                case TdsEnums.SQLINT1:
                case TdsEnums.SQLBIT:
                case TdsEnums.SQLBIGVARBINARY:
                case TdsEnums.SQLBIGVARCHAR:
                case TdsEnums.SQLUNIQUEID:
                case TdsEnums.SQLNVARCHAR:
                case TdsEnums.SQLDATETIME:
                case TdsEnums.SQLMONEY:
                case TdsEnums.SQLNUMERICN:
                case TdsEnums.SQLDATE:
                case TdsEnums.SQLTIME:
                case TdsEnums.SQLDATETIME2:
                case TdsEnums.SQLDATETIMEOFFSET:
                    if (value is INullable)
                    {   // Current limitation in the SqlBulkCopy Variant code limits BulkCopy to CLR/COM Types.
                        return MetaType.GetComValueFromSqlVariant(value);
                    }
                    else
                    {
                        return value;
                    }
                default:
                    throw SQL.BulkLoadInvalidVariantValue();
            }
        }

        private object ConvertValue(object value, _SqlMetaData metadata, bool isNull, ref bool isSqlType, out bool coercedToDataFeed)
        {
            coercedToDataFeed = false;

            if (isNull)
            {
                if (!metadata.isNullable)
                {
                    throw SQL.BulkLoadBulkLoadNotAllowDBNull(metadata.column);
                }
                return value;
            }

            MetaType type = metadata.metaType;
            bool typeChanged = false;
            try
            {
                MetaType mt;
                switch (type.NullableType)
                {
                    case TdsEnums.SQLNUMERICN:
                    case TdsEnums.SQLDECIMALN:
                        mt = MetaType.GetMetaTypeFromSqlDbType(type.SqlDbType, false);
                        value = SqlParameter.CoerceValue(value, mt, out coercedToDataFeed, out typeChanged, false);

                        // Convert Source Decimal Precision and Scale to Destination Precision and Scale
                        // Sql decimal data could get corrupted on insert if the scale of
                        // the source and destination weren't the same. The BCP protocol, specifies the
                        // scale of the incoming data in the insert statement, we just tell the server we
                        // are inserting the same scale back.
                        SqlDecimal sqlValue;
                        if ((isSqlType) && (!typeChanged))
                        {
                            sqlValue = (SqlDecimal)value;
                        }
                        else
                        {
                            sqlValue = new SqlDecimal((Decimal)value);
                        }

                        if (sqlValue.Scale != metadata.scale)
                        {
                            sqlValue = TdsParser.AdjustSqlDecimalScale(sqlValue, metadata.scale);
                        }

                        if (sqlValue.Precision > metadata.precision)
                        {
                            try
                            {
                                sqlValue = SqlDecimal.ConvertToPrecScale(sqlValue, metadata.precision, sqlValue.Scale);
                            }
                            catch (SqlTruncateException)
                            {
                                throw SQL.BulkLoadCannotConvertValue(value.GetType(), mt, ADP.ParameterValueOutOfRange(sqlValue));
                            }
                        }

                        // Perf: It is more efficient to write a SqlDecimal than a decimal since we need to break it into its 'bits' when writing
                        value = sqlValue;
                        isSqlType = true;
                        typeChanged = false; // Setting this to false as SqlParameter.CoerceValue will only set it to true when converting to a CLR type
                        break;

                    case TdsEnums.SQLINTN:
                    case TdsEnums.SQLFLTN:
                    case TdsEnums.SQLFLT4:
                    case TdsEnums.SQLFLT8:
                    case TdsEnums.SQLMONEYN:
                    case TdsEnums.SQLDATETIM4:
                    case TdsEnums.SQLDATETIME:
                    case TdsEnums.SQLDATETIMN:
                    case TdsEnums.SQLBIT:
                    case TdsEnums.SQLBITN:
                    case TdsEnums.SQLUNIQUEID:
                    case TdsEnums.SQLBIGBINARY:
                    case TdsEnums.SQLBIGVARBINARY:
                    case TdsEnums.SQLIMAGE:
                    case TdsEnums.SQLBIGCHAR:
                    case TdsEnums.SQLBIGVARCHAR:
                    case TdsEnums.SQLTEXT:
                    case TdsEnums.SQLDATE:
                    case TdsEnums.SQLTIME:
                    case TdsEnums.SQLDATETIME2:
                    case TdsEnums.SQLDATETIMEOFFSET:
                        mt = MetaType.GetMetaTypeFromSqlDbType(type.SqlDbType, false);
                        value = SqlParameter.CoerceValue(value, mt, out coercedToDataFeed, out typeChanged, false);
                        break;
                    case TdsEnums.SQLNCHAR:
                    case TdsEnums.SQLNVARCHAR:
                    case TdsEnums.SQLNTEXT:
                        mt = MetaType.GetMetaTypeFromSqlDbType(type.SqlDbType, false);
                        value = SqlParameter.CoerceValue(value, mt, out coercedToDataFeed, out typeChanged, false);
                        if (!coercedToDataFeed)
                        {   // We do not need to test for TextDataFeed as it is only assigned to (N)VARCHAR(MAX)
                            int len = ((isSqlType) && (!typeChanged)) ? ((SqlString)value).Value.Length : ((string)value).Length;
                            if (len > metadata.length / 2)
                            {
                                throw SQL.BulkLoadStringTooLong();
                            }
                        }
                        break;
                    case TdsEnums.SQLVARIANT:
                        value = ValidateBulkCopyVariant(value);
                        typeChanged = true;
                        break;
                    case TdsEnums.SQLUDT:
                        // UDTs are sent as varbinary so we need to get the raw bytes
                        // unlike other types the parser does not like SQLUDT in form of SqlType
                        // so we cast to a CLR type.

                        // Hack for type system version knob - only call GetBytes if the value is not already
                        // in byte[] form.
                        if (!(value is byte[]))
                        {
                            value = _connection.GetBytes(value);
                            typeChanged = true;
                        }
                        break;
                    case TdsEnums.SQLXMLTYPE:
                        // Could be either string, SqlCachedBuffer, XmlReader or XmlDataFeed
                        Debug.Assert((value is XmlReader) || (value is SqlCachedBuffer) || (value is string) || (value is SqlString) || (value is XmlDataFeed), "Invalid value type of Xml datatype");
                        if (value is XmlReader)
                        {
                            value = new XmlDataFeed((XmlReader)value);
                            typeChanged = true;
                            coercedToDataFeed = true;
                        }
                        break;

                    default:
                        Debug.Fail("Unknown TdsType!" + type.NullableType.ToString("x2", (IFormatProvider)null));
                        throw SQL.BulkLoadCannotConvertValue(value.GetType(), metadata.metaType, null);
                }

                if (typeChanged)
                {
                    // All type changes change to CLR types
                    isSqlType = false;
                }

                return value;
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
                throw SQL.BulkLoadCannotConvertValue(value.GetType(), metadata.metaType, e);
            }
        }

        public void WriteToServer(DbDataReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                _rowSource = reader;
                _DbDataReaderRowSource = reader;
                _SqlDataReaderRowSource = reader as SqlDataReader;

                _dataTableSource = null;
                _rowSourceType = ValueSourceType.DbDataReader;

                _isAsyncBulkCopy = false;
                WriteRowSourceToServerAsync(reader.FieldCount, CancellationToken.None); //It returns null since _isAsyncBulkCopy = false;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public void WriteToServer(IDataReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                _rowSource = reader;
                _SqlDataReaderRowSource = _rowSource as SqlDataReader;
                _DbDataReaderRowSource = _rowSource as DbDataReader;
                _dataTableSource = null;
                _rowSourceType = ValueSourceType.IDataReader;
                _isAsyncBulkCopy = false;
                WriteRowSourceToServerAsync(reader.FieldCount, CancellationToken.None); //It returns null since _isAsyncBulkCopy = false;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public void WriteToServer(DataTable table) => WriteToServer(table, 0);

        public void WriteToServer(DataTable table, DataRowState rowState)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                _rowStateToSkip = ((rowState == 0) || (rowState == DataRowState.Deleted)) ? DataRowState.Deleted : ~rowState | DataRowState.Deleted;
                _rowSource = table;
                _dataTableSource = table;
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.DataTable;
                _rowEnumerator = table.Rows.GetEnumerator();
                _isAsyncBulkCopy = false;

                WriteRowSourceToServerAsync(table.Columns.Count, CancellationToken.None); //It returns null since _isAsyncBulkCopy = false;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public void WriteToServer(DataRow[] rows)
        {
            SqlStatistics statistics = Statistics;

            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            if (rows.Length == 0)
            {
                return; // Nothing to do. user passed us an empty array
            }

            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                DataTable table = rows[0].Table;
                Debug.Assert(null != table, "How can we have rows without a table?");
                _rowStateToSkip = DataRowState.Deleted;      // Don't allow deleted rows
                _rowSource = rows;
                _dataTableSource = table;
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.RowArray;
                _rowEnumerator = rows.GetEnumerator();
                _isAsyncBulkCopy = false;

                WriteRowSourceToServerAsync(table.Columns.Count, CancellationToken.None); //It returns null since _isAsyncBulkCopy = false;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
        }

        public Task WriteToServerAsync(DataRow[] rows) => WriteToServerAsync(rows, CancellationToken.None);

        public Task WriteToServerAsync(DataRow[] rows, CancellationToken cancellationToken)
        {
            Task resultTask = null;

            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);

                if (rows.Length == 0)
                {
                    return cancellationToken.IsCancellationRequested ?
                            Task.FromCanceled(cancellationToken) :
                            Task.CompletedTask;
                }

                DataTable table = rows[0].Table;
                Debug.Assert(null != table, "How can we have rows without a table?");
                _rowStateToSkip = DataRowState.Deleted; // Don't allow deleted rows
                _rowSource = rows;
                _dataTableSource = table;
                _SqlDataReaderRowSource = null;
                _rowSourceType = ValueSourceType.RowArray;
                _rowEnumerator = rows.GetEnumerator();
                _isAsyncBulkCopy = true;
                resultTask = WriteRowSourceToServerAsync(table.Columns.Count, cancellationToken); // It returns Task since _isAsyncBulkCopy = true;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
            return resultTask;
        }

        public Task WriteToServerAsync(DbDataReader reader) => WriteToServerAsync(reader, CancellationToken.None);

        public Task WriteToServerAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
            Task resultTask = null;
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                _rowSource = reader;
                _SqlDataReaderRowSource = reader as SqlDataReader;
                _DbDataReaderRowSource = reader;
                _dataTableSource = null;
                _rowSourceType = ValueSourceType.DbDataReader;
                _isAsyncBulkCopy = true;
                resultTask = WriteRowSourceToServerAsync(reader.FieldCount, cancellationToken); // It returns Task since _isAsyncBulkCopy = true;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
            return resultTask;
        }

        public Task WriteToServerAsync(IDataReader reader) => WriteToServerAsync(reader, CancellationToken.None);

        public Task WriteToServerAsync(IDataReader reader, CancellationToken cancellationToken)
        {
            Task resultTask = null;

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                _rowSource = reader;
                _SqlDataReaderRowSource = _rowSource as SqlDataReader;
                _DbDataReaderRowSource = _rowSource as DbDataReader;
                _dataTableSource = null;
                _rowSourceType = ValueSourceType.IDataReader;
                _isAsyncBulkCopy = true;
                resultTask = WriteRowSourceToServerAsync(reader.FieldCount, cancellationToken); // It returns Task since _isAsyncBulkCopy = true;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
            return resultTask;
        }

        public Task WriteToServerAsync(DataTable table) => WriteToServerAsync(table, 0, CancellationToken.None);

        public Task WriteToServerAsync(DataTable table, CancellationToken cancellationToken) => WriteToServerAsync(table, 0, cancellationToken);

        public Task WriteToServerAsync(DataTable table, DataRowState rowState) => WriteToServerAsync(table, rowState, CancellationToken.None);

        public Task WriteToServerAsync(DataTable table, DataRowState rowState, CancellationToken cancellationToken)
        {
            Task resultTask = null;

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (_isBulkCopyingInProgress)
            {
                throw SQL.BulkLoadPendingOperation();
            }

            SqlStatistics statistics = Statistics;
            try
            {
                statistics = SqlStatistics.StartTimer(Statistics);
                _rowStateToSkip = ((rowState == 0) || (rowState == DataRowState.Deleted)) ? DataRowState.Deleted : ~rowState | DataRowState.Deleted;
                _rowSource = table;
                _SqlDataReaderRowSource = null;
                _dataTableSource = table;
                _rowSourceType = ValueSourceType.DataTable;
                _rowEnumerator = table.Rows.GetEnumerator();
                _isAsyncBulkCopy = true;
                resultTask = WriteRowSourceToServerAsync(table.Columns.Count, cancellationToken); // It returns Task since _isAsyncBulkCopy = true;
            }
            finally
            {
                SqlStatistics.StopTimer(statistics);
            }
            return resultTask;
        }

        private Task WriteRowSourceToServerAsync(int columnCount, CancellationToken ctoken)
        {
            Task reconnectTask = _connection._currentReconnectionTask;
            if (reconnectTask != null && !reconnectTask.IsCompleted)
            {
                if (_isAsyncBulkCopy)
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    reconnectTask.ContinueWith((t) =>
                    {
                        Task writeTask = WriteRowSourceToServerAsync(columnCount, ctoken);
                        if (writeTask == null)
                        {
                            tcs.SetResult(null);
                        }
                        else
                        {
                            AsyncHelper.ContinueTask(writeTask, tcs, () => tcs.SetResult(null));
                        }
                    }, ctoken); // We do not need to propagate exception, etc, from reconnect task, we just need to wait for it to finish.
                    return tcs.Task;
                }
                else
                {
                    AsyncHelper.WaitForCompletion(reconnectTask, BulkCopyTimeout, () => { throw SQL.CR_ReconnectTimeout(); }, rethrowExceptions: false);
                }
            }

            bool finishedSynchronously = true;
            _isBulkCopyingInProgress = true;

            CreateOrValidateConnection(nameof(WriteToServer));
            SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();

            Debug.Assert(_parserLock == null, "Previous parser lock not cleaned");
            _parserLock = internalConnection._parserLock;
            _parserLock.Wait(canReleaseFromAnyThread: _isAsyncBulkCopy);

            try
            {
                WriteRowSourceToServerCommon(columnCount); // This is common in both sync and async
                Task resultTask = WriteToServerInternalAsync(ctoken); // resultTask is null for sync, but Task for async.
                if (resultTask != null)
                {
                    finishedSynchronously = false;
                    return resultTask.ContinueWith((t) =>
                    {
                        AbortTransaction(); // If there is one, on success transactions will be committed.
                        _isBulkCopyingInProgress = false;
                        if (_parser != null)
                        {
                            _parser._asyncWrite = false;
                        }
                        if (_parserLock != null)
                        {
                            _parserLock.Release();
                            _parserLock = null;
                        }
                        return t;
                    }, TaskScheduler.Default).Unwrap();
                }
                return null;
            }
            catch (System.OutOfMemoryException e)
            {
                _connection.Abort(e);
                throw;
            }
            catch (System.StackOverflowException e)
            {
                _connection.Abort(e);
                throw;
            }
            catch (System.Threading.ThreadAbortException e)
            {
                _connection.Abort(e);
                throw;
            }
            finally
            {
                _columnMappings.ReadOnly = false;
                if (finishedSynchronously)
                {
                    AbortTransaction(); // If there is one, on success transactions will be committed.
                    _isBulkCopyingInProgress = false;
                    if (_parser != null)
                    {
                        _parser._asyncWrite = false;
                    }
                    if (_parserLock != null)
                    {
                        _parserLock.Release();
                        _parserLock = null;
                    }
                }
            }
        }

        // Handles the column mapping.
        private void WriteRowSourceToServerCommon(int columnCount)
        {
            bool unspecifiedColumnOrdinals = false;

            _columnMappings.ReadOnly = true;
            _localColumnMappings = _columnMappings;
            if (_localColumnMappings.Count > 0)
            {
                _localColumnMappings.ValidateCollection();
                foreach (SqlBulkCopyColumnMapping bulkCopyColumn in _localColumnMappings)
                {
                    if (bulkCopyColumn._internalSourceColumnOrdinal == -1)
                    {
                        unspecifiedColumnOrdinals = true;
                        break;
                    }
                }
            }
            else
            {
                _localColumnMappings = new SqlBulkCopyColumnMappingCollection();
                _localColumnMappings.CreateDefaultMapping(columnCount);
            }

            // perf: If the user specified all column ordinals we do not need to get a schematable
            if (unspecifiedColumnOrdinals)
            {
                int index = -1;
                unspecifiedColumnOrdinals = false;

                // Match up sourceColumn names with sourceColumn ordinals
                if (_localColumnMappings.Count > 0)
                {
                    foreach (SqlBulkCopyColumnMapping bulkCopyColumn in _localColumnMappings)
                    {
                        if (bulkCopyColumn._internalSourceColumnOrdinal == -1)
                        {
                            string unquotedColumnName = UnquotedName(bulkCopyColumn.SourceColumn);

                            switch (_rowSourceType)
                            {
                                case ValueSourceType.DataTable:
                                    index = ((DataTable)_rowSource).Columns.IndexOf(unquotedColumnName);
                                    break;
                                case ValueSourceType.RowArray:
                                    index = ((DataRow[])_rowSource)[0].Table.Columns.IndexOf(unquotedColumnName);
                                    break;
                                case ValueSourceType.DbDataReader:
                                case ValueSourceType.IDataReader:
                                    try
                                    {
                                        index = ((IDataReader)_rowSource).GetOrdinal(unquotedColumnName);
                                    }
                                    catch (IndexOutOfRangeException e)
                                    {
                                        throw (SQL.BulkLoadNonMatchingColumnName(unquotedColumnName, e));
                                    }
                                    break;
                            }

                            if (index == -1)
                            {
                                throw (SQL.BulkLoadNonMatchingColumnName(unquotedColumnName));
                            }
                            bulkCopyColumn._internalSourceColumnOrdinal = index;
                        }
                    }
                }
            }
        }

        internal void OnConnectionClosed()
        {
            TdsParserStateObject stateObj = _stateObj;
            if (stateObj != null)
            {
                stateObj.OnConnectionClosed();
            }
        }

        private void OnRowsCopied(SqlRowsCopiedEventArgs value)
        {
            SqlRowsCopiedEventHandler handler = _rowsCopiedEventHandler;
            if (handler != null)
            {
                handler(this, value);
            }
        }

        private bool FireRowsCopiedEvent(long rowsCopied)
        {
            // Release lock to prevent possible deadlocks
            SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();
            bool semaphoreLock = internalConnection._parserLock.CanBeReleasedFromAnyThread;
            internalConnection._parserLock.Release();

            SqlRowsCopiedEventArgs eventArgs = new SqlRowsCopiedEventArgs(rowsCopied);
            try
            {
                _insideRowsCopiedEvent = true;
                this.OnRowsCopied(eventArgs);
            }
            finally
            {
                _insideRowsCopiedEvent = false;
                internalConnection._parserLock.Wait(canReleaseFromAnyThread: semaphoreLock);
            }
            return eventArgs.Abort;
        }

        // Reads a cell and then writes it.
        // Read may block at this moment since there is no getValueAsync or DownStream async at this moment.
        // When _isAsyncBulkCopy == true: Write will return Task (when async method runs asynchronously) or Null (when async call actually ran synchronously) for performance.
        // When _isAsyncBulkCopy == false: Writes are purely sync. This method return null at the end.
        private Task ReadWriteColumnValueAsync(int col)
        {
            bool isSqlType;
            bool isDataFeed;
            bool isNull;
            Object value = GetValueFromSourceRow(col, out isSqlType, out isDataFeed, out isNull); //this will return Task/null in future: as rTask

            _SqlMetaData metadata = _sortedColumnMappings[col]._metadata;
            if (!isDataFeed)
            {
                value = ConvertValue(value, metadata, isNull, ref isSqlType, out isDataFeed);
            }

            //write part
            Task writeTask = null;
            if (metadata.type != SqlDbType.Variant)
            {
                //this is the most common path
                writeTask = _parser.WriteBulkCopyValue(value, metadata, _stateObj, isSqlType, isDataFeed, isNull); //returns Task/Null
            }
            else
            {
                SqlBuffer.StorageType variantInternalType = SqlBuffer.StorageType.Empty;
                if ((_SqlDataReaderRowSource != null) && (_connection.IsKatmaiOrNewer))
                {
                    variantInternalType = _SqlDataReaderRowSource.GetVariantInternalStorageType(_sortedColumnMappings[col]._sourceColumnOrdinal);
                }

                if (variantInternalType == SqlBuffer.StorageType.DateTime2)
                {
                    _parser.WriteSqlVariantDateTime2(((DateTime)value), _stateObj);
                }
                else if (variantInternalType == SqlBuffer.StorageType.Date)
                {
                    _parser.WriteSqlVariantDate(((DateTime)value), _stateObj);
                }
                else
                {
                    writeTask = _parser.WriteSqlVariantDataRowValue(value, _stateObj); //returns Task/Null
                }
            }

            return writeTask;
        }

        private void RegisterForConnectionCloseNotification<T>(ref Task<T> outerTask)
        {
            SqlConnection connection = _connection;
            if (connection == null)
            {
                // No connection
                throw ADP.ClosedConnectionError();
            }

            connection.RegisterForConnectionCloseNotification<T>(ref outerTask, this, SqlReferenceCollection.BulkCopyTag);
        }

        // Runs a loop to copy all columns of a single row.
        // Maintains a state by remembering #columns copied so far (int col).
        // Returned Task could be null in two cases: (1) _isAsyncBulkCopy == false, (2) _isAsyncBulkCopy == true but all async writes finished synchronously.
        private Task CopyColumnsAsync(int col, TaskCompletionSource<object> source = null)
        {
            Task resultTask = null, task = null;
            int i;
            try
            {
                for (i = col; i < _sortedColumnMappings.Count; i++)
                {
                    task = ReadWriteColumnValueAsync(i); //First reads and then writes one cell value. Task 'task' is completed when reading task and writing task both are complete.
                    if (task != null) break; //task != null means we have a pending read/write Task.
                }
                if (task != null)
                {
                    if (source == null)
                    {
                        source = new TaskCompletionSource<object>();
                        resultTask = source.Task;
                    }
                    CopyColumnsAsyncSetupContinuation(source, task, i);
                    return resultTask; //associated task will be completed when all columns (i.e. the entire row) is written
                }
                if (source != null)
                {
                    source.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                }
                else
                {
                    throw;
                }
            }
            return resultTask;
        }

        // This is in its own method to avoid always allocating the lambda in CopyColumnsAsync
        private void CopyColumnsAsyncSetupContinuation(TaskCompletionSource<object> source, Task task, int i)
        {
            AsyncHelper.ContinueTask(task, source, () =>
            {
                if (i + 1 < _sortedColumnMappings.Count)
                {
                    CopyColumnsAsync(i + 1, source); //continue from the next column
                }
                else
                {
                    source.SetResult(null);
                }
            },
                _connection.GetOpenTdsConnection());
        }

        // The notification logic.
        private void CheckAndRaiseNotification()
        {
            bool abortOperation = false; //returns if the operation needs to be aborted.
            Exception exception = null;

            _rowsCopied++;

            // Fire event logic
            if (_notifyAfter > 0)
            {
                if (_rowsUntilNotification > 0)
                {
                    if (--_rowsUntilNotification == 0)
                    {
                        // Fire event during operation. This is the users chance to abort the operation.
                        try
                        {
                            // It's also the user's chance to cause an exception.
                            _stateObj.BcpLock = true;
                            abortOperation = FireRowsCopiedEvent(_rowsCopied);

                            // In case the target connection is closed accidentally.
                            if (ConnectionState.Open != _connection.State)
                            {
                                exception = ADP.OpenConnectionRequired(nameof(CheckAndRaiseNotification), _connection.State);
                            }
                        }
                        catch (Exception e)
                        {
                            if (!ADP.IsCatchableExceptionType(e))
                            {
                                exception = e;
                            }
                            else
                            {
                                exception = OperationAbortedException.Aborted(e);
                            }
                        }
                        finally
                        {
                            _stateObj.BcpLock = false;
                        }
                        if (!abortOperation)
                        {
                            _rowsUntilNotification = _notifyAfter;
                        }
                    }
                }
            }
            if (!abortOperation && _rowsUntilNotification > _notifyAfter)
            {
                _rowsUntilNotification = _notifyAfter;      // Update on decrement of count
            }
            if (exception == null && abortOperation)
            {
                exception = OperationAbortedException.Aborted(null);
            }
            if (_connection.State != ConnectionState.Open)
            {
                throw ADP.OpenConnectionRequired(nameof(WriteToServer), _connection.State);
            }
            if (exception != null)
            {
                _parser._asyncWrite = false;
                Task writeTask = _parser.WriteBulkCopyDone(_stateObj); //We should complete the current batch up to this row.
                Debug.Assert(writeTask == null, "Task should not pend while doing sync bulk copy");
                RunParser();
                AbortTransaction();
                throw exception; //this will be caught and put inside the Task's exception.
            }
        }

        // Checks for cancellation. If cancel requested, cancels the task and returns the cancelled task
        private Task CheckForCancellation(CancellationToken cts, TaskCompletionSource<object> tcs)
        {
            if (cts.IsCancellationRequested)
            {
                if (tcs == null)
                {
                    tcs = new TaskCompletionSource<object>();
                }
                tcs.SetCanceled();
                return tcs.Task;
            }
            else
            {
                return null;
            }
        }

        private TaskCompletionSource<object> ContinueTaskPend(Task task, TaskCompletionSource<object> source, Func<TaskCompletionSource<object>> action)
        {
            if (task == null)
            {
                return action();
            }
            else
            {
                Debug.Assert(source != null, "source should already be initialized if task is not null");
                AsyncHelper.ContinueTask(task, source, () =>
                {
                    TaskCompletionSource<object> newSource = action();
                    Debug.Assert(newSource == null, "Shouldn't create a new source when one already exists");
                });
            }
            return null;
        }

        // Copies all the rows in a batch.
        // Maintains state machine with state variable: rowSoFar.
        // Returned Task could be null in two cases: (1) _isAsyncBulkCopy == false, or (2) _isAsyncBulkCopy == true but all async writes finished synchronously.
        private Task CopyRowsAsync(int rowsSoFar, int totalRows, CancellationToken cts, TaskCompletionSource<object> source = null)
        {
            Task resultTask = null;
            Task task = null;
            int i;
            try
            {
                // totalRows is batchsize which is 0 by default. In that case, we keep copying till the end (until _hasMoreRowToCopy == false).
                for (i = rowsSoFar; (totalRows <= 0 || i < totalRows) && _hasMoreRowToCopy == true; i++)
                {
                    if (_isAsyncBulkCopy == true)
                    {
                        resultTask = CheckForCancellation(cts, source);
                        if (resultTask != null)
                        {
                            return resultTask; // Task got cancelled!
                        }
                    }

                    _stateObj.WriteByte(TdsEnums.SQLROW);

                    task = CopyColumnsAsync(0); // Copy 1 row

                    if (task == null)
                    {   // Task is done.
                        CheckAndRaiseNotification(); // Check notification logic after copying the row

                        // Now we will read the next row.
                        Task readTask = ReadFromRowSourceAsync(cts); // Read the next row. Caution: more is only valid if the task returns null. Otherwise, we wait for Task.Result
                        if (readTask != null)
                        {
                            if (source == null)
                            {
                                source = new TaskCompletionSource<object>();
                            }
                            resultTask = source.Task;

                            AsyncHelper.ContinueTask(readTask, source, () => CopyRowsAsync(i + 1, totalRows, cts, source), connectionToDoom: _connection.GetOpenTdsConnection());
                            return resultTask; // Associated task will be completed when all rows are copied to server/exception/cancelled.
                        }
                    }
                    else
                    {   // task != null, so add continuation for it.
                        source = source ?? new TaskCompletionSource<object>();
                        resultTask = source.Task;

                        AsyncHelper.ContinueTask(task, source, onSuccess: () =>
                        {
                            CheckAndRaiseNotification(); // Check for notification now as the current row copy is done at this moment.

                            Task readTask = ReadFromRowSourceAsync(cts);
                            if (readTask == null)
                            {
                                CopyRowsAsync(i + 1, totalRows, cts, source);
                            }
                            else
                            {
                                AsyncHelper.ContinueTask(readTask, source, onSuccess: () => CopyRowsAsync(i + 1, totalRows, cts, source), connectionToDoom: _connection.GetOpenTdsConnection());
                            }
                        }, connectionToDoom: _connection.GetOpenTdsConnection());
                        return resultTask;
                    }
                }

                if (source != null)
                {
                    source.TrySetResult(null); // This is set only on the last call of async copy. But may not be set if everything runs synchronously.
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                }
                else
                {
                    throw;
                }
            }
            return resultTask;
        }

        // Copies all the batches in a loop. One iteration for one batch.
        // state variable is essentially not needed. (however, _hasMoreRowToCopy might be thought as a state variable)
        // Returned Task could be null in two cases: (1) _isAsyncBulkCopy == false, or (2) _isAsyncBulkCopy == true but all async writes finished synchronously.
        private Task CopyBatchesAsync(BulkCopySimpleResultSet internalResults, string updateBulkCommandText, CancellationToken cts, TaskCompletionSource<object> source = null)
        {
            Debug.Assert(source == null || !source.Task.IsCompleted, "Called into CopyBatchesAsync with a completed task!");
            try
            {
                while (_hasMoreRowToCopy)
                {
                    //pre->before every batch: Transaction, BulkCmd and metadata are done.
                    SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();

                    if (IsCopyOption(SqlBulkCopyOptions.UseInternalTransaction))
                    { //internal transaction is started prior to each batch if the Option is set.
                        internalConnection.ThreadHasParserLockForClose = true;     // In case of error, tell the connection we already have the parser lock
                        try
                        {
                            _internalTransaction = _connection.BeginTransaction();
                        }
                        finally
                        {
                            internalConnection.ThreadHasParserLockForClose = false;
                        }
                    }

                    Task commandTask = SubmitUpdateBulkCommand(updateBulkCommandText);

                    if (commandTask == null)
                    {
                        Task continuedTask = CopyBatchesAsyncContinued(internalResults, updateBulkCommandText, cts, source);
                        if (continuedTask != null)
                        {
                            // Continuation will take care of re-calling CopyBatchesAsync
                            return continuedTask;
                        }
                    }
                    else
                    {
                        Debug.Assert(_isAsyncBulkCopy, "Task should not pend while doing sync bulk copy");
                        if (source == null)
                        {
                            source = new TaskCompletionSource<object>();
                        }

                        AsyncHelper.ContinueTask(commandTask, source, () =>
                        {
                            Task continuedTask = CopyBatchesAsyncContinued(internalResults, updateBulkCommandText, cts, source);
                            if (continuedTask == null)
                            {
                                // Continuation finished sync, recall into CopyBatchesAsync to continue
                                CopyBatchesAsync(internalResults, updateBulkCommandText, cts, source);
                            }
                        }, _connection.GetOpenTdsConnection());
                        return source.Task;
                    }
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                    return source.Task;
                }
                else
                {
                    throw;
                }
            }

            // If we are here, then we finished everything
            if (source != null)
            {
                source.SetResult(null);
                return source.Task;
            }
            else
            {
                return null;
            }
        }

        // Writes the MetaData and a single batch.
        // If this returns true, then the caller is responsible for starting the next stage.
        private Task CopyBatchesAsyncContinued(BulkCopySimpleResultSet internalResults, string updateBulkCommandText, CancellationToken cts, TaskCompletionSource<object> source)
        {
            Debug.Assert(source == null || !source.Task.IsCompleted, "Called into CopyBatchesAsync with a completed task!");
            try
            {
                WriteMetaData(internalResults);
                Task task = CopyRowsAsync(0, _savedBatchSize, cts); // This is copying 1 batch of rows and setting _hasMoreRowToCopy = true/false.

                // post->after every batch
                if (task != null)
                {
                    Debug.Assert(_isAsyncBulkCopy, "Task should not pend while doing sync bulk copy");
                    if (source == null)
                    {   // First time only
                        source = new TaskCompletionSource<object>();
                    }
                    AsyncHelper.ContinueTask(task, source, () =>
                    {
                        Task continuedTask = CopyBatchesAsyncContinuedOnSuccess(internalResults, updateBulkCommandText, cts, source);
                        if (continuedTask == null)
                        {
                            // Continuation finished sync, recall into CopyBatchesAsync to continue
                            CopyBatchesAsync(internalResults, updateBulkCommandText, cts, source);
                        }
                    }, _connection.GetOpenTdsConnection(), _ => CopyBatchesAsyncContinuedOnError(cleanupParser: false), () => CopyBatchesAsyncContinuedOnError(cleanupParser: true));

                    return source.Task;
                }
                else
                {
                    return CopyBatchesAsyncContinuedOnSuccess(internalResults, updateBulkCommandText, cts, source);
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                    return source.Task;
                }
                else
                {
                    throw;
                }
            }
        }

        // Takes care of finishing a single batch (write done, run parser, commit transaction).
        // If this returns true, then the caller is responsible for starting the next stage.
        private Task CopyBatchesAsyncContinuedOnSuccess(BulkCopySimpleResultSet internalResults, string updateBulkCommandText, CancellationToken cts, TaskCompletionSource<object> source)
        {
            Debug.Assert(source == null || !source.Task.IsCompleted, "Called into CopyBatchesAsync with a completed task!");
            try
            {
                Task writeTask = _parser.WriteBulkCopyDone(_stateObj);

                if (writeTask == null)
                {
                    RunParser();
                    CommitTransaction();

                    return null;
                }
                else
                {
                    Debug.Assert(_isAsyncBulkCopy, "Task should not pend while doing sync bulk copy");
                    if (source == null)
                    {
                        source = new TaskCompletionSource<object>();
                    }

                    AsyncHelper.ContinueTask(writeTask, source, () =>
                    {
                        try
                        {
                            RunParser();
                            CommitTransaction();
                        }
                        catch (Exception)
                        {
                            CopyBatchesAsyncContinuedOnError(cleanupParser: false);
                            throw;
                        }

                        // Always call back into CopyBatchesAsync
                        CopyBatchesAsync(internalResults, updateBulkCommandText, cts, source);
                    }, connectionToDoom: _connection.GetOpenTdsConnection(), onFailure: _ => CopyBatchesAsyncContinuedOnError(cleanupParser: false));
                    return source.Task;
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                    return source.Task;
                }
                else
                {
                    throw;
                }
            }
        }

        // Takes care of cleaning up the parser, stateObj and transaction when CopyBatchesAsync fails.
        private void CopyBatchesAsyncContinuedOnError(bool cleanupParser)
        {
            SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();
            try
            {
                if ((cleanupParser) && (_parser != null) && (_stateObj != null))
                {
                    _parser._asyncWrite = false;
                    Task task = _parser.WriteBulkCopyDone(_stateObj);
                    Debug.Assert(task == null, "Write should not pend when error occurs");
                    RunParser();
                }

                if (_stateObj != null)
                {
                    CleanUpStateObjectOnError();
                }
            }
            catch (OutOfMemoryException)
            {
                internalConnection.DoomThisConnection();
                throw;
            }
            catch (StackOverflowException)
            {
                internalConnection.DoomThisConnection();
                throw;
            }
            catch (ThreadAbortException)
            {
                internalConnection.DoomThisConnection();
                throw;
            }

            AbortTransaction();
        }

        // Cleans the stateobj. Used in a number of places, specially in  exceptions.
        private void CleanUpStateObjectOnError()
        {
            if (_stateObj != null)
            {
                _parser.Connection.ThreadHasParserLockForClose = true;
                try
                {
                    _stateObj.ResetBuffer();
                    _stateObj._outputPacketNumber = 1;
                    // If _parser is closed, sending attention will raise debug assertion, so we avoid it (but not calling CancelRequest).
                    if (_parser.State == TdsParserState.OpenNotLoggedIn || _parser.State == TdsParserState.OpenLoggedIn)
                    {
                        _stateObj.CancelRequest();
                    }
                    _stateObj._internalTimeout = false;
                    _stateObj.CloseSession();
                    _stateObj._bulkCopyOpperationInProgress = false;
                    _stateObj._bulkCopyWriteTimeout = false;
                    _stateObj = null;
                }
                finally
                {
                    _parser.Connection.ThreadHasParserLockForClose = false;
                }
            }
        }

        // The continuation part of WriteToServerInternalRest. Executes when the initial query task is completed. (see, WriteToServerInternalRest).
        // It carries on the source which is passed from the WriteToServerInternalRest and performs SetResult when the entire copy is done.
        // The carried on source may be null in case of Sync copy. So no need to SetResult at that time.
        // It launches the copy operation.
        private void WriteToServerInternalRestContinuedAsync(BulkCopySimpleResultSet internalResults, CancellationToken cts, TaskCompletionSource<object> source)
        {
            Task task = null;
            string updateBulkCommandText = null;

            try
            {
                updateBulkCommandText = AnalyzeTargetAndCreateUpdateBulkCommand(internalResults);

                if (_sortedColumnMappings.Count != 0)
                {
                    _stateObj.SniContext = SniContext.Snix_SendRows;
                    _savedBatchSize = _batchSize; // For safety. If someone changes the batchsize during copy we still be using _savedBatchSize.
                    _rowsUntilNotification = _notifyAfter;
                    _rowsCopied = 0;

                    _currentRowMetadata = new SourceColumnMetadata[_sortedColumnMappings.Count];
                    for (int i = 0; i < _currentRowMetadata.Length; i++)
                    {
                        _currentRowMetadata[i] = GetColumnMetadata(i);
                    }

                    task = CopyBatchesAsync(internalResults, updateBulkCommandText, cts); // Launch the BulkCopy
                }

                if (task != null)
                {
                    if (source == null)
                    {
                        source = new TaskCompletionSource<object>();
                    }
                    AsyncHelper.ContinueTask(task, source, () =>
                    {
                        // Bulk copy task is completed at this moment.
                        if (task.IsCanceled)
                        {
                            _localColumnMappings = null;
                            try
                            {
                                CleanUpStateObjectOnError();
                            }
                            finally
                            {
                                source.SetCanceled();
                            }
                        }
                        else if (task.Exception != null)
                        {
                            source.SetException(task.Exception.InnerException);
                        }
                        else
                        {
                            _localColumnMappings = null;
                            try
                            {
                                CleanUpStateObjectOnError();
                            }
                            finally
                            {
                                if (source != null)
                                {
                                    if (cts.IsCancellationRequested)
                                    {   // We may get cancellation req even after the entire copy.
                                        source.SetCanceled();
                                    }
                                    else
                                    {
                                        source.SetResult(null);
                                    }
                                }
                            }
                        }
                    }, _connection.GetOpenTdsConnection());
                    return;
                }
                else
                {
                    _localColumnMappings = null;

                    try
                    {
                        CleanUpStateObjectOnError();
                    }
                    catch (Exception cleanupEx)
                    {
                        Debug.Fail("Unexpected exception during CleanUpstateObjectOnError (ignored)", cleanupEx.ToString());
                    }

                    if (source != null)
                    {
                        source.SetResult(null);
                    }
                }
            }
            catch (Exception ex)
            {
                _localColumnMappings = null;

                try
                {
                    CleanUpStateObjectOnError();
                }
                catch (Exception cleanupEx)
                {
                    Debug.Fail("Unexpected exception during CleanUpstateObjectOnError (ignored)", cleanupEx.ToString());
                }

                if (source != null)
                {
                    source.TrySetException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        // Rest of the WriteToServerInternalAsync method.
        // It carries on the source from its caller WriteToServerInternal.
        // source is null in case of Sync bcp. But valid in case of Async bcp.
        // It calls the WriteToServerInternalRestContinuedAsync as a continuation of the initial query task.
        private void WriteToServerInternalRestAsync(CancellationToken cts, TaskCompletionSource<object> source)
        {
            Debug.Assert(_hasMoreRowToCopy, "first time it is true, otherwise this method would not have been called.");
            _hasMoreRowToCopy = true;
            Task<BulkCopySimpleResultSet> internalResultsTask = null;
            BulkCopySimpleResultSet internalResults = new BulkCopySimpleResultSet();
            SqlInternalConnectionTds internalConnection = _connection.GetOpenTdsConnection();
            try
            {
                _parser = _connection.Parser;
                _parser._asyncWrite = _isAsyncBulkCopy; // Very important!

                Task reconnectTask;
                try
                {
                    reconnectTask = _connection.ValidateAndReconnect(
                        () =>
                        {
                            if (_parserLock != null)
                            {
                                _parserLock.Release();
                                _parserLock = null;
                            }
                        }, BulkCopyTimeout);
                }
                catch (SqlException ex)
                {
                    throw SQL.BulkLoadInvalidDestinationTable(_destinationTableName, ex);
                }

                if (reconnectTask != null)
                {
                    if (_isAsyncBulkCopy)
                    {
                        CancellationTokenRegistration regReconnectCancel = new CancellationTokenRegistration();
                        TaskCompletionSource<object> cancellableReconnectTS = new TaskCompletionSource<object>();
                        if (cts.CanBeCanceled)
                        {
                            regReconnectCancel = cts.Register(s => ((TaskCompletionSource<object>)s).TrySetCanceled(), cancellableReconnectTS);
                        }
                        AsyncHelper.ContinueTask(reconnectTask, cancellableReconnectTS, () => { cancellableReconnectTS.SetResult(null); });
                        // No need to cancel timer since SqlBulkCopy creates specific task source for reconnection.
                        AsyncHelper.SetTimeoutException(cancellableReconnectTS, BulkCopyTimeout,
                                () => { return SQL.BulkLoadInvalidDestinationTable(_destinationTableName, SQL.CR_ReconnectTimeout()); }, CancellationToken.None);
                        AsyncHelper.ContinueTask(cancellableReconnectTS.Task, source,
                            () =>
                            {
                                regReconnectCancel.Dispose();
                                if (_parserLock != null)
                                {
                                    _parserLock.Release();
                                    _parserLock = null;
                                }
                                _parserLock = _connection.GetOpenTdsConnection()._parserLock;
                                _parserLock.Wait(canReleaseFromAnyThread: true);
                                WriteToServerInternalRestAsync(cts, source);
                            },
                            connectionToAbort: _connection,
                            onFailure: (e) => { regReconnectCancel.Dispose(); },
                            onCancellation: () => { regReconnectCancel.Dispose(); },
                            exceptionConverter: (ex) => SQL.BulkLoadInvalidDestinationTable(_destinationTableName, ex));
                        return;
                    }
                    else
                    {
                        try
                        {
                            AsyncHelper.WaitForCompletion(reconnectTask, this.BulkCopyTimeout, () => { throw SQL.CR_ReconnectTimeout(); });
                        }
                        catch (SqlException ex)
                        {
                            throw SQL.BulkLoadInvalidDestinationTable(_destinationTableName, ex); // Preserve behavior (throw InvalidOperationException on failure to connect)
                        }
                        _parserLock = _connection.GetOpenTdsConnection()._parserLock;
                        _parserLock.Wait(canReleaseFromAnyThread: false);
                        WriteToServerInternalRestAsync(cts, source);
                        return;
                    }
                }
                if (_isAsyncBulkCopy)
                {
                    _connection.AddWeakReference(this, SqlReferenceCollection.BulkCopyTag);
                }

                internalConnection.ThreadHasParserLockForClose = true;    // In case of error, let the connection know that we already have the parser lock.

                try
                {
                    _stateObj = _parser.GetSession(this);
                    _stateObj._bulkCopyOpperationInProgress = true;
                    _stateObj.StartSession(this);
                }
                finally
                {
                    internalConnection.ThreadHasParserLockForClose = false;
                }

                try
                {
                    internalResultsTask = CreateAndExecuteInitialQueryAsync(out internalResults); // Task/Null
                }
                catch (SqlException ex)
                {
                    throw SQL.BulkLoadInvalidDestinationTable(_destinationTableName, ex);
                }

                if (internalResultsTask != null)
                {
                    AsyncHelper.ContinueTask(internalResultsTask, source, () => WriteToServerInternalRestContinuedAsync(internalResultsTask.Result, cts, source), _connection.GetOpenTdsConnection());
                }
                else
                {
                    Debug.Assert(internalResults != null, "Executing initial query finished synchronously, but there were no results");
                    WriteToServerInternalRestContinuedAsync(internalResults, cts, source); // internalResults is valid here.
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        // This returns Task for Async, Null for Sync
        private Task WriteToServerInternalAsync(CancellationToken ctoken)
        {
            TaskCompletionSource<object> source = null;
            Task<object> resultTask = null;

            if (_isAsyncBulkCopy)
            {
                source = new TaskCompletionSource<object>(); // Creating the completion source/Task that we pass to application
                resultTask = source.Task;

                RegisterForConnectionCloseNotification(ref resultTask);
            }

            if (_destinationTableName == null)
            {
                if (source != null)
                {
                    source.SetException(SQL.BulkLoadMissingDestinationTable()); // No table to copy
                }
                else
                {
                    throw SQL.BulkLoadMissingDestinationTable();
                }
                return resultTask;
            }

            try
            {
                Task readTask = ReadFromRowSourceAsync(ctoken); // readTask == reading task. This is the first read call. "more" is valid only if readTask == null;

                if (readTask == null)
                {   // Synchronously finished reading.
                    if (!_hasMoreRowToCopy)
                    {   // No rows in the source to copy!
                        if (source != null)
                        {
                            source.SetResult(null);
                        }
                        return resultTask;
                    }
                    else
                    {   // True, we have more rows.
                        WriteToServerInternalRestAsync(ctoken, source); //rest of the method, passing the same completion and returning the incomplete task (ret).
                        return resultTask;
                    }
                }
                else
                {
                    Debug.Assert(_isAsyncBulkCopy, "Read must not return a Task in the Sync mode");
                    AsyncHelper.ContinueTask(readTask, source, () =>
                    {
                        if (!_hasMoreRowToCopy)
                        {
                            source.SetResult(null); // No rows to copy!
                        }
                        else
                        {
                            WriteToServerInternalRestAsync(ctoken, source); // Passing the same completion which will be completed by the Callee.
                        }
                    }, _connection.GetOpenTdsConnection());
                    return resultTask;
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.TrySetException(ex);
                }
                else
                {
                    throw;
                }
            }
            return resultTask;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    internal static partial class ADP
    {
        // The class ADP defines the exceptions that are specific to the Adapters.
        // The class contains functions that take the proper informational variables and then construct
        // the appropriate exception with an error string obtained from the resource framework.
        // The exception is then returned to the caller, so that the caller may then throw from its
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error messages.

        internal static Task<T> CreatedTaskWithCancellation<T>() => Task.FromCanceled<T>(new CancellationToken(true));

        // this method accepts BID format as an argument, this attribute allows FXCopBid rule to validate calls to it
        static partial void TraceException(string trace, Exception e)
        {
            Debug.Assert(e != null, "TraceException: null Exception");
            if (e != null)
            {
                DataCommonEventSource.Log.Trace(trace, e);
            }
        }

        internal static void TraceExceptionForCapture(Exception e)
        {
            Debug.Assert(IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
        }

        internal static DataException Data(string message)
        {
            DataException e = new DataException(message);
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static void CheckArgumentLength(string value, string parameterName)
        {
            CheckArgumentNull(value, parameterName);
            if (0 == value.Length)
            {
                throw Argument(SR.Format(SR.ADP_EmptyString, parameterName));
            }
        }
        internal static void CheckArgumentLength(Array value, string parameterName)
        {
            CheckArgumentNull(value, parameterName);
            if (0 == value.Length)
            {
                throw Argument(SR.Format(SR.ADP_EmptyArray, parameterName));
            }
        }

        // Invalid Enumeration

        internal static ArgumentOutOfRangeException InvalidAcceptRejectRule(AcceptRejectRule value)
        {
#if DEBUG
            switch (value)
            {
                case AcceptRejectRule.None:
                case AcceptRejectRule.Cascade:
                    Debug.Assert(false, "valid AcceptRejectRule " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(AcceptRejectRule), (int)value);
        }

        // DbCommandBuilder.CatalogLocation
        internal static ArgumentOutOfRangeException InvalidCatalogLocation(CatalogLocation value)
        {
#if DEBUG
            switch (value)
            {
                case CatalogLocation.Start:
                case CatalogLocation.End:
                    Debug.Assert(false, "valid CatalogLocation " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(CatalogLocation), (int)value);
        }

        internal static ArgumentOutOfRangeException InvalidConflictOptions(ConflictOption value)
        {
#if DEBUG
            switch (value)
            {
                case ConflictOption.CompareAllSearchableValues:
                case ConflictOption.CompareRowVersion:
                case ConflictOption.OverwriteChanges:
                    Debug.Assert(false, "valid ConflictOption " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(ConflictOption), (int)value);
        }

        // IDataAdapter.Update
        internal static ArgumentOutOfRangeException InvalidDataRowState(DataRowState value)
        {
#if DEBUG
            switch (value)
            {
                case DataRowState.Detached:
                case DataRowState.Unchanged:
                case DataRowState.Added:
                case DataRowState.Deleted:
                case DataRowState.Modified:
                    Debug.Assert(false, "valid DataRowState " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(DataRowState), (int)value);
        }

        // KeyRestrictionBehavior
        internal static ArgumentOutOfRangeException InvalidKeyRestrictionBehavior(KeyRestrictionBehavior value)
        {
#if DEBUG
            switch (value)
            {
                case KeyRestrictionBehavior.PreventUsage:
                case KeyRestrictionBehavior.AllowOnly:
                    Debug.Assert(false, "valid KeyRestrictionBehavior " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(KeyRestrictionBehavior), (int)value);
        }

        // IDataAdapter.FillLoadOption
        internal static ArgumentOutOfRangeException InvalidLoadOption(LoadOption value)
        {
#if DEBUG
            switch (value)
            {
                case LoadOption.OverwriteChanges:
                case LoadOption.PreserveChanges:
                case LoadOption.Upsert:
                    Debug.Assert(false, "valid LoadOption " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(LoadOption), (int)value);
        }

        // IDataAdapter.MissingMappingAction
        internal static ArgumentOutOfRangeException InvalidMissingMappingAction(MissingMappingAction value)
        {
#if DEBUG
            switch (value)
            {
                case MissingMappingAction.Passthrough:
                case MissingMappingAction.Ignore:
                case MissingMappingAction.Error:
                    Debug.Assert(false, "valid MissingMappingAction " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(MissingMappingAction), (int)value);
        }

        // IDataAdapter.MissingSchemaAction
        internal static ArgumentOutOfRangeException InvalidMissingSchemaAction(MissingSchemaAction value)
        {
#if DEBUG
            switch (value)
            {
                case MissingSchemaAction.Add:
                case MissingSchemaAction.Ignore:
                case MissingSchemaAction.Error:
                case MissingSchemaAction.AddWithKey:
                    Debug.Assert(false, "valid MissingSchemaAction " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(MissingSchemaAction), (int)value);
        }

        internal static ArgumentOutOfRangeException InvalidRule(Rule value)
        {
#if DEBUG
            switch (value)
            {
                case Rule.None:
                case Rule.Cascade:
                case Rule.SetNull:
                case Rule.SetDefault:
                    Debug.Assert(false, "valid Rule " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(Rule), (int)value);
        }

        // IDataAdapter.FillSchema
        internal static ArgumentOutOfRangeException InvalidSchemaType(SchemaType value)
        {
#if DEBUG
            switch (value)
            {
                case SchemaType.Source:
                case SchemaType.Mapped:
                    Debug.Assert(false, "valid SchemaType " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(SchemaType), (int)value);
        }

        // RowUpdatingEventArgs.StatementType
        internal static ArgumentOutOfRangeException InvalidStatementType(StatementType value)
        {
#if DEBUG
            switch (value)
            {
                case StatementType.Select:
                case StatementType.Insert:
                case StatementType.Update:
                case StatementType.Delete:
                case StatementType.Batch:
                    Debug.Assert(false, "valid StatementType " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(StatementType), (int)value);
        }

        // RowUpdatingEventArgs.UpdateStatus
        internal static ArgumentOutOfRangeException InvalidUpdateStatus(UpdateStatus value)
        {
#if DEBUG
            switch (value)
            {
                case UpdateStatus.Continue:
                case UpdateStatus.ErrorsOccurred:
                case UpdateStatus.SkipAllRemainingRows:
                case UpdateStatus.SkipCurrentRow:
                    Debug.Assert(false, "valid UpdateStatus " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(UpdateStatus), (int)value);
        }

        internal static ArgumentOutOfRangeException NotSupportedStatementType(StatementType value, string method)
        {
            return NotSupportedEnumerationValue(typeof(StatementType), value.ToString(), method);
        }

        //
        // DbConnectionOptions, DataAccess
        //
        internal static ArgumentException InvalidKeyname(string parameterName)
        {
            return Argument(SR.ADP_InvalidKey, parameterName);
        }
        internal static ArgumentException InvalidValue(string parameterName)
        {
            return Argument(SR.ADP_InvalidValue, parameterName);
        }

        //
        // DataAccess
        //
        internal static Exception WrongType(Type got, Type expected)
        {
            return Argument(SR.Format(SR.SQL_WrongType, got.ToString(), expected.ToString()));
        }

        //
        // Generic Data Provider Collection
        //
        internal static Exception CollectionUniqueValue(Type itemType, string propertyName, string propertyValue)
        {
            return Argument(SR.Format(SR.ADP_CollectionUniqueValue, itemType.Name, propertyName, propertyValue));
        }

        // IDbDataAdapter.Fill(Schema)
        internal static InvalidOperationException MissingSelectCommand(string method)
        {
            return Provider(SR.Format(SR.ADP_MissingSelectCommand, method));
        }

        //
        // AdapterMappingException
        //
        private static InvalidOperationException DataMapping(string error)
        {
            return InvalidOperation(error);
        }

        // DataColumnMapping.GetDataColumnBySchemaAction
        internal static InvalidOperationException ColumnSchemaExpression(string srcColumn, string cacheColumn)
        {
            return DataMapping(SR.Format(SR.ADP_ColumnSchemaExpression, srcColumn, cacheColumn));
        }

        // DataColumnMapping.GetDataColumnBySchemaAction
        internal static InvalidOperationException ColumnSchemaMismatch(string srcColumn, Type srcType, DataColumn column)
        {
            return DataMapping(SR.Format(SR.ADP_ColumnSchemaMismatch, srcColumn, srcType.Name, column.ColumnName, column.DataType.Name));
        }

        // DataColumnMapping.GetDataColumnBySchemaAction
        internal static InvalidOperationException ColumnSchemaMissing(string cacheColumn, string tableName, string srcColumn)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return InvalidOperation(SR.Format(SR.ADP_ColumnSchemaMissing1, cacheColumn, tableName, srcColumn));
            }
            return DataMapping(SR.Format(SR.ADP_ColumnSchemaMissing2, cacheColumn, tableName, srcColumn));
        }

        // DataColumnMappingCollection.GetColumnMappingBySchemaAction
        internal static InvalidOperationException MissingColumnMapping(string srcColumn)
        {
            return DataMapping(SR.Format(SR.ADP_MissingColumnMapping, srcColumn));
        }

        // DataTableMapping.GetDataTableBySchemaAction
        internal static InvalidOperationException MissingTableSchema(string cacheTable, string srcTable)
        {
            return DataMapping(SR.Format(SR.ADP_MissingTableSchema, cacheTable, srcTable));
        }

        // DataTableMappingCollection.GetTableMappingBySchemaAction
        internal static InvalidOperationException MissingTableMapping(string srcTable)
        {
            return DataMapping(SR.Format(SR.ADP_MissingTableMapping, srcTable));
        }

        // DbDataAdapter.Update
        internal static InvalidOperationException MissingTableMappingDestination(string dstTable)
        {
            return DataMapping(SR.Format(SR.ADP_MissingTableMappingDestination, dstTable));
        }

        //
        // DataColumnMappingCollection, DataAccess
        //
        internal static Exception InvalidSourceColumn(string parameter)
        {
            return Argument(SR.ADP_InvalidSourceColumn, parameter);
        }
        internal static Exception ColumnsAddNullAttempt(string parameter)
        {
            return CollectionNullValue(parameter, typeof(DataColumnMappingCollection), typeof(DataColumnMapping));
        }
        internal static Exception ColumnsDataSetColumn(string cacheColumn)
        {
            return CollectionIndexString(typeof(DataColumnMapping), ADP.DataSetColumn, cacheColumn, typeof(DataColumnMappingCollection));
        }
        internal static Exception ColumnsIndexInt32(int index, IColumnMappingCollection collection)
        {
            return CollectionIndexInt32(index, collection.GetType(), collection.Count);
        }
        internal static Exception ColumnsIndexSource(string srcColumn)
        {
            return CollectionIndexString(typeof(DataColumnMapping), ADP.SourceColumn, srcColumn, typeof(DataColumnMappingCollection));
        }
        internal static Exception ColumnsIsNotParent(ICollection collection)
        {
            return ParametersIsNotParent(typeof(DataColumnMapping), collection);
        }
        internal static Exception ColumnsIsParent(ICollection collection)
        {
            return ParametersIsParent(typeof(DataColumnMapping), collection);
        }
        internal static Exception ColumnsUniqueSourceColumn(string srcColumn)
        {
            return CollectionUniqueValue(typeof(DataColumnMapping), ADP.SourceColumn, srcColumn);
        }
        internal static Exception NotADataColumnMapping(object value)
        {
            return CollectionInvalidType(typeof(DataColumnMappingCollection), typeof(DataColumnMapping), value);
        }

        //
        // DataTableMappingCollection, DataAccess
        //
        internal static Exception InvalidSourceTable(string parameter)
        {
            return Argument(SR.ADP_InvalidSourceTable, parameter);
        }
        internal static Exception TablesAddNullAttempt(string parameter)
        {
            return CollectionNullValue(parameter, typeof(DataTableMappingCollection), typeof(DataTableMapping));
        }
        internal static Exception TablesDataSetTable(string cacheTable)
        {
            return CollectionIndexString(typeof(DataTableMapping), ADP.DataSetTable, cacheTable, typeof(DataTableMappingCollection));
        }
        internal static Exception TablesIndexInt32(int index, ITableMappingCollection collection)
        {
            return CollectionIndexInt32(index, collection.GetType(), collection.Count);
        }
        internal static Exception TablesIsNotParent(ICollection collection)
        {
            return ParametersIsNotParent(typeof(DataTableMapping), collection);
        }
        internal static Exception TablesIsParent(ICollection collection)
        {
            return ParametersIsParent(typeof(DataTableMapping), collection);
        }
        internal static Exception TablesSourceIndex(string srcTable)
        {
            return CollectionIndexString(typeof(DataTableMapping), ADP.SourceTable, srcTable, typeof(DataTableMappingCollection));
        }
        internal static Exception TablesUniqueSourceTable(string srcTable)
        {
            return CollectionUniqueValue(typeof(DataTableMapping), ADP.SourceTable, srcTable);
        }
        internal static Exception NotADataTableMapping(object value)
        {
            return CollectionInvalidType(typeof(DataTableMappingCollection), typeof(DataTableMapping), value);
        }

        //
        // IDbCommand
        //

        internal static InvalidOperationException UpdateConnectionRequired(StatementType statementType, bool isRowUpdatingCommand)
        {
            string resource;
            if (isRowUpdatingCommand)
            {
                resource = SR.ADP_ConnectionRequired_Clone;
            }
            else
            {
                switch (statementType)
                {
                    case StatementType.Insert:
                        resource = SR.ADP_ConnectionRequired_Insert;
                        break;
                    case StatementType.Update:
                        resource = SR.ADP_ConnectionRequired_Update;
                        break;
                    case StatementType.Delete:
                        resource = SR.ADP_ConnectionRequired_Delete;
                        break;
                    case StatementType.Batch:
                        resource = SR.ADP_ConnectionRequired_Batch;
                        goto default;
#if DEBUG
                    case StatementType.Select:
                        Debug.Assert(false, "shouldn't be here");
                        goto default;
#endif
                    default:
                        throw ADP.InvalidStatementType(statementType);
                }
            }
            return InvalidOperation(resource);
        }

        internal static InvalidOperationException ConnectionRequired_Res(string method) =>
            InvalidOperation("ADP_ConnectionRequired_" + method);

        internal static InvalidOperationException UpdateOpenConnectionRequired(StatementType statementType, bool isRowUpdatingCommand, ConnectionState state)
        {
            string resource;
            if (isRowUpdatingCommand)
            {
                resource = SR.ADP_OpenConnectionRequired_Clone;
            }
            else
            {
                switch (statementType)
                {
                    case StatementType.Insert:
                        resource = SR.ADP_OpenConnectionRequired_Insert;
                        break;
                    case StatementType.Update:
                        resource = SR.ADP_OpenConnectionRequired_Update;
                        break;
                    case StatementType.Delete:
                        resource = SR.ADP_OpenConnectionRequired_Delete;
                        break;
#if DEBUG
                    case StatementType.Select:
                        Debug.Assert(false, "shouldn't be here");
                        goto default;
                    case StatementType.Batch:
                        Debug.Assert(false, "isRowUpdatingCommand should have been true");
                        goto default;
#endif
                    default:
                        throw ADP.InvalidStatementType(statementType);
                }
            }
            return InvalidOperation(SR.Format(resource, ADP.ConnectionStateMsg(state)));
        }

        //
        // DbDataAdapter
        //
        internal static ArgumentException UnwantedStatementType(StatementType statementType)
        {
            return Argument(SR.Format(SR.ADP_UnwantedStatementType, statementType.ToString()));
        }

        //
        // DbDataAdapter.FillSchema
        //
        internal static Exception FillSchemaRequiresSourceTableName(string parameter)
        {
            return Argument(SR.ADP_FillSchemaRequiresSourceTableName, parameter);
        }

        //
        // DbDataAdapter.Fill
        //
        internal static Exception InvalidMaxRecords(string parameter, int max)
        {
            return Argument(SR.Format(SR.ADP_InvalidMaxRecords, max.ToString(CultureInfo.InvariantCulture)), parameter);
        }
        internal static Exception InvalidStartRecord(string parameter, int start)
        {
            return Argument(SR.Format(SR.ADP_InvalidStartRecord, start.ToString(CultureInfo.InvariantCulture)), parameter);
        }
        internal static Exception FillRequires(string parameter)
        {
            return ArgumentNull(parameter);
        }
        internal static Exception FillRequiresSourceTableName(string parameter)
        {
            return Argument(SR.ADP_FillRequiresSourceTableName, parameter);
        }
        internal static Exception FillChapterAutoIncrement()
        {
            return InvalidOperation(SR.ADP_FillChapterAutoIncrement);
        }
        internal static InvalidOperationException MissingDataReaderFieldType(int index)
        {
            return DataAdapter(SR.Format(SR.ADP_MissingDataReaderFieldType, index));
        }
        internal static InvalidOperationException OnlyOneTableForStartRecordOrMaxRecords()
        {
            return DataAdapter(SR.ADP_OnlyOneTableForStartRecordOrMaxRecords);
        }
        //
        // DbDataAdapter.Update
        //
        internal static ArgumentNullException UpdateRequiresNonNullDataSet(string parameter)
        {
            return ArgumentNull(parameter);
        }
        internal static InvalidOperationException UpdateRequiresSourceTable(string defaultSrcTableName)
        {
            return InvalidOperation(SR.Format(SR.ADP_UpdateRequiresSourceTable, defaultSrcTableName));
        }
        internal static InvalidOperationException UpdateRequiresSourceTableName(string srcTable)
        {
            return InvalidOperation(SR.Format(SR.ADP_UpdateRequiresSourceTableName, srcTable));
        }
        internal static ArgumentNullException UpdateRequiresDataTable(string parameter)
        {
            return ArgumentNull(parameter);
        }

        internal static Exception UpdateConcurrencyViolation(StatementType statementType, int affected, int expected, DataRow[] dataRows)
        {
            string resource;
            switch (statementType)
            {
                case StatementType.Update:
                    resource = SR.ADP_UpdateConcurrencyViolation_Update;
                    break;
                case StatementType.Delete:
                    resource = SR.ADP_UpdateConcurrencyViolation_Delete;
                    break;
                case StatementType.Batch:
                    resource = SR.ADP_UpdateConcurrencyViolation_Batch;
                    break;
#if DEBUG
                case StatementType.Select:
                case StatementType.Insert:
                    Debug.Assert(false, "should be here");
                    goto default;
#endif
                default:
                    throw ADP.InvalidStatementType(statementType);
            }
            DBConcurrencyException exception = new DBConcurrencyException(SR.Format(resource, affected.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture)), null, dataRows);
            TraceExceptionAsReturnValue(exception);
            return exception;
        }

        internal static InvalidOperationException UpdateRequiresCommand(StatementType statementType, bool isRowUpdatingCommand)
        {
            string resource;
            if (isRowUpdatingCommand)
            {
                resource = SR.ADP_UpdateRequiresCommandClone;
            }
            else
            {
                switch (statementType)
                {
                    case StatementType.Select:
                        resource = SR.ADP_UpdateRequiresCommandSelect;
                        break;
                    case StatementType.Insert:
                        resource = SR.ADP_UpdateRequiresCommandInsert;
                        break;
                    case StatementType.Update:
                        resource = SR.ADP_UpdateRequiresCommandUpdate;
                        break;
                    case StatementType.Delete:
                        resource = SR.ADP_UpdateRequiresCommandDelete;
                        break;
#if DEBUG
                    case StatementType.Batch:
                        Debug.Assert(false, "isRowUpdatingCommand should have been true");
                        goto default;
#endif
                    default:
                        throw ADP.InvalidStatementType(statementType);
                }
            }
            return InvalidOperation(resource);
        }
        internal static ArgumentException UpdateMismatchRowTable(int i)
        {
            return Argument(SR.Format(SR.ADP_UpdateMismatchRowTable, i.ToString(CultureInfo.InvariantCulture)));
        }
        internal static DataException RowUpdatedErrors()
        {
            return Data(SR.ADP_RowUpdatedErrors);
        }
        internal static DataException RowUpdatingErrors()
        {
            return Data(SR.ADP_RowUpdatingErrors);
        }
        internal static InvalidOperationException ResultsNotAllowedDuringBatch()
        {
            return DataAdapter(SR.ADP_ResultsNotAllowedDuringBatch);
        }

        //
        // : DbDataAdapter
        //
        internal static InvalidOperationException DynamicSQLJoinUnsupported()
        {
            return InvalidOperation(SR.ADP_DynamicSQLJoinUnsupported);
        }
        internal static InvalidOperationException DynamicSQLNoTableInfo()
        {
            return InvalidOperation(SR.ADP_DynamicSQLNoTableInfo);
        }
        internal static InvalidOperationException DynamicSQLNoKeyInfoDelete()
        {
            return InvalidOperation(SR.ADP_DynamicSQLNoKeyInfoDelete);
        }
        internal static InvalidOperationException DynamicSQLNoKeyInfoUpdate()
        {
            return InvalidOperation(SR.ADP_DynamicSQLNoKeyInfoUpdate);
        }
        internal static InvalidOperationException DynamicSQLNoKeyInfoRowVersionDelete()
        {
            return InvalidOperation(SR.ADP_DynamicSQLNoKeyInfoRowVersionDelete);
        }
        internal static InvalidOperationException DynamicSQLNoKeyInfoRowVersionUpdate()
        {
            return InvalidOperation(SR.ADP_DynamicSQLNoKeyInfoRowVersionUpdate);
        }
        internal static InvalidOperationException DynamicSQLNestedQuote(string name, string quote)
        {
            return InvalidOperation(SR.Format(SR.ADP_DynamicSQLNestedQuote, name, quote));
        }
        internal static InvalidOperationException NoQuoteChange()
        {
            return InvalidOperation(SR.ADP_NoQuoteChange);
        }
        internal static InvalidOperationException MissingSourceCommand()
        {
            return InvalidOperation(SR.ADP_MissingSourceCommand);
        }
        internal static InvalidOperationException MissingSourceCommandConnection()
        {
            return InvalidOperation(SR.ADP_MissingSourceCommandConnection);
        }

        // global constant strings
        internal const string ConnectionString = "ConnectionString";
        internal const string DataSetColumn = "DataSetColumn";
        internal const string DataSetTable = "DataSetTable";
        internal const string Fill = "Fill";
        internal const string FillSchema = "FillSchema";
        internal const string SourceColumn = "SourceColumn";
        internal const string SourceTable = "SourceTable";

        internal static DataRow[] SelectAdapterRows(DataTable dataTable, bool sorted)
        {
            const DataRowState rowStates = DataRowState.Added | DataRowState.Deleted | DataRowState.Modified;

            // equivalent to but faster than 'return dataTable.Select("", "", rowStates);'
            int countAdded = 0, countDeleted = 0, countModifed = 0;
            DataRowCollection rowCollection = dataTable.Rows;
            foreach (DataRow dataRow in rowCollection)
            {
                switch (dataRow.RowState)
                {
                    case DataRowState.Added:
                        countAdded++;
                        break;
                    case DataRowState.Deleted:
                        countDeleted++;
                        break;
                    case DataRowState.Modified:
                        countModifed++;
                        break;
                    default:
                        Debug.Assert(0 == (rowStates & dataRow.RowState), "flagged RowState");
                        break;
                }
            }
            var dataRows = new DataRow[countAdded + countDeleted + countModifed];
            if (sorted)
            {
                countModifed = countAdded + countDeleted;
                countDeleted = countAdded;
                countAdded = 0;

                foreach (DataRow dataRow in rowCollection)
                {
                    switch (dataRow.RowState)
                    {
                        case DataRowState.Added:
                            dataRows[countAdded++] = dataRow;
                            break;
                        case DataRowState.Deleted:
                            dataRows[countDeleted++] = dataRow;
                            break;
                        case DataRowState.Modified:
                            dataRows[countModifed++] = dataRow;
                            break;
                        default:
                            Debug.Assert(0 == (rowStates & dataRow.RowState), "flagged RowState");
                            break;
                    }
                }
            }
            else
            {
                int index = 0;
                foreach (DataRow dataRow in rowCollection)
                {
                    if (0 != (dataRow.RowState & rowStates))
                    {
                        dataRows[index++] = dataRow;
                        if (index == dataRows.Length)
                        {
                            break;
                        }
                    }
                }
            }
            return dataRows;
        }

        // { "a", "a", "a" } -> { "a", "a1", "a2" }
        // { "a", "a", "a1" } -> { "a", "a2", "a1" }
        // { "a", "A", "a" } -> { "a", "A1", "a2" }
        // { "a", "A", "a1" } -> { "a", "A2", "a1" }
        internal static void BuildSchemaTableInfoTableNames(string[] columnNameArray)
        {
            Dictionary<string, int> hash = new Dictionary<string, int>(columnNameArray.Length);

            int startIndex = columnNameArray.Length; // lowest non-unique index
            for (int i = columnNameArray.Length - 1; 0 <= i; --i)
            {
                string columnName = columnNameArray[i];
                if ((null != columnName) && (0 < columnName.Length))
                {
                    columnName = columnName.ToLower(CultureInfo.InvariantCulture);
                    int index;
                    if (hash.TryGetValue(columnName, out index))
                    {
                        startIndex = Math.Min(startIndex, index);
                    }
                    hash[columnName] = i;
                }
                else
                {
                    columnNameArray[i] = string.Empty;
                    startIndex = i;
                }
            }
            int uniqueIndex = 1;
            for (int i = startIndex; i < columnNameArray.Length; ++i)
            {
                string columnName = columnNameArray[i];
                if (0 == columnName.Length)
                {
                    // generate a unique name
                    columnNameArray[i] = "Column";
                    uniqueIndex = GenerateUniqueName(hash, ref columnNameArray[i], i, uniqueIndex);
                }
                else
                {
                    columnName = columnName.ToLower(CultureInfo.InvariantCulture);
                    if (i != hash[columnName])
                    {
                        GenerateUniqueName(hash, ref columnNameArray[i], i, 1);
                    }
                }
            }
        }

        private static int GenerateUniqueName(Dictionary<string, int> hash, ref string columnName, int index, int uniqueIndex)
        {
            for (; ; ++uniqueIndex)
            {
                string uniqueName = columnName + uniqueIndex.ToString(CultureInfo.InvariantCulture);
                string lowerName = uniqueName.ToLower(CultureInfo.InvariantCulture);
                if (hash.TryAdd(lowerName, index))
                {
                    columnName = uniqueName;
                    break;
                }
            }
            return uniqueIndex;
        }

        internal static int SrcCompare(string strA, string strB) => strA == strB ? 0 : 1;
    }
}
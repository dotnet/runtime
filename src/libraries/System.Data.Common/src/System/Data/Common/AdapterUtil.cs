// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    internal static class ADP
    {
        // The class ADP defines the exceptions that are specific to the Adapters.
        // The class contains functions that take the proper informational variables and then construct
        // the appropriate exception with an error string obtained from the resource framework.
        // The exception is then returned to the caller, so that the caller may then throw from its
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error messages.

        internal static Task<T> CreatedTaskWithCancellation<T>() => Task.FromCanceled<T>(new CancellationToken(true));
        internal static readonly Task<bool> s_trueTask = Task.FromResult(true);
        internal static readonly Task<bool> s_falseTask = Task.FromResult(false);

        // this method accepts BID format as an argument, this attribute allows FXCopBid rule to validate calls to it
        private static void TraceException(string trace, Exception e)
        {
            Debug.Assert(e != null, "TraceException: null Exception");
            if (e != null)
            {
                DataCommonEventSource.Log.Trace(trace, e);
            }
        }

        internal static void TraceExceptionAsReturnValue(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|THROW> '{0}'", e);
        }

        internal static void TraceExceptionForCapture(Exception e)
        {
            Debug.Assert(IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
        }

        internal static void TraceExceptionWithoutRethrow(Exception e)
        {
            Debug.Assert(IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
        }

        internal static ArgumentException Argument(string error)
        {
            ArgumentException e = new ArgumentException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentException Argument(string error, Exception inner)
        {
            ArgumentException e = new ArgumentException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentException Argument(string error, string parameter)
        {
            ArgumentException e = new ArgumentException(error, parameter);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentNullException ArgumentNull(string parameter)
        {
            ArgumentNullException e = new ArgumentNullException(parameter);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentNullException ArgumentNull(string parameter, string error)
        {
            ArgumentNullException e = new ArgumentNullException(parameter, error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName, message);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static DataException Data(string message)
        {
            DataException e = new DataException(message);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static IndexOutOfRangeException IndexOutOfRange(string error)
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidCastException InvalidCast(string error)
        {
            return InvalidCast(error, null);
        }
        internal static InvalidCastException InvalidCast(string error, Exception inner)
        {
            InvalidCastException e = new InvalidCastException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidOperationException InvalidOperation(string error)
        {
            InvalidOperationException e = new InvalidOperationException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static NotSupportedException NotSupported()
        {
            NotSupportedException e = new NotSupportedException();
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static NotSupportedException NotSupported(string error)
        {
            NotSupportedException e = new NotSupportedException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidOperationException DataAdapter(string error)
        {
            return InvalidOperation(error);
        }
        private static InvalidOperationException Provider(string error)
        {
            return InvalidOperation(error);
        }

        internal static ArgumentException InvalidMultipartName(string property, string value)
        {
            ArgumentException e = new ArgumentException(SR.Format(SR.ADP_InvalidMultipartName, property, value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException InvalidMultipartNameIncorrectUsageOfQuotes(string property, string value)
        {
            ArgumentException e = new ArgumentException(SR.Format(SR.ADP_InvalidMultipartNameQuoteUsage, property, value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException InvalidMultipartNameToManyParts(string property, string value, int limit)
        {
            ArgumentException e = new ArgumentException(SR.Format(SR.ADP_InvalidMultipartNameToManyParts, property, value, limit));
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
        internal static void CheckArgumentNull(object value, string parameterName)
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
        }

        private static readonly Type s_outOfMemoryType = typeof(OutOfMemoryException);
        private static readonly Type s_nullReferenceType = typeof(NullReferenceException);
        private static readonly Type s_accessViolationType = typeof(AccessViolationException);
        private static readonly Type s_securityType = typeof(SecurityException);

        internal static bool IsCatchableExceptionType(Exception e)
        {
            Debug.Assert(e != null, "Unexpected null exception!");

            // a 'catchable' exception is defined by what it is not.
            Type type = e.GetType();
            return
                type != s_outOfMemoryType &&
                type != s_nullReferenceType &&
                type != s_accessViolationType &&
                !s_securityType.IsAssignableFrom(type);
        }

        internal static bool IsCatchableOrSecurityExceptionType(Exception e)
        {
            Debug.Assert(e != null, "Unexpected null exception!");

            // a 'catchable' exception is defined by what it is not.
            // since IsCatchableExceptionType defined SecurityException as not 'catchable'
            // this method will return true for SecurityException has being catchable.
            // the other way to write this method is, but then SecurityException is checked twice
            // return ((e is SecurityException) || IsCatchableExceptionType(e));
            Type type = e.GetType();
            return
                type != s_outOfMemoryType &&
                type != s_nullReferenceType &&
                type != s_accessViolationType;
        }

        // Invalid Enumeration
        internal static ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_InvalidEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }

        internal static ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, string value, string method)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_NotSupportedEnumerationValue, type.Name, value, method), type.Name);
        }

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
        internal static ArgumentException ConnectionStringSyntax(int index)
        {
            return Argument(SR.Format(SR.ADP_ConnectionStringSyntax, index));
        }
        internal static ArgumentException KeywordNotSupported(string keyword)
        {
            return Argument(SR.Format(SR.ADP_KeywordNotSupported, keyword));
        }
        internal static ArgumentException InvalidKeyname(string parameterName)
        {
            return Argument(SR.ADP_InvalidKey, parameterName);
        }
        internal static ArgumentException InvalidValue(string parameterName)
        {
            return Argument(SR.ADP_InvalidValue, parameterName);
        }
        internal static ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(SR.Format(SR.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
        }

        //
        // DbConnection
        //
        private static string ConnectionStateMsg(ConnectionState state)
        {
            switch (state)
            {
                case (ConnectionState.Closed):
                case (ConnectionState.Connecting | ConnectionState.Broken): // treated the same as closed
                    return SR.ADP_ConnectionStateMsg_Closed;
                case (ConnectionState.Connecting):
                    return SR.ADP_ConnectionStateMsg_Connecting;
                case (ConnectionState.Open):
                    return SR.ADP_ConnectionStateMsg_Open;
                case (ConnectionState.Open | ConnectionState.Executing):
                    return SR.ADP_ConnectionStateMsg_OpenExecuting;
                case (ConnectionState.Open | ConnectionState.Fetching):
                    return SR.ADP_ConnectionStateMsg_OpenFetching;
                default:
                    return SR.Format(SR.ADP_ConnectionStateMsg, state.ToString());
            }
        }

        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //
        internal static Exception InvalidConnectionOptionValue(string key)
        {
            return InvalidConnectionOptionValue(key, null);
        }
        internal static Exception InvalidConnectionOptionValue(string key, Exception inner)
        {
            return Argument(SR.Format(SR.ADP_InvalidConnectionOptionValue, key), inner);
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
        internal static ArgumentException CollectionRemoveInvalidObject(Type itemType, ICollection collection)
        {
            return Argument(SR.Format(SR.ADP_CollectionRemoveInvalidObject, itemType.Name, collection.GetType().Name));
        }
        internal static ArgumentNullException CollectionNullValue(string parameter, Type collection, Type itemType)
        {
            return ArgumentNull(parameter, SR.Format(SR.ADP_CollectionNullValue, collection.Name, itemType.Name));
        }
        internal static IndexOutOfRangeException CollectionIndexInt32(int index, Type collection, int count)
        {
            return IndexOutOfRange(SR.Format(SR.ADP_CollectionIndexInt32, index.ToString(CultureInfo.InvariantCulture), collection.Name, count.ToString(CultureInfo.InvariantCulture)));
        }
        internal static IndexOutOfRangeException CollectionIndexString(Type itemType, string propertyName, string propertyValue, Type collection)
        {
            return IndexOutOfRange(SR.Format(SR.ADP_CollectionIndexString, itemType.Name, propertyName, propertyValue, collection.Name));
        }
        internal static InvalidCastException CollectionInvalidType(Type collection, Type itemType, object invalidValue)
        {
            return InvalidCast(SR.Format(SR.ADP_CollectionInvalidType, collection.Name, itemType.Name, invalidValue.GetType().Name));
        }
        internal static Exception CollectionUniqueValue(Type itemType, string propertyName, string propertyValue)
        {
            return Argument(SR.Format(SR.ADP_CollectionUniqueValue, itemType.Name, propertyName, propertyValue));
        }
        internal static ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.Format(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }
        internal static ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.Format(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
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

        internal static Exception InvalidSeekOrigin(string parameterName)
        {
            return ArgumentOutOfRange(SR.ADP_InvalidSeekOrigin, parameterName);
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

        internal enum InternalErrorCode
        {
            UnpooledObjectHasOwner = 0,
            UnpooledObjectHasWrongOwner = 1,
            PushingObjectSecondTime = 2,
            PooledObjectHasOwner = 3,
            PooledObjectInPoolMoreThanOnce = 4,
            CreateObjectReturnedNull = 5,
            NewObjectCannotBePooled = 6,
            NonPooledObjectUsedMoreThanOnce = 7,
            AttemptingToPoolOnRestrictedToken = 8,
            ConvertSidToStringSidWReturnedNull = 10,
            AttemptingToConstructReferenceCollectionOnStaticObject = 12,
            AttemptingToEnlistTwice = 13,
            CreateReferenceCollectionReturnedNull = 14,
            PooledObjectWithoutPool = 15,
            UnexpectedWaitAnyResult = 16,
            SynchronousConnectReturnedPending = 17,
            CompletedConnectReturnedPending = 18,

            NameValuePairNext = 20,
            InvalidParserState1 = 21,
            InvalidParserState2 = 22,
            InvalidParserState3 = 23,

            InvalidBuffer = 30,

            UnimplementedSMIMethod = 40,
            InvalidSmiCall = 41,

            SqlDependencyObtainProcessDispatcherFailureObjectHandle = 50,
            SqlDependencyProcessDispatcherFailureCreateInstance = 51,
            SqlDependencyProcessDispatcherFailureAppDomain = 52,
            SqlDependencyCommandHashIsNotAssociatedWithNotification = 53,

            UnknownTransactionFailure = 60,
        }
        internal static Exception InternalError(InternalErrorCode internalError)
        {
            return InvalidOperation(SR.Format(SR.ADP_InternalProviderError, (int)internalError));
        }

        //
        // : DbDataReader
        //
        internal static Exception DataReaderClosed(string method)
        {
            return InvalidOperation(SR.Format(SR.ADP_DataReaderClosed, method));
        }
        internal static ArgumentOutOfRangeException InvalidSourceBufferIndex(int maxLen, long srcOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_InvalidSourceBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), srcOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        internal static ArgumentOutOfRangeException InvalidDestinationBufferIndex(int maxLen, int dstOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_InvalidDestinationBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), dstOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        internal static IndexOutOfRangeException InvalidBufferSizeOrIndex(int numBytes, int bufferIndex)
        {
            return IndexOutOfRange(SR.Format(SR.SQL_InvalidBufferSizeOrIndex, numBytes.ToString(CultureInfo.InvariantCulture), bufferIndex.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception InvalidDataLength(long length)
        {
            return IndexOutOfRange(SR.Format(SR.SQL_InvalidDataLength, length.ToString(CultureInfo.InvariantCulture)));
        }

        //
        // : Stream
        //
        internal static Exception StreamClosed(string method)
        {
            return InvalidOperation(SR.Format(SR.ADP_StreamClosed, method));
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

        internal const CompareOptions DefaultCompareOptions = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;

        internal static bool CompareInsensitiveInvariant(string strvalue, string strconst) =>
            0 == CultureInfo.InvariantCulture.CompareInfo.Compare(strvalue, strconst, CompareOptions.IgnoreCase);

        internal static string BuildQuotedString(string quotePrefix, string quoteSuffix, string unQuotedString)
        {
            var resultString = new StringBuilder();
            if (!string.IsNullOrEmpty(quotePrefix))
            {
                resultString.Append(quotePrefix);
            }

            // Assuming that the suffix is escaped by doubling it. i.e. foo"bar becomes "foo""bar".
            if (!string.IsNullOrEmpty(quoteSuffix))
            {
                resultString.Append(unQuotedString.Replace(quoteSuffix, quoteSuffix + quoteSuffix));
                resultString.Append(quoteSuffix);
            }
            else
            {
                resultString.Append(unQuotedString);
            }

            return resultString.ToString();
        }

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

        internal static int DstCompare(string strA, string strB) => CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, ADP.DefaultCompareOptions);

        internal static bool IsEmptyArray(string[] array) => (null == array) || (0 == array.Length);

        internal static bool IsNull(object value)
        {
            if ((null == value) || (DBNull.Value == value))
            {
                return true;
            }
            INullable nullable = (value as INullable);
            return ((null != nullable) && nullable.IsNull);
        }
    }
}

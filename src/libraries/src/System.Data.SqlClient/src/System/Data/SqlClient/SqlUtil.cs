// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Transactions;

namespace System.Data.SqlClient
{
    internal static class AsyncHelper
    {
        internal static Task CreateContinuationTask(Task task, Action onSuccess, Action<Exception> onFailure = null)
        {
            if (task == null)
            {
                onSuccess();
                return null;
            }
            else
            {
                TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
                ContinueTaskWithState(task, completion,
                    state: Tuple.Create(onSuccess, onFailure,completion),
                    onSuccess: (state) => {
                        var parameters = (Tuple<Action, Action<Exception>, TaskCompletionSource<object>>)state;
                        Action success = parameters.Item1;
                        TaskCompletionSource<object> taskCompletionSource = parameters.Item3;
                        success();
                        taskCompletionSource.SetResult(null);
                    },
                    onFailure: (exception,state) =>
                    {
                        var parameters = (Tuple<Action, Action<Exception>, TaskCompletionSource<object>>)state;
                        Action<Exception> failure = parameters.Item2;
                        failure?.Invoke(exception);
                    }
                );
                return completion.Task;
            }
        }

        internal static Task CreateContinuationTaskWithState(Task task, object state, Action<object> onSuccess, Action<Exception,object> onFailure = null)
        {
            if (task == null)
            {
                onSuccess(state);
                return null;
            }
            else
            {
                var completion = new TaskCompletionSource<object>();
                ContinueTaskWithState(task, completion, state,
                    onSuccess: (continueState) => {
                        onSuccess(continueState);
                        completion.SetResult(null);
                    },
                    onFailure: onFailure
                );
                return completion.Task;
            }
        }

        internal static Task CreateContinuationTask<T1, T2>(Task task, Action<T1, T2> onSuccess, T1 arg1, T2 arg2, Action<Exception> onFailure = null)
        {
            return CreateContinuationTask(task, () => onSuccess(arg1, arg2), onFailure);
        }


        internal static void ContinueTask(Task task,
                TaskCompletionSource<object> completion,
                Action onSuccess,
                Action<Exception> onFailure = null,
                Action onCancellation = null,
                Func<Exception, Exception> exceptionConverter = null
            )
        {
            task.ContinueWith(
                tsk =>
                {
                    if (tsk.Exception != null)
                    {
                        Exception exc = tsk.Exception.InnerException;
                        if (exceptionConverter != null)
                        {
                            exc = exceptionConverter(exc);
                        }
                        try
                        {
                            onFailure?.Invoke(exc);
                        }
                        finally
                        {
                            completion.TrySetException(exc);
                        }
                    }
                    else if (tsk.IsCanceled)
                    {
                        try
                        {
                            onCancellation?.Invoke();
                        }
                        finally
                        {
                            completion.TrySetCanceled();
                        }
                    }
                    else
                    {
                        try
                        {
                            onSuccess();
                        }
                        catch (Exception e)
                        {
                            completion.SetException(e);
                        }
                    }
                }, TaskScheduler.Default
            );
        }

        // the same logic as ContinueTask but with an added state parameter to allow the caller to avoid the use of a closure
        // the parameter allocation cannot be avoided here and using closure names is clearer than Tuple numbered properties
        internal static void ContinueTaskWithState(Task task,
            TaskCompletionSource<object> completion,
            object state,
            Action<object> onSuccess,
            Action<Exception, object> onFailure = null,
            Action<object> onCancellation = null,
            Func<Exception, Exception> exceptionConverter = null
        )
        {
            task.ContinueWith(
                tsk =>
                {
                    if (tsk.Exception != null)
                    {
                        Exception exc = tsk.Exception.InnerException;
                        if (exceptionConverter != null)
                        {
                            exc = exceptionConverter(exc);
                        }
                        try
                        {
                            onFailure?.Invoke(exc, state);
                        }
                        finally
                        {
                            completion.TrySetException(exc);
                        }
                    }
                    else if (tsk.IsCanceled)
                    {
                        try
                        {
                            onCancellation?.Invoke(state);
                        }
                        finally
                        {
                            completion.TrySetCanceled();
                        }
                    }
                    else
                    {
                        try
                        {
                            onSuccess(state);
                        }
                        catch (Exception e)
                        {
                            completion.SetException(e);
                        }
                    }
                }, TaskScheduler.Default
            );
        }

        internal static void WaitForCompletion(Task task, int timeout, Action onTimeout = null, bool rethrowExceptions = true)
        {
            try
            {
                task.Wait(timeout > 0 ? (1000 * timeout) : Timeout.Infinite);
            }
            catch (AggregateException ae)
            {
                if (rethrowExceptions)
                {
                    Debug.Assert(ae.InnerExceptions.Count == 1, "There is more than one exception in AggregateException");
                    ExceptionDispatchInfo.Capture(ae.InnerException).Throw();
                }
            }
            if (!task.IsCompleted)
            {
                if (onTimeout != null)
                {
                    onTimeout();
                }
            }
        }

        internal static void SetTimeoutException(TaskCompletionSource<object> completion, int timeout, Func<Exception> exc, CancellationToken ctoken)
        {
            if (timeout > 0)
            {
                Task.Delay(timeout * 1000, ctoken).ContinueWith((tsk) =>
                {
                    if (!tsk.IsCanceled && !completion.Task.IsCompleted)
                    {
                        completion.TrySetException(exc());
                    }
                });
            }
        }
    }


    internal static class SQL
    {
        // The class SQL defines the exceptions that are specific to the SQL Adapter.
        // The class contains functions that take the proper informational variables and then construct
        // the appropriate exception with an error string obtained from the resource Framework.txt.
        // The exception is then returned to the caller, so that the caller may then throw from its
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error
        // messages.  The resource Framework.txt will ensure proper string text based on the appropriate
        // locale.

        //
        // SQL specific exceptions
        //

        //
        // SQL.Connection
        //
        internal static Exception CannotGetDTCAddress()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_CannotGetDTCAddress));
        }

        internal static Exception InvalidInternalPacketSize(string str)
        {
            return ADP.ArgumentOutOfRange(str);
        }
        internal static Exception InvalidPacketSize()
        {
            return ADP.ArgumentOutOfRange(SR.GetString(SR.SQL_InvalidTDSPacketSize));
        }
        internal static Exception InvalidPacketSizeValue()
        {
            return ADP.Argument(SR.GetString(SR.SQL_InvalidPacketSizeValue));
        }
        internal static Exception InvalidSSPIPacketSize()
        {
            return ADP.Argument(SR.GetString(SR.SQL_InvalidSSPIPacketSize));
        }
        internal static Exception NullEmptyTransactionName()
        {
            return ADP.Argument(SR.GetString(SR.SQL_NullEmptyTransactionName));
        }
        internal static Exception UserInstanceFailoverNotCompatible()
        {
            return ADP.Argument(SR.GetString(SR.SQL_UserInstanceFailoverNotCompatible));
        }
        internal static Exception ParsingErrorLibraryType(ParsingErrorState state, int libraryType)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ParsingErrorAuthLibraryType, ((int)state).ToString(CultureInfo.InvariantCulture), libraryType));
        }       
        internal static Exception InvalidSQLServerVersionUnknown()
        {
            return ADP.DataAdapter(SR.GetString(SR.SQL_InvalidSQLServerVersionUnknown));
        }
        internal static Exception SynchronousCallMayNotPend()
        {
            return new Exception(SR.GetString(SR.Sql_InternalError));
        }
        internal static Exception ConnectionLockedForBcpEvent()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ConnectionLockedForBcpEvent));
        }
        internal static Exception InstanceFailure()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_InstanceFailure));
        }
        internal static Exception ChangePasswordArgumentMissing(string argumentName)
        {
            return ADP.ArgumentNull(SR.GetString(SR.SQL_ChangePasswordArgumentMissing, argumentName));
        }
        internal static Exception ChangePasswordConflictsWithSSPI()
        {
            return ADP.Argument(SR.GetString(SR.SQL_ChangePasswordConflictsWithSSPI));
        }
        internal static Exception ChangePasswordRequiresYukon()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ChangePasswordRequiresYukon));
        }
        static internal Exception ChangePasswordUseOfUnallowedKey(string key)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ChangePasswordUseOfUnallowedKey, key));
        }

        //
        // Global Transactions.
        //
        internal static Exception GlobalTransactionsNotEnabled()
        {
            return ADP.InvalidOperation(SR.GetString(SR.GT_Disabled));
        }
        internal static Exception UnknownSysTxIsolationLevel(Transactions.IsolationLevel isolationLevel)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_UnknownSysTxIsolationLevel, isolationLevel.ToString()));
        }


        internal static Exception InvalidPartnerConfiguration(string server, string database)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_InvalidPartnerConfiguration, server, database));
        }
        internal static Exception MARSUnspportedOnConnection()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_MarsUnsupportedOnConnection));
        }

        internal static Exception CannotModifyPropertyAsyncOperationInProgress([CallerMemberName] string property = "")
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_CannotModifyPropertyAsyncOperationInProgress, property));
        }
        internal static Exception NonLocalSSEInstance()
        {
            return ADP.NotSupported(SR.GetString(SR.SQL_NonLocalSSEInstance));
        }
        //
        // SQL.DataCommand
        //

        internal static ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, int value)
        {
            return ADP.ArgumentOutOfRange(SR.GetString(SR.SQL_NotSupportedEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }

        internal static ArgumentOutOfRangeException NotSupportedCommandType(CommandType value)
        {
#if DEBUG
            switch (value)
            {
                case CommandType.Text:
                case CommandType.StoredProcedure:
                    Debug.Fail("valid CommandType " + value.ToString());
                    break;
                case CommandType.TableDirect:
                    break;
                default:
                    Debug.Fail("invalid CommandType " + value.ToString());
                    break;
            }
#endif
            return NotSupportedEnumerationValue(typeof(CommandType), (int)value);
        }
        internal static ArgumentOutOfRangeException NotSupportedIsolationLevel(IsolationLevel value)
        {
#if DEBUG
            switch (value)
            {
                case IsolationLevel.Unspecified:
                case IsolationLevel.ReadCommitted:
                case IsolationLevel.ReadUncommitted:
                case IsolationLevel.RepeatableRead:
                case IsolationLevel.Serializable:
                case IsolationLevel.Snapshot:
                    Debug.Fail("valid IsolationLevel " + value.ToString());
                    break;
                case IsolationLevel.Chaos:
                    break;
                default:
                    Debug.Fail("invalid IsolationLevel " + value.ToString());
                    break;
            }
#endif
            return NotSupportedEnumerationValue(typeof(IsolationLevel), (int)value);
        }

        internal static Exception OperationCancelled()
        {
            Exception exception = ADP.InvalidOperation(SR.GetString(SR.SQL_OperationCancelled));
            return exception;
        }

        internal static Exception PendingBeginXXXExists()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_PendingBeginXXXExists));
        }

        internal static ArgumentOutOfRangeException InvalidSqlDependencyTimeout(string param)
        {
            return ADP.ArgumentOutOfRange(SR.GetString(SR.SqlDependency_InvalidTimeout), param);
        }

        internal static Exception NonXmlResult()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_NonXmlResult));
        }

        //
        // SQL.DataParameter
        //
        internal static Exception InvalidUdt3PartNameFormat()
        {
            return ADP.Argument(SR.GetString(SR.SQL_InvalidUdt3PartNameFormat));
        }
        internal static Exception InvalidParameterTypeNameFormat()
        {
            return ADP.Argument(SR.GetString(SR.SQL_InvalidParameterTypeNameFormat));
        }
        internal static Exception InvalidParameterNameLength(string value)
        {
            return ADP.Argument(SR.GetString(SR.SQL_InvalidParameterNameLength, value));
        }
        internal static Exception PrecisionValueOutOfRange(byte precision)
        {
            return ADP.Argument(SR.GetString(SR.SQL_PrecisionValueOutOfRange, precision.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception ScaleValueOutOfRange(byte scale)
        {
            return ADP.Argument(SR.GetString(SR.SQL_ScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception TimeScaleValueOutOfRange(byte scale)
        {
            return ADP.Argument(SR.GetString(SR.SQL_TimeScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception InvalidSqlDbType(SqlDbType value)
        {
            return ADP.InvalidEnumerationValue(typeof(SqlDbType), (int)value);
        }
        internal static Exception UnsupportedTVPOutputParameter(ParameterDirection direction, string paramName)
        {
            return ADP.NotSupported(SR.GetString(SR.SqlParameter_UnsupportedTVPOutputParameter,
                        direction.ToString(), paramName));
        }
        internal static Exception DBNullNotSupportedForTVPValues(string paramName)
        {
            return ADP.NotSupported(SR.GetString(SR.SqlParameter_DBNullNotSupportedForTVP, paramName));
        }
        internal static Exception UnexpectedTypeNameForNonStructParams(string paramName)
        {
            return ADP.NotSupported(SR.GetString(SR.SqlParameter_UnexpectedTypeNameForNonStruct, paramName));
        }
        internal static Exception ParameterInvalidVariant(string paramName)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ParameterInvalidVariant, paramName));
        }

        internal static Exception MustSetTypeNameForParam(string paramType, string paramName)
        {
            return ADP.Argument(SR.GetString(SR.SQL_ParameterTypeNameRequired, paramType, paramName));
        }
        internal static Exception NullSchemaTableDataTypeNotSupported(string columnName)
        {
            return ADP.Argument(SR.GetString(SR.NullSchemaTableDataTypeNotSupported, columnName));
        }
        internal static Exception InvalidSchemaTableOrdinals()
        {
            return ADP.Argument(SR.GetString(SR.InvalidSchemaTableOrdinals));
        }
        internal static Exception EnumeratedRecordMetaDataChanged(string fieldName, int recordNumber)
        {
            return ADP.Argument(SR.GetString(SR.SQL_EnumeratedRecordMetaDataChanged, fieldName, recordNumber));
        }
        internal static Exception EnumeratedRecordFieldCountChanged(int recordNumber)
        {
            return ADP.Argument(SR.GetString(SR.SQL_EnumeratedRecordFieldCountChanged, recordNumber));
        }

        //
        // SQL.SqlDataAdapter
        //

        //
        // SQL.TDSParser
        //
        internal static Exception InvalidTDSVersion()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_InvalidTDSVersion));
        }
        internal static Exception ParsingError()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ParsingError));
        }
        static internal Exception ParsingError(ParsingErrorState state)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ParsingErrorWithState, ((int)state).ToString(CultureInfo.InvariantCulture)));
        }
        static internal Exception ParsingErrorValue(ParsingErrorState state, int value)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ParsingErrorValue, ((int)state).ToString(CultureInfo.InvariantCulture), value));
        }
        static internal Exception ParsingErrorFeatureId(ParsingErrorState state, int featureId)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ParsingErrorFeatureId, ((int)state).ToString(CultureInfo.InvariantCulture), featureId));
        }
        internal static Exception MoneyOverflow(string moneyValue)
        {
            return ADP.Overflow(SR.GetString(SR.SQL_MoneyOverflow, moneyValue));
        }
        internal static Exception SmallDateTimeOverflow(string datetime)
        {
            return ADP.Overflow(SR.GetString(SR.SQL_SmallDateTimeOverflow, datetime));
        }
        internal static Exception SNIPacketAllocationFailure()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_SNIPacketAllocationFailure));
        }
        internal static Exception TimeOverflow(string time)
        {
            return ADP.Overflow(SR.GetString(SR.SQL_TimeOverflow, time));
        }

        //
        // SQL.SqlDataReader
        //
        internal static Exception InvalidRead()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_InvalidRead));
        }

        internal static Exception NonBlobColumn(string columnName)
        {
            return ADP.InvalidCast(SR.GetString(SR.SQL_NonBlobColumn, columnName));
        }

        internal static Exception NonCharColumn(string columnName)
        {
            return ADP.InvalidCast(SR.GetString(SR.SQL_NonCharColumn, columnName));
        }

        internal static Exception StreamNotSupportOnColumnType(string columnName)
        {
            return ADP.InvalidCast(SR.GetString(SR.SQL_StreamNotSupportOnColumnType, columnName));
        }

        internal static Exception TextReaderNotSupportOnColumnType(string columnName)
        {
            return ADP.InvalidCast(SR.GetString(SR.SQL_TextReaderNotSupportOnColumnType, columnName));
        }

        internal static Exception XmlReaderNotSupportOnColumnType(string columnName)
        {
            return ADP.InvalidCast(SR.GetString(SR.SQL_XmlReaderNotSupportOnColumnType, columnName));
        }

        internal static Exception UDTUnexpectedResult(string exceptionText)
        {
            return ADP.TypeLoad(SR.GetString(SR.SQLUDT_Unexpected, exceptionText));
        }

        //
        // SQL.SqlDependency
        //
        internal static Exception SqlCommandHasExistingSqlNotificationRequest()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQLNotify_AlreadyHasCommand));
        }

        internal static Exception SqlDepDefaultOptionsButNoStart()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_DefaultOptionsButNoStart));
        }

        internal static Exception SqlDependencyDatabaseBrokerDisabled()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_DatabaseBrokerDisabled));
        }

        internal static Exception SqlDependencyEventNoDuplicate()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_EventNoDuplicate));
        }

        internal static Exception SqlDependencyDuplicateStart()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_DuplicateStart));
        }

        internal static Exception SqlDependencyIdMismatch()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_IdMismatch));
        }

        internal static Exception SqlDependencyNoMatchingServerStart()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_NoMatchingServerStart));
        }

        internal static Exception SqlDependencyNoMatchingServerDatabaseStart()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlDependency_NoMatchingServerDatabaseStart));
        }

        //
        // SQL.SqlDelegatedTransaction
        //
        internal static TransactionPromotionException PromotionFailed(Exception inner)
        {
            TransactionPromotionException e = new TransactionPromotionException(SR.GetString(SR.SqlDelegatedTransaction_PromotionFailed), inner);
            ADP.TraceExceptionAsReturnValue(e);
            return e;
        }
        //Failure while attempting to promote transaction.

        //
        // SQL.SqlMetaData
        //
        internal static Exception UnexpectedUdtTypeNameForNonUdtParams()
        {
            return ADP.Argument(SR.GetString(SR.SQLUDT_UnexpectedUdtTypeName));
        }
        internal static Exception MustSetUdtTypeNameForUdtParams()
        {
            return ADP.Argument(SR.GetString(SR.SQLUDT_InvalidUdtTypeName));
        }
        internal static Exception UDTInvalidSqlType(string typeName)
        {
            return ADP.Argument(SR.GetString(SR.SQLUDT_InvalidSqlType, typeName));
        }
        internal static Exception InvalidSqlDbTypeForConstructor(SqlDbType type)
        {
            return ADP.Argument(SR.GetString(SR.SqlMetaData_InvalidSqlDbTypeForConstructorFormat, type.ToString()));
        }

        internal static Exception NameTooLong(string parameterName)
        {
            return ADP.Argument(SR.GetString(SR.SqlMetaData_NameTooLong), parameterName);
        }

        internal static Exception InvalidSortOrder(SortOrder order)
        {
            return ADP.InvalidEnumerationValue(typeof(SortOrder), (int)order);
        }

        internal static Exception MustSpecifyBothSortOrderAndOrdinal(SortOrder order, int ordinal)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlMetaData_SpecifyBothSortOrderAndOrdinal, order.ToString(), ordinal));
        }

        internal static Exception UnsupportedColumnTypeForSqlProvider(string columnName, string typeName)
        {
            return ADP.Argument(SR.GetString(SR.SqlProvider_InvalidDataColumnType, columnName, typeName));
        }
        internal static Exception InvalidColumnMaxLength(string columnName, long maxLength)
        {
            return ADP.Argument(SR.GetString(SR.SqlProvider_InvalidDataColumnMaxLength, columnName, maxLength));
        }
        internal static Exception InvalidColumnPrecScale()
        {
            return ADP.Argument(SR.GetString(SR.SqlMisc_InvalidPrecScaleMessage));
        }
        internal static Exception NotEnoughColumnsInStructuredType()
        {
            return ADP.Argument(SR.GetString(SR.SqlProvider_NotEnoughColumnsInStructuredType));
        }
        internal static Exception DuplicateSortOrdinal(int sortOrdinal)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlProvider_DuplicateSortOrdinal, sortOrdinal));
        }
        internal static Exception MissingSortOrdinal(int sortOrdinal)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlProvider_MissingSortOrdinal, sortOrdinal));
        }
        internal static Exception SortOrdinalGreaterThanFieldCount(int columnOrdinal, int sortOrdinal)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlProvider_SortOrdinalGreaterThanFieldCount, sortOrdinal, columnOrdinal));
        }
        internal static Exception IEnumerableOfSqlDataRecordHasNoRows()
        {
            return ADP.Argument(SR.GetString(SR.IEnumerableOfSqlDataRecordHasNoRows));
        }




        //
        // SQL.BulkLoad
        //
        internal static Exception BulkLoadMappingInaccessible()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadMappingInaccessible));
        }
        internal static Exception BulkLoadMappingsNamesOrOrdinalsOnly()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadMappingsNamesOrOrdinalsOnly));
        }
        internal static Exception BulkLoadCannotConvertValue(Type sourcetype, MetaType metatype, Exception e)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadCannotConvertValue, sourcetype.Name, metatype.TypeName), e);
        }
        internal static Exception BulkLoadNonMatchingColumnMapping()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadNonMatchingColumnMapping));
        }
        internal static Exception BulkLoadNonMatchingColumnName(string columnName)
        {
            return BulkLoadNonMatchingColumnName(columnName, null);
        }
        internal static Exception BulkLoadNonMatchingColumnName(string columnName, Exception e)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadNonMatchingColumnName, columnName), e);
        }
        internal static Exception BulkLoadStringTooLong()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadStringTooLong));
        }
        internal static Exception BulkLoadInvalidVariantValue()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadInvalidVariantValue));
        }
        internal static Exception BulkLoadInvalidTimeout(int timeout)
        {
            return ADP.Argument(SR.GetString(SR.SQL_BulkLoadInvalidTimeout, timeout.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception BulkLoadExistingTransaction()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadExistingTransaction));
        }
        internal static Exception BulkLoadNoCollation()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadNoCollation));
        }
        internal static Exception BulkLoadConflictingTransactionOption()
        {
            return ADP.Argument(SR.GetString(SR.SQL_BulkLoadConflictingTransactionOption));
        }
        internal static Exception BulkLoadLcidMismatch(int sourceLcid, string sourceColumnName, int destinationLcid, string destinationColumnName)
        {
            return ADP.InvalidOperation(SR.GetString(SR.Sql_BulkLoadLcidMismatch, sourceLcid, sourceColumnName, destinationLcid, destinationColumnName));
        }
        internal static Exception InvalidOperationInsideEvent()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadInvalidOperationInsideEvent));
        }
        internal static Exception BulkLoadMissingDestinationTable()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadMissingDestinationTable));
        }
        internal static Exception BulkLoadInvalidDestinationTable(string tableName, Exception inner)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadInvalidDestinationTable, tableName), inner);
        }
        internal static Exception BulkLoadBulkLoadNotAllowDBNull(string columnName)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadNotAllowDBNull, columnName));
        }
        internal static Exception BulkLoadPendingOperation()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BulkLoadPendingOperation));
        }
        internal static Exception InvalidTableDerivedPrecisionForTvp(string columnName, byte precision)
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlParameter_InvalidTableDerivedPrecisionForTvp, precision, columnName, System.Data.SqlTypes.SqlDecimal.MaxPrecision));
        }

        //
        // transactions.
        //
        internal static Exception ConnectionDoomed()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_ConnectionDoomed));
        }

        internal static Exception OpenResultCountExceeded()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_OpenResultCountExceeded));
        }

        internal static Exception UnsupportedSysTxForGlobalTransactions()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_UnsupportedSysTxVersion));
        }

        internal static readonly byte[] AttentionHeader = new byte[] {
            TdsEnums.MT_ATTN,               // Message Type
            TdsEnums.ST_EOM,                // Status
            TdsEnums.HEADER_LEN >> 8,       // length - upper byte
            TdsEnums.HEADER_LEN & 0xff,     // length - lower byte
            0,                              // spid
            0,                              // spid
            0,                              // packet (out of band)
            0                               // window
        };

        //
        // MultiSubnetFailover
        //

        /// <summary>
        /// used to block two scenarios if MultiSubnetFailover is true: 
        /// * server-provided failover partner - raising SqlException in this case
        /// * connection string with failover partner and MultiSubnetFailover=true - raising argument one in this case with the same message
        /// </summary>
        internal static Exception MultiSubnetFailoverWithFailoverPartner(bool serverProvidedFailoverPartner, SqlInternalConnectionTds internalConnection)
        {
            string msg = SR.GetString(SR.SQLMSF_FailoverPartnerNotSupported);
            if (serverProvidedFailoverPartner)
            {
                // Replacing InvalidOperation with SQL exception
                SqlErrorCollection errors = new SqlErrorCollection();
                errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, msg, "", 0));
                SqlException exc = SqlException.CreateException(errors, null, internalConnection);
                exc._doNotReconnect = true; // disable open retry logic on this error
                return exc;
            }
            else
            {
                return ADP.Argument(msg);
            }
        }

        internal static Exception MultiSubnetFailoverWithMoreThan64IPs()
        {
            string msg = GetSNIErrorMessage((int)SNINativeMethodWrapper.SniSpecialErrors.MultiSubnetFailoverWithMoreThan64IPs);
            return ADP.InvalidOperation(msg);
        }

        internal static Exception MultiSubnetFailoverWithInstanceSpecified()
        {
            string msg = GetSNIErrorMessage((int)SNINativeMethodWrapper.SniSpecialErrors.MultiSubnetFailoverWithInstanceSpecified);
            return ADP.Argument(msg);
        }

        internal static Exception MultiSubnetFailoverWithNonTcpProtocol()
        {
            string msg = GetSNIErrorMessage((int)SNINativeMethodWrapper.SniSpecialErrors.MultiSubnetFailoverWithNonTcpProtocol);
            return ADP.Argument(msg);
        }

        //
        // Read-only routing
        //

        internal static Exception ROR_FailoverNotSupportedConnString()
        {
            return ADP.Argument(SR.GetString(SR.SQLROR_FailoverNotSupported));
        }

        internal static Exception ROR_FailoverNotSupportedServer(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (SR.GetString(SR.SQLROR_FailoverNotSupported)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_RecursiveRoutingNotSupported(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (SR.GetString(SR.SQLROR_RecursiveRoutingNotSupported)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_UnexpectedRoutingInfo(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (SR.GetString(SR.SQLROR_UnexpectedRoutingInfo)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_InvalidRoutingInfo(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (SR.GetString(SR.SQLROR_InvalidRoutingInfo)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_TimeoutAfterRoutingInfo(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (SR.GetString(SR.SQLROR_TimeoutAfterRoutingInfo)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        //
        // Connection resiliency
        //
        internal static SqlException CR_ReconnectTimeout()
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(TdsEnums.TIMEOUT_EXPIRED, (byte)0x00, TdsEnums.MIN_ERROR_CLASS, null, SQLMessage.Timeout(), "", 0, TdsEnums.SNI_WAIT_TIMEOUT));
            SqlException exc = SqlException.CreateException(errors, "");
            return exc;
        }

        internal static SqlException CR_ReconnectionCancelled()
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.MIN_ERROR_CLASS, null, SQLMessage.OperationCancelled(), "", 0));
            SqlException exc = SqlException.CreateException(errors, "");
            return exc;
        }

        internal static Exception CR_NextAttemptWillExceedQueryTimeout(SqlException innerException, Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.MIN_ERROR_CLASS, null, SR.GetString(SR.SQLCR_NextAttemptWillExceedQueryTimeout), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId, innerException);
            return exc;
        }

        internal static Exception CR_EncryptionChanged(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, SR.GetString(SR.SQLCR_EncryptionChanged), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection);
            return exc;
        }

        internal static SqlException CR_AllAttemptsFailed(SqlException innerException, Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.MIN_ERROR_CLASS, null, SR.GetString(SR.SQLCR_AllAttemptsFailed), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId, innerException);
            return exc;
        }

        internal static SqlException CR_NoCRAckAtReconnection(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, SR.GetString(SR.SQLCR_NoCRAckAtReconnection), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection);
            return exc;
        }

        internal static SqlException CR_TDSVersionNotPreserved(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, SR.GetString(SR.SQLCR_TDSVestionNotPreserved), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection);
            return exc;
        }

        internal static SqlException CR_UnrecoverableServer(Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, SR.GetString(SR.SQLCR_UnrecoverableServer), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId);
            return exc;
        }

        internal static SqlException CR_UnrecoverableClient(Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, SR.GetString(SR.SQLCR_UnrecoverableClient), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId);
            return exc;
        }

        internal static Exception StreamWriteNotSupported()
        {
            return ADP.NotSupported(SR.GetString(SR.SQL_StreamWriteNotSupported));
        }
        internal static Exception StreamReadNotSupported()
        {
            return ADP.NotSupported(SR.GetString(SR.SQL_StreamReadNotSupported));
        }
        internal static Exception StreamSeekNotSupported()
        {
            return ADP.NotSupported(SR.GetString(SR.SQL_StreamSeekNotSupported));
        }
        internal static System.Data.SqlTypes.SqlNullValueException SqlNullValue()
        {
            System.Data.SqlTypes.SqlNullValueException e = new System.Data.SqlTypes.SqlNullValueException();
            return e;
        }
        internal static Exception SubclassMustOverride()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SqlMisc_SubclassMustOverride));
        }

        // ProjectK\CoreCLR specific errors
        internal static Exception UnsupportedKeyword(string keyword)
        {
            return ADP.NotSupported(SR.GetString(SR.SQL_UnsupportedKeyword, keyword));
        }
        internal static Exception NetworkLibraryKeywordNotSupported()
        {
            return ADP.NotSupported(SR.GetString(SR.SQL_NetworkLibraryNotSupported));
        }
        internal static Exception UnsupportedFeatureAndToken(SqlInternalConnectionTds internalConnection, string token)
        {
            var innerException = ADP.NotSupported(SR.GetString(SR.SQL_UnsupportedToken, token));

            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, SR.GetString(SR.SQL_UnsupportedFeature), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection, innerException);
            return exc;
        }

        internal static Exception BatchedUpdatesNotAvailableOnContextConnection()
        {
            return ADP.InvalidOperation(SR.GetString(SR.SQL_BatchedUpdatesNotAvailableOnContextConnection));
        }

        /// <summary>
        /// gets a message for SNI error (sniError must be valid, non-zero error code)
        /// </summary>
        internal static string GetSNIErrorMessage(int sniError)
        {
            Debug.Assert(sniError > 0 && sniError <= (int)SNINativeMethodWrapper.SniSpecialErrors.MaxErrorValue, "SNI error is out of range");

            string errorMessageId = string.Format("SNI_ERROR_{0}", sniError);
            return SR.GetResourceString(errorMessageId, errorMessageId);
        }

        // Default values for SqlDependency and SqlNotificationRequest
        internal const int SqlDependencyTimeoutDefault = 0;
        internal const int SqlDependencyServerTimeout = 5 * 24 * 3600; // 5 days - used to compute default TTL of the dependency
        internal const string SqlNotificationServiceDefault = "SqlQueryNotificationService";
        internal const string SqlNotificationStoredProcedureDefault = "SqlQueryNotificationStoredProcedure";
    }

    sealed internal class SQLMessage
    {
        private SQLMessage() { /* prevent utility class from being instantiated*/ }

        // The class SQLMessage defines the error messages that are specific to the SqlDataAdapter
        // that are caused by a netlib error.  The functions will be called and then return the
        // appropriate error message from the resource Framework.txt.  The SqlDataAdapter will then
        // take the error message and then create a SqlError for the message and then place
        // that into a SqlException that is either thrown to the user or cached for throwing at
        // a later time.  This class is used so that there will be compile time checking of error
        // messages.  The resource Framework.txt will ensure proper string text based on the appropriate
        // locale.

        internal static string CultureIdError()
        {
            return SR.GetString(SR.SQL_CultureIdError);
        }
        internal static string EncryptionNotSupportedByClient()
        {
            return SR.GetString(SR.SQL_EncryptionNotSupportedByClient);
        }
        internal static string EncryptionNotSupportedByServer()
        {
            return SR.GetString(SR.SQL_EncryptionNotSupportedByServer);
        }
        internal static string OperationCancelled()
        {
            return SR.GetString(SR.SQL_OperationCancelled);
        }
        internal static string SevereError()
        {
            return SR.GetString(SR.SQL_SevereError);
        }
        internal static string SSPIInitializeError()
        {
            return SR.GetString(SR.SQL_SSPIInitializeError);
        }
        internal static string SSPIGenerateError()
        {
            return SR.GetString(SR.SQL_SSPIGenerateError);
        }
        internal static string SqlServerBrowserNotAccessible()
        {
            return SR.GetString(SR.SQL_SqlServerBrowserNotAccessible);
        }
        internal static string KerberosTicketMissingError()
        {
            return SR.GetString(SR.SQL_KerberosTicketMissingError);
        }
        internal static string Timeout()
        {
            return SR.GetString(SR.SQL_Timeout);
        }
        internal static string Timeout_PreLogin_Begin()
        {
            return SR.GetString(SR.SQL_Timeout_PreLogin_Begin);
        }
        internal static string Timeout_PreLogin_InitializeConnection()
        {
            return SR.GetString(SR.SQL_Timeout_PreLogin_InitializeConnection);
        }
        internal static string Timeout_PreLogin_SendHandshake()
        {
            return SR.GetString(SR.SQL_Timeout_PreLogin_SendHandshake);
        }
        internal static string Timeout_PreLogin_ConsumeHandshake()
        {
            return SR.GetString(SR.SQL_Timeout_PreLogin_ConsumeHandshake);
        }
        internal static string Timeout_Login_Begin()
        {
            return SR.GetString(SR.SQL_Timeout_Login_Begin);
        }
        internal static string Timeout_Login_ProcessConnectionAuth()
        {
            return SR.GetString(SR.SQL_Timeout_Login_ProcessConnectionAuth);
        }
        internal static string Timeout_PostLogin()
        {
            return SR.GetString(SR.SQL_Timeout_PostLogin);
        }
        internal static string Timeout_FailoverInfo()
        {
            return SR.GetString(SR.SQL_Timeout_FailoverInfo);
        }
        internal static string Timeout_RoutingDestination()
        {
            return SR.GetString(SR.SQL_Timeout_RoutingDestinationInfo);
        }
        internal static string Duration_PreLogin_Begin(long PreLoginBeginDuration)
        {
            return SR.GetString(SR.SQL_Duration_PreLogin_Begin, PreLoginBeginDuration);
        }
        internal static string Duration_PreLoginHandshake(long PreLoginBeginDuration, long PreLoginHandshakeDuration)
        {
            return SR.GetString(SR.SQL_Duration_PreLoginHandshake, PreLoginBeginDuration, PreLoginHandshakeDuration);
        }
        internal static string Duration_Login_Begin(long PreLoginBeginDuration, long PreLoginHandshakeDuration, long LoginBeginDuration)
        {
            return SR.GetString(SR.SQL_Duration_Login_Begin, PreLoginBeginDuration, PreLoginHandshakeDuration, LoginBeginDuration);
        }
        internal static string Duration_Login_ProcessConnectionAuth(long PreLoginBeginDuration, long PreLoginHandshakeDuration, long LoginBeginDuration, long LoginAuthDuration)
        {
            return SR.GetString(SR.SQL_Duration_Login_ProcessConnectionAuth, PreLoginBeginDuration, PreLoginHandshakeDuration, LoginBeginDuration, LoginAuthDuration);
        }
        internal static string Duration_PostLogin(long PreLoginBeginDuration, long PreLoginHandshakeDuration, long LoginBeginDuration, long LoginAuthDuration, long PostLoginDuration)
        {
            return SR.GetString(SR.SQL_Duration_PostLogin, PreLoginBeginDuration, PreLoginHandshakeDuration, LoginBeginDuration, LoginAuthDuration, PostLoginDuration);
        }
        internal static string UserInstanceFailure()
        {
            return SR.GetString(SR.SQL_UserInstanceFailure);
        }
        internal static string PreloginError()
        {
            return SR.GetString(SR.Snix_PreLogin);
        }
        internal static string ExClientConnectionId()
        {
            return SR.GetString(SR.SQL_ExClientConnectionId);
        }
        internal static string ExErrorNumberStateClass()
        {
            return SR.GetString(SR.SQL_ExErrorNumberStateClass);
        }
        internal static string ExOriginalClientConnectionId()
        {
            return SR.GetString(SR.SQL_ExOriginalClientConnectionId);
        }
        internal static string ExRoutingDestination()
        {
            return SR.GetString(SR.SQL_ExRoutingDestination);
        }
    }

    /// <summary>
    /// This class holds helper methods to escape Microsoft SQL Server identifiers, such as table, schema, database or other names
    /// </summary>
    internal static class SqlServerEscapeHelper
    {
        /// <summary>
        /// Escapes the identifier with square brackets. The input has to be in unescaped form, like the parts received from MultipartIdentifier.ParseMultipartIdentifier.
        /// </summary>
        /// <param name="name">name of the identifier, in unescaped form</param>
        /// <returns>escapes the name with [], also escapes the last close bracket with double-bracket</returns>
        internal static string EscapeIdentifier(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "null or empty identifiers are not allowed");
            return "[" + name.Replace("]", "]]") + "]";
        }

        /// <summary>
        /// Same as above EscapeIdentifier, except that output is written into StringBuilder
        /// </summary>
        internal static void EscapeIdentifier(StringBuilder builder, string name)
        {
            Debug.Assert(builder != null, "builder cannot be null");
            Debug.Assert(!string.IsNullOrEmpty(name), "null or empty identifiers are not allowed");

            builder.Append("[");
            builder.Append(name.Replace("]", "]]"));
            builder.Append("]");
        }

        /// <summary>
        ///  Escape a string to be used inside TSQL literal, such as N'somename' or 'somename'
        /// </summary>
        internal static string EscapeStringAsLiteral(string input)
        {
            Debug.Assert(input != null, "input string cannot be null");
            return input.Replace("'", "''");
        }

        /// <summary>
        /// Escape a string as a TSQL literal, wrapping it around with single quotes.
        /// Use this method to escape input strings to prevent SQL injection 
        /// and to get correct behavior for embedded quotes.
        /// </summary>
        /// <param name="input">unescaped string</param>
        /// <returns>escaped and quoted literal string</returns>
        internal static string MakeStringLiteral(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "''";
            }
            else
            {
                return "'" + EscapeStringAsLiteral(input) + "'";
            }
        }
    }

    /// <summary>
    /// This class holds methods invoked on System.Transactions through reflection for Global Transactions
    /// </summary>
    static internal class SysTxForGlobalTransactions
    {
        private static readonly Lazy<MethodInfo> _enlistPromotableSinglePhase = new Lazy<MethodInfo>(() =>
            typeof(Transaction).GetMethod("EnlistPromotableSinglePhase", new Type[] { typeof(IPromotableSinglePhaseNotification), typeof(Guid) }));

        private static readonly Lazy<MethodInfo> _setDistributedTransactionIdentifier = new Lazy<MethodInfo>(() =>
            typeof(Transaction).GetMethod("SetDistributedTransactionIdentifier", new Type[] { typeof(IPromotableSinglePhaseNotification), typeof(Guid) }));

        private static readonly Lazy<MethodInfo> _getPromotedToken = new Lazy<MethodInfo>(() =>
            typeof(Transaction).GetMethod("GetPromotedToken"));

        /// <summary>
        /// Enlists the given IPromotableSinglePhaseNotification and Non-MSDTC Promoter type into a transaction
        /// </summary>
        /// <returns>The MethodInfo instance to be invoked. Null if the method doesn't exist</returns>
        public static MethodInfo EnlistPromotableSinglePhase
        {
            get
            {
                return _enlistPromotableSinglePhase.Value;
            }
        }

        /// <summary>
        /// Sets the given DistributedTransactionIdentifier for a Transaction instance.
        /// Needs to be invoked when using a Non-MSDTC Promoter type
        /// </summary>
        /// <returns>The MethodInfo instance to be invoked. Null if the method doesn't exist</returns>
        public static MethodInfo SetDistributedTransactionIdentifier
        {
            get
            {
                return _setDistributedTransactionIdentifier.Value;
            }
        }

        /// <summary>
        /// Gets the Promoted Token for a Transaction
        /// </summary>
        /// <returns>The MethodInfo instance to be invoked. Null if the method doesn't exist</returns>
        public static MethodInfo GetPromotedToken
        {
            get
            {
                return _getPromotedToken.Value;
            }
        }
    }
}//namespace

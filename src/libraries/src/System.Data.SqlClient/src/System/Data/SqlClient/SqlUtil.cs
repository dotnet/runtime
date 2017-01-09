// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient
{
    using Res = System.SR;

    internal static class AsyncHelper
    {
        internal static Task CreateContinuationTask(Task task, Action onSuccess, SqlInternalConnectionTds connectionToDoom = null, Action<Exception> onFailure = null)
        {
            if (task == null)
            {
                onSuccess();
                return null;
            }
            else
            {
                TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
                ContinueTask(task, completion,
                    () => { onSuccess(); completion.SetResult(null); },
                    connectionToDoom, onFailure);
                return completion.Task;
            }
        }

        internal static Task CreateContinuationTask<T1, T2>(Task task, Action<T1, T2> onSuccess, T1 arg1, T2 arg2, SqlInternalConnectionTds connectionToDoom = null, Action<Exception> onFailure = null)
        {
            return CreateContinuationTask(task, () => onSuccess(arg1, arg2), connectionToDoom, onFailure);
        }

        internal static void ContinueTask(Task task,
                TaskCompletionSource<object> completion,
                Action onSuccess,
                SqlInternalConnectionTds connectionToDoom = null,
                Action<Exception> onFailure = null,
                Action onCancellation = null,
                Func<Exception, Exception> exceptionConverter = null,
                SqlConnection connectionToAbort = null
            )
        {
            Debug.Assert((connectionToAbort == null) || (connectionToDoom == null), "Should not specify both connectionToDoom and connectionToAbort");
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
                            if (onFailure != null)
                            {
                                onFailure(exc);
                            }
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
                            if (onCancellation != null)
                            {
                                onCancellation();
                            }
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

        internal static Exception InvalidInternalPacketSize(string str)
        {
            return ADP.ArgumentOutOfRange(str);
        }
        internal static Exception InvalidPacketSize()
        {
            return ADP.ArgumentOutOfRange(Res.GetString(Res.SQL_InvalidTDSPacketSize));
        }
        internal static Exception InvalidPacketSizeValue()
        {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidPacketSizeValue));
        }
        internal static Exception InvalidSSPIPacketSize()
        {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidSSPIPacketSize));
        }
        internal static Exception NullEmptyTransactionName()
        {
            return ADP.Argument(Res.GetString(Res.SQL_NullEmptyTransactionName));
        }
        internal static Exception UserInstanceFailoverNotCompatible()
        {
            return ADP.Argument(Res.GetString(Res.SQL_UserInstanceFailoverNotCompatible));
        }
        internal static Exception InvalidSQLServerVersionUnknown()
        {
            return ADP.DataAdapter(Res.GetString(Res.SQL_InvalidSQLServerVersionUnknown));
        }
        internal static Exception SynchronousCallMayNotPend()
        {
            return new Exception(Res.GetString(Res.Sql_InternalError));
        }
        internal static Exception ConnectionLockedForBcpEvent()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ConnectionLockedForBcpEvent));
        }
        internal static Exception InstanceFailure()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InstanceFailure));
        }
        internal static Exception InvalidPartnerConfiguration(string server, string database)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidPartnerConfiguration, server, database));
        }
        internal static Exception MARSUnspportedOnConnection()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_MarsUnsupportedOnConnection));
        }

        internal static Exception CannotModifyPropertyAsyncOperationInProgress([CallerMemberName] string property = "")
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_CannotModifyPropertyAsyncOperationInProgress, property));
        }
        internal static Exception NonLocalSSEInstance()
        {
            return ADP.NotSupported(Res.GetString(Res.SQL_NonLocalSSEInstance));
        }
        //
        // SQL.DataCommand
        //

        internal static ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, int value)
        {
            return ADP.ArgumentOutOfRange(Res.GetString(Res.SQL_NotSupportedEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }

        internal static ArgumentOutOfRangeException NotSupportedCommandType(CommandType value)
        {
#if DEBUG
            switch (value)
            {
                case CommandType.Text:
                case CommandType.StoredProcedure:
                    Debug.Assert(false, "valid CommandType " + value.ToString());
                    break;
                case CommandType.TableDirect:
                    break;
                default:
                    Debug.Assert(false, "invalid CommandType " + value.ToString());
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
                    Debug.Assert(false, "valid IsolationLevel " + value.ToString());
                    break;
                case IsolationLevel.Chaos:
                    break;
                default:
                    Debug.Assert(false, "invalid IsolationLevel " + value.ToString());
                    break;
            }
#endif
            return NotSupportedEnumerationValue(typeof(IsolationLevel), (int)value);
        }

        internal static Exception OperationCancelled()
        {
            Exception exception = ADP.InvalidOperation(Res.GetString(Res.SQL_OperationCancelled));
            return exception;
        }

        internal static Exception PendingBeginXXXExists()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_PendingBeginXXXExists));
        }


        internal static Exception NonXmlResult()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_NonXmlResult));
        }

        //
        // SQL.DataParameter
        //
        internal static Exception InvalidParameterTypeNameFormat()
        {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidParameterTypeNameFormat));
        }
        internal static Exception InvalidParameterNameLength(string value)
        {
            return ADP.Argument(Res.GetString(Res.SQL_InvalidParameterNameLength, value));
        }
        internal static Exception PrecisionValueOutOfRange(byte precision)
        {
            return ADP.Argument(Res.GetString(Res.SQL_PrecisionValueOutOfRange, precision.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception ScaleValueOutOfRange(byte scale)
        {
            return ADP.Argument(Res.GetString(Res.SQL_ScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception TimeScaleValueOutOfRange(byte scale)
        {
            return ADP.Argument(Res.GetString(Res.SQL_TimeScaleValueOutOfRange, scale.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception InvalidSqlDbType(SqlDbType value)
        {
            return ADP.InvalidEnumerationValue(typeof(SqlDbType), (int)value);
        }
        internal static Exception UnsupportedTVPOutputParameter(ParameterDirection direction, string paramName)
        {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_UnsupportedTVPOutputParameter,
                        direction.ToString(), paramName));
        }
        internal static Exception DBNullNotSupportedForTVPValues(string paramName)
        {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_DBNullNotSupportedForTVP, paramName));
        }
        internal static Exception UnexpectedTypeNameForNonStructParams(string paramName)
        {
            return ADP.NotSupported(Res.GetString(Res.SqlParameter_UnexpectedTypeNameForNonStruct, paramName));
        }
        internal static Exception ParameterInvalidVariant(string paramName)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ParameterInvalidVariant, paramName));
        }

        internal static Exception MustSetTypeNameForParam(string paramType, string paramName)
        {
            return ADP.Argument(Res.GetString(Res.SQL_ParameterTypeNameRequired, paramType, paramName));
        }
        internal static Exception EnumeratedRecordMetaDataChanged(string fieldName, int recordNumber)
        {
            return ADP.Argument(Res.GetString(Res.SQL_EnumeratedRecordMetaDataChanged, fieldName, recordNumber));
        }
        internal static Exception EnumeratedRecordFieldCountChanged(int recordNumber)
        {
            return ADP.Argument(Res.GetString(Res.SQL_EnumeratedRecordFieldCountChanged, recordNumber));
        }

        //
        // SQL.SqlDataAdapter
        //

        //
        // SQL.TDSParser
        //
        internal static Exception InvalidTDSVersion()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidTDSVersion));
        }
        internal static Exception ParsingError()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ParsingError));
        }
        internal static Exception MoneyOverflow(string moneyValue)
        {
            return ADP.Overflow(Res.GetString(Res.SQL_MoneyOverflow, moneyValue));
        }
        internal static Exception SmallDateTimeOverflow(string datetime)
        {
            return ADP.Overflow(Res.GetString(Res.SQL_SmallDateTimeOverflow, datetime));
        }
        internal static Exception SNIPacketAllocationFailure()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_SNIPacketAllocationFailure));
        }
        internal static Exception TimeOverflow(string time)
        {
            return ADP.Overflow(Res.GetString(Res.SQL_TimeOverflow, time));
        }

        //
        // SQL.SqlDataReader
        //
        internal static Exception InvalidRead()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_InvalidRead));
        }

        internal static Exception NonBlobColumn(string columnName)
        {
            return ADP.InvalidCast(Res.GetString(Res.SQL_NonBlobColumn, columnName));
        }

        internal static Exception NonCharColumn(string columnName)
        {
            return ADP.InvalidCast(Res.GetString(Res.SQL_NonCharColumn, columnName));
        }

        internal static Exception StreamNotSupportOnColumnType(string columnName)
        {
            return ADP.InvalidCast(Res.GetString(Res.SQL_StreamNotSupportOnColumnType, columnName));
        }

        internal static Exception TextReaderNotSupportOnColumnType(string columnName)
        {
            return ADP.InvalidCast(Res.GetString(Res.SQL_TextReaderNotSupportOnColumnType, columnName));
        }

        internal static Exception XmlReaderNotSupportOnColumnType(string columnName)
        {
            return ADP.InvalidCast(Res.GetString(Res.SQL_XmlReaderNotSupportOnColumnType, columnName));
        }


        internal static Exception InvalidSqlDbTypeForConstructor(SqlDbType type)
        {
            return ADP.Argument(Res.GetString(Res.SqlMetaData_InvalidSqlDbTypeForConstructorFormat, type.ToString()));
        }

        internal static Exception NameTooLong(string parameterName)
        {
            return ADP.Argument(Res.GetString(Res.SqlMetaData_NameTooLong), parameterName);
        }

        internal static Exception InvalidSortOrder(SortOrder order)
        {
            return ADP.InvalidEnumerationValue(typeof(SortOrder), (int)order);
        }

        internal static Exception MustSpecifyBothSortOrderAndOrdinal(SortOrder order, int ordinal)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SqlMetaData_SpecifyBothSortOrderAndOrdinal, order.ToString(), ordinal));
        }

        internal static Exception UnsupportedColumnTypeForSqlProvider(string columnName, string typeName)
        {
            return ADP.Argument(Res.GetString(Res.SqlProvider_InvalidDataColumnType, columnName, typeName));
        }
        internal static Exception NotEnoughColumnsInStructuredType()
        {
            return ADP.Argument(Res.GetString(Res.SqlProvider_NotEnoughColumnsInStructuredType));
        }
        internal static Exception DuplicateSortOrdinal(int sortOrdinal)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_DuplicateSortOrdinal, sortOrdinal));
        }
        internal static Exception MissingSortOrdinal(int sortOrdinal)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_MissingSortOrdinal, sortOrdinal));
        }
        internal static Exception SortOrdinalGreaterThanFieldCount(int columnOrdinal, int sortOrdinal)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SqlProvider_SortOrdinalGreaterThanFieldCount, sortOrdinal, columnOrdinal));
        }
        internal static Exception IEnumerableOfSqlDataRecordHasNoRows()
        {
            return ADP.Argument(Res.GetString(Res.IEnumerableOfSqlDataRecordHasNoRows));
        }




        //
        // SQL.BulkLoad
        //
        internal static Exception BulkLoadMappingInaccessible()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMappingInaccessible));
        }
        internal static Exception BulkLoadMappingsNamesOrOrdinalsOnly()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMappingsNamesOrOrdinalsOnly));
        }
        internal static Exception BulkLoadCannotConvertValue(Type sourcetype, MetaType metatype, Exception e)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadCannotConvertValue, sourcetype.Name, metatype.TypeName), e);
        }
        internal static Exception BulkLoadNonMatchingColumnMapping()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNonMatchingColumnMapping));
        }
        internal static Exception BulkLoadNonMatchingColumnName(string columnName)
        {
            return BulkLoadNonMatchingColumnName(columnName, null);
        }
        internal static Exception BulkLoadNonMatchingColumnName(string columnName, Exception e)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNonMatchingColumnName, columnName), e);
        }
        internal static Exception BulkLoadStringTooLong()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadStringTooLong));
        }
        internal static Exception BulkLoadInvalidVariantValue()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidVariantValue));
        }
        internal static Exception BulkLoadInvalidTimeout(int timeout)
        {
            return ADP.Argument(Res.GetString(Res.SQL_BulkLoadInvalidTimeout, timeout.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception BulkLoadExistingTransaction()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadExistingTransaction));
        }
        internal static Exception BulkLoadNoCollation()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNoCollation));
        }
        internal static Exception BulkLoadConflictingTransactionOption()
        {
            return ADP.Argument(Res.GetString(Res.SQL_BulkLoadConflictingTransactionOption));
        }
        internal static Exception BulkLoadLcidMismatch(int sourceLcid, string sourceColumnName, int destinationLcid, string destinationColumnName)
        {
            return ADP.InvalidOperation(Res.GetString(Res.Sql_BulkLoadLcidMismatch, sourceLcid, sourceColumnName, destinationLcid, destinationColumnName));
        }
        internal static Exception InvalidOperationInsideEvent()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidOperationInsideEvent));
        }
        internal static Exception BulkLoadMissingDestinationTable()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadMissingDestinationTable));
        }
        internal static Exception BulkLoadInvalidDestinationTable(string tableName, Exception inner)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadInvalidDestinationTable, tableName), inner);
        }
        internal static Exception BulkLoadBulkLoadNotAllowDBNull(string columnName)
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadNotAllowDBNull, columnName));
        }
        internal static Exception BulkLoadPendingOperation()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_BulkLoadPendingOperation));
        }

        //
        // transactions.
        //
        internal static Exception ConnectionDoomed()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_ConnectionDoomed));
        }

        internal static Exception OpenResultCountExceeded()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SQL_OpenResultCountExceeded));
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
            string msg = Res.GetString(Res.SQLMSF_FailoverPartnerNotSupported);
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
            return ADP.Argument(Res.GetString(Res.SQLROR_FailoverNotSupported));
        }

        internal static Exception ROR_FailoverNotSupportedServer(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (Res.GetString(Res.SQLROR_FailoverNotSupported)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_RecursiveRoutingNotSupported(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (Res.GetString(Res.SQLROR_RecursiveRoutingNotSupported)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_UnexpectedRoutingInfo(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (Res.GetString(Res.SQLROR_UnexpectedRoutingInfo)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_InvalidRoutingInfo(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (Res.GetString(Res.SQLROR_InvalidRoutingInfo)), "", 0));
            SqlException exc = SqlException.CreateException(errors, null, internalConnection);
            exc._doNotReconnect = true;
            return exc;
        }

        internal static Exception ROR_TimeoutAfterRoutingInfo(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, (byte)0x00, TdsEnums.FATAL_ERROR_CLASS, null, (Res.GetString(Res.SQLROR_TimeoutAfterRoutingInfo)), "", 0));
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
            errors.Add(new SqlError(0, 0, TdsEnums.MIN_ERROR_CLASS, null, Res.GetString(Res.SQLCR_NextAttemptWillExceedQueryTimeout), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId, innerException);
            return exc;
        }

        internal static Exception CR_EncryptionChanged(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, Res.GetString(Res.SQLCR_EncryptionChanged), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection);
            return exc;
        }

        internal static SqlException CR_AllAttemptsFailed(SqlException innerException, Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.MIN_ERROR_CLASS, null, Res.GetString(Res.SQLCR_AllAttemptsFailed), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId, innerException);
            return exc;
        }

        internal static SqlException CR_NoCRAckAtReconnection(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, Res.GetString(Res.SQLCR_NoCRAckAtReconnection), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection);
            return exc;
        }

        internal static SqlException CR_TDSVersionNotPreserved(SqlInternalConnectionTds internalConnection)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, Res.GetString(Res.SQLCR_TDSVestionNotPreserved), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection);
            return exc;
        }

        internal static SqlException CR_UnrecoverableServer(Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, Res.GetString(Res.SQLCR_UnrecoverableServer), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId);
            return exc;
        }

        internal static SqlException CR_UnrecoverableClient(Guid connectionId)
        {
            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, Res.GetString(Res.SQLCR_UnrecoverableClient), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", connectionId);
            return exc;
        }

        internal static Exception StreamWriteNotSupported()
        {
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamWriteNotSupported));
        }
        internal static Exception StreamReadNotSupported()
        {
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamReadNotSupported));
        }
        internal static Exception StreamSeekNotSupported()
        {
            return ADP.NotSupported(Res.GetString(Res.SQL_StreamSeekNotSupported));
        }
        internal static System.Data.SqlTypes.SqlNullValueException SqlNullValue()
        {
            System.Data.SqlTypes.SqlNullValueException e = new System.Data.SqlTypes.SqlNullValueException();
            return e;
        }
        internal static Exception SubclassMustOverride()
        {
            return ADP.InvalidOperation(Res.GetString(Res.SqlMisc_SubclassMustOverride));
        }

        // ProjectK\CoreCLR specific errors
        internal static Exception UnsupportedKeyword(string keyword)
        {
            return ADP.NotSupported(Res.GetString(Res.SQL_UnsupportedKeyword, keyword));
        }
        internal static Exception NetworkLibraryKeywordNotSupported()
        {
            return ADP.NotSupported(Res.GetString(Res.SQL_NetworkLibraryNotSupported));
        }
        internal static Exception UnsupportedFeatureAndToken(SqlInternalConnectionTds internalConnection, string token)
        {
            var innerException = ADP.NotSupported(Res.GetString(Res.SQL_UnsupportedToken, token));

            SqlErrorCollection errors = new SqlErrorCollection();
            errors.Add(new SqlError(0, 0, TdsEnums.FATAL_ERROR_CLASS, null, Res.GetString(Res.SQL_UnsupportedFeature), "", 0));
            SqlException exc = SqlException.CreateException(errors, "", internalConnection, innerException);
            return exc;
        }

        /// <summary>
        /// gets a message for SNI error (sniError must be valid, non-zero error code)
        /// </summary>
        internal static string GetSNIErrorMessage(int sniError)
        {
            Debug.Assert(sniError > 0 && sniError <= (int)SNINativeMethodWrapper.SniSpecialErrors.MaxErrorValue, "SNI error is out of range");

            string errorMessageId = String.Format((IFormatProvider)null, "SNI_ERROR_{0}", sniError);
            return SR.GetResourceString(errorMessageId, errorMessageId);
        }
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
            return Res.GetString(Res.SQL_CultureIdError);
        }
        internal static string EncryptionNotSupportedByClient()
        {
            return Res.GetString(Res.SQL_EncryptionNotSupportedByClient);
        }
        internal static string EncryptionNotSupportedByServer()
        {
            return Res.GetString(Res.SQL_EncryptionNotSupportedByServer);
        }
        internal static string OperationCancelled()
        {
            return Res.GetString(Res.SQL_OperationCancelled);
        }
        internal static string SevereError()
        {
            return Res.GetString(Res.SQL_SevereError);
        }
        internal static string SSPIInitializeError()
        {
            return Res.GetString(Res.SQL_SSPIInitializeError);
        }
        internal static string SSPIGenerateError()
        {
            return Res.GetString(Res.SQL_SSPIGenerateError);
        }
        internal static string Timeout()
        {
            return Res.GetString(Res.SQL_Timeout);
        }
        internal static string Timeout_PreLogin_Begin()
        {
            return Res.GetString(Res.SQL_Timeout_PreLogin_Begin);
        }
        internal static string Timeout_PreLogin_InitializeConnection()
        {
            return Res.GetString(Res.SQL_Timeout_PreLogin_InitializeConnection);
        }
        internal static string Timeout_PreLogin_SendHandshake()
        {
            return Res.GetString(Res.SQL_Timeout_PreLogin_SendHandshake);
        }
        internal static string Timeout_PreLogin_ConsumeHandshake()
        {
            return Res.GetString(Res.SQL_Timeout_PreLogin_ConsumeHandshake);
        }
        internal static string Timeout_Login_Begin()
        {
            return Res.GetString(Res.SQL_Timeout_Login_Begin);
        }
        internal static string Timeout_Login_ProcessConnectionAuth()
        {
            return Res.GetString(Res.SQL_Timeout_Login_ProcessConnectionAuth);
        }
        internal static string Timeout_PostLogin()
        {
            return Res.GetString(Res.SQL_Timeout_PostLogin);
        }
        internal static string Timeout_FailoverInfo()
        {
            return Res.GetString(Res.SQL_Timeout_FailoverInfo);
        }
        internal static string Timeout_RoutingDestination()
        {
            return Res.GetString(Res.SQL_Timeout_RoutingDestinationInfo);
        }
        internal static string Duration_PreLogin_Begin(long PreLoginBeginDuration)
        {
            return Res.GetString(Res.SQL_Duration_PreLogin_Begin, PreLoginBeginDuration);
        }
        internal static string Duration_PreLoginHandshake(long PreLoginBeginDuration, long PreLoginHandshakeDuration)
        {
            return Res.GetString(Res.SQL_Duration_PreLoginHandshake, PreLoginBeginDuration, PreLoginHandshakeDuration);
        }
        internal static string Duration_Login_Begin(long PreLoginBeginDuration, long PreLoginHandshakeDuration, long LoginBeginDuration)
        {
            return Res.GetString(Res.SQL_Duration_Login_Begin, PreLoginBeginDuration, PreLoginHandshakeDuration, LoginBeginDuration);
        }
        internal static string Duration_Login_ProcessConnectionAuth(long PreLoginBeginDuration, long PreLoginHandshakeDuration, long LoginBeginDuration, long LoginAuthDuration)
        {
            return Res.GetString(Res.SQL_Duration_Login_ProcessConnectionAuth, PreLoginBeginDuration, PreLoginHandshakeDuration, LoginBeginDuration, LoginAuthDuration);
        }
        internal static string Duration_PostLogin(long PreLoginBeginDuration, long PreLoginHandshakeDuration, long LoginBeginDuration, long LoginAuthDuration, long PostLoginDuration)
        {
            return Res.GetString(Res.SQL_Duration_PostLogin, PreLoginBeginDuration, PreLoginHandshakeDuration, LoginBeginDuration, LoginAuthDuration, PostLoginDuration);
        }
        internal static string UserInstanceFailure()
        {
            return Res.GetString(Res.SQL_UserInstanceFailure);
        }
        internal static string PreloginError()
        {
            return Res.GetString(Res.Snix_PreLogin);
        }
        internal static string ExClientConnectionId()
        {
            return Res.GetString(Res.SQL_ExClientConnectionId);
        }
        internal static string ExErrorNumberStateClass()
        {
            return Res.GetString(Res.SQL_ExErrorNumberStateClass);
        }
        internal static string ExOriginalClientConnectionId()
        {
            return Res.GetString(Res.SQL_ExOriginalClientConnectionId);
        }
        internal static string ExRoutingDestination()
        {
            return Res.GetString(Res.SQL_ExRoutingDestination);
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
    }
}//namespace

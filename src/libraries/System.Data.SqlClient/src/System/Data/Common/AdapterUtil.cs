// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Res = System.SR;

namespace System
{
    internal static partial class SR
    {
        internal static string GetString(string value)
        {
            return value;
        }

        internal static string GetString(string format, params object[] args)
        {
            return SR.Format(format, args);
        }
    }
}


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

        static internal Exception ExceptionWithStackTrace(Exception e)
        {
            try
            {
                throw e;
            }
            catch (Exception caught)
            {
                return caught;
            }
        }

        // NOTE: Initializing a Task in SQL CLR requires the "UNSAFE" permission set (http://msdn.microsoft.com/en-us/library/ms172338.aspx)
        // Therefore we are lazily initializing these Tasks to avoid forcing customers to use the "UNSAFE" set when they are actually using no Async features
        static private Task<bool> s_trueTask = null;
        static internal Task<bool> TrueTask
        {
            get
            {
                if (s_trueTask == null)
                {
                    s_trueTask = Task.FromResult<bool>(true);
                }
                return s_trueTask;
            }
        }

        static private Task<bool> s_falseTask = null;
        static internal Task<bool> FalseTask
        {
            get
            {
                if (s_falseTask == null)
                {
                    s_falseTask = Task.FromResult<bool>(false);
                }
                return s_falseTask;
            }
        }

        static private readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        static internal bool IsWindows
        {
            get
            {
                return s_isWindows;
            }
        }


        //
        // COM+ exceptions
        //
        static internal ArgumentException Argument(string error)
        {
            ArgumentException e = new ArgumentException(error);
            return e;
        }
        static internal ArgumentException Argument(string error, Exception inner)
        {
            ArgumentException e = new ArgumentException(error, inner);
            return e;
        }
        static internal ArgumentException Argument(string error, string parameter)
        {
            ArgumentException e = new ArgumentException(error, parameter);
            return e;
        }
        static internal ArgumentNullException ArgumentNull(string parameter)
        {
            ArgumentNullException e = new ArgumentNullException(parameter);
            return e;
        }
        static internal ArgumentNullException ArgumentNull(string parameter, string error)
        {
            ArgumentNullException e = new ArgumentNullException(parameter, error);
            return e;
        }
        static internal ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName);
            return e;
        }
        static internal ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName, message);
            return e;
        }
        static internal IndexOutOfRangeException IndexOutOfRange(int value)
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException(value.ToString(CultureInfo.InvariantCulture));
            return e;
        }
        static internal IndexOutOfRangeException IndexOutOfRange(string error)
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException(error);
            return e;
        }
        static internal IndexOutOfRangeException IndexOutOfRange()
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException();
            return e;
        }
        static internal InvalidCastException InvalidCast(string error)
        {
            return InvalidCast(error, null);
        }
        static internal InvalidCastException InvalidCast(string error, Exception inner)
        {
            InvalidCastException e = new InvalidCastException(error, inner);
            return e;
        }
        static internal InvalidOperationException InvalidOperation(string error)
        {
            InvalidOperationException e = new InvalidOperationException(error);
            return e;
        }
        static internal TimeoutException TimeoutException(string error)
        {
            TimeoutException e = new TimeoutException(error);
            return e;
        }
        static internal InvalidOperationException InvalidOperation(string error, Exception inner)
        {
            InvalidOperationException e = new InvalidOperationException(error, inner);
            return e;
        }
        static internal NotSupportedException NotSupported()
        {
            NotSupportedException e = new NotSupportedException();
            return e;
        }
        static internal NotSupportedException NotSupported(string error)
        {
            NotSupportedException e = new NotSupportedException(error);
            return e;
        }
        static internal OverflowException Overflow(string error)
        {
            return Overflow(error, null);
        }
        static internal OverflowException Overflow(string error, Exception inner)
        {
            OverflowException e = new OverflowException(error, inner);
            return e;
        }

        static internal PlatformNotSupportedException DbTypeNotSupported(string dbType)
        {
            PlatformNotSupportedException e = new PlatformNotSupportedException(Res.GetString(Res.SQL_DbTypeNotSupportedOnThisPlatform, dbType));
            return e;
        }
        static internal InvalidCastException InvalidCast()
        {
            InvalidCastException e = new InvalidCastException();
            return e;
        }
        static internal IOException IO(string error)
        {
            IOException e = new IOException(error);
            return e;
        }
        static internal IOException IO(string error, Exception inner)
        {
            IOException e = new IOException(error, inner);
            return e;
        }
        static internal InvalidOperationException DataAdapter(string error)
        {
            return InvalidOperation(error);
        }
        static private InvalidOperationException Provider(string error)
        {
            return InvalidOperation(error);
        }
        static internal ObjectDisposedException ObjectDisposed(object instance)
        {
            ObjectDisposedException e = new ObjectDisposedException(instance.GetType().Name);
            return e;
        }

        static internal InvalidOperationException MethodCalledTwice(string method)
        {
            InvalidOperationException e = new InvalidOperationException(Res.GetString(Res.ADP_CalledTwice, method));
            return e;
        }


        static internal ArgumentException InvalidMultipartName(string property, string value)
        {
            ArgumentException e = new ArgumentException(Res.GetString(Res.ADP_InvalidMultipartName, Res.GetString(property), value));
            return e;
        }

        static internal ArgumentException InvalidMultipartNameIncorrectUsageOfQuotes(string property, string value)
        {
            ArgumentException e = new ArgumentException(Res.GetString(Res.ADP_InvalidMultipartNameQuoteUsage, Res.GetString(property), value));
            return e;
        }

        static internal ArgumentException InvalidMultipartNameToManyParts(string property, string value, int limit)
        {
            ArgumentException e = new ArgumentException(Res.GetString(Res.ADP_InvalidMultipartNameToManyParts, Res.GetString(property), value, limit));
            return e;
        }

        static internal void CheckArgumentNull(object value, string parameterName)
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
        }



        static internal bool IsCatchableExceptionType(Exception e)
        {
            return !((e is NullReferenceException) ||
                     (e is SecurityException));
        }

        static internal bool IsCatchableOrSecurityExceptionType(Exception e)
        {
            // a 'catchable' exception is defined by what it is not.
            // since IsCatchableExceptionType defined SecurityException as not 'catchable'
            // this method will return true for SecurityException has being catchable.

            // the other way to write this method is, but then SecurityException is checked twice
            // return ((e is SecurityException) || IsCatchableExceptionType(e));

            Debug.Assert(e != null, "Unexpected null exception!");
            // Most of the exception types above will cause the process to fail fast
            // So they are no longer needed in this check
            return !(e is NullReferenceException);
        }

        // Invalid Enumeration

        static internal ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
        {
            return ADP.ArgumentOutOfRange(Res.GetString(Res.ADP_InvalidEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }


        // IDbCommand.CommandType
        static internal ArgumentOutOfRangeException InvalidCommandType(CommandType value)
        {
#if DEBUG
            switch (value)
            {
                case CommandType.Text:
                case CommandType.StoredProcedure:
                case CommandType.TableDirect:
                    Debug.Assert(false, "valid CommandType " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(CommandType), (int)value);
        }


        // IDbConnection.BeginTransaction, OleDbTransaction.Begin
        static internal ArgumentOutOfRangeException InvalidIsolationLevel(IsolationLevel value)
        {
#if DEBUG
            switch (value)
            {
                case IsolationLevel.Unspecified:
                case IsolationLevel.Chaos:
                case IsolationLevel.ReadUncommitted:
                case IsolationLevel.ReadCommitted:
                case IsolationLevel.RepeatableRead:
                case IsolationLevel.Serializable:
                case IsolationLevel.Snapshot:
                    Debug.Fail("valid IsolationLevel " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(IsolationLevel), (int)value);
        }


        // IDataParameter.Direction
        static internal ArgumentOutOfRangeException InvalidParameterDirection(ParameterDirection value)
        {
#if DEBUG
            switch (value)
            {
                case ParameterDirection.Input:
                case ParameterDirection.Output:
                case ParameterDirection.InputOutput:
                case ParameterDirection.ReturnValue:
                    Debug.Assert(false, "valid ParameterDirection " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(ParameterDirection), (int)value);
        }


        // IDbCommand.UpdateRowSource
        static internal ArgumentOutOfRangeException InvalidUpdateRowSource(UpdateRowSource value)
        {
#if DEBUG
            switch (value)
            {
                case UpdateRowSource.None:
                case UpdateRowSource.OutputParameters:
                case UpdateRowSource.FirstReturnedRecord:
                case UpdateRowSource.Both:
                    Debug.Assert(false, "valid UpdateRowSource " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(UpdateRowSource), (int)value);
        }


        //
        // DbConnectionOptions, DataAccess
        //
        static internal ArgumentException ConnectionStringSyntax(int index)
        {
            return Argument(Res.GetString(Res.ADP_ConnectionStringSyntax, index));
        }
        static internal ArgumentException KeywordNotSupported(string keyword)
        {
            return Argument(Res.GetString(Res.ADP_KeywordNotSupported, keyword));
        }
        static internal ArgumentException InvalidMinMaxPoolSizeValues()
        {
            return ADP.Argument(Res.GetString(Res.ADP_InvalidMinMaxPoolSizeValues));
        }
        static internal ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(Res.GetString(Res.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
        }


        //
        // DbConnection
        //
        static internal InvalidOperationException NoConnectionString()
        {
            return InvalidOperation(Res.GetString(Res.ADP_NoConnectionString));
        }

        static internal Exception MethodNotImplemented([CallerMemberName] string methodName = "")
        {
            return NotImplemented.ByDesignWithMessage(methodName);
        }

        static private string ConnectionStateMsg(ConnectionState state)
        {
            switch (state)
            {
                case (ConnectionState.Closed):
                case (ConnectionState.Connecting | ConnectionState.Broken):
                    return Res.GetString(Res.ADP_ConnectionStateMsg_Closed);
                case (ConnectionState.Connecting):
                    return Res.GetString(Res.ADP_ConnectionStateMsg_Connecting);
                case (ConnectionState.Open):
                    return Res.GetString(Res.ADP_ConnectionStateMsg_Open);
                case (ConnectionState.Open | ConnectionState.Executing):
                    return Res.GetString(Res.ADP_ConnectionStateMsg_OpenExecuting);
                case (ConnectionState.Open | ConnectionState.Fetching):
                    return Res.GetString(Res.ADP_ConnectionStateMsg_OpenFetching);
                default:
                    return Res.GetString(Res.ADP_ConnectionStateMsg, state.ToString());
            }
        }


        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //
        static internal Exception InvalidConnectionOptionValue(string key)
        {
            return InvalidConnectionOptionValue(key, null);
        }
        static internal Exception InvalidConnectionOptionValueLength(string key, int limit)
        {
            return Argument(Res.GetString(Res.ADP_InvalidConnectionOptionValueLength, key, limit));
        }
        static internal Exception InvalidConnectionOptionValue(string key, Exception inner)
        {
            return Argument(Res.GetString(Res.ADP_InvalidConnectionOptionValue, key), inner);
        }
        static internal Exception MissingConnectionOptionValue(string key, string requiredAdditionalKey)
        {
            return Argument(Res.GetString(Res.ADP_MissingConnectionOptionValue, key, requiredAdditionalKey));
        }


        static internal Exception WrongType(Type got, Type expected)
        {
            return Argument(Res.GetString(Res.SQL_WrongType, got.ToString(), expected.ToString()));
        }


        //
        // DbConnectionPool and related
        //
        static internal Exception PooledOpenTimeout()
        {
            return ADP.InvalidOperation(Res.GetString(Res.ADP_PooledOpenTimeout));
        }

        static internal Exception NonPooledOpenTimeout()
        {
            return ADP.TimeoutException(Res.GetString(Res.ADP_NonPooledOpenTimeout));
        }

        //
        // Generic Data Provider Collection
        //
        static internal ArgumentException CollectionRemoveInvalidObject(Type itemType, ICollection collection)
        {
            return Argument(Res.GetString(Res.ADP_CollectionRemoveInvalidObject, itemType.Name, collection.GetType().Name));
        }
        static internal ArgumentNullException CollectionNullValue(string parameter, Type collection, Type itemType)
        {
            return ArgumentNull(parameter, Res.GetString(Res.ADP_CollectionNullValue, collection.Name, itemType.Name));
        }
        static internal IndexOutOfRangeException CollectionIndexInt32(int index, Type collection, int count)
        {
            return IndexOutOfRange(Res.GetString(Res.ADP_CollectionIndexInt32, index.ToString(CultureInfo.InvariantCulture), collection.Name, count.ToString(CultureInfo.InvariantCulture)));
        }
        static internal IndexOutOfRangeException CollectionIndexString(Type itemType, string propertyName, string propertyValue, Type collection)
        {
            return IndexOutOfRange(Res.GetString(Res.ADP_CollectionIndexString, itemType.Name, propertyName, propertyValue, collection.Name));
        }
        static internal InvalidCastException CollectionInvalidType(Type collection, Type itemType, object invalidValue)
        {
            return InvalidCast(Res.GetString(Res.ADP_CollectionInvalidType, collection.Name, itemType.Name, invalidValue.GetType().Name));
        }
        static internal ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
        {
            return Argument(Res.GetString(Res.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }
        static internal ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
        {
            return Argument(Res.GetString(Res.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }

        //
        // DbProviderException
        //
        static internal InvalidOperationException TransactionConnectionMismatch()
        {
            return Provider(Res.GetString(Res.ADP_TransactionConnectionMismatch));
        }
        static internal InvalidOperationException TransactionRequired(string method)
        {
            return Provider(Res.GetString(Res.ADP_TransactionRequired, method));
        }


        static internal Exception CommandTextRequired(string method)
        {
            return InvalidOperation(Res.GetString(Res.ADP_CommandTextRequired, method));
        }

        static internal InvalidOperationException ConnectionRequired(string method)
        {
            return InvalidOperation(Res.GetString(Res.ADP_ConnectionRequired, method));
        }
        static internal InvalidOperationException OpenConnectionRequired(string method, ConnectionState state)
        {
            return InvalidOperation(Res.GetString(Res.ADP_OpenConnectionRequired, method, ADP.ConnectionStateMsg(state)));
        }

        static internal Exception OpenReaderExists()
        {
            return OpenReaderExists(null);
        }

        static internal Exception OpenReaderExists(Exception e)
        {
            return InvalidOperation(Res.GetString(Res.ADP_OpenReaderExists), e);
        }


        //
        // DbDataReader
        //
        static internal Exception NonSeqByteAccess(long badIndex, long currIndex, string method)
        {
            return InvalidOperation(Res.GetString(Res.ADP_NonSeqByteAccess, badIndex.ToString(CultureInfo.InvariantCulture), currIndex.ToString(CultureInfo.InvariantCulture), method));
        }

        static internal Exception NegativeParameter(string parameterName)
        {
            return InvalidOperation(Res.GetString(Res.ADP_NegativeParameter, parameterName));
        }


        static internal Exception InvalidSeekOrigin(string parameterName)
        {
            return ArgumentOutOfRange(Res.GetString(Res.ADP_InvalidSeekOrigin), parameterName);
        }

        //
        // SqlMetaData, SqlTypes, SqlClient
        //
        static internal Exception InvalidMetaDataValue()
        {
            return ADP.Argument(Res.GetString(Res.ADP_InvalidMetaDataValue));
        }

        static internal InvalidOperationException NonSequentialColumnAccess(int badCol, int currCol)
        {
            return InvalidOperation(Res.GetString(Res.ADP_NonSequentialColumnAccess, badCol.ToString(CultureInfo.InvariantCulture), currCol.ToString(CultureInfo.InvariantCulture)));
        }


        //
        // : IDbCommand
        //
        static internal Exception InvalidCommandTimeout(int value, [CallerMemberName] string property = "")
        {
            return Argument(Res.GetString(Res.ADP_InvalidCommandTimeout, value.ToString(CultureInfo.InvariantCulture)), property);
        }
        static internal Exception UninitializedParameterSize(int index, Type dataType)
        {
            return InvalidOperation(Res.GetString(Res.ADP_UninitializedParameterSize, index.ToString(CultureInfo.InvariantCulture), dataType.Name));
        }
        static internal Exception PrepareParameterType(DbCommand cmd)
        {
            return InvalidOperation(Res.GetString(Res.ADP_PrepareParameterType, cmd.GetType().Name));
        }
        static internal Exception PrepareParameterSize(DbCommand cmd)
        {
            return InvalidOperation(Res.GetString(Res.ADP_PrepareParameterSize, cmd.GetType().Name));
        }
        static internal Exception PrepareParameterScale(DbCommand cmd, string type)
        {
            return InvalidOperation(Res.GetString(Res.ADP_PrepareParameterScale, cmd.GetType().Name, type));
        }
        static internal Exception MismatchedAsyncResult(string expectedMethod, string gotMethod)
        {
            return InvalidOperation(Res.GetString(Res.ADP_MismatchedAsyncResult, expectedMethod, gotMethod));
        }

        //
        // : ConnectionUtil
        //
        static internal Exception ConnectionIsDisabled(Exception InnerException)
        {
            return InvalidOperation(Res.GetString(Res.ADP_ConnectionIsDisabled), InnerException);
        }
        static internal Exception ClosedConnectionError()
        {
            return InvalidOperation(Res.GetString(Res.ADP_ClosedConnectionError));
        }
        static internal Exception ConnectionAlreadyOpen(ConnectionState state)
        {
            return InvalidOperation(Res.GetString(Res.ADP_ConnectionAlreadyOpen, ADP.ConnectionStateMsg(state)));
        }
        static internal Exception OpenConnectionPropertySet(string property, ConnectionState state)
        {
            return InvalidOperation(Res.GetString(Res.ADP_OpenConnectionPropertySet, property, ADP.ConnectionStateMsg(state)));
        }
        static internal Exception EmptyDatabaseName()
        {
            return Argument(Res.GetString(Res.ADP_EmptyDatabaseName));
        }

        internal enum ConnectionError
        {
            BeginGetConnectionReturnsNull,
            GetConnectionReturnsNull,
            ConnectionOptionsMissing,
            CouldNotSwitchToClosedPreviouslyOpenedState,
        }
        static internal Exception InternalConnectionError(ConnectionError internalError)
        {
            return InvalidOperation(Res.GetString(Res.ADP_InternalConnectionError, (int)internalError));
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
            //          ConnectionOptionsInUse                                  =  9,
            ConvertSidToStringSidWReturnedNull = 10,
            //          UnexpectedTransactedObject                              = 11,
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
        static internal Exception InternalError(InternalErrorCode internalError)
        {
            return InvalidOperation(Res.GetString(Res.ADP_InternalProviderError, (int)internalError));
        }

        static internal Exception InvalidConnectRetryCountValue()
        {
            return Argument(Res.GetString(Res.SQLCR_InvalidConnectRetryCountValue));
        }

        static internal Exception InvalidConnectRetryIntervalValue()
        {
            return Argument(Res.GetString(Res.SQLCR_InvalidConnectRetryIntervalValue));
        }

        //
        // : DbDataReader
        //
        static internal Exception DataReaderClosed([CallerMemberName] string method = "")
        {
            return InvalidOperation(Res.GetString(Res.ADP_DataReaderClosed, method));
        }
        static internal ArgumentOutOfRangeException InvalidSourceBufferIndex(int maxLen, long srcOffset, string parameterName)
        {
            return ArgumentOutOfRange(Res.GetString(Res.ADP_InvalidSourceBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), srcOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        static internal ArgumentOutOfRangeException InvalidDestinationBufferIndex(int maxLen, int dstOffset, string parameterName)
        {
            return ArgumentOutOfRange(Res.GetString(Res.ADP_InvalidDestinationBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), dstOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        static internal IndexOutOfRangeException InvalidBufferSizeOrIndex(int numBytes, int bufferIndex)
        {
            return IndexOutOfRange(Res.GetString(Res.SQL_InvalidBufferSizeOrIndex, numBytes.ToString(CultureInfo.InvariantCulture), bufferIndex.ToString(CultureInfo.InvariantCulture)));
        }
        static internal Exception InvalidDataLength(long length)
        {
            return IndexOutOfRange(Res.GetString(Res.SQL_InvalidDataLength, length.ToString(CultureInfo.InvariantCulture)));
        }
        static internal InvalidOperationException AsyncOperationPending()
        {
            return InvalidOperation(Res.GetString(Res.ADP_PendingAsyncOperation));
        }

        //
        // : Stream
        //
        static internal Exception StreamClosed([CallerMemberName] string method = "")
        {
            return InvalidOperation(Res.GetString(Res.ADP_StreamClosed, method));
        }
        static internal IOException ErrorReadingFromStream(Exception internalException)
        {
            return IO(Res.GetString(Res.SqlMisc_StreamErrorMessage), internalException);
        }

        static internal ArgumentException InvalidDataType(string typeName)
        {
            return Argument(Res.GetString(Res.ADP_InvalidDataType, typeName));
        }
        static internal ArgumentException UnknownDataType(Type dataType)
        {
            return Argument(Res.GetString(Res.ADP_UnknownDataType, dataType.FullName));
        }
        static internal ArgumentException DbTypeNotSupported(System.Data.DbType type, Type enumtype)
        {
            return Argument(Res.GetString(Res.ADP_DbTypeNotSupported, type.ToString(), enumtype.Name));
        }
        static internal ArgumentException InvalidOffsetValue(int value)
        {
            return Argument(Res.GetString(Res.ADP_InvalidOffsetValue, value.ToString(CultureInfo.InvariantCulture)));
        }
        static internal ArgumentException InvalidSizeValue(int value)
        {
            return Argument(Res.GetString(Res.ADP_InvalidSizeValue, value.ToString(CultureInfo.InvariantCulture)));
        }
        static internal ArgumentException ParameterValueOutOfRange(Decimal value)
        {
            return ADP.Argument(Res.GetString(Res.ADP_ParameterValueOutOfRange, value.ToString((IFormatProvider)null)));
        }
        static internal ArgumentException ParameterValueOutOfRange(SqlDecimal value)
        {
            return ADP.Argument(Res.GetString(Res.ADP_ParameterValueOutOfRange, value.ToString()));
        }

        static internal ArgumentException VersionDoesNotSupportDataType(string typeName)
        {
            return Argument(Res.GetString(Res.ADP_VersionDoesNotSupportDataType, typeName));
        }
        static internal Exception ParameterConversionFailed(object value, Type destType, Exception inner)
        {
            Debug.Assert(null != value, "null value on conversion failure");
            Debug.Assert(null != inner, "null inner on conversion failure");

            Exception e;
            string message = Res.GetString(Res.ADP_ParameterConversionFailed, value.GetType().Name, destType.Name);
            if (inner is ArgumentException)
            {
                e = new ArgumentException(message, inner);
            }
            else if (inner is FormatException)
            {
                e = new FormatException(message, inner);
            }
            else if (inner is InvalidCastException)
            {
                e = new InvalidCastException(message, inner);
            }
            else if (inner is OverflowException)
            {
                e = new OverflowException(message, inner);
            }
            else
            {
                e = inner;
            }
            return e;
        }

        //
        // : IDataParameterCollection
        //
        static internal Exception ParametersMappingIndex(int index, DbParameterCollection collection)
        {
            return CollectionIndexInt32(index, collection.GetType(), collection.Count);
        }
        static internal Exception ParametersSourceIndex(string parameterName, DbParameterCollection collection, Type parameterType)
        {
            return CollectionIndexString(parameterType, ADP.ParameterName, parameterName, collection.GetType());
        }
        static internal Exception ParameterNull(string parameter, DbParameterCollection collection, Type parameterType)
        {
            return CollectionNullValue(parameter, collection.GetType(), parameterType);
        }
        static internal Exception InvalidParameterType(DbParameterCollection collection, Type parameterType, object invalidValue)
        {
            return CollectionInvalidType(collection.GetType(), parameterType, invalidValue);
        }

        //
        // : IDbTransaction
        //
        static internal Exception ParallelTransactionsNotSupported(DbConnection obj)
        {
            return InvalidOperation(Res.GetString(Res.ADP_ParallelTransactionsNotSupported, obj.GetType().Name));
        }
        static internal Exception TransactionZombied(DbTransaction obj)
        {
            return InvalidOperation(Res.GetString(Res.ADP_TransactionZombied, obj.GetType().Name));
        }


        // global constant strings
        internal const string Parameter = "Parameter";
        internal const string ParameterName = "ParameterName";
        internal const string ParameterSetPosition = "set_Position";

        internal const CompareOptions compareOptions = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
        internal const int DecimalMaxPrecision = 29;
        internal const int DecimalMaxPrecision28 = 28;  // there are some cases in Odbc where we need that ...
        internal const int DefaultCommandTimeout = 30;
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;
        internal const float FailoverTimeoutStep = 0.08F;    // fraction of timeout to use for fast failover connections

        // security issue, don't rely upon static public readonly values - AS/URT 109635
        static internal readonly String StrEmpty = ""; // String.Empty

        internal const int CharSize = sizeof(char);


        static internal void TimerCurrent(out long ticks)
        {
            ticks = DateTime.UtcNow.ToFileTimeUtc();
        }

        static internal long TimerCurrent()
        {
            return DateTime.UtcNow.ToFileTimeUtc();
        }

        static internal long TimerFromSeconds(int seconds)
        {
            long result = checked((long)seconds * TimeSpan.TicksPerSecond);
            return result;
        }

        static internal long TimerFromMilliseconds(long milliseconds)
        {
            long result = checked(milliseconds * TimeSpan.TicksPerMillisecond);
            return result;
        }

        static internal bool TimerHasExpired(long timerExpire)
        {
            bool result = TimerCurrent() > timerExpire;
            return result;
        }

        static internal long TimerRemaining(long timerExpire)
        {
            long timerNow = TimerCurrent();
            long result = checked(timerExpire - timerNow);
            return result;
        }

        static internal long TimerRemainingMilliseconds(long timerExpire)
        {
            long result = TimerToMilliseconds(TimerRemaining(timerExpire));
            return result;
        }

        static internal long TimerRemainingSeconds(long timerExpire)
        {
            long result = TimerToSeconds(TimerRemaining(timerExpire));
            return result;
        }

        static internal long TimerToMilliseconds(long timerValue)
        {
            long result = timerValue / TimeSpan.TicksPerMillisecond;
            return result;
        }

        static private long TimerToSeconds(long timerValue)
        {
            long result = timerValue / TimeSpan.TicksPerSecond;
            return result;
        }

        static internal string MachineName() 
        {
            return Environment.MachineName;
        }

        static internal string BuildQuotedString(string quotePrefix, string quoteSuffix, string unQuotedString)
        {
            StringBuilder resultString = new StringBuilder();
            if (string.IsNullOrEmpty(quotePrefix) == false)
            {
                resultString.Append(quotePrefix);
            }

            // Assuming that the suffix is escaped by doubling it. i.e. foo"bar becomes "foo""bar".
            if (string.IsNullOrEmpty(quoteSuffix) == false)
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


        // { "a", "a", "a" } -> { "a", "a1", "a2" }
        // { "a", "a", "a1" } -> { "a", "a2", "a1" }
        // { "a", "A", "a" } -> { "a", "A1", "a2" }
        // { "a", "A", "a1" } -> { "a", "A2", "a1" }
        static internal void BuildSchemaTableInfoTableNames(string[] columnNameArray)
        {
            Dictionary<string, int> hash = new Dictionary<string, int>(columnNameArray.Length);

            int startIndex = columnNameArray.Length; // lowest non-unique index
            for (int i = columnNameArray.Length - 1; 0 <= i; --i)
            {
                string columnName = columnNameArray[i];
                if ((null != columnName) && (0 < columnName.Length))
                {
                    columnName = columnName.ToLowerInvariant();
                    int index;
                    if (hash.TryGetValue(columnName, out index))
                    {
                        startIndex = Math.Min(startIndex, index);
                    }
                    hash[columnName] = i;
                }
                else
                {
                    columnNameArray[i] = ADP.StrEmpty;
                    startIndex = i;
                }
            }
            int uniqueIndex = 1;
            for (int i = startIndex; i < columnNameArray.Length; ++i)
            {
                string columnName = columnNameArray[i];
                if (0 == columnName.Length)
                { // generate a unique name
                    columnNameArray[i] = "Column";
                    uniqueIndex = GenerateUniqueName(hash, ref columnNameArray[i], i, uniqueIndex);
                }
                else
                {
                    columnName = columnName.ToLowerInvariant();
                    if (i != hash[columnName])
                    {
                        GenerateUniqueName(hash, ref columnNameArray[i], i, 1);
                    }
                }
            }
        }

        static private int GenerateUniqueName(Dictionary<string, int> hash, ref string columnName, int index, int uniqueIndex)
        {
            for (; ; ++uniqueIndex)
            {
                string uniqueName = columnName + uniqueIndex.ToString(CultureInfo.InvariantCulture);
                string lowerName = uniqueName.ToLowerInvariant();
                if (!hash.ContainsKey(lowerName))
                {
                    columnName = uniqueName;
                    hash.Add(lowerName, index);
                    break;
                }
            }
            return uniqueIndex;
        }


        static internal int DstCompare(string strA, string strB)
        { // this is null safe
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, ADP.compareOptions);
        }

        static internal bool IsDirection(DbParameter value, ParameterDirection condition)
        {
#if DEBUG
            IsDirectionValid(condition);
#endif
            return (condition == (condition & value.Direction));
        }
#if DEBUG
        static private void IsDirectionValid(ParameterDirection value)
        {
            switch (value)
            { // @perfnote: Enum.IsDefined
                case ParameterDirection.Input:
                case ParameterDirection.Output:
                case ParameterDirection.InputOutput:
                case ParameterDirection.ReturnValue:
                    break;
                default:
                    throw ADP.InvalidParameterDirection(value);
            }
        }
#endif

        static internal bool IsNull(object value)
        {
            if ((null == value) || (DBNull.Value == value))
            {
                return true;
            }
            INullable nullable = (value as INullable);
            return ((null != nullable) && nullable.IsNull);
        }

        static internal void IsNullOrSqlType(object value, out bool isNull, out bool isSqlType)
        {
            if ((value == null) || (value == DBNull.Value))
            {
                isNull = true;
                isSqlType = false;
            }
            else
            {
                INullable nullable = (value as INullable);
                if (nullable != null)
                {
                    isNull = nullable.IsNull;
                    // Duplicated from DataStorage.cs
                    // For back-compat, SqlXml is not in this list
                    isSqlType = ((value is SqlBinary) ||
                                (value is SqlBoolean) ||
                                (value is SqlByte) ||
                                (value is SqlBytes) ||
                                (value is SqlChars) ||
                                (value is SqlDateTime) ||
                                (value is SqlDecimal) ||
                                (value is SqlDouble) ||
                                (value is SqlGuid) ||
                                (value is SqlInt16) ||
                                (value is SqlInt32) ||
                                (value is SqlInt64) ||
                                (value is SqlMoney) ||
                                (value is SqlSingle) ||
                                (value is SqlString));
                }
                else
                {
                    isNull = false;
                    isSqlType = false;
                }
            }
        }


        private static Version s_systemDataVersion;

        static internal Version GetAssemblyVersion()
        {
            // NOTE: Using lazy thread-safety since we don't care if two threads both happen to update the value at the same time
            if (s_systemDataVersion == null)
            {
                s_systemDataVersion = new Version(ThisAssembly.InformationalVersion);
            }

            return s_systemDataVersion;
        }


        static internal readonly string[] AzureSqlServerEndpoints = {Res.GetString(Res.AZURESQL_GenericEndpoint),
                                                                     Res.GetString(Res.AZURESQL_GermanEndpoint),
                                                                     Res.GetString(Res.AZURESQL_UsGovEndpoint),
                                                                     Res.GetString(Res.AZURESQL_ChinaEndpoint)};

        // This method assumes dataSource parameter is in TCP connection string format.
        static internal bool IsAzureSqlServerEndpoint(string dataSource)
        {
            // remove server port
            int i = dataSource.LastIndexOf(',');
            if (i >= 0)
            {
                dataSource = dataSource.Substring(0, i);
            }

            // check for the instance name
            i = dataSource.LastIndexOf('\\');
            if (i >= 0)
            {
                dataSource = dataSource.Substring(0, i);
            }

            // trim redundant whitespaces
            dataSource = dataSource.Trim();

            // check if servername end with any azure endpoints
            for (i = 0; i < AzureSqlServerEndpoints.Length; i++)
            {
                if (dataSource.EndsWith(AzureSqlServerEndpoints[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

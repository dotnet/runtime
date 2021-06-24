// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Win32;
using SysTx = System.Transactions;

namespace System.Data.Common
{
    internal static class ADP
    {
        internal static Exception ExceptionWithStackTrace(Exception e)
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
            TraceException("<comm.ADP.TraceException|ERR|THROW> '%ls'\n", e);
        }
        internal static void TraceExceptionForCapture(Exception e)
        {
            Debug.Assert(ADP.IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '%ls'\n", e);
        }
        internal static void TraceExceptionWithoutRethrow(Exception e)
        {
            Debug.Assert(ADP.IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '%ls'\n", e);
        }

        //
        // COM+ exceptions
        //
        internal static ArgumentException Argument(string error)
        {
            ArgumentException e = new ArgumentException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentException Argument(string error, Exception? inner)
        {
            ArgumentException e = new ArgumentException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentException Argument(string error, string? parameter)
        {
            ArgumentException e = new ArgumentException(error, parameter);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ArgumentException Argument(string error, string parameter, Exception inner)
        {
            ArgumentException e = new ArgumentException(error, parameter, inner);
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
        internal static ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName, message);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static ConfigurationException Configuration(string message)
        {
            ConfigurationException e = new ConfigurationErrorsException(message);
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
        internal static InvalidCastException InvalidCast(string error, Exception? inner)
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
        internal static TimeoutException TimeoutException(string error)
        {
            TimeoutException e = new TimeoutException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidOperationException InvalidOperation(string error, Exception? inner)
        {
            InvalidOperationException e = new InvalidOperationException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static NotSupportedException NotSupported()
        {
            NotSupportedException e = new NotSupportedException();
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidCastException InvalidCast()
        {
            InvalidCastException e = new InvalidCastException();
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidOperationException DataAdapter(string error)
        {
            return InvalidOperation(error);
        }
        internal static InvalidOperationException DataAdapter(string error, Exception? inner)
        {
            return InvalidOperation(error, inner);
        }
        private static InvalidOperationException Provider(string error)
        {
            return InvalidOperation(error);
        }

        internal static ArgumentException InvalidMultipartName(string property, string value)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidMultipartName, SR.GetString(property), value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException InvalidMultipartNameIncorrectUsageOfQuotes(string property, string value)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidMultipartNameQuoteUsage, SR.GetString(property), value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException InvalidMultipartNameToManyParts(string property, string value, int limit)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidMultipartNameToManyParts, SR.GetString(property), value, limit));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        //
        // Helper Functions
        //
        internal static void CheckArgumentLength(string value, string parameterName)
        {
            CheckArgumentNull(value, parameterName);
            if (0 == value.Length)
            {
                throw Argument(SR.GetString(SR.ADP_EmptyString, parameterName));
            }
        }

        internal static void CheckArgumentNull(object value, string parameterName)
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
        }

        // only StackOverflowException & ThreadAbortException are sealed classes
        private static readonly Type StackOverflowType = typeof(StackOverflowException);
        private static readonly Type OutOfMemoryType = typeof(OutOfMemoryException);
        private static readonly Type ThreadAbortType = typeof(ThreadAbortException);
        private static readonly Type NullReferenceType = typeof(NullReferenceException);
        private static readonly Type AccessViolationType = typeof(AccessViolationException);
        private static readonly Type SecurityType = typeof(SecurityException);

        internal static bool IsCatchableExceptionType(Exception e)
        {
            // a 'catchable' exception is defined by what it is not.
            Debug.Assert(e != null, "Unexpected null exception!");
            Type type = e.GetType();

            return ((type != StackOverflowType) &&
                     (type != OutOfMemoryType) &&
                     (type != ThreadAbortType) &&
                     (type != NullReferenceType) &&
                     (type != AccessViolationType) &&
                     !SecurityType.IsAssignableFrom(type));
        }

        internal static bool IsCatchableOrSecurityExceptionType(Exception e)
        {
            // a 'catchable' exception is defined by what it is not.
            // since IsCatchableExceptionType defined SecurityException as not 'catchable'
            // this method will return true for SecurityException has being catchable.

            // the other way to write this method is, but then SecurityException is checked twice
            // return ((e is SecurityException) || IsCatchableExceptionType(e));

            Debug.Assert(e != null, "Unexpected null exception!");
            Type type = e.GetType();

            return ((type != StackOverflowType) &&
                     (type != OutOfMemoryType) &&
                     (type != ThreadAbortType) &&
                     (type != NullReferenceType) &&
                     (type != AccessViolationType));
        }

        // Invalid Enumeration

        internal static ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
        {
            return ADP.ArgumentOutOfRange(SR.GetString(SR.ADP_InvalidEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }

        // IDbCommand.CommandType
        internal static ArgumentOutOfRangeException InvalidCommandType(CommandType value)
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

        // IDataParameter.SourceVersion
        internal static ArgumentOutOfRangeException InvalidDataRowVersion(DataRowVersion value)
        {
#if DEBUG
            switch (value)
            {
                case DataRowVersion.Default:
                case DataRowVersion.Current:
                case DataRowVersion.Original:
                case DataRowVersion.Proposed:
                    Debug.Assert(false, "valid DataRowVersion " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(DataRowVersion), (int)value);
        }

        // IDbConnection.BeginTransaction, OleDbTransaction.Begin
        internal static ArgumentOutOfRangeException InvalidIsolationLevel(IsolationLevel value)
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
                    Debug.Assert(false, "valid IsolationLevel " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(IsolationLevel), (int)value);
        }

        // IDataParameter.Direction
        internal static ArgumentOutOfRangeException InvalidParameterDirection(ParameterDirection value)
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
        internal static ArgumentOutOfRangeException InvalidUpdateRowSource(UpdateRowSource value)
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
        internal static ArgumentException ConnectionStringSyntax(int index)
        {
            return Argument(SR.GetString(SR.ADP_ConnectionStringSyntax, index));
        }
        internal static ArgumentException KeywordNotSupported(string keyword)
        {
            return Argument(SR.GetString(SR.ADP_KeywordNotSupported, keyword));
        }
        internal static ArgumentException UdlFileError(Exception inner)
        {
            return Argument(SR.GetString(SR.ADP_UdlFileError), inner);
        }
        internal static ArgumentException InvalidUDL()
        {
            return Argument(SR.GetString(SR.ADP_InvalidUDL));
        }
        internal static InvalidOperationException InvalidDataDirectory()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_InvalidDataDirectory));
        }
        internal static ArgumentException InvalidKeyname(string? parameterName)
        {
            return Argument(SR.GetString(SR.ADP_InvalidKey), parameterName);
        }
        internal static ArgumentException InvalidValue(string parameterName)
        {
            return Argument(SR.GetString(SR.ADP_InvalidValue), parameterName);
        }
        internal static ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(SR.GetString(SR.SqlConvert_ConvertFailed, fromType.FullName!, toType.FullName!), innerException);
        }

        //
        // DbConnection
        //
        internal static InvalidOperationException NoConnectionString()
        {
            return InvalidOperation(SR.GetString(SR.ADP_NoConnectionString));
        }

        private static string ConnectionStateMsg(ConnectionState state)
        {
            switch (state)
            {
                case (ConnectionState.Closed):
                case (ConnectionState.Connecting | ConnectionState.Broken): // treated the same as closed
                    return SR.GetString(SR.ADP_ConnectionStateMsg_Closed);
                case (ConnectionState.Connecting):
                    return SR.GetString(SR.ADP_ConnectionStateMsg_Connecting);
                case (ConnectionState.Open):
                    return SR.GetString(SR.ADP_ConnectionStateMsg_Open);
                case (ConnectionState.Open | ConnectionState.Executing):
                    return SR.GetString(SR.ADP_ConnectionStateMsg_OpenExecuting);
                case (ConnectionState.Open | ConnectionState.Fetching):
                    return SR.GetString(SR.ADP_ConnectionStateMsg_OpenFetching);
                default:
                    return SR.GetString(SR.ADP_ConnectionStateMsg, state.ToString());
            }
        }

        internal static ConfigurationException ConfigUnableToLoadXmlMetaDataFile(string settingName)
        {
            return Configuration(SR.GetString(SR.OleDb_ConfigUnableToLoadXmlMetaDataFile, settingName));
        }

        internal static ConfigurationException ConfigWrongNumberOfValues(string settingName)
        {
            return Configuration(SR.GetString(SR.OleDb_ConfigWrongNumberOfValues, settingName));
        }

        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //
        internal static Exception InvalidConnectionOptionValue(string key)
        {
            return InvalidConnectionOptionValue(key, null);
        }
        internal static Exception InvalidConnectionOptionValue(string key, Exception? inner)
        {
            return Argument(SR.GetString(SR.ADP_InvalidConnectionOptionValue, key), inner);
        }

        //
        // DbConnectionPool and related
        //
        internal static Exception PooledOpenTimeout()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_PooledOpenTimeout));
        }

        internal static Exception NonPooledOpenTimeout()
        {
            return ADP.TimeoutException(SR.GetString(SR.ADP_NonPooledOpenTimeout));
        }

        //
        // Generic Data Provider Collection
        //
        internal static ArgumentException CollectionRemoveInvalidObject(Type itemType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionRemoveInvalidObject, itemType.Name, collection.GetType().Name));
        }
        internal static ArgumentNullException CollectionNullValue(string parameter, Type collection, Type itemType)
        {
            return ArgumentNull(parameter, SR.GetString(SR.ADP_CollectionNullValue, collection.Name, itemType.Name));
        }
        internal static IndexOutOfRangeException CollectionIndexInt32(int index, Type collection, int count)
        {
            return IndexOutOfRange(SR.GetString(SR.ADP_CollectionIndexInt32, index.ToString(CultureInfo.InvariantCulture), collection.Name, count.ToString(CultureInfo.InvariantCulture)));
        }
        internal static IndexOutOfRangeException CollectionIndexString(Type itemType, string propertyName, string propertyValue, Type collection)
        {
            return IndexOutOfRange(SR.GetString(SR.ADP_CollectionIndexString, itemType.Name, propertyName, propertyValue, collection.Name));
        }
        internal static InvalidCastException CollectionInvalidType(Type collection, Type itemType, object invalidValue)
        {
            return InvalidCast(SR.GetString(SR.ADP_CollectionInvalidType, collection.Name, itemType.Name, invalidValue.GetType().Name));
        }
        internal static ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }
        internal static ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }

        //
        // DbProviderException
        //
        internal static InvalidOperationException TransactionConnectionMismatch()
        {
            return Provider(SR.GetString(SR.ADP_TransactionConnectionMismatch));
        }
        internal static InvalidOperationException TransactionRequired(string method)
        {
            return Provider(SR.GetString(SR.ADP_TransactionRequired, method));
        }

        internal static Exception CommandTextRequired(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_CommandTextRequired, method));
        }

        internal static InvalidOperationException ConnectionRequired(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ConnectionRequired, method));
        }
        internal static InvalidOperationException OpenConnectionRequired(string method, ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenConnectionRequired, method, ADP.ConnectionStateMsg(state)));
        }

        internal static Exception NoStoredProcedureExists(string sproc)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NoStoredProcedureExists, sproc));
        }
        internal static Exception OpenReaderExists()
        {
            return OpenReaderExists(null);
        }

        internal static Exception OpenReaderExists(Exception? e)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenReaderExists), e);
        }

        internal static Exception TransactionCompleted()
        {
            return DataAdapter(SR.GetString(SR.ADP_TransactionCompleted));
        }

        //
        // DbDataReader
        //
        internal static Exception NonSeqByteAccess(long badIndex, long currIndex, string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NonSeqByteAccess, badIndex.ToString(CultureInfo.InvariantCulture), currIndex.ToString(CultureInfo.InvariantCulture), method));
        }

        internal static Exception NumericToDecimalOverflow()
        {
            return InvalidCast(SR.GetString(SR.ADP_NumericToDecimalOverflow));
        }
        internal static InvalidOperationException NonSequentialColumnAccess(int badCol, int currCol)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NonSequentialColumnAccess, badCol.ToString(CultureInfo.InvariantCulture), currCol.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception FillRequiresSourceTableName(string parameter)
        {
            return Argument(SR.GetString(SR.ADP_FillRequiresSourceTableName), parameter);
        }

        //
        // : IDbCommand
        //
        internal static Exception InvalidCommandTimeout(int value)
        {
            return Argument(SR.GetString(SR.ADP_InvalidCommandTimeout, value.ToString(CultureInfo.InvariantCulture)), ADP.CommandTimeout);
        }
        internal static Exception DeriveParametersNotSupported(IDbCommand value)
        {
            return DataAdapter(SR.GetString(SR.ADP_DeriveParametersNotSupported, value.GetType().Name, value.CommandType.ToString()));
        }
        internal static Exception UninitializedParameterSize(int index, Type dataType)
        {
            return InvalidOperation(SR.GetString(SR.ADP_UninitializedParameterSize, index.ToString(CultureInfo.InvariantCulture), dataType.Name));
        }
        internal static Exception PrepareParameterType(IDbCommand cmd)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterType, cmd.GetType().Name));
        }
        internal static Exception PrepareParameterSize(IDbCommand cmd)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterSize, cmd.GetType().Name));
        }
        internal static Exception PrepareParameterScale(IDbCommand cmd, string type)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterScale, cmd.GetType().Name, type));
        }

        //
        // : ConnectionUtil
        //

        internal static Exception ClosedConnectionError()
        {
            return InvalidOperation(SR.GetString(SR.ADP_ClosedConnectionError));
        }
        internal static Exception ConnectionAlreadyOpen(ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ConnectionAlreadyOpen, ADP.ConnectionStateMsg(state)));
        }
        internal static Exception TransactionPresent()
        {
            return InvalidOperation(SR.GetString(SR.ADP_TransactionPresent));
        }
        internal static Exception LocalTransactionPresent()
        {
            return InvalidOperation(SR.GetString(SR.ADP_LocalTransactionPresent));
        }
        internal static Exception OpenConnectionPropertySet(string property, ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenConnectionPropertySet, property, ADP.ConnectionStateMsg(state)));
        }
        internal static Exception EmptyDatabaseName()
        {
            return Argument(SR.GetString(SR.ADP_EmptyDatabaseName));
        }

        internal enum ConnectionError
        {
            BeginGetConnectionReturnsNull,
            GetConnectionReturnsNull,
            ConnectionOptionsMissing,
            CouldNotSwitchToClosedPreviouslyOpenedState,
        }
        internal static Exception InternalConnectionError(ConnectionError internalError)
        {
            return InvalidOperation(SR.GetString(SR.ADP_InternalConnectionError, (int)internalError));
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
        internal static Exception InternalError(InternalErrorCode internalError)
        {
            return InvalidOperation(SR.GetString(SR.ADP_InternalProviderError, (int)internalError));
        }
        internal static Exception InternalError(InternalErrorCode internalError, Exception innerException)
        {
            return InvalidOperation(SR.GetString(SR.ADP_InternalProviderError, (int)internalError), innerException);
        }
        internal static Exception InvalidConnectTimeoutValue()
        {
            return Argument(SR.GetString(SR.ADP_InvalidConnectTimeoutValue));
        }

        //
        // : DbDataReader
        //
        internal static Exception DataReaderNoData()
        {
            return InvalidOperation(SR.GetString(SR.ADP_DataReaderNoData));
        }
        internal static Exception DataReaderClosed(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_DataReaderClosed, method));
        }
        internal static ArgumentOutOfRangeException InvalidSourceBufferIndex(int maxLen, long srcOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.GetString(SR.ADP_InvalidSourceBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), srcOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        internal static ArgumentOutOfRangeException InvalidDestinationBufferIndex(int maxLen, int dstOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.GetString(SR.ADP_InvalidDestinationBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), dstOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        internal static Exception InvalidDataLength(long length)
        {
            return IndexOutOfRange(SR.GetString(SR.SQL_InvalidDataLength, length.ToString(CultureInfo.InvariantCulture)));
        }

        //
        // : IDataParameter
        //
        internal static ArgumentException InvalidDataType(TypeCode typecode)
        {
            return Argument(SR.GetString(SR.ADP_InvalidDataType, typecode.ToString()));
        }
        internal static ArgumentException DbTypeNotSupported(System.Data.DbType type, Type enumtype)
        {
            return Argument(SR.GetString(SR.ADP_DbTypeNotSupported, type.ToString(), enumtype.Name));
        }
        internal static ArgumentException UnknownDataTypeCode(Type dataType, TypeCode typeCode)
        {
            return Argument(SR.GetString(SR.ADP_UnknownDataTypeCode, ((int)typeCode).ToString(CultureInfo.InvariantCulture), dataType.FullName!));
        }
        internal static ArgumentException InvalidOffsetValue(int value)
        {
            return Argument(SR.GetString(SR.ADP_InvalidOffsetValue, value.ToString(CultureInfo.InvariantCulture)));
        }
        internal static ArgumentException InvalidSizeValue(int value)
        {
            return Argument(SR.GetString(SR.ADP_InvalidSizeValue, value.ToString(CultureInfo.InvariantCulture)));
        }
        internal static Exception ParameterConversionFailed(object value, Type destType, Exception inner)
        {
            Debug.Assert(null != value, "null value on conversion failure");
            Debug.Assert(null != inner, "null inner on conversion failure");

            Exception e;
            string message = SR.GetString(SR.ADP_ParameterConversionFailed, value.GetType().Name, destType.Name);
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
            TraceExceptionAsReturnValue(e);
            return e;
        }

        //
        // : IDataParameterCollection
        //
        internal static Exception ParametersMappingIndex(int index, IDataParameterCollection collection)
        {
            return CollectionIndexInt32(index, collection.GetType(), collection.Count);
        }
        internal static Exception ParametersSourceIndex(string parameterName, IDataParameterCollection collection, Type parameterType)
        {
            return CollectionIndexString(parameterType, ADP.ParameterName, parameterName, collection.GetType());
        }
        internal static Exception ParameterNull(string parameter, IDataParameterCollection collection, Type parameterType)
        {
            return CollectionNullValue(parameter, collection.GetType(), parameterType);
        }
        internal static Exception InvalidParameterType(IDataParameterCollection collection, Type parameterType, object invalidValue)
        {
            return CollectionInvalidType(collection.GetType(), parameterType, invalidValue);
        }

        //
        // : IDbTransaction
        //
        internal static Exception ParallelTransactionsNotSupported(IDbConnection obj)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ParallelTransactionsNotSupported, obj.GetType().Name));
        }
        internal static Exception TransactionZombied(IDbTransaction obj)
        {
            return InvalidOperation(SR.GetString(SR.ADP_TransactionZombied, obj.GetType().Name));
        }

        //
        // : DbMetaDataFactory
        //

        internal static Exception AmbigousCollectionName(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_AmbigousCollectionName, collectionName));
        }

        internal static Exception CollectionNameIsNotUnique(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_CollectionNameISNotUnique, collectionName));
        }

        internal static Exception DataTableDoesNotExist(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_DataTableDoesNotExist, collectionName));
        }

        internal static Exception IncorrectNumberOfDataSourceInformationRows()
        {
            return Argument(SR.GetString(SR.MDF_IncorrectNumberOfDataSourceInformationRows));
        }

        internal static ArgumentException InvalidRestrictionValue(string collectionName, string restrictionName, string restrictionValue)
        {
            return ADP.Argument(SR.GetString(SR.MDF_InvalidRestrictionValue, collectionName, restrictionName, restrictionValue));
        }

        internal static Exception InvalidXml()
        {
            return Argument(SR.GetString(SR.MDF_InvalidXml));
        }

        internal static Exception InvalidXmlMissingColumn(string collectionName, string columnName)
        {
            return Argument(SR.GetString(SR.MDF_InvalidXmlMissingColumn, collectionName, columnName));
        }

        internal static Exception InvalidXmlInvalidValue(string collectionName, string columnName)
        {
            return Argument(SR.GetString(SR.MDF_InvalidXmlInvalidValue, collectionName, columnName));
        }

        internal static Exception MissingDataSourceInformationColumn()
        {
            return Argument(SR.GetString(SR.MDF_MissingDataSourceInformationColumn));
        }

        internal static Exception MissingRestrictionColumn()
        {
            return Argument(SR.GetString(SR.MDF_MissingRestrictionColumn));
        }

        internal static Exception MissingRestrictionRow()
        {
            return Argument(SR.GetString(SR.MDF_MissingRestrictionRow));
        }

        internal static Exception NoColumns()
        {
            return Argument(SR.GetString(SR.MDF_NoColumns));
        }

        internal static Exception QueryFailed(string collectionName, Exception e)
        {
            return InvalidOperation(SR.GetString(SR.MDF_QueryFailed, collectionName), e);
        }

        internal static Exception TooManyRestrictions(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_TooManyRestrictions, collectionName));
        }

        internal static Exception UnableToBuildCollection(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UnableToBuildCollection, collectionName));
        }

        internal static Exception UndefinedCollection(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UndefinedCollection, collectionName));
        }

        internal static Exception UndefinedPopulationMechanism(string populationMechanism)
        {
            return Argument(SR.GetString(SR.MDF_UndefinedPopulationMechanism, populationMechanism));
        }

        internal static Exception UnsupportedVersion(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UnsupportedVersion, collectionName));
        }

        //
        // : CommandBuilder
        //
        internal static InvalidOperationException QuotePrefixNotSet(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_QuotePrefixNotSet, method));
        }

        // global constant strings
        internal const string BeginTransaction = nameof(BeginTransaction);
        internal const string ChangeDatabase = nameof(ChangeDatabase);
        internal const string CommandTimeout = nameof(CommandTimeout);
        internal const string ConnectionString = nameof(ConnectionString);
        internal const string DeriveParameters = nameof(DeriveParameters);
        internal const string ExecuteReader = nameof(ExecuteReader);
        internal const string ExecuteNonQuery = nameof(ExecuteNonQuery);
        internal const string ExecuteScalar = nameof(ExecuteScalar);
        internal const string GetBytes = nameof(GetBytes);
        internal const string GetChars = nameof(GetChars);
        internal const string GetOleDbSchemaTable = nameof(GetOleDbSchemaTable);
        internal const string GetSchema = nameof(GetSchema);
        internal const string GetSchemaTable = nameof(GetSchemaTable);
        internal const string Parameter = nameof(Parameter);
        internal const string ParameterName = nameof(ParameterName);
        internal const string Prepare = nameof(Prepare);
        internal const string QuoteIdentifier = nameof(QuoteIdentifier);
        internal const string SetProperties = nameof(SetProperties);
        internal const string UnquoteIdentifier = nameof(UnquoteIdentifier);

        internal const CompareOptions compareOptions = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
        internal const int DefaultCommandTimeout = 30;
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;

        internal static readonly IntPtr PtrZero = new IntPtr(0); // IntPtr.Zero
        internal static readonly int PtrSize = IntPtr.Size;
        internal static readonly IntPtr RecordsUnaffected = new IntPtr(-1);

        internal const int CharSize = System.Text.UnicodeEncoding.CharSize;

        internal static bool CompareInsensitiveInvariant(string strvalue, string strconst)
        {
            return (0 == CultureInfo.InvariantCulture.CompareInfo.Compare(strvalue, strconst, CompareOptions.IgnoreCase));
        }

        internal static Delegate? FindBuilder(MulticastDelegate? mcd)
        { // V1.2.3300
            if (null != mcd)
            {
                Delegate[] d = mcd.GetInvocationList();
                for (int i = 0; i < d.Length; i++)
                {
                    if (d[i].Target is DbCommandBuilder)
                        return d[i];
                }
            }

            return null;
        }

        internal static readonly bool IsWindowsNT = (PlatformID.Win32NT == Environment.OSVersion.Platform);
        internal static readonly bool IsPlatformNT5 = (ADP.IsWindowsNT && (Environment.OSVersion.Version.Major >= 5));

        internal static SysTx.Transaction? GetCurrentTransaction()
        {
            SysTx.Transaction? transaction = SysTx.Transaction.Current;
            return transaction;
        }

        internal static void SetCurrentTransaction(SysTx.Transaction? transaction)
        {
            SysTx.Transaction.Current = transaction;
        }

        internal static SysTx.IDtcTransaction? GetOletxTransaction(SysTx.Transaction? transaction)
        {
            SysTx.IDtcTransaction? oleTxTransaction = null;

            if (null != transaction)
            {
                oleTxTransaction = SysTx.TransactionInterop.GetDtcTransaction(transaction);
            }
            return oleTxTransaction;
        }

        internal static bool NeedManualEnlistment()
        {
            return false;
        }

        internal static long TimerCurrent()
        {
            return DateTime.UtcNow.ToFileTimeUtc();
        }

        internal static long TimerFromSeconds(int seconds)
        {
            long result = checked((long)seconds * TimeSpan.TicksPerSecond);
            return result;
        }

        internal static long TimerRemaining(long timerExpire)
        {
            long timerNow = TimerCurrent();
            long result = checked(timerExpire - timerNow);
            return result;
        }

        internal static long TimerRemainingMilliseconds(long timerExpire)
        {
            long result = TimerToMilliseconds(TimerRemaining(timerExpire));
            return result;
        }

        internal static long TimerToMilliseconds(long timerValue)
        {
            long result = timerValue / TimeSpan.TicksPerMillisecond;
            return result;
        }

        internal static string BuildQuotedString(string quotePrefix, string quoteSuffix, string unQuotedString)
        {
            StringBuilder resultString = new StringBuilder();
            if (ADP.IsEmpty(quotePrefix) == false)
            {
                resultString.Append(quotePrefix);
            }

            // Assuming that the suffix is escaped by doubling it. i.e. foo"bar becomes "foo""bar".
            if (ADP.IsEmpty(quoteSuffix) == false)
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

        internal static void EscapeSpecialCharacters(string unescapedString, StringBuilder escapedString)
        {
            // note special characters list is from character escapes
            // in the MSDN regular expression language elements documentation
            // added ] since escaping it seems necessary
            const string specialCharacters = ".$^{[(|)*+?\\]";

            foreach (char currentChar in unescapedString)
            {
                if (specialCharacters.IndexOf(currentChar) >= 0)
                {
                    escapedString.Append('\\');
                }
                escapedString.Append(currentChar);
            }
            return;
        }

        internal static string GetFullPath(string filename)
        {
            return Path.GetFullPath(filename);
        }

        // SxS: the file is opened in FileShare.Read mode allowing several threads/apps to read it simultaneously
        internal static Stream GetFileStream(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        internal static FileVersionInfo GetVersionInfo(string filename)
        {
            return FileVersionInfo.GetVersionInfo(filename);
        }

        internal static Stream GetXmlStreamFromValues(string[] values, string errorString)
        {
            if (values.Length != 1)
            {
                throw ADP.ConfigWrongNumberOfValues(errorString);
            }
            return ADP.GetXmlStream(values[0], errorString);
        }

        // metadata files are opened from <.NetRuntimeFolder>\CONFIG\<metadatafilename.xml>
        // this operation is safe in SxS because the file is opened in read-only mode and each NDP runtime accesses its own copy of the metadata
        // under the runtime folder.
        internal static Stream GetXmlStream(string value, string errorString)
        {
            Stream XmlStream;
            const string config = "config\\";
            // get location of config directory
            string rootPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            if (rootPath == null)
            {
                throw ADP.ConfigUnableToLoadXmlMetaDataFile(errorString);
            }
            StringBuilder tempstring = new StringBuilder(rootPath.Length + config.Length + value.Length);
            tempstring.Append(rootPath);
            tempstring.Append(config);
            tempstring.Append(value);
            string fullPath = tempstring.ToString();

            // don't allow relative paths
            if (ADP.GetFullPath(fullPath) != fullPath)
            {
                throw ADP.ConfigUnableToLoadXmlMetaDataFile(errorString);
            }

            try
            {
                XmlStream = ADP.GetFileStream(fullPath);
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
                throw ADP.ConfigUnableToLoadXmlMetaDataFile(errorString);
            }

            return XmlStream;

        }

        internal static object? ClassesRootRegistryValue(string subkey, string queryvalue)
        {
            try
            {
                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(subkey, false))
                {
                    return ((null != key) ? key.GetValue(queryvalue) : null);
                }
            }
            catch (SecurityException e)
            {
                // it's possible there are
                // ACL's on registry that cause SecurityException to be thrown.
                ADP.TraceExceptionWithoutRethrow(e);
                return null;
            }
        }

        internal static object? LocalMachineRegistryValue(string subkey, string queryvalue)
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(subkey, false))
                {
                    return ((null != key) ? key.GetValue(queryvalue) : null);
                }
            }
            catch (SecurityException e)
            {
                // it's possible there are
                // ACL's on registry that cause SecurityException to be thrown.
                ADP.TraceExceptionWithoutRethrow(e);
                return null;
            }
        }

        // SxS: although this method uses registry, it does not expose anything out
        internal static void CheckVersionMDAC(bool ifodbcelseoledb)
        {
            int major, minor, build;
            string? version;

            try
            {
                version = (string?)ADP.LocalMachineRegistryValue("Software\\Microsoft\\DataAccess", "FullInstallVer");
                if (ADP.IsEmpty(version))
                {
                    string filename = (string)ADP.ClassesRootRegistryValue(System.Data.OleDb.ODB.DataLinks_CLSID, string.Empty)!;
                    FileVersionInfo versionInfo = ADP.GetVersionInfo(filename);
                    major = versionInfo.FileMajorPart;
                    minor = versionInfo.FileMinorPart;
                    build = versionInfo.FileBuildPart;
                    version = versionInfo.FileVersion;
                }
                else
                {
                    string[] parts = version.Split('.');
                    major = int.Parse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture);
                    minor = int.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture);
                    build = int.Parse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture);
                    int.Parse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception e)
            {
                // UNDONE - should not be catching all exceptions!!!
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }

                throw System.Data.OleDb.ODB.MDACNotAvailable(e);
            }

            // disallow any MDAC version before MDAC 2.6 rtm
            // include MDAC 2.51 that ships with Win2k
            if ((major < 2) || ((major == 2) && ((minor < 60) || ((minor == 60) && (build < 6526)))))
            {
                if (ifodbcelseoledb)
                {
                    throw ADP.DataAdapter(SR.GetString(SR.Odbc_MDACWrongVersion, version));
                }
                else
                {
                    throw ADP.DataAdapter(SR.GetString(SR.OleDb_MDACWrongVersion, version));
                }
            }
        }

        // the return value is true if the string was quoted and false if it was not
        // this allows the caller to determine if it is an error or not for the quotedString to not be quoted
        internal static bool RemoveStringQuotes(string? quotePrefix, string? quoteSuffix, string? quotedString, out string? unquotedString)
        {
            int prefixLength;
            if (quotePrefix == null)
            {
                prefixLength = 0;
            }
            else
            {
                prefixLength = quotePrefix.Length;
            }

            int suffixLength;
            if (quoteSuffix == null)
            {
                suffixLength = 0;
            }
            else
            {
                suffixLength = quoteSuffix.Length;
            }

            if ((suffixLength + prefixLength) == 0)
            {
                unquotedString = quotedString;
                return true;
            }

            if (quotedString == null)
            {
                unquotedString = quotedString;
                return false;
            }

            int quotedStringLength = quotedString.Length;

            // is the source string too short to be quoted
            if (quotedStringLength < prefixLength + suffixLength)
            {
                unquotedString = quotedString;
                return false;
            }

            // is the prefix present?
            if (prefixLength > 0)
            {
                if (quotedString.StartsWith(quotePrefix!, StringComparison.Ordinal) == false)
                {
                    unquotedString = quotedString;
                    return false;
                }
            }

            // is the suffix present?
            if (suffixLength > 0)
            {
                if (quotedString.EndsWith(quoteSuffix!, StringComparison.Ordinal) == false)
                {
                    unquotedString = quotedString;
                    return false;
                }
                unquotedString = quotedString.Substring(prefixLength, quotedStringLength - (prefixLength + suffixLength)).Replace(quoteSuffix + quoteSuffix, quoteSuffix);
            }
            else
            {
                unquotedString = quotedString.Substring(prefixLength, quotedStringLength - prefixLength);
            }
            return true;
        }

        internal static IntPtr IntPtrOffset(IntPtr pbase, int offset)
        {
            if (4 == ADP.PtrSize)
            {
                return (IntPtr)checked(pbase.ToInt32() + offset);
            }
            Debug.Assert(8 == ADP.PtrSize, "8 != IntPtr.Size");
            return (IntPtr)checked(pbase.ToInt64() + offset);
        }

        internal static int IntPtrToInt32(IntPtr value)
        {
            if (4 == ADP.PtrSize)
            {
                return (int)value;
            }
            else
            {
                long lval = (long)value;
                lval = Math.Min((long)int.MaxValue, lval);
                lval = Math.Max((long)int.MinValue, lval);
                return (int)lval;
            }
        }

        // TODO: are those names appropriate for common code?
        internal static int SrcCompare(string? strA, string? strB)
        { // this is null safe
            return ((strA == strB) ? 0 : 1);
        }

        internal static int DstCompare(string? strA, string? strB)
        { // this is null safe
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, ADP.compareOptions);
        }

        internal static bool IsDirection(IDataParameter value, ParameterDirection condition)
        {
#if DEBUG
            IsDirectionValid(condition);
#endif
            return (condition == (condition & value.Direction));
        }
#if DEBUG
        private static void IsDirectionValid(ParameterDirection value)
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

        internal static bool IsEmpty([NotNullWhen(false)] string? str)
        {
            return ((null == str) || (0 == str.Length));
        }

        internal static bool IsEmptyArray([NotNullWhen(false)] string?[]? array)
        {
            return ((null == array) || (0 == array.Length));
        }

        internal static bool IsNull([NotNullWhen(false)] object? value)
        {
            if ((null == value) || (DBNull.Value == value))
            {
                return true;
            }
            INullable? nullable = (value as INullable);
            return ((null != nullable) && nullable.IsNull);
        }
    }
}

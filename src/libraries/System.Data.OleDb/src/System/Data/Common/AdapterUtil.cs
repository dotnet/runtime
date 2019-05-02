// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlTypes;
using System.Diagnostics;
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

        static void TraceException(string trace, Exception e)
        {
            Debug.Assert(e != null, "TraceException: null Exception");
            if (e != null)
            {
                DataCommonEventSource.Log.Trace(trace, e);
            }
        }

        static internal void TraceExceptionAsReturnValue(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|THROW> '%ls'\n", e);
        }
        static internal void TraceExceptionForCapture(Exception e)
        {
            Debug.Assert(ADP.IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '%ls'\n", e);
        }
        static internal void TraceExceptionWithoutRethrow(Exception e)
        {
            Debug.Assert(ADP.IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '%ls'\n", e);
        }

        //
        // COM+ exceptions
        //
        static internal ArgumentException Argument(string error)
        {
            ArgumentException e = new ArgumentException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ArgumentException Argument(string error, Exception inner)
        {
            ArgumentException e = new ArgumentException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ArgumentException Argument(string error, string parameter)
        {
            ArgumentException e = new ArgumentException(error, parameter);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ArgumentException Argument(string error, string parameter, Exception inner)
        {
            ArgumentException e = new ArgumentException(error, parameter, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ArgumentNullException ArgumentNull(string parameter)
        {
            ArgumentNullException e = new ArgumentNullException(parameter);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ArgumentNullException ArgumentNull(string parameter, string error)
        {
            ArgumentNullException e = new ArgumentNullException(parameter, error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName, message);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal ConfigurationException Configuration(string message)
        {
            ConfigurationException e = new ConfigurationErrorsException(message);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal IndexOutOfRangeException IndexOutOfRange(string error)
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal InvalidCastException InvalidCast(string error)
        {
            return InvalidCast(error, null);
        }
        static internal InvalidCastException InvalidCast(string error, Exception inner)
        {
            InvalidCastException e = new InvalidCastException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal InvalidOperationException InvalidOperation(string error)
        {
            InvalidOperationException e = new InvalidOperationException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal TimeoutException TimeoutException(string error)
        {
            TimeoutException e = new TimeoutException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal InvalidOperationException InvalidOperation(string error, Exception inner)
        {
            InvalidOperationException e = new InvalidOperationException(error, inner);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal NotSupportedException NotSupported()
        {
            NotSupportedException e = new NotSupportedException();
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal InvalidCastException InvalidCast()
        {
            InvalidCastException e = new InvalidCastException();
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal InvalidOperationException DataAdapter(string error)
        {
            return InvalidOperation(error);
        }
        static internal InvalidOperationException DataAdapter(string error, Exception inner)
        {
            return InvalidOperation(error, inner);
        }
        static private InvalidOperationException Provider(string error)
        {
            return InvalidOperation(error);
        }

        static internal ArgumentException InvalidMultipartName(string property, string value)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidMultipartName, SR.GetString(property), value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        static internal ArgumentException InvalidMultipartNameIncorrectUsageOfQuotes(string property, string value)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidMultipartNameQuoteUsage, SR.GetString(property), value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        static internal ArgumentException InvalidMultipartNameToManyParts(string property, string value, int limit)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidMultipartNameToManyParts, SR.GetString(property), value, limit));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        //
        // Helper Functions
        //
        static internal void CheckArgumentLength(string value, string parameterName)
        {
            CheckArgumentNull(value, parameterName);
            if (0 == value.Length)
            {
                throw Argument(SR.GetString(SR.ADP_EmptyString, parameterName));
            }
        }

        static internal void CheckArgumentNull(object value, string parameterName)
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
        }

        // only StackOverflowException & ThreadAbortException are sealed classes
        static private readonly Type StackOverflowType = typeof(StackOverflowException);
        static private readonly Type OutOfMemoryType = typeof(OutOfMemoryException);
        static private readonly Type ThreadAbortType = typeof(ThreadAbortException);
        static private readonly Type NullReferenceType = typeof(NullReferenceException);
        static private readonly Type AccessViolationType = typeof(AccessViolationException);
        static private readonly Type SecurityType = typeof(SecurityException);

        static internal bool IsCatchableExceptionType(Exception e)
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

        static internal bool IsCatchableOrSecurityExceptionType(Exception e)
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

        static internal ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
        {
            return ADP.ArgumentOutOfRange(SR.GetString(SR.ADP_InvalidEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
        }

        // IDbCommand.CommandType
        static internal ArgumentOutOfRangeException InvalidCommandType(CommandType value)
        {
#if DEBUG
            switch(value) {
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
        static internal ArgumentOutOfRangeException InvalidDataRowVersion(DataRowVersion value)
        {
#if DEBUG
            switch(value) {
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
        static internal ArgumentOutOfRangeException InvalidIsolationLevel(IsolationLevel value)
        {
#if DEBUG
            switch(value) {
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
        static internal ArgumentOutOfRangeException InvalidParameterDirection(ParameterDirection value)
        {
#if DEBUG
            switch(value) {
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
            switch(value) {
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
            return Argument(SR.GetString(SR.ADP_ConnectionStringSyntax, index));
        }
        static internal ArgumentException KeywordNotSupported(string keyword)
        {
            return Argument(SR.GetString(SR.ADP_KeywordNotSupported, keyword));
        }
        static internal ArgumentException UdlFileError(Exception inner)
        {
            return Argument(SR.GetString(SR.ADP_UdlFileError), inner);
        }
        static internal ArgumentException InvalidUDL()
        {
            return Argument(SR.GetString(SR.ADP_InvalidUDL));
        }
        static internal InvalidOperationException InvalidDataDirectory()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_InvalidDataDirectory));
        }
        static internal ArgumentException InvalidKeyname(string parameterName)
        {
            return Argument(SR.GetString(SR.ADP_InvalidKey), parameterName);
        }
        static internal ArgumentException InvalidValue(string parameterName)
        {
            return Argument(SR.GetString(SR.ADP_InvalidValue), parameterName);
        }
        static internal ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(SR.GetString(SR.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
        }

        //
        // DbConnection
        //
        static internal InvalidOperationException NoConnectionString()
        {
            return InvalidOperation(SR.GetString(SR.ADP_NoConnectionString));
        }

        static private string ConnectionStateMsg(ConnectionState state)
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

        static internal ConfigurationException ConfigUnableToLoadXmlMetaDataFile(string settingName)
        {
            return Configuration(SR.GetString(SR.OleDb_ConfigUnableToLoadXmlMetaDataFile, settingName));
        }

        static internal ConfigurationException ConfigWrongNumberOfValues(string settingName)
        {
            return Configuration(SR.GetString(SR.OleDb_ConfigWrongNumberOfValues, settingName));
        }

        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //
        static internal Exception InvalidConnectionOptionValue(string key)
        {
            return InvalidConnectionOptionValue(key, null);
        }
        static internal Exception InvalidConnectionOptionValue(string key, Exception inner)
        {
            return Argument(SR.GetString(SR.ADP_InvalidConnectionOptionValue, key), inner);
        }

        //
        // DbConnectionPool and related
        //
        static internal Exception PooledOpenTimeout()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_PooledOpenTimeout));
        }

        static internal Exception NonPooledOpenTimeout()
        {
            return ADP.TimeoutException(SR.GetString(SR.ADP_NonPooledOpenTimeout));
        }

        //
        // Generic Data Provider Collection
        //
        static internal ArgumentException CollectionRemoveInvalidObject(Type itemType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionRemoveInvalidObject, itemType.Name, collection.GetType().Name));
        }
        static internal ArgumentNullException CollectionNullValue(string parameter, Type collection, Type itemType)
        {
            return ArgumentNull(parameter, SR.GetString(SR.ADP_CollectionNullValue, collection.Name, itemType.Name));
        }
        static internal IndexOutOfRangeException CollectionIndexInt32(int index, Type collection, int count)
        {
            return IndexOutOfRange(SR.GetString(SR.ADP_CollectionIndexInt32, index.ToString(CultureInfo.InvariantCulture), collection.Name, count.ToString(CultureInfo.InvariantCulture)));
        }
        static internal IndexOutOfRangeException CollectionIndexString(Type itemType, string propertyName, string propertyValue, Type collection)
        {
            return IndexOutOfRange(SR.GetString(SR.ADP_CollectionIndexString, itemType.Name, propertyName, propertyValue, collection.Name));
        }
        static internal InvalidCastException CollectionInvalidType(Type collection, Type itemType, object invalidValue)
        {
            return InvalidCast(SR.GetString(SR.ADP_CollectionInvalidType, collection.Name, itemType.Name, invalidValue.GetType().Name));
        }
        static internal ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }
        static internal ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }

        //
        // DbProviderException
        //
        static internal InvalidOperationException TransactionConnectionMismatch()
        {
            return Provider(SR.GetString(SR.ADP_TransactionConnectionMismatch));
        }
        static internal InvalidOperationException TransactionRequired(string method)
        {
            return Provider(SR.GetString(SR.ADP_TransactionRequired, method));
        }

        static internal Exception CommandTextRequired(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_CommandTextRequired, method));
        }

        static internal InvalidOperationException ConnectionRequired(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ConnectionRequired, method));
        }
        static internal InvalidOperationException OpenConnectionRequired(string method, ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenConnectionRequired, method, ADP.ConnectionStateMsg(state)));
        }

        static internal Exception NoStoredProcedureExists(string sproc)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NoStoredProcedureExists, sproc));
        }
        static internal Exception OpenReaderExists()
        {
            return OpenReaderExists(null);
        }

        static internal Exception OpenReaderExists(Exception e)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenReaderExists), e);
        }

        static internal Exception TransactionCompleted()
        {
            return DataAdapter(SR.GetString(SR.ADP_TransactionCompleted));
        }

        //
        // DbDataReader
        //
        static internal Exception NonSeqByteAccess(long badIndex, long currIndex, string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NonSeqByteAccess, badIndex.ToString(CultureInfo.InvariantCulture), currIndex.ToString(CultureInfo.InvariantCulture), method));
        }

        static internal Exception NumericToDecimalOverflow()
        {
            return InvalidCast(SR.GetString(SR.ADP_NumericToDecimalOverflow));
        }
        static internal InvalidOperationException NonSequentialColumnAccess(int badCol, int currCol)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NonSequentialColumnAccess, badCol.ToString(CultureInfo.InvariantCulture), currCol.ToString(CultureInfo.InvariantCulture)));
        }
        static internal Exception FillRequiresSourceTableName(string parameter)
        {
            return Argument(SR.GetString(SR.ADP_FillRequiresSourceTableName), parameter);
        }

        //
        // : IDbCommand
        //
        static internal Exception InvalidCommandTimeout(int value)
        {
            return Argument(SR.GetString(SR.ADP_InvalidCommandTimeout, value.ToString(CultureInfo.InvariantCulture)), ADP.CommandTimeout);
        }
        static internal Exception DeriveParametersNotSupported(IDbCommand value)
        {
            return DataAdapter(SR.GetString(SR.ADP_DeriveParametersNotSupported, value.GetType().Name, value.CommandType.ToString()));
        }
        static internal Exception UninitializedParameterSize(int index, Type dataType)
        {
            return InvalidOperation(SR.GetString(SR.ADP_UninitializedParameterSize, index.ToString(CultureInfo.InvariantCulture), dataType.Name));
        }
        static internal Exception PrepareParameterType(IDbCommand cmd)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterType, cmd.GetType().Name));
        }
        static internal Exception PrepareParameterSize(IDbCommand cmd)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterSize, cmd.GetType().Name));
        }
        static internal Exception PrepareParameterScale(IDbCommand cmd, string type)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterScale, cmd.GetType().Name, type));
        }

        //
        // : ConnectionUtil
        //

        static internal Exception ClosedConnectionError()
        {
            return InvalidOperation(SR.GetString(SR.ADP_ClosedConnectionError));
        }
        static internal Exception ConnectionAlreadyOpen(ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ConnectionAlreadyOpen, ADP.ConnectionStateMsg(state)));
        }
        static internal Exception TransactionPresent()
        {
            return InvalidOperation(SR.GetString(SR.ADP_TransactionPresent));
        }
        static internal Exception LocalTransactionPresent()
        {
            return InvalidOperation(SR.GetString(SR.ADP_LocalTransactionPresent));
        }
        static internal Exception OpenConnectionPropertySet(string property, ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenConnectionPropertySet, property, ADP.ConnectionStateMsg(state)));
        }
        static internal Exception EmptyDatabaseName()
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
        static internal Exception InternalConnectionError(ConnectionError internalError)
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
        static internal Exception InternalError(InternalErrorCode internalError)
        {
            return InvalidOperation(SR.GetString(SR.ADP_InternalProviderError, (int)internalError));
        }
        static internal Exception InternalError(InternalErrorCode internalError, Exception innerException)
        {
            return InvalidOperation(SR.GetString(SR.ADP_InternalProviderError, (int)internalError), innerException);
        }
        static internal Exception InvalidConnectTimeoutValue()
        {
            return Argument(SR.GetString(SR.ADP_InvalidConnectTimeoutValue));
        }

        //
        // : DbDataReader
        //
        static internal Exception DataReaderNoData()
        {
            return InvalidOperation(SR.GetString(SR.ADP_DataReaderNoData));
        }
        static internal Exception DataReaderClosed(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_DataReaderClosed, method));
        }
        static internal ArgumentOutOfRangeException InvalidSourceBufferIndex(int maxLen, long srcOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.GetString(SR.ADP_InvalidSourceBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), srcOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        static internal ArgumentOutOfRangeException InvalidDestinationBufferIndex(int maxLen, int dstOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.GetString(SR.ADP_InvalidDestinationBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), dstOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        static internal Exception InvalidDataLength(long length)
        {
            return IndexOutOfRange(SR.GetString(SR.SQL_InvalidDataLength, length.ToString(CultureInfo.InvariantCulture)));
        }

        //
        // : IDataParameter
        //
        static internal ArgumentException InvalidDataType(TypeCode typecode)
        {
            return Argument(SR.GetString(SR.ADP_InvalidDataType, typecode.ToString()));
        }
        static internal ArgumentException DbTypeNotSupported(System.Data.DbType type, Type enumtype)
        {
            return Argument(SR.GetString(SR.ADP_DbTypeNotSupported, type.ToString(), enumtype.Name));
        }
        static internal ArgumentException UnknownDataTypeCode(Type dataType, TypeCode typeCode)
        {
            return Argument(SR.GetString(SR.ADP_UnknownDataTypeCode, ((int)typeCode).ToString(CultureInfo.InvariantCulture), dataType.FullName));
        }
        static internal ArgumentException InvalidOffsetValue(int value)
        {
            return Argument(SR.GetString(SR.ADP_InvalidOffsetValue, value.ToString(CultureInfo.InvariantCulture)));
        }
        static internal ArgumentException InvalidSizeValue(int value)
        {
            return Argument(SR.GetString(SR.ADP_InvalidSizeValue, value.ToString(CultureInfo.InvariantCulture)));
        }
        static internal Exception ParameterConversionFailed(object value, Type destType, Exception inner)
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
        static internal Exception ParametersMappingIndex(int index, IDataParameterCollection collection)
        {
            return CollectionIndexInt32(index, collection.GetType(), collection.Count);
        }
        static internal Exception ParametersSourceIndex(string parameterName, IDataParameterCollection collection, Type parameterType)
        {
            return CollectionIndexString(parameterType, ADP.ParameterName, parameterName, collection.GetType());
        }
        static internal Exception ParameterNull(string parameter, IDataParameterCollection collection, Type parameterType)
        {
            return CollectionNullValue(parameter, collection.GetType(), parameterType);
        }
        static internal Exception InvalidParameterType(IDataParameterCollection collection, Type parameterType, object invalidValue)
        {
            return CollectionInvalidType(collection.GetType(), parameterType, invalidValue);
        }

        //
        // : IDbTransaction
        //
        static internal Exception ParallelTransactionsNotSupported(IDbConnection obj)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ParallelTransactionsNotSupported, obj.GetType().Name));
        }
        static internal Exception TransactionZombied(IDbTransaction obj)
        {
            return InvalidOperation(SR.GetString(SR.ADP_TransactionZombied, obj.GetType().Name));
        }

        //
        // : DbMetaDataFactory
        //

        static internal Exception AmbigousCollectionName(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_AmbigousCollectionName, collectionName));
        }

        static internal Exception CollectionNameIsNotUnique(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_CollectionNameISNotUnique, collectionName));
        }

        static internal Exception DataTableDoesNotExist(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_DataTableDoesNotExist, collectionName));
        }

        static internal Exception IncorrectNumberOfDataSourceInformationRows()
        {
            return Argument(SR.GetString(SR.MDF_IncorrectNumberOfDataSourceInformationRows));
        }

        static internal ArgumentException InvalidRestrictionValue(string collectionName, string restrictionName, string restrictionValue)
        {
            return ADP.Argument(SR.GetString(SR.MDF_InvalidRestrictionValue, collectionName, restrictionName, restrictionValue));
        }

        static internal Exception InvalidXml()
        {
            return Argument(SR.GetString(SR.MDF_InvalidXml));
        }

        static internal Exception InvalidXmlMissingColumn(string collectionName, string columnName)
        {
            return Argument(SR.GetString(SR.MDF_InvalidXmlMissingColumn, collectionName, columnName));
        }

        static internal Exception InvalidXmlInvalidValue(string collectionName, string columnName)
        {
            return Argument(SR.GetString(SR.MDF_InvalidXmlInvalidValue, collectionName, columnName));
        }

        static internal Exception MissingDataSourceInformationColumn()
        {
            return Argument(SR.GetString(SR.MDF_MissingDataSourceInformationColumn));
        }

        static internal Exception MissingRestrictionColumn()
        {
            return Argument(SR.GetString(SR.MDF_MissingRestrictionColumn));
        }

        static internal Exception MissingRestrictionRow()
        {
            return Argument(SR.GetString(SR.MDF_MissingRestrictionRow));
        }

        static internal Exception NoColumns()
        {
            return Argument(SR.GetString(SR.MDF_NoColumns));
        }

        static internal Exception QueryFailed(string collectionName, Exception e)
        {
            return InvalidOperation(SR.GetString(SR.MDF_QueryFailed, collectionName), e);
        }

        static internal Exception TooManyRestrictions(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_TooManyRestrictions, collectionName));
        }

        static internal Exception UnableToBuildCollection(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UnableToBuildCollection, collectionName));
        }

        static internal Exception UndefinedCollection(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UndefinedCollection, collectionName));
        }

        static internal Exception UndefinedPopulationMechanism(string populationMechanism)
        {
            return Argument(SR.GetString(SR.MDF_UndefinedPopulationMechanism, populationMechanism));
        }

        static internal Exception UnsupportedVersion(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UnsupportedVersion, collectionName));
        }

        //
        // : CommandBuilder
        //
        static internal InvalidOperationException QuotePrefixNotSet(string method)
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

        static internal readonly IntPtr PtrZero = new IntPtr(0); // IntPtr.Zero
        static internal readonly int PtrSize = IntPtr.Size;
        static internal readonly IntPtr RecordsUnaffected = new IntPtr(-1);

        internal const int CharSize = System.Text.UnicodeEncoding.CharSize;

        static internal bool CompareInsensitiveInvariant(string strvalue, string strconst)
        {
            return (0 == CultureInfo.InvariantCulture.CompareInfo.Compare(strvalue, strconst, CompareOptions.IgnoreCase));
        }

        static internal Delegate FindBuilder(MulticastDelegate mcd)
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

        static internal readonly bool IsWindowsNT = (PlatformID.Win32NT == Environment.OSVersion.Platform);
        static internal readonly bool IsPlatformNT5 = (ADP.IsWindowsNT && (Environment.OSVersion.Version.Major >= 5));

        static internal SysTx.Transaction GetCurrentTransaction()
        {
            SysTx.Transaction transaction = SysTx.Transaction.Current;
            return transaction;
        }

        static internal void SetCurrentTransaction(SysTx.Transaction transaction)
        {
            SysTx.Transaction.Current = transaction;
        }

        static internal SysTx.IDtcTransaction GetOletxTransaction(SysTx.Transaction transaction)
        {
            SysTx.IDtcTransaction oleTxTransaction = null;

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

        static internal long TimerCurrent()
        {
            return DateTime.UtcNow.ToFileTimeUtc();
        }

        static internal long TimerFromSeconds(int seconds)
        {
            long result = checked((long)seconds * TimeSpan.TicksPerSecond);
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

        static internal long TimerToMilliseconds(long timerValue)
        {
            long result = timerValue / TimeSpan.TicksPerMillisecond;
            return result;
        }

        static internal string BuildQuotedString(string quotePrefix, string quoteSuffix, string unQuotedString)
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

        static internal void EscapeSpecialCharacters(string unescapedString, StringBuilder escapedString)
        {
            // note special characters list is from character escapes
            // in the MSDN regular expression language elements documentation
            // added ] since escaping it seems necessary
            const string specialCharacters = ".$^{[(|)*+?\\]";

            foreach (char currentChar in unescapedString)
            {
                if (specialCharacters.IndexOf(currentChar) >= 0)
                {
                    escapedString.Append("\\");
                }
                escapedString.Append(currentChar);
            }
            return;
        }

        static internal string GetFullPath(string filename)
        {
            return Path.GetFullPath(filename);
        }

        // SxS: the file is opened in FileShare.Read mode allowing several threads/apps to read it simultaneously
        static internal Stream GetFileStream(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        static internal FileVersionInfo GetVersionInfo(string filename)
        {
            return FileVersionInfo.GetVersionInfo(filename);
        }

        static internal Stream GetXmlStreamFromValues(String[] values, String errorString)
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
        static internal Stream GetXmlStream(String value, String errorString)
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
            String fullPath = tempstring.ToString();

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

        static internal object ClassesRootRegistryValue(string subkey, string queryvalue)
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(subkey, false))
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

        static internal object LocalMachineRegistryValue(string subkey, string queryvalue)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey, false))
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
        static internal void CheckVersionMDAC(bool ifodbcelseoledb)
        {
            int major, minor, build;
            string version;

            try
            {
                version = (string)ADP.LocalMachineRegistryValue("Software\\Microsoft\\DataAccess", "FullInstallVer");
                if (ADP.IsEmpty(version))
                {
                    string filename = (string)ADP.ClassesRootRegistryValue(System.Data.OleDb.ODB.DataLinks_CLSID, string.Empty);
                    FileVersionInfo versionInfo = ADP.GetVersionInfo(filename);
                    major = versionInfo.FileMajorPart;
                    minor = versionInfo.FileMinorPart;
                    build = versionInfo.FileBuildPart;
                    version = versionInfo.FileVersion;
                }
                else
                {
                    string[] parts = version.Split('.');
                    major = Int32.Parse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture);
                    minor = Int32.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture);
                    build = Int32.Parse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture);
                    Int32.Parse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture);
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
        static internal Boolean RemoveStringQuotes(string quotePrefix, string quoteSuffix, string quotedString, out string unquotedString)
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
                if (quotedString.StartsWith(quotePrefix, StringComparison.Ordinal) == false)
                {
                    unquotedString = quotedString;
                    return false;
                }
            }

            // is the suffix present?
            if (suffixLength > 0)
            {
                if (quotedString.EndsWith(quoteSuffix, StringComparison.Ordinal) == false)
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

        static internal IntPtr IntPtrOffset(IntPtr pbase, Int32 offset)
        {
            if (4 == ADP.PtrSize)
            {
                return (IntPtr)checked(pbase.ToInt32() + offset);
            }
            Debug.Assert(8 == ADP.PtrSize, "8 != IntPtr.Size");
            return (IntPtr)checked(pbase.ToInt64() + offset);
        }

        static internal int IntPtrToInt32(IntPtr value)
        {
            if (4 == ADP.PtrSize)
            {
                return (int)value;
            }
            else
            {
                long lval = (long)value;
                lval = Math.Min((long)Int32.MaxValue, lval);
                lval = Math.Max((long)Int32.MinValue, lval);
                return (int)lval;
            }
        }

        // TODO: are those names appropriate for common code?
        static internal int SrcCompare(string strA, string strB)
        { // this is null safe
            return ((strA == strB) ? 0 : 1);
        }

        static internal int DstCompare(string strA, string strB)
        { // this is null safe
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, ADP.compareOptions);
        }

        static internal bool IsDirection(IDataParameter value, ParameterDirection condition)
        {
#if DEBUG
            IsDirectionValid(condition);
#endif
            return (condition == (condition & value.Direction));
        }
#if DEBUG
        static private void IsDirectionValid(ParameterDirection value) {
            switch (value) { // @perfnote: Enum.IsDefined
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

        static internal bool IsEmpty(string str)
        {
            return ((null == str) || (0 == str.Length));
        }

        static internal bool IsEmptyArray(string[] array)
        {
            return ((null == array) || (0 == array.Length));
        }

        static internal bool IsNull(object value)
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

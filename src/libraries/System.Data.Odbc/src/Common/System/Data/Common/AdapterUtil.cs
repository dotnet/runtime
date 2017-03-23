// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        // The class ADP defines the exceptions that are specific to the Adapters.f
        // The class contains functions that take the proper informational variables and then construct
        // the appropriate exception with an error string obtained from the resource Framework.txt.
        // The exception is then returned to the caller, so that the caller may then throw from its
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error messages.
        // The resource Framework.txt will ensure proper string text based on the appropriate
        // locale.

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

        // this method accepts BID format as an argument, this attribute allows FXCopBid rule to validate calls to it
        private static void TraceException(string trace, Exception e)
        {
            Debug.Assert(null != e, "TraceException: null Exception");
        }

        internal static void TraceExceptionAsReturnValue(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|THROW> '%ls'\n", e);
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
        internal static TimeoutException TimeoutException(string error)
        {
            TimeoutException e = new TimeoutException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static InvalidOperationException InvalidOperation(string error, Exception inner)
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
        internal static InvalidOperationException DataAdapter(string error, Exception inner)
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
                throw Argument(SR.GetString(SR.ADP_EmptyString, parameterName)); // MDAC 94859
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
        private static readonly Type s_stackOverflowType = typeof(StackOverflowException);
        private static readonly Type s_outOfMemoryType = typeof(OutOfMemoryException);
        private static readonly Type s_threadAbortType = typeof(ThreadAbortException);
        private static readonly Type s_nullReferenceType = typeof(NullReferenceException);
        private static readonly Type s_accessViolationType = typeof(AccessViolationException);
        private static readonly Type s_securityType = typeof(SecurityException);

        internal static bool IsCatchableExceptionType(Exception e)
        {
            // a 'catchable' exception is defined by what it is not.
            Debug.Assert(e != null, "Unexpected null exception!");
            Type type = e.GetType();

            return ((type != s_stackOverflowType) &&
                     (type != s_outOfMemoryType) &&
                     (type != s_threadAbortType) &&
                     (type != s_nullReferenceType) &&
                     (type != s_accessViolationType) &&
                     !s_securityType.IsAssignableFrom(type));
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

            return ((type != s_stackOverflowType) &&
                     (type != s_outOfMemoryType) &&
                     (type != s_threadAbortType) &&
                     (type != s_nullReferenceType) &&
                     (type != s_accessViolationType));
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
        static internal ArgumentOutOfRangeException InvalidDataRowVersion(DataRowVersion value)
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

        // DBDataPermissionAttribute.KeyRestrictionBehavior
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

        internal static ArgumentOutOfRangeException InvalidPermissionState(PermissionState value)
        {
#if DEBUG
            switch (value)
            {
                case PermissionState.Unrestricted:
                case PermissionState.None:
                    Debug.Assert(false, "valid PermissionState " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(PermissionState), (int)value);
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
        /*
        static internal ArgumentException EmptyKeyValue(string keyword) { // MDAC 80715
            return Argument(Res.GetString(Res.ADP_EmptyKeyValue, keyword));
        }
        */
        internal static InvalidOperationException InvalidDataDirectory()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_InvalidDataDirectory));
        }
        internal static ArgumentException InvalidKeyname(string parameterName)
        {
            return Argument(SR.GetString(SR.ADP_InvalidKey), parameterName);
        }
        internal static ArgumentException InvalidValue(string parameterName)
        {
            return Argument(SR.GetString(SR.ADP_InvalidValue), parameterName);
        }
        internal static ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(SR.GetString(SR.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
        }

        //
        // DbConnection
        //
        internal static InvalidOperationException NoConnectionString()
        {
            return InvalidOperation(SR.GetString(SR.ADP_NoConnectionString));
        }

        internal static Exception MethodNotImplemented([CallerMemberName] string methodName = "")
        {
            return System.NotImplemented.ByDesignWithMessage(methodName);
        }

        private static string ConnectionStateMsg(ConnectionState state)
        { // MDAC 82165, if the ConnectionState enum to msg the localization looks weird
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

        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //
        internal static Exception InvalidConnectionOptionValue(string key)
        {
            return InvalidConnectionOptionValue(key, null);
        }
        internal static Exception InvalidConnectionOptionValue(string key, Exception inner)
        {
            return Argument(SR.GetString(SR.ADP_InvalidConnectionOptionValue, key), inner);
        }

        internal static Exception OdbcNoTypesFromProvider()
        {
            return InvalidOperation(SR.GetString(SR.ADP_OdbcNoTypesFromProvider));
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
            return Argument(SR.GetString(SR.ADP_CollectionRemoveInvalidObject, itemType.Name, collection.GetType().Name)); // MDAC 68201
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
        internal static Exception CollectionUniqueValue(Type itemType, string propertyName, string propertyValue)
        {
            return Argument(SR.GetString(SR.ADP_CollectionUniqueValue, itemType.Name, propertyName, propertyValue));
        }
        internal static ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }
        internal static ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.GetString(SR.ADP_CollectionIsParent, parameterType.Name, collection.GetType().Name));
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

        //
        // IDbCommand
        //
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


        internal static Exception OpenReaderExists()
        {
            return OpenReaderExists(null);
        }

        internal static Exception OpenReaderExists(Exception e)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenReaderExists), e);
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

        //
        // : ConnectionUtil
        //
        internal static Exception ConnectionIsDisabled(Exception InnerException)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ConnectionIsDisabled), InnerException);
        }
        internal static Exception ClosedConnectionError()
        {
            return InvalidOperation(SR.GetString(SR.ADP_ClosedConnectionError));
        }
        internal static Exception ConnectionAlreadyOpen(ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ConnectionAlreadyOpen, ADP.ConnectionStateMsg(state)));
        }
        internal static Exception OpenConnectionPropertySet(string property, ConnectionState state)
        {
            return InvalidOperation(SR.GetString(SR.ADP_OpenConnectionPropertySet, property, ADP.ConnectionStateMsg(state)));
        }
        internal static Exception EmptyDatabaseName()
        {
            return Argument(SR.GetString(SR.ADP_EmptyDatabaseName));
        }
        internal static Exception DatabaseNameTooLong()
        {
            return Argument(SR.GetString(SR.ADP_DatabaseNameTooLong));
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

        //
        // : IDataParameter
        //
        internal static ArgumentException InvalidDataType(TypeCode typecode)
        {
            return Argument(SR.GetString(SR.ADP_InvalidDataType, typecode.ToString()));
        }
        internal static ArgumentException UnknownDataType(Type dataType)
        {
            return Argument(SR.GetString(SR.ADP_UnknownDataType, dataType.FullName));
        }
        internal static ArgumentException DbTypeNotSupported(System.Data.DbType type, Type enumtype)
        {
            return Argument(SR.GetString(SR.ADP_DbTypeNotSupported, type.ToString(), enumtype.Name));
        }
        internal static ArgumentException UnknownDataTypeCode(Type dataType, TypeCode typeCode)
        {
            return Argument(SR.GetString(SR.ADP_UnknownDataTypeCode, ((int)typeCode).ToString(CultureInfo.InvariantCulture), dataType.FullName));
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
        { // WebData 75433
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

        internal static Exception DbRecordReadOnly(string methodname)
        {
            return InvalidOperation(SR.GetString(SR.ADP_DbRecordReadOnly, methodname));
        }

        internal static Exception OffsetOutOfRangeException()
        {
            return InvalidOperation(SR.GetString(SR.ADP_OffsetOutOfRangeException));
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


        // global constant strings
        internal const string Append = "Append";
        internal const string BeginExecuteNonQuery = "BeginExecuteNonQuery";
        internal const string BeginExecuteReader = "BeginExecuteReader";
        internal const string BeginTransaction = "BeginTransaction";
        internal const string BeginExecuteXmlReader = "BeginExecuteXmlReader";
        internal const string ChangeDatabase = "ChangeDatabase";
        internal const string Cancel = "Cancel";
        internal const string Clone = "Clone";
        internal const string ColumnEncryptionSystemProviderNamePrefix = "MSSQL_";
        internal const string CommitTransaction = "CommitTransaction";
        internal const string CommandTimeout = "CommandTimeout";
        internal const string ConnectionString = "ConnectionString";
        internal const string DataSetColumn = "DataSetColumn";
        internal const string DataSetTable = "DataSetTable";
        internal const string Delete = "Delete";
        internal const string DeleteCommand = "DeleteCommand";
        internal const string DeriveParameters = "DeriveParameters";
        internal const string EndExecuteNonQuery = "EndExecuteNonQuery";
        internal const string EndExecuteReader = "EndExecuteReader";
        internal const string EndExecuteXmlReader = "EndExecuteXmlReader";
        internal const string ExecuteReader = "ExecuteReader";
        internal const string ExecuteRow = "ExecuteRow";
        internal const string ExecuteNonQuery = "ExecuteNonQuery";
        internal const string ExecuteScalar = "ExecuteScalar";
        internal const string ExecuteSqlScalar = "ExecuteSqlScalar";
        internal const string ExecuteXmlReader = "ExecuteXmlReader";
        internal const string Fill = "Fill";
        internal const string FillPage = "FillPage";
        internal const string FillSchema = "FillSchema";
        internal const string GetBytes = "GetBytes";
        internal const string GetChars = "GetChars";
        internal const string GetOleDbSchemaTable = "GetOleDbSchemaTable";
        internal const string GetProperties = "GetProperties";
        internal const string GetSchema = "GetSchema";
        internal const string GetSchemaTable = "GetSchemaTable";
        internal const string GetServerTransactionLevel = "GetServerTransactionLevel";
        internal const string Insert = "Insert";
        internal const string Open = "Open";
        internal const string Parameter = "Parameter";
        internal const string ParameterBuffer = "buffer";
        internal const string ParameterCount = "count";
        internal const string ParameterDestinationType = "destinationType";
        internal const string ParameterIndex = "index";
        internal const string ParameterName = "ParameterName";
        internal const string ParameterOffset = "offset";
        internal const string ParameterSetPosition = "set_Position";
        internal const string ParameterService = "Service";
        internal const string ParameterTimeout = "Timeout";
        internal const string ParameterUserData = "UserData";
        internal const string Prepare = "Prepare";
        internal const string QuoteIdentifier = "QuoteIdentifier";
        internal const string Read = "Read";
        internal const string ReadAsync = "ReadAsync";
        internal const string Remove = "Remove";
        internal const string RollbackTransaction = "RollbackTransaction";
        internal const string SaveTransaction = "SaveTransaction";
        internal const string SetProperties = "SetProperties";
        internal const string SourceColumn = "SourceColumn";
        internal const string SourceVersion = "SourceVersion";
        internal const string SourceTable = "SourceTable";
        internal const string UnquoteIdentifier = "UnquoteIdentifier";
        internal const string Update = "Update";
        internal const string UpdateCommand = "UpdateCommand";
        internal const string UpdateRows = "UpdateRows";

        internal const CompareOptions compareOptions = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
        internal const int DecimalMaxPrecision = 29;
        internal const int DecimalMaxPrecision28 = 28;  // there are some cases in Odbc where we need that ...
        internal const int DefaultCommandTimeout = 30;
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;

        // security issue, don't rely upon static public readonly values - AS/URT 109635
        internal static readonly String StrEmpty = ""; // String.Empty

        internal static readonly IntPtr PtrZero = new IntPtr(0); // IntPtr.Zero
        internal static readonly int PtrSize = IntPtr.Size;

        internal static bool CompareInsensitiveInvariant(string strvalue, string strconst)
        {
            return (0 == CultureInfo.InvariantCulture.CompareInfo.Compare(strvalue, strconst, CompareOptions.IgnoreCase));
        }

        internal static Delegate FindBuilder(MulticastDelegate mcd)
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

        internal static bool TimerHasExpired(long timerExpire)
        {
            bool result = TimerCurrent() > timerExpire;
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

        private static long TimerToSeconds(long timerValue)
        {
            long result = timerValue / TimeSpan.TicksPerSecond;
            return result;
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
                    escapedString.Append("\\");
                }
                escapedString.Append(currentChar);
            }
            return;
        }


        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static string GetFullPath(string filename)
        { // MDAC 77686
            return Path.GetFullPath(filename);
        }

        internal static int StringLength(string inputString)
        {
            return ((null != inputString) ? inputString.Length : 0);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static IntPtr IntPtrOffset(IntPtr pbase, Int32 offset)
        {
            if (4 == ADP.PtrSize)
            {
                return (IntPtr)checked(pbase.ToInt32() + offset);
            }
            Debug.Assert(8 == ADP.PtrSize, "8 != IntPtr.Size"); // MDAC 73747
            return (IntPtr)checked(pbase.ToInt64() + offset);
        }

        internal static int DstCompare(string strA, string strB)
        { // this is null safe
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, ADP.compareOptions);
        }

        internal static bool IsEmptyArray(string[] array)
        {
            return ((null == array) || (0 == array.Length));
        }

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

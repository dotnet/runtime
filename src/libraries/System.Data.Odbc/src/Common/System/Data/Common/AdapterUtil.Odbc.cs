// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Text;

namespace System
{
    internal static partial class SR
    {
        internal static string GetString(string value)
        {
            return value;
        }

        internal static string GetString(string format, params object?[] args)
        {
            return SR.Format(format, args);
        }
    }
}

namespace System.Data.Common
{
    internal static partial class ADP
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

        //
        // COM+ exceptions
        //
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
        internal static InvalidCastException InvalidCast()
        {
            InvalidCastException e = new InvalidCastException();
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

        // IDbCommand.CommandType
        internal static ArgumentOutOfRangeException InvalidCommandType(CommandType value)
        {
#if DEBUG
            switch (value)
            {
                case CommandType.Text:
                case CommandType.StoredProcedure:
                case CommandType.TableDirect:
                    Debug.Fail("valid CommandType " + value.ToString());
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
                    Debug.Fail("valid DataRowVersion " + value.ToString());
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
                    Debug.Fail("valid IsolationLevel " + value.ToString());
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
                    Debug.Fail("valid KeyRestrictionBehavior " + value.ToString());
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
                    Debug.Fail("valid ParameterDirection " + value.ToString());
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
                    Debug.Fail("valid UpdateRowSource " + value.ToString());
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(UpdateRowSource), (int)value);
        }

        //
        // DbConnectionOptions, DataAccess
        //
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

        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //

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

        internal static Exception OpenReaderExists(Exception? e)
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
        internal static InvalidOperationException QuotePrefixNotSet(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_QuotePrefixNotSet, method));
        }

        //
        // : ConnectionUtil
        //
        internal static Exception ConnectionIsDisabled(Exception? InnerException)
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

        //
        // : DbDataReader
        //
        internal static Exception DataReaderNoData()
        {
            return InvalidOperation(SR.GetString(SR.ADP_DataReaderNoData));
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
            return Argument(SR.GetString(SR.ADP_UnknownDataType, dataType.FullName!));
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
        internal static Exception OffsetOutOfRangeException()
        {
            return InvalidOperation(SR.GetString(SR.ADP_OffsetOutOfRangeException));
        }

        //
        // : DbMetaDataFactory
        //

        internal static Exception AmbiguousCollectionName(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_AmbiguousCollectionName, collectionName));
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

        internal static ArgumentException InvalidRestrictionValue(string collectionName, string restrictionName, string? restrictionValue)
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
        internal const string BeginTransaction = "BeginTransaction";
        internal const string ChangeDatabase = "ChangeDatabase";
        internal const string CommitTransaction = "CommitTransaction";
        internal const string CommandTimeout = "CommandTimeout";
        internal const string DeriveParameters = "DeriveParameters";
        internal const string ExecuteReader = "ExecuteReader";
        internal const string ExecuteNonQuery = "ExecuteNonQuery";
        internal const string ExecuteScalar = "ExecuteScalar";
        internal const string GetSchema = "GetSchema";
        internal const string GetSchemaTable = "GetSchemaTable";
        internal const string Parameter = "Parameter";
        internal const string ParameterName = "ParameterName";
        internal const string Prepare = "Prepare";
        internal const string RollbackTransaction = "RollbackTransaction";
        internal const string QuoteIdentifier = "QuoteIdentifier";
        internal const string UnquoteIdentifier = "UnquoteIdentifier";

        internal const int DecimalMaxPrecision = 29;
        internal const int DecimalMaxPrecision28 = 28;  // there are some cases in Odbc where we need that ...
        internal const int DefaultCommandTimeout = 30;

        internal static readonly IntPtr PtrZero = new IntPtr(0); // IntPtr.Zero
        internal static readonly int PtrSize = IntPtr.Size;

        internal static Delegate? FindBuilder(MulticastDelegate mcd)
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
        }


        internal static string GetFullPath(string filename)
        { // MDAC 77686
            return Path.GetFullPath(filename);
        }

        internal static int StringLength(string? inputString)
        {
            return ((null != inputString) ? inputString.Length : 0);
        }

        internal static IntPtr IntPtrOffset(IntPtr pbase, int offset)
        {
            if (4 == ADP.PtrSize)
            {
                return (IntPtr)checked(pbase.ToInt32() + offset);
            }
            Debug.Assert(8 == ADP.PtrSize, "8 != IntPtr.Size"); // MDAC 73747
            return (IntPtr)checked(pbase.ToInt64() + offset);
        }
    }
}

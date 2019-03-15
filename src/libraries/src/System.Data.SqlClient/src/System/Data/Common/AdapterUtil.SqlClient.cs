// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Transactions;

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
        internal static IndexOutOfRangeException IndexOutOfRange(int value)
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException(value.ToString(CultureInfo.InvariantCulture));
            return e;
        }
        internal static IndexOutOfRangeException IndexOutOfRange()
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException();
            return e;
        }
        internal static TimeoutException TimeoutException(string error)
        {
            TimeoutException e = new TimeoutException(error);
            return e;
        }
        internal static InvalidOperationException InvalidOperation(string error, Exception inner)
        {
            InvalidOperationException e = new InvalidOperationException(error, inner);
            return e;
        }
        internal static OverflowException Overflow(string error)
        {
            return Overflow(error, null);
        }
        internal static OverflowException Overflow(string error, Exception inner)
        {
            OverflowException e = new OverflowException(error, inner);
            return e;
        }
        internal static TypeLoadException TypeLoad(string error)
        {
            TypeLoadException e = new TypeLoadException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        internal static PlatformNotSupportedException DbTypeNotSupported(string dbType)
        {
            PlatformNotSupportedException e = new PlatformNotSupportedException(SR.GetString(SR.SQL_DbTypeNotSupportedOnThisPlatform, dbType));
            return e;
        }
        internal static InvalidCastException InvalidCast()
        {
            InvalidCastException e = new InvalidCastException();
            return e;
        }
        internal static IOException IO(string error)
        {
            IOException e = new IOException(error);
            return e;
        }
        internal static IOException IO(string error, Exception inner)
        {
            IOException e = new IOException(error, inner);
            return e;
        }
        internal static ObjectDisposedException ObjectDisposed(object instance)
        {
            ObjectDisposedException e = new ObjectDisposedException(instance.GetType().Name);
            return e;
        }

        internal static Exception DataTableDoesNotExist(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_DataTableDoesNotExist, collectionName));
        }

        internal static InvalidOperationException MethodCalledTwice(string method)
        {
            InvalidOperationException e = new InvalidOperationException(SR.GetString(SR.ADP_CalledTwice, method));
            return e;
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

        internal static Exception TooManyRestrictions(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_TooManyRestrictions, collectionName));
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
        internal static ArgumentException InvalidMinMaxPoolSizeValues()
        {
            return ADP.Argument(SR.GetString(SR.ADP_InvalidMinMaxPoolSizeValues));
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
            return NotImplemented.ByDesignWithMessage(methodName);
        }

        internal static Exception QueryFailed(string collectionName, Exception e)
        {
            return InvalidOperation(SR.GetString(SR.MDF_QueryFailed, collectionName), e);
        }


        //
        // : DbConnectionOptions, DataAccess, SqlClient
        //
        internal static Exception InvalidConnectionOptionValueLength(string key, int limit)
        {
            return Argument(SR.GetString(SR.ADP_InvalidConnectionOptionValueLength, key, limit));
        }
        internal static Exception MissingConnectionOptionValue(string key, string requiredAdditionalKey)
        {
            return Argument(SR.GetString(SR.ADP_MissingConnectionOptionValue, key, requiredAdditionalKey));
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


        internal static Exception CommandTextRequired(string method)
        {
            return InvalidOperation(SR.GetString(SR.ADP_CommandTextRequired, method));
        }

        internal static Exception NoColumns()
        {
            return Argument(SR.GetString(SR.MDF_NoColumns));
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

        internal static Exception InvalidXml()
        {
            return Argument(SR.GetString(SR.MDF_InvalidXml));
        }

        internal static Exception NegativeParameter(string parameterName)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NegativeParameter, parameterName));
        }

        internal static Exception InvalidXmlMissingColumn(string collectionName, string columnName)
        {
            return Argument(SR.GetString(SR.MDF_InvalidXmlMissingColumn, collectionName, columnName));
        }

        //
        // SqlMetaData, SqlTypes, SqlClient
        //
        internal static Exception InvalidMetaDataValue()
        {
            return ADP.Argument(SR.GetString(SR.ADP_InvalidMetaDataValue));
        }

        internal static InvalidOperationException NonSequentialColumnAccess(int badCol, int currCol)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NonSequentialColumnAccess, badCol.ToString(CultureInfo.InvariantCulture), currCol.ToString(CultureInfo.InvariantCulture)));
        }

        internal static Exception InvalidXmlInvalidValue(string collectionName, string columnName)
        {
            return Argument(SR.GetString(SR.MDF_InvalidXmlInvalidValue, collectionName, columnName));
        }
        
        internal static Exception CollectionNameIsNotUnique(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_CollectionNameISNotUnique, collectionName));
        }


        //
        // : IDbCommand
        //
        internal static Exception InvalidCommandTimeout(int value, [CallerMemberName] string property = "")
        {
            return Argument(SR.GetString(SR.ADP_InvalidCommandTimeout, value.ToString(CultureInfo.InvariantCulture)), property);
        }
        internal static Exception UninitializedParameterSize(int index, Type dataType)
        {
            return InvalidOperation(SR.GetString(SR.ADP_UninitializedParameterSize, index.ToString(CultureInfo.InvariantCulture), dataType.Name));
        }

        internal static Exception UnableToBuildCollection(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UnableToBuildCollection, collectionName));
        }

        internal static Exception PrepareParameterType(DbCommand cmd)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterType, cmd.GetType().Name));
        }

        internal static Exception UndefinedCollection(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UndefinedCollection, collectionName));
        }

        internal static Exception UnsupportedVersion(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_UnsupportedVersion, collectionName));
        }

        internal static Exception AmbigousCollectionName(string collectionName)
        {
            return Argument(SR.GetString(SR.MDF_AmbigousCollectionName, collectionName));
        }

        internal static Exception PrepareParameterSize(DbCommand cmd)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterSize, cmd.GetType().Name));
        }
        internal static Exception PrepareParameterScale(DbCommand cmd, string type)
        {
            return InvalidOperation(SR.GetString(SR.ADP_PrepareParameterScale, cmd.GetType().Name, type));
        }

        internal static Exception MissingDataSourceInformationColumn()
        {
            return Argument(SR.GetString(SR.MDF_MissingDataSourceInformationColumn));
        }

        internal static Exception IncorrectNumberOfDataSourceInformationRows()
        {
            return Argument(SR.GetString(SR.MDF_IncorrectNumberOfDataSourceInformationRows));
        }

        internal static Exception MismatchedAsyncResult(string expectedMethod, string gotMethod)
        {
            return InvalidOperation(SR.GetString(SR.ADP_MismatchedAsyncResult, expectedMethod, gotMethod));
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

        internal static Exception MissingRestrictionColumn()
        {
            return Argument(SR.GetString(SR.MDF_MissingRestrictionColumn));
        }

        internal static Exception InternalConnectionError(ConnectionError internalError)
        {
            return InvalidOperation(SR.GetString(SR.ADP_InternalConnectionError, (int)internalError));
        }

        internal static Exception InvalidConnectRetryCountValue()
        {
            return Argument(SR.GetString(SR.SQLCR_InvalidConnectRetryCountValue));
        }

        internal static Exception MissingRestrictionRow()
        {
            return Argument(SR.GetString(SR.MDF_MissingRestrictionRow));
        }

        internal static Exception InvalidConnectRetryIntervalValue()
        {
            return Argument(SR.GetString(SR.SQLCR_InvalidConnectRetryIntervalValue));
        }

        //
        // : DbDataReader
        //
        internal static InvalidOperationException AsyncOperationPending()
        {
            return InvalidOperation(SR.GetString(SR.ADP_PendingAsyncOperation));
        }

        //
        // : Stream
        //
        internal static IOException ErrorReadingFromStream(Exception internalException)
        {
            return IO(SR.GetString(SR.SqlMisc_StreamErrorMessage), internalException);
        }

        internal static ArgumentException InvalidDataType(TypeCode typecode)
        {
            return Argument(SR.GetString(SR.ADP_InvalidDataType, typecode.ToString()));
        }

        internal static ArgumentException UnknownDataType(Type dataType)
        {
            return Argument(SR.GetString(SR.ADP_UnknownDataType, dataType.FullName));
        }

        internal static ArgumentException DbTypeNotSupported(DbType type, Type enumtype)
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
        internal static ArgumentException ParameterValueOutOfRange(decimal value)
        {
            return ADP.Argument(SR.GetString(SR.ADP_ParameterValueOutOfRange, value.ToString((IFormatProvider)null)));
        }
        internal static ArgumentException ParameterValueOutOfRange(SqlDecimal value)
        {
            return ADP.Argument(SR.GetString(SR.ADP_ParameterValueOutOfRange, value.ToString()));
        }

        internal static ArgumentException VersionDoesNotSupportDataType(string typeName)
        {
            return Argument(SR.GetString(SR.ADP_VersionDoesNotSupportDataType, typeName));
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
            return e;
        }

        //
        // : IDataParameterCollection
        //
        internal static Exception ParametersMappingIndex(int index, DbParameterCollection collection)
        {
            return CollectionIndexInt32(index, collection.GetType(), collection.Count);
        }
        internal static Exception ParametersSourceIndex(string parameterName, DbParameterCollection collection, Type parameterType)
        {
            return CollectionIndexString(parameterType, ADP.ParameterName, parameterName, collection.GetType());
        }
        internal static Exception ParameterNull(string parameter, DbParameterCollection collection, Type parameterType)
        {
            return CollectionNullValue(parameter, collection.GetType(), parameterType);
        }

        internal static Exception UndefinedPopulationMechanism(string populationMechanism)
        {
            throw new NotImplementedException();
        }

        internal static Exception InvalidParameterType(DbParameterCollection collection, Type parameterType, object invalidValue)
        {
            return CollectionInvalidType(collection.GetType(), parameterType, invalidValue);
        }

        //
        // : IDbTransaction
        //
        internal static Exception ParallelTransactionsNotSupported(DbConnection obj)
        {
            return InvalidOperation(SR.GetString(SR.ADP_ParallelTransactionsNotSupported, obj.GetType().Name));
        }
        internal static Exception TransactionZombied(DbTransaction obj)
        {
            return InvalidOperation(SR.GetString(SR.ADP_TransactionZombied, obj.GetType().Name));
        }

        // global constant strings
        internal const string Parameter = "Parameter";
        internal const string ParameterName = "ParameterName";
        internal const string ParameterSetPosition = "set_Position";

        internal const int DefaultCommandTimeout = 30;
        internal const float FailoverTimeoutStep = 0.08F;    // fraction of timeout to use for fast failover connections

        // security issue, don't rely upon public static readonly values
        internal static readonly string StrEmpty = ""; // String.Empty

        internal const int CharSize = sizeof(char);

        internal static Delegate FindBuilder(MulticastDelegate mcd)
        {
            if (null != mcd)
            {
                foreach (Delegate del in mcd.GetInvocationList())
                {
                    if (del.Target is DbCommandBuilder)
                        return del;
                }
            }

            return null;
        }

        internal static void TimerCurrent(out long ticks)
        {
            ticks = DateTime.UtcNow.ToFileTimeUtc();
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

        internal static long TimerFromMilliseconds(long milliseconds)
        {
            long result = checked(milliseconds * TimeSpan.TicksPerMillisecond);
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

        internal static long TimerRemainingSeconds(long timerExpire)
        {
            long result = TimerToSeconds(TimerRemaining(timerExpire));
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

        internal static string MachineName()
        {
            return Environment.MachineName;
        }

        internal static Transaction GetCurrentTransaction()
        {
            return Transaction.Current;
        }

        internal static bool IsDirection(DbParameter value, ParameterDirection condition)
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

        internal static void IsNullOrSqlType(object value, out bool isNull, out bool isSqlType)
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

        internal static Version GetAssemblyVersion()
        {
            // NOTE: Using lazy thread-safety since we don't care if two threads both happen to update the value at the same time
            if (s_systemDataVersion == null)
            {
                s_systemDataVersion = new Version(ThisAssembly.InformationalVersion);
            }

            return s_systemDataVersion;
        }


        internal static readonly string[] AzureSqlServerEndpoints = {SR.GetString(SR.AZURESQL_GenericEndpoint),
                                                                     SR.GetString(SR.AZURESQL_GermanEndpoint),
                                                                     SR.GetString(SR.AZURESQL_UsGovEndpoint),
                                                                     SR.GetString(SR.AZURESQL_ChinaEndpoint)};

        // This method assumes dataSource parameter is in TCP connection string format.
        internal static bool IsAzureSqlServerEndpoint(string dataSource)
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

            // trim redundant whitespace
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

        internal static ArgumentOutOfRangeException InvalidDataRowVersion(DataRowVersion value)
        {
#if DEBUG
            switch (value)
            {
                case DataRowVersion.Default:
                case DataRowVersion.Current:
                case DataRowVersion.Original:
                case DataRowVersion.Proposed:
                    Debug.Fail($"Invalid DataRowVersion {value}");
                    break;
            }
#endif
            return InvalidEnumerationValue(typeof(DataRowVersion), (int)value);
        }

        internal static ArgumentException SingleValuedProperty(string propertyName, string value)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_SingleValuedProperty, propertyName, value));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException DoubleValuedProperty(string propertyName, string value1, string value2)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_DoubleValuedProperty, propertyName, value1, value2));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException InvalidPrefixSuffix()
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_InvalidPrefixSuffix));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentOutOfRangeException InvalidCommandBehavior(CommandBehavior value)
        {
            Debug.Assert((0 > (int)value) || ((int)value > 0x3F), "valid CommandType " + value.ToString());

            return InvalidEnumerationValue(typeof(CommandBehavior), (int)value);
        }

        internal static void ValidateCommandBehavior(CommandBehavior value)
        {
            if (((int)value < 0) || (0x3F < (int)value))
            {
                throw InvalidCommandBehavior(value);
            }
        }

        internal static ArgumentOutOfRangeException NotSupportedCommandBehavior(CommandBehavior value, string method)
        {
            return NotSupportedEnumerationValue(typeof(CommandBehavior), value.ToString(), method);
        }

        internal static ArgumentException BadParameterName(string parameterName)
        {
            ArgumentException e = new ArgumentException(SR.GetString(SR.ADP_BadParameterName, parameterName));
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static Exception DeriveParametersNotSupported(IDbCommand value)
        {
            return DataAdapter(SR.GetString(SR.ADP_DeriveParametersNotSupported, value.GetType().Name, value.CommandType.ToString()));
        }

        internal static Exception NoStoredProcedureExists(string sproc)
        {
            return InvalidOperation(SR.GetString(SR.ADP_NoStoredProcedureExists, sproc));
        }

        //
        // DbProviderException
        //
        internal static InvalidOperationException TransactionCompletedButNotDisposed()
        {
            return Provider(SR.GetString(SR.ADP_TransactionCompletedButNotDisposed));
        }

        internal static ArgumentOutOfRangeException InvalidUserDefinedTypeSerializationFormat(Microsoft.SqlServer.Server.Format value)
        {
            return InvalidEnumerationValue(typeof(Microsoft.SqlServer.Server.Format), (int)value);
        }

        internal static ArgumentOutOfRangeException NotSupportedUserDefinedTypeSerializationFormat(Microsoft.SqlServer.Server.Format value, string method)
        {
            return NotSupportedEnumerationValue(typeof(Microsoft.SqlServer.Server.Format), value.ToString(), method);
        }

        internal static ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName, object value)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName, value, message);
            TraceExceptionAsReturnValue(e);
            return e;
        }

        internal static ArgumentException InvalidArgumentLength(string argumentName, int limit)
        {
            return Argument(SR.GetString(SR.ADP_InvalidArgumentLength, argumentName, limit));
        }

        internal static ArgumentException MustBeReadOnly(string argumentName)
        {
            return Argument(SR.GetString(SR.ADP_MustBeReadOnly, argumentName));
        }

        internal static InvalidOperationException InvalidMixedUsageOfSecureAndClearCredential()
        {
            return InvalidOperation(SR.GetString(SR.ADP_InvalidMixedUsageOfSecureAndClearCredential));
        }

        internal static ArgumentException InvalidMixedArgumentOfSecureAndClearCredential()
        {
            return Argument(SR.GetString(SR.ADP_InvalidMixedUsageOfSecureAndClearCredential));
        }

        internal static InvalidOperationException InvalidMixedUsageOfSecureCredentialAndIntegratedSecurity()
        {
            return InvalidOperation(SR.GetString(SR.ADP_InvalidMixedUsageOfSecureCredentialAndIntegratedSecurity));
        }

        internal static ArgumentException InvalidMixedArgumentOfSecureCredentialAndIntegratedSecurity()
        {
            return Argument(SR.GetString(SR.ADP_InvalidMixedUsageOfSecureCredentialAndIntegratedSecurity));
        }
        internal static InvalidOperationException InvalidMixedUsageOfAccessTokenAndIntegratedSecurity()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_InvalidMixedUsageOfAccessTokenAndIntegratedSecurity));
        }
        static internal InvalidOperationException InvalidMixedUsageOfAccessTokenAndUserIDPassword()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_InvalidMixedUsageOfAccessTokenAndUserIDPassword));
        }
        static internal Exception InvalidMixedUsageOfCredentialAndAccessToken()
        {
            return ADP.InvalidOperation(SR.GetString(SR.ADP_InvalidMixedUsageOfCredentialAndAccessToken));
        }
    }
}

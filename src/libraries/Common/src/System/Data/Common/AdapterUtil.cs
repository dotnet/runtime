// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace System.Data.Common
{
    internal static partial class ADP
    {
        // NOTE: Initializing a Task in SQL CLR requires the "UNSAFE" permission set (http://msdn.microsoft.com/en-us/library/ms172338.aspx)
        // Therefore we are lazily initializing these Tasks to avoid forcing customers to use the "UNSAFE" set when they are actually using no Async features
        private static Task<bool> _trueTask;
        internal static Task<bool> TrueTask => _trueTask ?? (_trueTask = Task.FromResult(true));

        private static Task<bool> _falseTask;
        internal static Task<bool> FalseTask => _falseTask ?? (_falseTask = Task.FromResult(false));

        internal const CompareOptions DefaultCompareOptions = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;

        static partial void TraceException(string trace, Exception e);

        internal static void TraceExceptionAsReturnValue(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|THROW> '{0}'", e);
        }

        internal static void TraceExceptionWithoutRethrow(Exception e)
        {
            Debug.Assert(ADP.IsCatchableExceptionType(e), "Invalid exception type, should have been re-thrown!");
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '%ls'\n", e);
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

        // the return value is true if the string was quoted and false if it was not
        // this allows the caller to determine if it is an error or not for the quotedString to not be quoted
        internal static bool RemoveStringQuotes(string quotePrefix, string quoteSuffix, string quotedString, out string unquotedString)
        {
            int prefixLength = quotePrefix != null ? quotePrefix.Length : 0;
            int suffixLength = quoteSuffix != null ? quoteSuffix.Length : 0;

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
                if (!quotedString.StartsWith(quotePrefix, StringComparison.Ordinal))
                {
                    unquotedString = quotedString;
                    return false;
                }
            }

            // is the suffix present?
            if (suffixLength > 0)
            {
                if (!quotedString.EndsWith(quoteSuffix, StringComparison.Ordinal))
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

        internal static ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, string value, string method)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_NotSupportedEnumerationValue, type.Name, value, method), type.Name);
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
            return ArgumentOutOfRange(SR.Format(SR.ADP_InvalidEnumerationValue, type.Name, value.ToString(CultureInfo.InvariantCulture)), type.Name);
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
        internal static ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(SR.Format(SR.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
        }

        //
        // DbConnectionOptions, DataAccess, SqlClient
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
        // : Stream
        //
        internal static Exception StreamClosed([CallerMemberName] string method = "")
        {
            return InvalidOperation(SR.Format(SR.ADP_StreamClosed, method));
        }

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

        //
        // Generic Data Provider Collection
        //
        internal static ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.Format(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
        }
        internal static ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
        {
            return Argument(SR.Format(SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
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
            return InvalidOperation(SR.Format(SR.ADP_InternalProviderError, (int)internalError));
        }

        //
        // : DbDataReader
        //
        internal static Exception DataReaderClosed([CallerMemberName] string method = "")
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

        internal static bool CompareInsensitiveInvariant(string strvalue, string strconst) =>
            0 == CultureInfo.InvariantCulture.CompareInfo.Compare(strvalue, strconst, CompareOptions.IgnoreCase);

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

        internal static Exception InvalidSeekOrigin(string parameterName)
        {
            return ArgumentOutOfRange(SR.ADP_InvalidSeekOrigin, parameterName);
        }

        internal static readonly bool IsWindowsNT = (PlatformID.Win32NT == Environment.OSVersion.Platform);
        internal static readonly bool IsPlatformNT5 = (ADP.IsWindowsNT && (Environment.OSVersion.Version.Major >= 5));

        internal static void SetCurrentTransaction(Transaction transaction)
        {
            Transaction.Current = transaction;
        }
    }
}
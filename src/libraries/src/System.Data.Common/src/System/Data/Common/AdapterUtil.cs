// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
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
        static internal InvalidOperationException InvalidOperation(string error)
        {
            InvalidOperationException e = new InvalidOperationException(error);
            return e;
        }
        static internal NotSupportedException NotSupported()
        {
            NotSupportedException e = new NotSupportedException();
            return e;
        }
        static internal void CheckArgumentLength(string value, string parameterName)
        {
            CheckArgumentNull(value, parameterName);
            if (0 == value.Length)
            {
                throw Argument(SR.Format(SR.ADP_EmptyString, parameterName));
            }
        }
        static internal void CheckArgumentNull(object value, string parameterName)
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
        }
        //
        // DbConnectionOptions, DataAccess
        //
        static internal ArgumentException ConnectionStringSyntax(int index)
        {
            return Argument(SR.Format(SR.ADP_ConnectionStringSyntax, index));
        }
        static internal ArgumentException KeywordNotSupported(string keyword)
        {
            return Argument(SR.Format(SR.ADP_KeywordNotSupported, keyword));
        }
        static internal ArgumentException InvalidKeyname(string parameterName)
        {
            return Argument(SR.Format(SR.ADP_InvalidKey), parameterName);
        }
        static internal ArgumentException InvalidValue(string parameterName)
        {
            return Argument(SR.Format(SR.ADP_InvalidValue), parameterName);
        }
        static internal ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(SR.Format(SR.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
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
            return InvalidOperation(SR.Format(SR.ADP_InternalProviderError, (int)internalError));
        }
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;

        internal const CompareOptions compareOptions = CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase;

        static internal bool IsEmpty(string str)
        {
            return string.IsNullOrEmpty(str);
        }
        static internal IndexOutOfRangeException IndexOutOfRange(string error)
        {
            IndexOutOfRangeException e = new IndexOutOfRangeException(error);
            return e;
        }
        static internal ArgumentOutOfRangeException InvalidSourceBufferIndex(int maxLen, long srcOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_InvalidSourceBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), srcOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
        }
        static internal ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
        {
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(parameterName, message);
            return e;
        }
        static internal Exception InvalidDataLength(long length)
        {
            return IndexOutOfRange(SR.Format(SR.SQL_InvalidDataLength, length.ToString(CultureInfo.InvariantCulture)));
        }
        static internal IndexOutOfRangeException InvalidBufferSizeOrIndex(int numBytes, int bufferIndex)
        {
            return IndexOutOfRange(SR.Format(SR.SQL_InvalidBufferSizeOrIndex, numBytes.ToString(CultureInfo.InvariantCulture), bufferIndex.ToString(CultureInfo.InvariantCulture)));
        }
        static internal ArgumentOutOfRangeException InvalidDestinationBufferIndex(int maxLen, int dstOffset, string parameterName)
        {
            return ArgumentOutOfRange(SR.Format(SR.ADP_InvalidDestinationBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), dstOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
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
                    columnNameArray[i] = string.Empty;
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

        static internal bool IsCatchableExceptionType(Exception e)
        {
            return !(e is NullReferenceException);
        }
    }
}

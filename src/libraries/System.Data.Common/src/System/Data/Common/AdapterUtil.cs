// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        // The class ADP defines the exceptions that are specific to the Adapters.f
        // The class contains functions that take the proper informational variables and then construct
        // the appropriate exception with an error string obtained from the resource Framework.txt.
        // The exception is then returned to the caller, so that the caller may then throw from its
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error messages.
        // The resource Framework.txt will ensure proper string text based on the appropriate
        // locale.

        static internal Task<T> CreatedTaskWithException<T>(Exception ex)
        {
            TaskCompletionSource<T> completion = new TaskCompletionSource<T>();
            completion.SetException(ex);
            return completion.Task;
        }

        static internal Task<T> CreatedTaskWithCancellation<T>()
        {
            TaskCompletionSource<T> completion = new TaskCompletionSource<T>();
            completion.SetCanceled();
            return completion.Task;
        }

        // NOTE: Initializing a Task in SQL CLR requires the "UNSAFE" permission set (http://msdn.microsoft.com/en-us/library/ms172338.aspx)
        // Therefore we are lazily initializing these Tasks to avoid forcing customers to use the "UNSAFE" set when they are actually using no Async features (See Dev11 
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

        static internal void TraceExceptionAsReturnValue(Exception e)
        {
        }
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
        static internal ArgumentNullException ArgumentNull(string parameter)
        {
            ArgumentNullException e = new ArgumentNullException(parameter);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal InvalidOperationException InvalidOperation(string error)
        {
            InvalidOperationException e = new InvalidOperationException(error);
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal NotSupportedException NotSupported()
        {
            NotSupportedException e = new NotSupportedException();
            TraceExceptionAsReturnValue(e);
            return e;
        }
        static internal void CheckArgumentLength(string value, string parameterName)
        {
            CheckArgumentNull(value, parameterName);
            if (0 == value.Length)
            {
                throw Argument(Res.GetString(Res.ADP_EmptyString, parameterName)); // MDAC 94859
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
            return Argument(Res.GetString(Res.ADP_ConnectionStringSyntax, index));
        }
        static internal ArgumentException KeywordNotSupported(string keyword)
        {
            return Argument(Res.GetString(Res.ADP_KeywordNotSupported, keyword));
        }
        static internal ArgumentException InvalidKeyname(string parameterName)
        {
            return Argument(Res.GetString(Res.ADP_InvalidKey), parameterName);
        }
        static internal ArgumentException InvalidValue(string parameterName)
        {
            return Argument(Res.GetString(Res.ADP_InvalidValue), parameterName);
        }
        static internal ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
        {
            return ADP.Argument(Res.GetString(Res.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
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
        internal const int DefaultConnectionTimeout = DbConnectionStringDefaults.ConnectTimeout;

        static internal bool IsEmpty(string str)
        {
            return ((null == str) || (0 == str.Length));
        }
    }
}

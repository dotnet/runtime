// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal static class TaskHelpers
    {
        internal struct VoidTaskResult
        {
        }

        internal static Task FromCancellation(CancellationToken cancellationToken)
        {
            return FromCancellation<VoidTaskResult>(cancellationToken);
        }

        internal static Task<T> FromCancellation<T>(CancellationToken cancellationToken)
        {
            Contract.Assert(cancellationToken.IsCancellationRequested, "Can only create a canceled task from a cancellation token if cancellation was requested.");

            return new Task<T>(new Func<T>(() => { return default(T); }), cancellationToken);
        }

        internal static Task FromException(Exception e)
        {
            return FromException<VoidTaskResult>(e);
        }

        internal static Task<T> FromException<T>(Exception e)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            tcs.SetException(e);
            return tcs.Task;
        }

        internal static Task CompletedTask()
        {
            TaskCompletionSource<VoidTaskResult> tcs = new TaskCompletionSource<VoidTaskResult>();
            tcs.SetResult(default(VoidTaskResult));
            return tcs.Task;
        }

        /// <summary>
        /// Wrap APM implementations while avoiding the overhead of closure and delegate allocations.
        /// Inspired by Task`1.FromAsyncTrim`2.
        /// </summary>
        /// <typeparam name="TInstance">The type of the first argument passed to the <paramref name="beginMethod"/> and
        ///     <paramref name="endMethod"/> delegates.</typeparam>
        /// <typeparam name="TArgs">The type of the second argument passed to the <paramref name="beginMethod"/> delegate.</typeparam>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="thisRef">Instance of caller they want as explicit first parameter to beginMethod and endMethod delegates.</param>
        /// <param name="args">Argument to beginMethod delegate.</param>
        /// <param name="beginMethod">The delegate that begins the asynchronous operation, and takes <paramref name="thisRef"/>
        ///     as an explicit first argument.</param>
        /// <param name="endMethod">The delegate that ends the asynchronous operation, and takes <paramref name="thisRef"/>
        ///     as an explicit first argument.</param>
        /// <returns></returns>
        internal static Task<TResult> FromAsyncTrim<TInstance, TArgs, TResult>(
            TInstance thisRef, TArgs args,
            Func<TInstance, TArgs, AsyncCallback, object, IAsyncResult> beginMethod,
            Func<TInstance, IAsyncResult, TResult> endMethod)
            where TInstance : class
        {
            Contract.Assert(thisRef != null, "Expected a non-null thisRef");
            Contract.Assert(beginMethod != null, "Expected a non-null beginMethod");
            Contract.Assert(endMethod != null, "Expected a non-null endMethod");

            // Because our method signature doesn't permit our caller to give us any asyncState to carry through the APM,
            // we use that to associate our caller's given instance variable with our caller's given beginMethod and
            // endMethod delegates which takes that instance variable as its first parameter.
            var asyncState = new FromAsyncTrimState<TInstance, TArgs, TResult>();
            asyncState._thisRef = thisRef;
            asyncState._beginMethod = beginMethod;
            asyncState._endMethod = endMethod;

            var taskFactory = new TaskFactory<TResult>();
            return taskFactory.FromAsync<TArgs>(
                (TArgs arg1, AsyncCallback callback, object state) =>
                {
                    var unpackedAsyncState = (FromAsyncTrimState<TInstance, TArgs, TResult>)state;
                    return unpackedAsyncState._beginMethod(unpackedAsyncState._thisRef, arg1, callback, state);
                },
                (IAsyncResult result) =>
                {
                    var unpackedAsyncState = ((FromAsyncTrimState<TInstance, TArgs, TResult>)result.AsyncState);
                    return unpackedAsyncState._endMethod(unpackedAsyncState._thisRef, result);
                },
                args,
                asyncState);
        }

        internal struct FromAsyncTrimState<TInstance, TArgs, TResult>
        {
            internal TInstance _thisRef;
            internal Func<TInstance, TArgs, AsyncCallback, object, IAsyncResult> _beginMethod;
            internal Func<TInstance, IAsyncResult, TResult> _endMethod;
        }
    }
}
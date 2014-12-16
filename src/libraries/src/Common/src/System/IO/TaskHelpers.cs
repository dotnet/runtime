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
        internal struct VoidTaskResult { }

        internal static Task FromCancellation(CancellationToken cancellationToken)
        {
            return FromCancellation<VoidTaskResult>(cancellationToken);
        }

        internal static Task<T> FromCancellation<T>(CancellationToken cancellationToken)
        {
            Contract.Assert(cancellationToken.IsCancellationRequested, "Can only create a canceled task from a cancellation token if cancellation was requested.");
            return new Task<T>(DelegateCache<T>.DefaultT ?? (DelegateCache<T>.DefaultT = () => default(T)), cancellationToken);
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
            return _completedTask ?? (_completedTask = Task.FromResult(default(VoidTaskResult)));
        }

        private static Task _completedTask;

        private static class DelegateCache<T>
        {
            internal static Func<T> DefaultT;
        }
    }
}
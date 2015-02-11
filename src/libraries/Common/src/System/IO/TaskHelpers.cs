// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Helpers for assemblies that don't yet depend on the System.Threading.Tasks contract
    /// that includes Task.CompletedTask, Task.FromCanceled, and Task.FromException.
    /// </summary>
    internal static class TaskHelpers
    {
        private struct VoidTaskResult { }

        internal static Task FromCancellation(CancellationToken cancellationToken)
        {
            return FromCancellation<VoidTaskResult>(cancellationToken);
        }

        internal static Task<T> FromCancellation<T>(CancellationToken cancellationToken)
        {
            Debug.Assert(cancellationToken.IsCancellationRequested, "Can only create a canceled task from a cancellation token if cancellation was requested.");
            return new Task<T>(DelegateCache<T>.DefaultT, cancellationToken);
        }

        internal static Task FromException(Exception e)
        {
            return FromException<VoidTaskResult>(e);
        }

        internal static Task<T> FromException<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(e);
            return tcs.Task;
        }

        internal static Task CompletedTask()
        {
            return s_completedTask ?? (s_completedTask = Task.FromResult(default(VoidTaskResult)));
        }

        private static Task s_completedTask;

        private static class DelegateCache<T>
        {
            private static Func<T> s_defaultT;

            internal static Func<T> DefaultT
            {
                get { return s_defaultT ?? (s_defaultT = () => default(T)); }
            }
        }
    }
}

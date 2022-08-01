// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.Channels
{
    internal partial class AsyncOperation<TResult>
    {
        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(static s => ((AsyncOperation<TResult>)s).SetCompletionAndInvokeContinuation(), this);

        private static void UnsafeQueueUserWorkItem(Action<object?> action, object? state) =>
            QueueUserWorkItem(action, state);

        private static void QueueUserWorkItem(Action<object?> action, object? state) =>
            Task.Factory.StartNew(action, state,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static CancellationTokenRegistration UnsafeRegister(CancellationToken cancellationToken, Action<object?> action, object? state) =>
            cancellationToken.Register(action, state);
    }
}

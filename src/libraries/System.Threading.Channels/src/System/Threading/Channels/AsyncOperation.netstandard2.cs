// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Channels
{
    internal partial class AsyncOperation<TResult>
    {
        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(static s => ((AsyncOperation<TResult>)s).SetCompletionAndInvokeContinuation(), this);

        private static void QueueUserWorkItem(Action<object?> action, object? state) =>
            ThreadPool.QueueUserWorkItem(new WaitCallback(action), state);

        private static CancellationTokenRegistration UnsafeRegister(CancellationToken cancellationToken, Action<object?> action, object? state)
        {
            if (ExecutionContext.IsFlowSuppressed())
            {
                return cancellationToken.Register(action, state);
            }

            using (ExecutionContext.SuppressFlow())
            {
                return cancellationToken.Register(action, state);
            }
        }
    }
}

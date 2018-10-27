// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Threading.Channels
{
    internal partial class AsyncOperation<TResult>
    {
        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            Task.Factory.StartNew(s => ((AsyncOperation<TResult>)s).SetCompletionAndInvokeContinuation(), this,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static void QueueUserWorkItem(Action<object> action, object state) =>
            Task.Factory.StartNew(action, state,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static CancellationTokenRegistration UnsafeRegister(CancellationToken cancellationToken, Action<object> action, object state) =>
            cancellationToken.Register(action, state);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Channels
{
    internal partial class AsyncOperation<TResult> : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => SetCompletionAndInvokeContinuation();

        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);

        private static void QueueUserWorkItem(Action<object> action, object state) =>
            ThreadPool.QueueUserWorkItem(action, state, preferLocal: false);

        private static CancellationTokenRegistration UnsafeRegister(CancellationToken cancellationToken, Action<object> action, object state) =>
            cancellationToken.UnsafeRegister(action, state);
    }
}

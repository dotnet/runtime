// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Channels
{
    internal partial class AsyncOperation : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => SetCompletionAndInvokeContinuation();

        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);

        private static void Unregister(CancellationTokenRegistration registration) =>
            registration.Unregister();
    }
}

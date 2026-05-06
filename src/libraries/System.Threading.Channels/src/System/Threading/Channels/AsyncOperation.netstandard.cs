// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Channels
{
    internal partial class AsyncOperation
    {
        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(static s => ((AsyncOperation)s).SetCompletionAndInvokeContinuation(), this);

        private static void Unregister(CancellationTokenRegistration registration) =>
            registration.Dispose();
    }
}

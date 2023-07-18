// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks
{
    public static class TimeProviderTaskExtensions
    {
        public static System.Threading.Tasks.Task Delay(this System.TimeProvider timeProvider, System.TimeSpan delay, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public static System.Threading.Tasks.Task<TResult> WaitAsync<TResult>(this System.Threading.Tasks.Task<TResult> task, System.TimeSpan timeout, System.TimeProvider timeProvider, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public static System.Threading.Tasks.Task WaitAsync(this System.Threading.Tasks.Task task, System.TimeSpan timeout, System.TimeProvider timeProvider, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public static System.Threading.CancellationTokenSource CreateCancellationTokenSource(this System.TimeProvider timeProvider, System.TimeSpan delay) { throw null; }
    }
}

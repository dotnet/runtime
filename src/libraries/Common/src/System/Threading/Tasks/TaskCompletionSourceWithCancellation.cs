// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Threading.Tasks
{
    /// <summary>
    /// A <see cref="TaskCompletionSource{TResult}"/> that supports cancellation registration so that any
    /// <seealso cref="OperationCanceledException"/>s contain the relevant <see cref="CancellationToken"/>,
    /// while also avoiding unnecessary allocations for closure captures.
    /// </summary>
    internal class TaskCompletionSourceWithCancellation<T> : TaskCompletionSource<T>
    {
        private CancellationToken _cancellationToken;

        public TaskCompletionSourceWithCancellation() : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }

        private void OnCancellation()
        {
            TrySetCanceled(_cancellationToken);
        }

        public async ValueTask<T> WaitWithCancellationAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            using (cancellationToken.UnsafeRegister(static s => ((TaskCompletionSourceWithCancellation<T>)s!).OnCancellation(), this))
            {
                return await Task.ConfigureAwait(false);
            }
        }
    }
}

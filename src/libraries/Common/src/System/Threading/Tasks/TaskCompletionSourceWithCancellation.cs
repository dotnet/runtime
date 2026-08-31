// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks
{
    /// <summary>
    /// A <see cref="TaskCompletionSource{TResult}"/> that supports cancellation registration so that any
    /// <seealso cref="OperationCanceledException"/>s contain the relevant <see cref="CancellationToken"/>,
    /// while also avoiding unnecessary allocations for closure captures.
    /// </summary>
    internal class TaskCompletionSourceWithCancellation<T> : TaskCompletionSource<T>
    {
        public TaskCompletionSourceWithCancellation() : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
        }

        public async ValueTask<T> WaitWithCancellationAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.UnsafeRegister(static (s, cancellationToken) => ((TaskCompletionSourceWithCancellation<T>)s!).TrySetCanceled(cancellationToken), this))
            {
                return await Task.ConfigureAwait(false);
            }
        }

        public T WaitWithCancellation(CancellationToken cancellationToken)
        {
            using (cancellationToken.UnsafeRegister(static (s, cancellationToken) => ((TaskCompletionSourceWithCancellation<T>)s!).TrySetCanceled(cancellationToken), this))
            {
                return Task.GetAwaiter().GetResult();
            }
        }

        public ValueTask<T> WaitWithCancellationAsync(bool async, CancellationToken cancellationToken)
        {
            return async ?
                WaitWithCancellationAsync(cancellationToken) :
                new ValueTask<T>(WaitWithCancellation(cancellationToken));
        }
    }
}

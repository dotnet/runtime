// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    /// <summary>
    /// An awaitable object that represents an asynchronous read operation
    /// </summary>
    public struct PipeAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly IPipeAwaiter<T> _awaiter;

        public PipeAwaiter(IPipeAwaiter<T> awaiter)
        {
            _awaiter = awaiter;
        }
        /// <summary>
        /// Gets whether the async operation being awaited is completed.
        /// </summary>
        public bool IsCompleted => _awaiter.IsCompleted;

        /// <summary>
        /// Ends the await on the completed <see cref="PipeAwaiter{T}"/>.
        /// </summary>
        public T GetResult() => _awaiter.GetResult();

        /// <summary>
        /// Gets an awaiter used to await this <see cref="PipeAwaiter{T}"/>.
        /// </summary>
        public PipeAwaiter<T> GetAwaiter() => this;

        /// <inheritdoc />
        public void UnsafeOnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

        /// <inheritdoc />
        public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);
    }
}

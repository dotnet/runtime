// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading.Tasks
{
    /// <summary>Provides a set of static methods for configuring <see cref="Task"/>-related behaviors on asynchronous enumerables and disposables.</summary>
    public static partial class TaskAsyncEnumerableExtensions
    {
        /// <summary>
        /// Converts an <see cref="IAsyncEnumerable{T}"/> instance into an <see cref="IEnumerable{T}"/> that enumerates elements in a blocking manner.
        /// </summary>
        /// <typeparam name="T">The type of the objects being iterated.</typeparam>
        /// <param name="source">The source enumerable being iterated.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> instance that enumerates the source <see cref="IAsyncEnumerable{T}"/> in a blocking manner.</returns>
        /// <remarks>
        /// This method is implemented by using deferred execution. The underlying <see cref="IAsyncEnumerable{T}"/> will not be enumerated
        /// unless the returned <see cref="IEnumerable{T}"/> is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        /// Async enumeration does not happen in the background; each MoveNext call will invoke the underlying <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> exactly once.
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static IEnumerable<T> ToBlockingEnumerable<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator(cancellationToken);
            // A ManualResetEventSlim variant that lets us reuse the same
            // awaiter callback allocation across the entire enumeration.
            ManualResetEventWithAwaiterSupport? mres = null;

            try
            {
                while (true)
                {
#pragma warning disable CA2012 // Use ValueTasks correctly
                    ValueTask<bool> moveNextTask = enumerator.MoveNextAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly

                    if (!moveNextTask.IsCompleted)
                    {
                        (mres ??= new ManualResetEventWithAwaiterSupport()).Wait(moveNextTask.ConfigureAwait(false).GetAwaiter());
                        Debug.Assert(moveNextTask.IsCompleted);
                    }

                    if (!moveNextTask.Result)
                    {
                        yield break;
                    }

                    yield return enumerator.Current;
                }
            }
            finally
            {
                ValueTask disposeTask = enumerator.DisposeAsync();

                if (!disposeTask.IsCompleted)
                {
                    (mres ?? new ManualResetEventWithAwaiterSupport()).Wait(disposeTask.ConfigureAwait(false).GetAwaiter());
                    Debug.Assert(disposeTask.IsCompleted);
                }

                disposeTask.GetAwaiter().GetResult();
            }
        }

        private sealed class ManualResetEventWithAwaiterSupport : ManualResetEventSlim
        {
            private readonly Action _onCompleted;

            public ManualResetEventWithAwaiterSupport()
            {
                _onCompleted = Set;
            }

            [UnsupportedOSPlatform("browser")]
            public void Wait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
            {
                awaiter.UnsafeOnCompleted(_onCompleted);
                Wait();
                Reset();
            }
        }
    }
}

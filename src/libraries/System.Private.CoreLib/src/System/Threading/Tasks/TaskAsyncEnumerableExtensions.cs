// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>Provides a set of static methods for configuring <see cref="Task"/>-related behaviors on asynchronous enumerables and disposables.</summary>
    public static class TaskAsyncEnumerableExtensions
    {
        /// <summary>Configures how awaits on the tasks returned from an async disposable will be performed.</summary>
        /// <param name="source">The source async disposable.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        /// <returns>The configured async disposable.</returns>
        public static ConfiguredAsyncDisposable ConfigureAwait(this IAsyncDisposable source, bool continueOnCapturedContext) =>
            new ConfiguredAsyncDisposable(source, continueOnCapturedContext);

        /// <summary>Configures how awaits on the tasks returned from an async iteration will be performed.</summary>
        /// <typeparam name="T">The type of the objects being iterated.</typeparam>
        /// <param name="source">The source enumerable being iterated.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        /// <returns>The configured enumerable.</returns>
        public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(
            this IAsyncEnumerable<T> source, bool continueOnCapturedContext) =>
            new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext, cancellationToken: default);

        /// <summary>Sets the <see cref="CancellationToken"/> to be passed to <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> when iterating.</summary>
        /// <typeparam name="T">The type of the objects being iterated.</typeparam>
        /// <param name="source">The source enumerable being iterated.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>The configured enumerable.</returns>
        public static ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(
            this IAsyncEnumerable<T> source, CancellationToken cancellationToken) =>
            new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext: true, cancellationToken);

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
        /// </remarks>
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
                        (mres ??= new()).UnsafeWait(moveNextTask.ConfigureAwait(false).GetAwaiter());
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
                    (mres ?? new()).UnsafeWait(disposeTask.ConfigureAwait(false).GetAwaiter());
                    Debug.Assert(disposeTask.IsCompleted);
                }

                disposeTask.GetAwaiter().GetResult();
            }
        }

        private class ManualResetEventWithAwaiterSupport : ManualResetEventSlim
        {
            private readonly Action _onCompleted;

            public ManualResetEventWithAwaiterSupport()
            {
                _onCompleted = Set;
            }

            public void UnsafeWait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
            {
                awaiter.UnsafeOnCompleted(_onCompleted);
                Wait();
                Reset();
            }
        }
    }
}

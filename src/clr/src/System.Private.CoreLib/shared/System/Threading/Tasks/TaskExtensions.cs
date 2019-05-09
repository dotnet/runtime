// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>Provides a set of static methods for working with specific kinds of <see cref="Task"/> instances.</summary>
    public static class TaskExtensions
    {
        /// <summary>Creates a proxy <see cref="Task"/> that represents the asynchronous operation of a <see cref="Task{Task}"/>.</summary>
        /// <param name="task">The <see cref="Task{Task}"/> to unwrap.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation of the provided <see cref="Task{Task}"/>.</returns>
        public static Task Unwrap(this Task<Task> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // If the task hasn't completed or was faulted/canceled, wrap it in an unwrap promise. Otherwise,
            // it completed successfully.  Return its inner task to avoid unnecessary wrapping, or if the inner
            // task is null, return a canceled task to match the same semantics as CreateUnwrapPromise.
            return
                !task.IsCompletedSuccessfully ? Task.CreateUnwrapPromise<VoidTaskResult>(task, lookForOce: false) :
                task.Result ??
                Task.FromCanceled(new CancellationToken(true));
        }

        /// <summary>Creates a proxy <see cref="Task{TResult}"/> that represents the asynchronous operation of a wrapped <see cref="Task{TResult}"/>.</summary>
        /// <param name="task">The wrapped <see cref="Task{TResult}"/> to unwrap.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation of the provided wrapped <see cref="Task{TResult}"/>.</returns>
        public static Task<TResult> Unwrap<TResult>(this Task<Task<TResult>> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // If the task hasn't completed or was faulted/canceled, wrap it in an unwrap promise. Otherwise,
            // it completed successfully.  Return its inner task to avoid unnecessary wrapping, or if the inner
            // task is null, return a canceled task to match the same semantics as CreateUnwrapPromise.
            return
                !task.IsCompletedSuccessfully ? Task.CreateUnwrapPromise<TResult>(task, lookForOce: false) :
                task.Result ??
                Task.FromCanceled<TResult>(new CancellationToken(true));
        }

        // TODO: Remove the below three methods once corefx has consumed a build with them in their new TaskAsyncEnumerableExtensions location.

        /// <summary>Configures how awaits on the tasks returned from an async disposable will be performed.</summary>
        /// <param name="source">The source async disposable.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        /// <returns>The configured async disposable.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ConfiguredAsyncDisposable ConfigureAwait(this IAsyncDisposable source, bool continueOnCapturedContext) =>
            new ConfiguredAsyncDisposable(source, continueOnCapturedContext);

        /// <summary>Configures how awaits on the tasks returned from an async iteration will be performed.</summary>
        /// <typeparam name="T">The type of the objects being iterated.</typeparam>
        /// <param name="source">The source enumerable being iterated.</param>
        /// <param name="continueOnCapturedContext">Whether to capture and marshal back to the current context.</param>
        /// <returns>The configured enumerable.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(
            this IAsyncEnumerable<T> source, bool continueOnCapturedContext) =>
            new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext, cancellationToken: default);

        /// <summary>Sets the <see cref="CancellationToken"/> to be passed to <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> when iterating.</summary>
        /// <typeparam name="T">The type of the objects being iterated.</typeparam>
        /// <param name="source">The source enumerable being iterated.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>The configured enumerable.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(
            this IAsyncEnumerable<T> source, CancellationToken cancellationToken) =>
            new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext: true, cancellationToken);
    }
}

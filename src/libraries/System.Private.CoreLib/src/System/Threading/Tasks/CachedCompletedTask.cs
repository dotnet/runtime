// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Encapsulates the logic of caching the last synchronously completed task.
    /// Used in classes like <see cref="MemoryStream"/> to reduce allocations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct CachedCompletedTask<T>
    {
        private Task<T>? _task;

        /// <summary>
        /// Gets a completed <see cref="Task{TResult}"/> whose result is <paramref name="result"/>.
        /// </summary>
        /// <remarks>
        /// This method will try to return an already cached task if available.
        /// </remarks>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> GetTask(T result)
        {
            Task<T>? task;
#pragma warning disable CA1849 // Call async methods when in an async method
            if ((task = _task) is not null && EqualityComparer<T>.Default.Equals(task.Result, result))
#pragma warning restore CA1849 // Call async methods when in an async method
            {
                Debug.Assert(task.IsCompletedSuccessfully,
                    "Expected that a stored last task completed successfully");
                return task;
            }
            else
            {
                return _task = Task.FromResult(result);
            }
        }
    }
}

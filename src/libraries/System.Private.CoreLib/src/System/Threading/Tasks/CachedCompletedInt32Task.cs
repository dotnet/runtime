// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Encapsulates the logic of caching the last synchronously completed task of integer.
    /// Used in classes like <see cref="MemoryStream"/> to reduce allocations.
    /// </summary>
    internal struct CachedCompletedInt32Task
    {
        private Task<int>? _task;

        /// <summary>
        /// Gets a completed <see cref="Task{Int32}"/> whose result is <paramref name="result"/>.
        /// </summary>
        /// <remarks>
        /// This method will try to return an already cached task if available.
        /// </remarks>
        /// <param name="result">The task's result.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> GetTask(int result)
        {
            Task<int>? task;
#pragma warning disable CA1849 // Call async methods when in an async method
            if ((task = _task) is not null && task.Result == result)
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

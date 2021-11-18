// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            // The BCL has its own task cache for common values. We will use it if we can.
            if (result is null)
            {
                return Task.FromResult(result);
            }
            else if (typeof(T).IsValueType)
            {
                if (typeof(T) == typeof(bool))
                {
                    return Task.FromResult(result);
                }
                else if (typeof(T) == typeof(int))
                {
                    int value = (int)(object)result!;
                    if ((uint)(value - TaskCache.InclusiveInt32Min) < (TaskCache.ExclusiveInt32Max - TaskCache.InclusiveInt32Min))
                    {
                        return Unsafe.As<Task<TResult>>(Task.FromResult(value));
                    }
                }
            }
            // Because it is a mutable struct, we will read from it only once.
            else if ((task = _task) is not null && task.Result == result)
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

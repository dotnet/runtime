// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides a cache of closed generic tasks for async methods.</summary>
    internal static class AsyncTaskCache
    {
        /// <summary>A cached Task{Boolean}.Result == true.</summary>
        internal static readonly Task<bool> s_trueTask = CreateCacheableTask(result: true);
        /// <summary>A cached Task{Boolean}.Result == false.</summary>
        internal static readonly Task<bool> s_falseTask = CreateCacheableTask(result: false);
        /// <summary>The cache of Task{Int32}.</summary>
        internal static readonly Task<int>[] s_int32Tasks = CreateInt32Tasks();
        /// <summary>The minimum value, inclusive, for which we want a cached task.</summary>
        internal const int InclusiveInt32Min = -1;
        /// <summary>The maximum value, exclusive, for which we want a cached task.</summary>
        internal const int ExclusiveInt32Max = 9;

        /// <summary>true if we should use reusable boxes for async completions of ValueTask methods; false if we should use tasks.</summary>
        /// <remarks>
        /// We rely on tiered compilation turning this into a const and doing dead code elimination to make checks on this efficient.
        /// It's also required for safety that this value never changes once observed, as Unsafe.As casts are employed based on its value.
        /// </remarks>
        internal static readonly bool s_valueTaskPoolingEnabled = GetPoolAsyncValueTasksSwitch();
        /// <summary>Maximum number of boxes that are allowed to be cached per state machine type.</summary>
        internal static readonly int s_valueTaskPoolingCacheSize = GetPoolAsyncValueTasksLimitValue();

        private static bool GetPoolAsyncValueTasksSwitch()
        {
            string? value = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKS");
            return value != null && (bool.IsTrueStringIgnoreCase(value) || value.Equals("1"));
        }

        private static int GetPoolAsyncValueTasksLimitValue() =>
            int.TryParse(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_THREADING_POOLASYNCVALUETASKSLIMIT"), out int result) && result > 0 ?
                result :
                Environment.ProcessorCount * 4; // arbitrary default value

        /// <summary>Creates a non-disposable task.</summary>
        /// <typeparam name="TResult">Specifies the result type.</typeparam>
        /// <param name="result">The result for the task.</param>
        /// <returns>The cacheable task.</returns>
        internal static Task<TResult> CreateCacheableTask<TResult>(TResult? result) =>
            new Task<TResult>(false, result, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, default);

        /// <summary>Creates an array of cached tasks for the values in the range [INCLUSIVE_MIN,EXCLUSIVE_MAX).</summary>
        private static Task<int>[] CreateInt32Tasks()
        {
            Debug.Assert(ExclusiveInt32Max >= InclusiveInt32Min, "Expected max to be at least min");

            var tasks = new Task<int>[ExclusiveInt32Max - InclusiveInt32Min];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = CreateCacheableTask(i + InclusiveInt32Min);
            }

            return tasks;
        }
    }
}

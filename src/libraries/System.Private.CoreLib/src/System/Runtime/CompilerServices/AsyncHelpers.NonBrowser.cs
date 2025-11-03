// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        /// <summary>
        /// This method is intended to be used by Roslyn-generated async entry point.
        /// On multi-threaded environments, it would block and await the specified task and return when the task has completed.
        /// On single-threaded environments (for example, browser), it would register a continuation and yield to the browser event loop immediately.
        /// </summary>
        /// <param name="task">The result from the main entrypoint to await.</param>
        public static void HandleAsyncEntryPoint(Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// This method is intended to be used by Roslyn-generated async entry point.
        /// On multi-threaded environments, it would block and await the specified task and return when the task has completed.
        /// On single-threaded environments (e.g., browser), it would register a continuation and yield to the browser event loop immediately.
        /// </summary>
        /// <param name="task">The result from the main entrypoint to await.</param>
        public static int HandleAsyncEntryPoint(Task<int> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}

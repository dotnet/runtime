// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        /// <summary>
        /// This method is intended to be used by a compiler-generated async entry point.
        /// </summary>
        /// <remarks>
        /// On multi-threaded environments, this method blocks and awaits the specified task, and returns when the task has completed.
        /// On single-threaded environments (for example, browser), it registers a continuation and yields to the browser event loop immediately.
        /// </remarks>
        /// <param name="task">The result from the main entry point to await.</param>
        public static void HandleAsyncEntryPoint(Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// This method is intended to be used by a compiler-generated async entry point.
        /// </summary>
        /// <remarks>
        /// On multi-threaded environments, this method blocks and awaits the specified task, and returns when the task has completed.
        /// On single-threaded environments (for example, browser), it registers a continuation and yields to the browser event loop immediately.
        /// </remarks>
        /// <param name="task">The result from the main entry point to await.</param>
        public static int HandleAsyncEntryPoint(Task<int> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}

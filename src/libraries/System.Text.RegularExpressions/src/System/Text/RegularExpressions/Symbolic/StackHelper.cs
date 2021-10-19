// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides tools for avoiding stack overflows.</summary>
    internal static class StackHelper
    {
        // Queues the supplied delegate to the thread pool, then block waiting for it to complete.
        // It does so in a way that prevents task inlining (which would defeat the purpose) but that
        // also plays nicely with the thread pool's sync-over-async aggressive thread injection policies.

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">The function to invoke.</param>
        public static T CallOnEmptyStack<T>(Func<T> func) =>
            Task.Run(func)
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided action on the stack of a different thread pool thread.</summary>
        /// <param name="action">The action to invoke.</param>
        public static void CallOnEmptyStack(Action action) =>
            Task.Run(action)
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();
    }
}

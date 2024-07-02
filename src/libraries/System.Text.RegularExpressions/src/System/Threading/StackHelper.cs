// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>Provides tools for avoiding stack overflows.</summary>
    internal static class StackHelper
    {
        /// <summary>Tries to ensure there is sufficient stack to execute the average .NET function.</summary>
        public static bool TryEnsureSufficientExecutionStack()
        {
#if NET
            return RuntimeHelpers.TryEnsureSufficientExecutionStack();
#else
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                return true;
            }
            catch
            {
                return false;
            }
#endif
        }

        // Queues the supplied delegate to the thread pool, then block waiting for it to complete.
        // It does so in a way that prevents task inlining (which would defeat the purpose) but that
        // also plays nicely with the thread pool's sync-over-async aggressive thread injection policies.

        /// <summary>Calls the provided action on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        public static void CallOnEmptyStack<TArg1>(Action<TArg1> action, TArg1 arg1) =>
            Task.Run(() => action(arg1))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided action on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        public static void CallOnEmptyStack<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2) =>
            Task.Run(() => action(arg1, arg2))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided action on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the function.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="arg3">The third argument to pass to the action.</param>
        public static void CallOnEmptyStack<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3) =>
            Task.Run(() => action(arg1, arg2, arg3))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the function.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth argument to pass to the function.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="arg3">The third argument to pass to the action.</param>
        /// <param name="arg4">The fourth argument to pass to the action.</param>
        public static void CallOnEmptyStack<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) =>
            Task.Run(() => action(arg1, arg2, arg3, arg4))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the function.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth argument to pass to the function.</typeparam>
        /// <typeparam name="TArg5">The type of the fifth argument to pass to the function.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="arg3">The third argument to pass to the action.</param>
        /// <param name="arg4">The fourth argument to pass to the action.</param>
        /// <param name="arg5">The fifth argument to pass to the action.</param>
        public static void CallOnEmptyStack<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) =>
            Task.Run(() => action(arg1, arg2, arg3, arg4, arg5))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the function.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth argument to pass to the function.</typeparam>
        /// <typeparam name="TArg5">The type of the fifth argument to pass to the function.</typeparam>
        /// <typeparam name="TArg6">The type of the sixth argument to pass to the function.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="arg3">The third argument to pass to the action.</param>
        /// <param name="arg4">The fourth argument to pass to the action.</param>
        /// <param name="arg5">The fifth argument to pass to the action.</param>
        /// <param name="arg6">The sixth argument to pass to the action.</param>
        public static void CallOnEmptyStack<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) =>
            Task.Run(() => action(arg1, arg2, arg3, arg4, arg5, arg6))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The function to invoke.</param>
        public static TResult CallOnEmptyStack<TResult>(Func<TResult> func) =>
            Task.Run(() => func())
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        public static TResult CallOnEmptyStack<TArg1, TResult>(Func<TArg1, TResult> func, TArg1 arg1) =>
            Task.Run(() => func(arg1))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        public static TResult CallOnEmptyStack<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2) =>
            Task.Run(() => func(arg1, arg2))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the function.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        public static TResult CallOnEmptyStack<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3) =>
            Task.Run(() => func(arg1, arg2, arg3))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();

        /// <summary>Calls the provided function on the stack of a different thread pool thread.</summary>
        /// <typeparam name="TArg1">The type of the first argument to pass to the function.</typeparam>
        /// <typeparam name="TArg2">The type of the second argument to pass to the function.</typeparam>
        /// <typeparam name="TArg3">The type of the third argument to pass to the function.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth argument to pass to the function.</typeparam>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        public static TResult CallOnEmptyStack<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) =>
            Task.Run(() => func(arg1, arg2, arg3, arg4))
                .ContinueWith(t => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
                .GetAwaiter().GetResult();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Internal.Threading.Tasks
{
    //
    // An internal contract that exposes just enough async debugger support needed by the AsTask() extension methods in the WindowsRuntimeSystemExtensions class.
    //
    public static class AsyncCausalitySupport
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToActiveTasks(Task task)
        {
            if (Task.s_asyncDebuggingEnabled)
                Task.AddToActiveTasks(task);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveFromActiveTasks(Task task)
        {
            if (Task.s_asyncDebuggingEnabled)
                Task.RemoveFromActiveTasks(task);
        }

        public static bool LoggingOn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return AsyncCausalityTracer.LoggingOn;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TraceOperationCreation(Task task, string operationName)
        {
            AsyncCausalityTracer.TraceOperationCreation(task, operationName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TraceOperationCompletedSuccess(Task task)
        {
            AsyncCausalityTracer.TraceOperationCompletion(task, AsyncCausalityStatus.Completed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TraceOperationCompletedError(Task task)
        {
            AsyncCausalityTracer.TraceOperationCompletion(task, AsyncCausalityStatus.Error);
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Threading.Tasks
{
    internal enum CausalityTraceLevel
    {
        Required = 0,
        Important = 1,
        Verbose = 2,
    }

    internal enum AsyncCausalityStatus
    {
        Started = 0,
        Completed = 1,
        Canceled = 2,
        Error = 3,
    }

    internal enum CausalityRelation
    {
        AssignDelegate = 0,
        Join = 1,
        Choice = 2,
        Cancel = 3,
        Error = 4,
    }

    internal enum CausalitySynchronousWork
    {
        CompletionNotification = 0,
        ProgressNotification = 1,
        Execution = 2,
    }

    //
    // Empty implementation of AsyncCausality events
    //
    internal static class AsyncCausalityTracer
    {
        public static bool LoggingOn => false;

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void EnableToETW(bool enabled)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceOperationCreation(CausalityTraceLevel traceLevel, int taskId, string operationName, ulong relatedContext)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceOperationCompletion(CausalityTraceLevel traceLevel, int taskId, AsyncCausalityStatus status)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceOperationRelation(CausalityTraceLevel traceLevel, int taskId, CausalityRelation relation)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, int taskId, CausalitySynchronousWork work)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySynchronousWork work)
        {
        }
    }
}

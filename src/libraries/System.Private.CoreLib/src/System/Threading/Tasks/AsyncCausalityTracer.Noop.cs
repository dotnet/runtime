// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Threading.Tasks
{
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
        public static void TraceOperationCreation(Task task, string operationName)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceOperationCompletion(Task task, AsyncCausalityStatus status)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceOperationRelation(Task task, CausalityRelation relation)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceSynchronousWorkStart(Task task, CausalitySynchronousWork work)
        {
        }

        [Conditional("NOOP_ASYNCCASUALITYTRACER")]
        public static void TraceSynchronousWorkCompletion(CausalitySynchronousWork work)
        {
        }
    }
}

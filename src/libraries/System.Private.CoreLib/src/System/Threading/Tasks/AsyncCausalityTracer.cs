// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    internal static class AsyncCausalityTracer
    {
        internal static void Enable()
        {
            Interlocked.Increment(ref listenerCnt);
        }

        internal static void Disable()
        {
            Interlocked.Decrement(ref listenerCnt);
        }

        internal static bool LoggingOn => listenerCnt > 0;
        private static int listenerCnt = 0;

        // The TraceXXX methods should be called only if LoggingOn property returned true
        //
        [MethodImpl(MethodImplOptions.NoInlining)] // Tracking is slow path. Disable inlining for it.
        internal static void TraceOperationCreation(Task task, string operationName)
        {
            try
            {
                TplEventSource.Log.TraceOperationBegin(task.Id, operationName, RelatedContext: 0);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TraceOperationCompletion(Task task, AsyncCausalityStatus status)
        {
            try
            {
                TplEventSource.Log.TraceOperationEnd(task.Id, status);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TraceOperationRelation(Task task, CausalityRelation relation)
        {
            try
            {
                TplEventSource.Log.TraceOperationRelation(task.Id, relation);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TraceSynchronousWorkStart(Task task, CausalitySynchronousWork work)
        {
            try
            {
                TplEventSource.Log.TraceSynchronousWorkBegin(task.Id, work);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TraceSynchronousWorkCompletion(CausalitySynchronousWork work)
        {
            try
            {
                TplEventSource.Log.TraceSynchronousWorkEnd(work);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        // we should catch and log exceptions but never propagate them.
        private static void LogAndDisable(Exception ex)
        {
            Disable();
            Debugger.Log(0, "AsyncCausalityTracer", ex.ToString());
        }
    }
}

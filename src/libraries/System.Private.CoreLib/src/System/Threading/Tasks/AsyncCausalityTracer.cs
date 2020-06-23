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
            LoggingOn = true;
        }

        internal static void Disable()
        {
            LoggingOn = false;
        }

        internal static bool LoggingOn = false;

        // The TraceXXX methods should be called only if LoggingOn property returned true
        //
        [MethodImpl(MethodImplOptions.NoInlining)] // Tracking is slow path. Disable inlining for it.
        internal static void TraceOperationCreation(Task task, string operationName)
        {
            try
            {
                if (LoggingOn)
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
                if (LoggingOn)
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
                if (LoggingOn)
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
                if (LoggingOn)
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
                if (LoggingOn)
                    TplEventSource.Log.TraceSynchronousWorkEnd(work);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        // fix for 796185: leaking internal exceptions to customers,
        // we should catch and log exceptions but never propagate them.
        private static void LogAndDisable(Exception ex)
        {
            Disable();
            Debugger.Log(0, "AsyncCausalityTracer", ex.ToString());
        }
    }
}

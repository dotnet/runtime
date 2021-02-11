// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Data.Common
{
    internal static partial class ADP
    {

        internal static Timer UnsafeCreateTimer(TimerCallback callback, object? state, int dueTime, int period)
        {
            // Don't capture the current ExecutionContext and its AsyncLocals onto
            // a global timer causing them to live forever
            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                return new Timer(callback, state, dueTime, period);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                    ExecutionContext.RestoreFlow();
            }
        }
    }
}

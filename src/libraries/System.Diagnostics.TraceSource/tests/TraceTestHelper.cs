// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace System.Diagnostics.TraceSourceTests
{
    static class TraceTestHelper
    {
        /// <summary>
        /// Resets the static state of the trace objects before a unit test.
        /// </summary>
        public static void ResetState()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new DefaultTraceListener());
            Trace.AutoFlush = false;
            Trace.IndentLevel = 0;
            Trace.IndentSize = 4;
            Trace.UseGlobalLock = true;
            // Trace holds on to instances through weak refs
            // this is intented to clean those up.
            GC.Collect();
            Trace.Refresh();
        }
    }
}

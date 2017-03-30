// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Runtime.InteropServices;

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
            // this is intended to clean those up.
            GC.Collect();
            Trace.Refresh();
        }
    }
}

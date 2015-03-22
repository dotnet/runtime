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
            TraceSwitch.RefreshAll();
        }

        public static String NewLine
        {
            get
            {
                return new StringBuilder().AppendLine().ToString();
                // NOTE: Want to use this line:                
                //return System.Environment.NewLine;
                // However, I get this compile error, that I have absoltely no idea how to resolve.
                // Error	1	The type name 'Environment' could not be found in the namespace 'System'. This type has been forwarded to assembly 'System.Runtime.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' Consider adding a reference to that assembly.	C:\Users\Mark\Documents\GitHub\corefx\src\System.Diagnostics.TraceSource\tests\TraceTestHelper.cs	30	31	System.Diagnostics.TraceSource.Tests
            }
        }
    }
}

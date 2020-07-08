// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    internal static partial class TraceListenerHelpers
    {
        private static volatile bool s_hasProcessId;
        private static volatile int s_processId;

        internal static int GetProcessId()
        {
            if (!s_hasProcessId)
            {
                s_processId = (int)Interop.Kernel32.GetCurrentProcessId();
                s_hasProcessId = true;
            }

            return s_processId;
        }

        internal static string GetProcessName()
        {

            if (s_processName == null)
            {
                using (var process = Process.GetCurrentProcess())
                {
                    s_processName = process.ProcessName;
                }
            }

            return s_processName;
        }
    }
}

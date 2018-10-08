// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    internal static partial class TraceListenerHelpers
    {
        internal static int GetProcessId()
        {
            // Whereas the Win32 implementation caches the GetProcessId result, the Unix 
            // implementation doesn't so as to avoid problems with fork'd child processes 
            // ending up returning the same id as the parent.
            return Interop.Sys.GetPid();
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

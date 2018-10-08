// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#if uap
                // In UAP getting process infos is not supported and the only way to get the process name is through Process.GetCurrentProcess.ProcessName:
                // https://github.com/dotnet/corefx/blob/561f555830e4eaef1fa015796690915e6f4923da/src/System.Diagnostics.Process/src/System/Diagnostics/ProcessManager.Uap.cs#L82
                s_processName = string.Empty;
#else
                using (var process = Process.GetCurrentProcess())
                {
                    s_processName = process.ProcessName;
                }
#endif
            }

            return s_processName;
        }
    }
}

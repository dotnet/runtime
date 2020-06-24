// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Diagnostics
{
    internal static partial class TraceListenerHelpers
    {
        private static volatile string? s_processName;

        internal static int GetThreadId()
        {
            return Environment.CurrentManagedThreadId;
        }

        internal static int GetProcessId()
        {
            int id;
            using (var process = Process.GetCurrentProcess())
            {
                id = process.Id;
            }

            return id;
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

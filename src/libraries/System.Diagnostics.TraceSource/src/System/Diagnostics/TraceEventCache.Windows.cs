// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public partial class TraceEventCache
    {
        private static volatile int s_processId;
        private static volatile bool s_hasProcessId;

        internal static int GetProcessId()
        {
            if (!s_hasProcessId)
            {
                s_processId = (int)Interop.Kernel32.GetCurrentProcessId();
                s_hasProcessId = true;
            }

            return s_processId;
        }
    }
}


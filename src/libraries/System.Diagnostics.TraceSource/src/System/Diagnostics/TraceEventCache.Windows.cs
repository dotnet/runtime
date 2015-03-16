// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                s_processId = (int)Interop.mincore.GetCurrentProcessId();
                s_hasProcessId = true;
            }

            return s_processId;
        }
    }
}


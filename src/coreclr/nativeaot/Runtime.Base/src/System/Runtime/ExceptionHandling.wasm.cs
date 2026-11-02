// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    internal static unsafe partial class EH
    {
        [RuntimeExport("RhGetCurrentThreadStackTrace")]
        private static int RhGetCurrentThreadStackTrace(IntPtr[] outputBuffer)
        {
            return 0;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        public static int[] GetProcessIds()
        {
            throw new PlatformNotSupportedException();
        }

        private static ProcessInfo CreateProcessInfo(int pid)
        {
            throw new PlatformNotSupportedException();
        }

        private static string GetProcPath(int processId)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

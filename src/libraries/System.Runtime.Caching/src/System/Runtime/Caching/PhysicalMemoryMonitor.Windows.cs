// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Caching.Configuration;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Caching
{
    internal sealed partial class PhysicalMemoryMonitor : MemoryMonitor
    {
        protected override int GetCurrentPressure()
        {
            Interop.Kernel32.MEMORYSTATUSEX memoryStatusEx = default;
            memoryStatusEx.dwLength = (uint)Marshal.SizeOf<Interop.Kernel32.MEMORYSTATUSEX>();
            if (Interop.Kernel32.GlobalMemoryStatusEx(out memoryStatusEx) == 0)
            {
                return 0;
            }

            int memoryLoad = (int)memoryStatusEx.dwMemoryLoad;
            return memoryLoad;
        }
    }
}

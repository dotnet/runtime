// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Caching.Configuration;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Caching
{
    internal sealed partial class PhysicalMemoryMonitor : MemoryMonitor
    {
        protected override unsafe int GetCurrentPressure()
        {
            Interop.Kernel32.MEMORYSTATUSEX memoryStatus = default;
            memoryStatus.dwLength = (uint)sizeof(Interop.Kernel32.MEMORYSTATUSEX);
            if (Interop.Kernel32.GlobalMemoryStatusEx(&memoryStatus) == Interop.BOOL.FALSE)
            {
                return 0;
            }

            int memoryLoad = (int)memoryStatus.dwMemoryLoad;
            return memoryLoad;
        }
    }
}

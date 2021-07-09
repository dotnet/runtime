// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;
using System.Security;
using System.Runtime.InteropServices;


namespace System.Runtime.Caching
{
    internal abstract partial class MemoryMonitor
    {
#pragma warning disable CA1810 // explicit static cctor
        static unsafe MemoryMonitor()
        {
            Interop.Kernel32.MEMORYSTATUSEX memoryStatus = default;
            memoryStatus.dwLength = (uint)sizeof(Interop.Kernel32.MEMORYSTATUSEX);
            if (Interop.Kernel32.GlobalMemoryStatusEx(&memoryStatus) != Interop.BOOL.FALSE)
            {
                s_totalPhysical = (long)memoryStatus.ullTotalPhys;
                s_totalVirtual = (long)memoryStatus.ullTotalVirtual;
            }
        }
#pragma warning restore CA1810
    }
}

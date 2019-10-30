// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Security;
using System.Runtime.InteropServices;


namespace System.Runtime.Caching
{
    internal abstract partial class MemoryMonitor
    {
#pragma warning disable CA1810 // explicit static cctor
        static MemoryMonitor()
        {
            Interop.Kernel32.MEMORYSTATUSEX memoryStatusEx = default;
            memoryStatusEx.dwLength = (uint)Marshal.SizeOf<Interop.Kernel32.MEMORYSTATUSEX>();
            if (Interop.Kernel32.GlobalMemoryStatusEx(out memoryStatusEx) != 0)
            {
                s_totalPhysical = (long)memoryStatusEx.ullTotalPhys;
                s_totalVirtual = (long)memoryStatusEx.ullTotalVirtual;
            }
        }
#pragma warning restore CA1810
    }
}

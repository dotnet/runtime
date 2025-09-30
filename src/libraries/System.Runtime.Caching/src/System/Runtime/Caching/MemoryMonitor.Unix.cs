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
#if NETCOREAPP
#pragma warning disable CA1810 // explicit static cctor
        static MemoryMonitor()
        {
            // Get stats from GC.
            GCMemoryInfo memInfo = GC.GetGCMemoryInfo();

            // s_totalPhysical/TotalPhysical are used in two places:
            //   1. PhysicalMemoryMonitor - to determine the high pressure level. We really just need to know if we have more memory than a 2005-era x86 machine.
            //   2. CacheMemoryMonitor - for setting the "Auto" memory limit of the cache size - which is never enforced anyway because we don't have SRef's in .NET Core.
            // Which is to say, it's OK if 'TotalAvailableMemoryBytes' is not an exact representation of the physical memory on the machine, as long as it is in the ballpark of magnitude.
            s_totalPhysical = memInfo.TotalAvailableMemoryBytes;

            // s_totalVirtual/TotalVirtual on the other hand is only used to decide if an x86 Windows machine is running in /3GB mode or not...
            //  ... but only so it can appropriately set the "Auto" memory limit of the cache size - which again, is never enforced in .Net Core.
            //  So we don't need to worry about it here.
            //s_totalVirtual = default(long);
        }
#pragma warning restore CA1810
#endif
    }
}

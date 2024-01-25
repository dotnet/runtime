// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Refresh
{
    internal static class Program
    {
        private static int Main()
        {
            long hundred_mb = 100 * 1024 * 1024;
            long two_hundred_mb = 2 * hundred_mb;
            AppContext.SetData("GCHeapHardLimit", (ulong)hundred_mb);
            GC.RefreshMemoryLimit();
            GCMemoryInfo gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes != hundred_mb)
            {
                Console.WriteLine("Fail");
                return 101;
            }
            AppContext.SetData("GCHeapHardLimit", (ulong)two_hundred_mb);
            GC.RefreshMemoryLimit();
            gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes != two_hundred_mb)
            {
                Console.WriteLine("Fail");
                return 101;
            }
            AppContext.SetData("GCHeapHardLimit", (ulong)hundred_mb);
            GC.RefreshMemoryLimit();
            gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes != hundred_mb)
            {
                Console.WriteLine("Fail");
                return 101;
            }
            Console.WriteLine("Pass");
            return 100;
        }
    }
}

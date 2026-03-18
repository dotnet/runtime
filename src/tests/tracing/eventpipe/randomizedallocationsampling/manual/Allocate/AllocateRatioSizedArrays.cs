// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Allocate
{
    public class AllocateRatioSizedArrays : IAllocations
    {
        public void Allocate(int count)
        {
            // We can't keep the objects in memory, just keep their size
            List<int> sizes= new List<int>(count * 5);

            var gcCount = GC.CollectionCount(0);

            for (int i = 0; i < count; i++)
            {
                var bytes1 = new byte[1024];
                bytes1[1] = 1;
                sizes.Add(bytes1.Length);
                var bytes2 = new byte[10240];
                bytes2[2] = 2;
                sizes.Add(bytes2.Length);
                var bytes3 = new byte[102400];
                bytes3[3] = 3;
                sizes.Add(bytes3.Length);
                var bytes4 = new byte[1024000];
                bytes4[4] = 4;
                sizes.Add(bytes4.Length);
                var bytes5 = new byte[10240000];
                bytes5[5] = 5;
                sizes.Add(bytes5.Length);
            }

            Console.WriteLine($"+ {GC.CollectionCount(0) - gcCount} collections");

            long totalAllocated = 0;
            foreach (int size in sizes)
            {
                totalAllocated += size;
            }
            Console.WriteLine($"{sizes.Count} arrays for {totalAllocated / 1024} KB");
        }
    }
}

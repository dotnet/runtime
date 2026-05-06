// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0169 // Remove unused private members
#pragma warning disable IDE0049 // Simplify Names

using System;
using System.Collections.Generic;

namespace Allocate
{
    public class AllocateSmallAndBig : IAllocations
    {
        public void Allocate(int count)
        {
            Dictionary<string, AllocStats> allocations = Initialize();
            List<Object0> objects = new List<Object0>(1024 * 1024);

            AllocateSmallThenBig(count/2, objects, allocations);
            Console.WriteLine();
            AllocateBigThenSmall(count/2, objects, allocations);
            Console.WriteLine();
        }

        private void AllocateSmallThenBig(int count, List<Object0> objects, Dictionary<string, AllocStats> allocations)
        {
            for (int i = 0; i < count; i++)
            {
                // allocate from smaller to larger
                objects.Add(new Object24());
                objects.Add(new Object32());
                objects.Add(new Object48());
                objects.Add(new Object80());
                objects.Add(new Object144());
            }

            allocations[nameof(Object24)].Count = count;
            allocations[nameof(Object24)].Size = count * 24;
            allocations[nameof(Object32)].Count = count;
            allocations[nameof(Object32)].Size = count * 32;
            allocations[nameof(Object48)].Count = count;
            allocations[nameof(Object48)].Size = count * 48;
            allocations[nameof(Object80)].Count = count;
            allocations[nameof(Object80)].Size = count * 80;
            allocations[nameof(Object144)].Count = count;
            allocations[nameof(Object144)].Size = count * 144;

            DumpAllocations(allocations);
            Clear(allocations);
            objects.Clear();
        }

        private void AllocateBigThenSmall(int count, List<Object0> objects, Dictionary<string, AllocStats> allocations)
        {
            for (int i = 0; i < count; i++)
            {
                // allocate from larger to smaller
                objects.Add(new Object144());
                objects.Add(new Object80());
                objects.Add(new Object48());
                objects.Add(new Object32());
                objects.Add(new Object24());
            }

            allocations[nameof(Object24)].Count = count;
            allocations[nameof(Object24)].Size = count * 24;
            allocations[nameof(Object32)].Count = count;
            allocations[nameof(Object32)].Size = count * 32;
            allocations[nameof(Object48)].Count = count;
            allocations[nameof(Object48)].Size = count * 48;
            allocations[nameof(Object80)].Count = count;
            allocations[nameof(Object80)].Size = count * 80;
            allocations[nameof(Object144)].Count = count;
            allocations[nameof(Object144)].Size = count * 144;

            DumpAllocations(allocations);
            Clear(allocations);
            objects.Clear();
        }

        private Dictionary<string, AllocStats> Initialize()
        {
            var allocations = new Dictionary<string, AllocStats>(16);
            allocations[nameof(Object24)] = new AllocStats();
            allocations[nameof(Object32)] = new AllocStats();
            allocations[nameof(Object48)] = new AllocStats();
            allocations[nameof(Object80)] = new AllocStats();
            allocations[nameof(Object144)] = new AllocStats();

            Clear(allocations);
            return allocations;
        }

        private void Clear(Dictionary<string, AllocStats> allocations)
        {
            allocations[nameof(Object24)].Count = 0;
            allocations[nameof(Object24)].Size = 0;
            allocations[nameof(Object32)].Count = 0;
            allocations[nameof(Object32)].Size = 0;
            allocations[nameof(Object48)].Count = 0;
            allocations[nameof(Object48)].Size = 0;
            allocations[nameof(Object80)].Count = 0;
            allocations[nameof(Object80)].Size = 0;
            allocations[nameof(Object144)].Count = 0;
            allocations[nameof(Object144)].Size = 0;
        }

        private void DumpAllocations(Dictionary<string, AllocStats> objects)
        {
            Console.WriteLine("Allocations start");
            foreach (var allocation in objects)
            {
                Console.WriteLine($"{allocation.Key}={allocation.Value.Count},{allocation.Value.Size}");
            }

            Console.WriteLine("Allocations end");
        }

        internal class AllocStats
        {
            public int Count { get; set; }
            public long Size { get; set; }
        }

        internal class Object0
        {
        }

        internal class Object24 : Object0
        {
            private readonly UInt32 _x1;
            private readonly UInt32 _x2;
        }

        internal class Object32 : Object0
        {
            private readonly UInt64 _x1;
            private readonly UInt64 _x2;
        }

        internal class Object48 : Object0
        {
            private readonly UInt64 _x1;
            private readonly UInt64 _x2;
            private readonly UInt64 _x3;
            private readonly UInt64 _x4;
        }

        internal class Object80 : Object0
        {
            private readonly UInt64 _x1;
            private readonly UInt64 _x2;
            private readonly UInt64 _x3;
            private readonly UInt64 _x4;
            private readonly UInt64 _x5;
            private readonly UInt64 _x6;
            private readonly UInt64 _x7;
            private readonly UInt64 _x8;
        }

        internal class Object144 : Object0
        {
            private readonly UInt64 _x1;
            private readonly UInt64 _x2;
            private readonly UInt64 _x3;
            private readonly UInt64 _x4;
            private readonly UInt64 _x5;
            private readonly UInt64 _x6;
            private readonly UInt64 _x7;
            private readonly UInt64 _x8;
            private readonly UInt64 _x9;
            private readonly UInt64 _x10;
            private readonly UInt64 _x11;
            private readonly UInt64 _x12;
            private readonly UInt64 _x13;
            private readonly UInt64 _x14;
            private readonly UInt64 _x15;
            private readonly UInt64 _x16;
        }
    }
}
#pragma warning restore IDE0049 // Simplify Names
#pragma warning restore CS0169 // Remove unused private members

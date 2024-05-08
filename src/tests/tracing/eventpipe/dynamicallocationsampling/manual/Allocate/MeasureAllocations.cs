#pragma warning disable CS0169 // Remove unused private members
#pragma warning disable IDE0049 // Simplify Names

using System;
using System.Collections.Generic;

namespace Allocate
{
    public class MeasureAllocations
    {
        public void Allocate(int count)
        {
            Dictionary<string, AllocStats> allocations = Initialize();
            List<Object0> objects = new List<Object0>(1024 * 1024);

            AllocateSmallThenBig(count, objects, allocations);
            Console.WriteLine();
            AllocateBigThenSmall(count, objects, allocations);
            Console.WriteLine();
        }

        private void AllocateSmallThenBig(int count, List<Object0> objects, Dictionary<string, AllocStats> allocations)
        {
            for (int i = 0; i < count; i++)
            {
                // allocate from smaller to larger
                objects.Add(new Object8());
                objects.Add(new Object16());
                objects.Add(new Object32());
                objects.Add(new Object64());
                objects.Add(new Object128());
            }

            allocations[nameof(Object8)].Count = count;
            allocations[nameof(Object8)].Size = count * 24;
            allocations[nameof(Object16)].Count = count;
            allocations[nameof(Object16)].Size = count * 32;
            allocations[nameof(Object32)].Count = count;
            allocations[nameof(Object32)].Size = count * 48;
            allocations[nameof(Object64)].Count = count;
            allocations[nameof(Object64)].Size = count * 80;
            allocations[nameof(Object128)].Count = count;
            allocations[nameof(Object128)].Size = count * 144;

            DumpAllocations(allocations);
            Clear(allocations);
            objects.Clear();
        }

        private void AllocateBigThenSmall(int count, List<Object0> objects, Dictionary<string, AllocStats> allocations)
        {
            for (int i = 0; i < count; i++)
            {
                // allocate from larger to smaller
                objects.Add(new Object128());
                objects.Add(new Object64());
                objects.Add(new Object32());
                objects.Add(new Object16());
                objects.Add(new Object8());
            }

            allocations[nameof(Object8)].Count = count;
            allocations[nameof(Object8)].Size = count * 24;
            allocations[nameof(Object16)].Count = count;
            allocations[nameof(Object16)].Size = count * 32;
            allocations[nameof(Object32)].Count = count;
            allocations[nameof(Object32)].Size = count * 48;
            allocations[nameof(Object64)].Count = count;
            allocations[nameof(Object64)].Size = count * 80;
            allocations[nameof(Object128)].Count = count;
            allocations[nameof(Object128)].Size = count * 144;

            DumpAllocations(allocations);
            Clear(allocations);
            objects.Clear();
        }

        private Dictionary<string, AllocStats> Initialize()
        {
            var allocations = new Dictionary<string, AllocStats>(16);
            allocations[nameof(Object8)] = new AllocStats();
            allocations[nameof(Object16)] = new AllocStats();
            allocations[nameof(Object32)] = new AllocStats();
            allocations[nameof(Object64)] = new AllocStats();
            allocations[nameof(Object128)] = new AllocStats();

            Clear(allocations);
            return allocations;
        }

        private void Clear(Dictionary<string, AllocStats> allocations)
        {
            allocations[nameof(Object8)].Count = 0;
            allocations[nameof(Object8)].Size = 0;
            allocations[nameof(Object16)].Count = 0;
            allocations[nameof(Object16)].Size = 0;
            allocations[nameof(Object32)].Count = 0;
            allocations[nameof(Object32)].Size = 0;
            allocations[nameof(Object64)].Count = 0;
            allocations[nameof(Object64)].Size = 0;
            allocations[nameof(Object128)].Count = 0;
            allocations[nameof(Object128)].Size = 0;
        }

        //private (Object0 Instance, long Size) Allocate(int type)
        //{
        //    if (type == 0)
        //    {
        //        return (new Object8(), 24);
        //    }
        //    else
        //    if (type == 1)
        //    {
        //        return (new Object16(), 32);
        //    }
        //    else
        //    if (type == 2)
        //    {
        //        return (new Object32(), 48);
        //    }
        //    else
        //    if (type == 3)
        //    {
        //        return (new Object64(), 80);
        //    }
        //    else
        //    if (type == 4)
        //    {
        //        return (new Object128(), 144);
        //    }
        //    else
        //    {
        //        throw new ArgumentOutOfRangeException("type", type, "Type cannot be greater than 128");
        //    }
        //}

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

        internal class Object8 : Object0
        {
            private readonly UInt32 _x1;
            private readonly UInt32 _x2;
        }

        internal class Object16 : Object0
        {
            private readonly UInt64 _x1;
            private readonly UInt64 _x2;
        }

        internal class Object32 : Object0
        {
            private readonly UInt64 _x1;
            private readonly UInt64 _x2;
            private readonly UInt64 _x3;
            private readonly UInt64 _x4;
        }

        internal class Object64 : Object0
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

        internal class Object128 : Object0
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
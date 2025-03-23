// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0169 // Remove unused private members
#pragma warning disable IDE0049 // Simplify Names

using System;
using System.Collections.Generic;
using System.Threading;

namespace Allocate
{
    public class ThreadedAllocations : IAllocations
    {
        public void Allocate(int count)
        {
            List<Object0> objects1 = new List<Object0>(1024 * 1024);
            List<Object0> objects2 = new List<Object0>(1024 * 1024);

            Thread[] threads = new Thread[2];
            threads[0] = new Thread(() => Allocate1(count, objects1));
            threads[1] = new Thread(() => Allocate2(count, objects2));

            for (int i = 0; i < threads.Length; i++) { threads[i].Start(); }
            for (int i = 0; i < threads.Length; i++) { threads[i].Join(); }

            Console.WriteLine($"Allocated {objects1.Count + objects2.Count} objects");
        }

        private void Allocate1(int count, List<Object0> objects)
        {
            for (int i = 0; i < count; i++)
            {
                objects.Add(new Object24());
                objects.Add(new Object48());
                objects.Add(new Object72());
            }
        }

        private void Allocate2(int count, List<Object0> objects)
        {
            for (int i = 0; i < count; i++)
            {
                objects.Add(new Object32());
                objects.Add(new Object64());
                objects.Add(new Object96());
            }
        }

        internal class Object0
        {
        }

        internal class Object24 : Object0
        {
            private readonly UInt16 _x1;
            private readonly UInt16 _x2;
            private readonly UInt16 _x3;
        }

        internal class Object32 : Object0
        {
            private readonly UInt16 _x1;
            private readonly UInt16 _x2;
            private readonly UInt16 _x3;
            private readonly UInt16 _x4;
            private readonly UInt16 _x5;
            private readonly UInt16 _x6;
            private readonly UInt16 _x7;
        }

        internal class Object48 : Object0
        {
            private readonly UInt16 _x1;
            private readonly UInt16 _x2;
            private readonly UInt16 _x3;
            private readonly UInt16 _x4;
            private readonly UInt16 _x5;
            private readonly UInt16 _x6;
            private readonly UInt16 _x7;
            private readonly UInt16 _x8;
            private readonly UInt16 _x9;
            private readonly UInt16 _x10;
            private readonly UInt16 _x11;
            private readonly UInt16 _x12;
            private readonly UInt16 _x13;
            private readonly UInt16 _x14;
            private readonly UInt16 _x15;
        }

        internal class Object64 : Object0
        {
            private readonly UInt16 _x1;
            private readonly UInt16 _x2;
            private readonly UInt16 _x3;
            private readonly UInt16 _x4;
            private readonly UInt16 _x5;
            private readonly UInt16 _x6;
            private readonly UInt16 _x7;
            private readonly UInt16 _x8;
            private readonly UInt16 _x9;
            private readonly UInt16 _x10;
            private readonly UInt16 _x11;
            private readonly UInt16 _x12;
            private readonly UInt16 _x13;
            private readonly UInt16 _x14;
            private readonly UInt16 _x15;
            private readonly UInt16 _x16;
            private readonly UInt16 _x17;
            private readonly UInt16 _x18;
            private readonly UInt16 _x19;
            private readonly UInt16 _x20;
            private readonly UInt16 _x21;
            private readonly UInt16 _x22;
            private readonly UInt16 _x23;
            private readonly UInt16 _x24;
        }

        internal class Object72 : Object0
        {
            private readonly UInt16 _x1;
            private readonly UInt16 _x2;
            private readonly UInt16 _x3;
            private readonly UInt16 _x4;
            private readonly UInt16 _x5;
            private readonly UInt16 _x6;
            private readonly UInt16 _x7;
            private readonly UInt16 _x8;
            private readonly UInt16 _x9;
            private readonly UInt16 _x10;
            private readonly UInt16 _x11;
            private readonly UInt16 _x12;
            private readonly UInt16 _x13;
            private readonly UInt16 _x14;
            private readonly UInt16 _x15;
            private readonly UInt16 _x16;
            private readonly UInt16 _x17;
            private readonly UInt16 _x18;
            private readonly UInt16 _x19;
            private readonly UInt16 _x20;
            private readonly UInt16 _x21;
            private readonly UInt16 _x22;
            private readonly UInt16 _x23;
            private readonly UInt16 _x24;
            private readonly UInt16 _x25;
            private readonly UInt16 _x26;
            private readonly UInt16 _x27;
            private readonly UInt16 _x28;
        }

        internal class Object96 : Object0
        {
            private readonly UInt32 _x1;
            private readonly UInt32 _x2;
            private readonly UInt32 _x3;
            private readonly UInt32 _x4;
            private readonly UInt32 _x5;
            private readonly UInt32 _x6;
            private readonly UInt32 _x7;
            private readonly UInt32 _x8;
            private readonly UInt32 _x9;
            private readonly UInt32 _x10;
            private readonly UInt32 _x11;
            private readonly UInt32 _x12;
            private readonly UInt32 _x13;
            private readonly UInt32 _x14;
            private readonly UInt32 _x15;
            private readonly UInt32 _x16;
            private readonly UInt32 _x17;
            private readonly UInt32 _x18;
            private readonly UInt32 _x19;
            private readonly UInt32 _x20;
        }
    }
}


#pragma warning restore IDE0049 // Simplify Names
#pragma warning restore CS0169 // Remove unused private members

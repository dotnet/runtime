// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace LOHAlignment
{
    // Objects on the large object heap are padded so that their data - where the first
    // element of an array lives - is aligned on a cache line. The padding has to survive
    // allocations from the free list as well as a LOH compaction.
    public class LOHAlignment
    {
        private const int Alignment = 64;

        private static readonly Random s_random = new Random(1234);

        private static unsafe nint DataAddress(Array array) =>
            (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(array));

        private static void VerifyAligned(string what, Array array)
        {
            nint address = DataAddress(array);
            Assert.True((address % Alignment) == 0,
                $"The data of a {what} on the LOH is at 0x{address:x}, {address % Alignment} bytes off a {Alignment} byte boundary.");
        }

        private static Array AllocateLarge()
        {
            // ~85KB is what makes an object go on the LOH, allocate a bit more than that
            // and vary the size so that objects end up at all kinds of offsets.
            int size = 100000 + s_random.Next(0, 40000);
            return (s_random.Next(0, 3)) switch
            {
                0 => new byte[size],
                1 => new double[size / sizeof(double)],
                _ => new object[size / IntPtr.Size],
            };
        }

        private static void VerifyAll(string what, List<Array> arrays)
        {
            foreach (Array array in arrays)
            {
                if (array != null)
                {
                    VerifyAligned(what, array);
                }
            }
        }

        [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
        public static void ObjectsOnLOHAreAligned()
        {
            var live = new List<Array>();

            // Objects allocated at the end of a segment/region.
            for (int i = 0; i < 200; i++)
            {
                Array array = AllocateLarge();
                VerifyAligned("newly allocated array", array);
                live.Add(array);
            }

            // Objects allocated out of the free list. Punch holes into the LOH first so
            // that the free list has entries of all kinds of sizes.
            for (int i = 0; i < live.Count; i++)
            {
                if ((i % 3) != 0)
                {
                    live[i] = null;
                }
            }
            GC.Collect();

            for (int i = 0; i < 200; i++)
            {
                Array array = AllocateLarge();
                VerifyAligned("array allocated from the free list", array);
                if ((i % 4) == 0)
                {
                    live.Add(array);
                }
            }

            // Objects that are moved by a LOH compaction, with a few pinned ones in
            // between which do not move at all.
            var pins = new List<GCHandle>();
            for (int i = 0; i < live.Count; i += 13)
            {
                // Only arrays without references can be pinned.
                if (live[i] is byte[] or double[])
                {
                    pins.Add(GCHandle.Alloc(live[i], GCHandleType.Pinned));
                }
            }

            for (int pass = 0; pass < 3; pass++)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                VerifyAll("compacted array", live);

                for (int i = 0; i < 50; i++)
                {
                    Array array = AllocateLarge();
                    VerifyAligned("array allocated after a compaction", array);
                    if ((i % 5) == 0)
                    {
                        live.Add(array);
                    }
                    else if (live.Count > 10)
                    {
                        live[s_random.Next(live.Count)] = null;
                    }
                }
            }

            foreach (GCHandle pin in pins)
            {
                pin.Free();
            }

            GC.KeepAlive(live);
        }

        [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
        public static unsafe void StringsOnLOHAreAligned()
        {
            // What the GC aligns is where an array keeps its data. The characters of a
            // string start a bit before that (on 64 bit their offset isn't even a multiple
            // of the pointer size) and objects themselves are only aligned on a pointer
            // boundary, so this is how far off the boundary the characters end up.
            int skew = (IntPtr.Size * 2) - RuntimeHelpers.OffsetToStringData;

            var live = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                string str = new string('a', 50000 + s_random.Next(0, 20000));
                live.Add(str);
                VerifyAligned(str, skew, "string");
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            foreach (string str in live)
            {
                VerifyAligned(str, skew, "compacted string");
            }
        }

        private static unsafe void VerifyAligned(string str, int skew, string what)
        {
            fixed (char* chars = str)
            {
                nint address = (nint)chars;
                Assert.True(((address + skew) % Alignment) == 0,
                    $"The characters of a {what} on the LOH are at 0x{address:x}, {(address + skew) % Alignment} bytes off a {Alignment} byte boundary.");
            }
        }
    }
}

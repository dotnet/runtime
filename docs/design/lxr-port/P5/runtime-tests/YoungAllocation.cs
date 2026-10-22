// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// P22 (D2): LXR allocates through the stock bump-pointer helpers. A young object has no start bit; the pause finds an
// interior root into it by walking the objects of the buffer that the thread filled. These scenarios keep objects
// alive only through interior pointers (byrefs and spans over fresh arrays), allocate from many threads while pauses
// stop the threads at arbitrary points, and check sizes and content across the pauses.
internal static partial class Program
{
    // The reference goes back to the caller; the array itself is garbage as far as the stack map is concerned, so the
    // only thing that keeps it alive is an interior pointer into a young object.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref long YoungLongRef(int seed, int length, int index)
    {
        long[] array = new long[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = seed * 31L + i;
        }
        return ref array[index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long VerifyYoungLongRef(ref long element, int seed, int length, int index)
    {
        long sum = 0;
        for (int i = 0; i < length; i++)
        {
            long value = Unsafe.Add(ref element, i - index);
            if (value != seed * 31L + i)
            {
                return -1;
            }
            sum += value;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref char YoungCharRef(int seed, int length, int index)
    {
        string text = string.Create(length, seed, static (span, state) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = (char)('A' + (state + i) % 26);
            }
        });
        return ref Unsafe.Add(ref Unsafe.AsRef(in text.GetPinnableReference()), index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool VerifyYoungCharRef(ref char element, int seed, int length, int index)
    {
        for (int i = 0; i < length; i++)
        {
            if (Unsafe.Add(ref element, i - index) != (char)('A' + (seed + i) % 26))
            {
                return false;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Span<byte> YoungSpan(int seed, int length)
    {
        byte[] array = new byte[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = (byte)(seed + i * 7);
        }
        return array.AsSpan(length / 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool VerifyYoungSpan(Span<byte> span, int seed, int length)
    {
        int skipped = length / 4;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] != (byte)(seed + (i + skipped) * 7))
            {
                return false;
            }
        }
        return span.Length == length - skipped;
    }

    // One round of interior roots: fresh arrays, strings and spans whose objects nothing else refers to, pauses while
    // only the interior pointers are alive, and a check of every element afterwards.
    private static int YoungInteriorRound(int seed, Collect? collect)
    {
        Random random = new(seed);
        int bad = 0;
        for (int iteration = 0; iteration < 1500; iteration++)
        {
            int length = iteration % 9 == 0 ? 2500 + random.Next(1500) : 1 + random.Next(500);
            int index = random.Next(length);
            ref long element = ref YoungLongRef(iteration, length, index);
            int charLength = 1 + random.Next(700);
            int charIndex = random.Next(charLength);
            ref char letter = ref YoungCharRef(iteration, charLength, charIndex);
            int spanLength = 8 + random.Next(2000);
            Span<byte> span = YoungSpan(iteration, spanLength);
            if (iteration % 4 == 0)
            {
                AllocateGarbage(40, 60 + random.Next(300));
            }
            if (collect is not null && iteration % 3 == 0)
            {
                if (collect(1) != 0)
                {
                    return -1;
                }
            }
            if (VerifyYoungLongRef(ref element, iteration, length, index) < 0 ||
                !VerifyYoungCharRef(ref letter, iteration, charLength, charIndex) ||
                !VerifyYoungSpan(span, iteration, spanLength))
            {
                bad++;
            }
        }
        return bad;
    }

    private static void VerifyYoungInteriorRoots(Collect collect)
    {
        int start = GC.CollectionCount(0);
        Check(YoungInteriorRound(1, collect) == 0,
            "An interior pointer into a young object lost its object, its size or its content across a pause.");
        // The same, while several threads allocate and a controller thread pauses them at arbitrary points.
        int failures = 0;
        bool stop = false;
        Thread[] workers = new Thread[4];
        for (int index = 0; index < workers.Length; index++)
        {
            int id = index;
            workers[index] = new Thread(() =>
            {
                int rounds = 0;
                while (!Volatile.Read(ref stop) && rounds < 6)
                {
                    int result = YoungInteriorRound(100 + id * 17 + rounds, null);
                    if (result != 0)
                    {
                        Interlocked.Increment(ref failures);
                    }
                    rounds++;
                }
            });
            workers[index].Start();
        }
        int pauses = 0;
        Stopwatch watch = Stopwatch.StartNew();
        bool alive = true;
        while (alive && watch.ElapsedMilliseconds < 20000)
        {
            Thread.Sleep(1);
            if (collect(1) != 0)
            {
                Interlocked.Increment(ref failures);
                break;
            }
            pauses++;
            alive = false;
            foreach (Thread worker in workers)
            {
                alive |= worker.IsAlive;
            }
        }
        Volatile.Write(ref stop, true);
        foreach (Thread worker in workers)
        {
            worker.Join();
        }
        Check(Volatile.Read(ref failures) == 0, "An interior pointer into a young object failed while threads allocated.");
        Check(pauses >= 5, $"Only {pauses} pauses interrupted the allocating threads.");
        Console.WriteLine($"young-interior pauses={pauses} collections={GC.CollectionCount(0) - start}");
    }

    private sealed class StormNode
    {
        internal object? First;
        internal object? Second;
        internal long Tag;
    }

    private sealed class StormFinalizable
    {
        internal static long s_created;
        internal static long s_finalized;
        internal long Tag;
        internal long[] Payload;

        internal StormFinalizable(long tag)
        {
            Tag = tag;
            Payload = new long[1 + (int)(tag % 13)];
            Payload[0] = tag;
            Interlocked.Increment(ref s_created);
        }

        ~StormFinalizable()
        {
            if (Payload[0] == Tag)
            {
                Interlocked.Increment(ref s_finalized);
            }
        }
    }

    // Creates an object of a random kind and size. The checksum is a function of the content.
    private static object MakeStormObject(Random random, long tag, out long checksum)
    {
        switch (random.Next(10))
        {
            case 0:
            {
                StormNode node = new() { Tag = tag, First = new byte[random.Next(1, 200)] };
                node.Second = new StormNode { Tag = tag + 1 };
                checksum = tag * 2 + 1;
                return node;
            }
            case 1:
            {
                long[] values = new long[random.Next(1, 400)];
                long sum = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = tag + i;
                    sum += values[i];
                }
                checksum = sum;
                return values;
            }
            case 2:
            {
                string text = string.Create(random.Next(1, 300), (int)tag, static (span, state) =>
                {
                    for (int i = 0; i < span.Length; i++)
                    {
                        span[i] = (char)('a' + (state + i) % 26);
                    }
                });
                long sum = 0;
                foreach (char letter in text)
                {
                    sum += letter;
                }
                checksum = sum;
                return text;
            }
            case 3:
            {
                object[] items = new object[random.Next(1, 40)];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = new StormNode { Tag = tag + i };
                }
                checksum = tag * items.Length + items.Length * (items.Length - 1) / 2;
                return items;
            }
            case 4:
            case 5:
                checksum = tag;
                return new StormFinalizable(tag);
            case 6:
            {
                byte[] pinned = random.Next(8) == 0 ? GC.AllocateArray<byte>(random.Next(1, 3000), pinned: true) :
                    new byte[random.Next(1, 3000)];
                long sum = 0;
                for (int i = 0; i < pinned.Length; i++)
                {
                    pinned[i] = (byte)(tag + i);
                    sum += pinned[i];
                }
                checksum = sum;
                return pinned;
            }
            case 7:
            {
                byte[] large = new byte[random.Next(60) == 0 ? 90_000 + random.Next(30_000) : random.Next(3001, 40_000)];
                large[0] = (byte)tag;
                large[^1] = (byte)(tag >> 3);
                checksum = (byte)tag + (byte)(tag >> 3) * 256L + large.Length;
                return large;
            }
            default:
                checksum = tag;
                return (object)tag;
        }
    }

    private static long StormChecksum(object value)
    {
        switch (value)
        {
            case StormNode node:
                return ((node.Tag) * 2 + 1) + (node.First is byte[] && node.Second is StormNode second && second.Tag == node.Tag + 1 ? 0 : 1L << 40);
            case long[] values:
            {
                long sum = 0;
                foreach (long item in values)
                {
                    sum += item;
                }
                return sum;
            }
            case string text:
            {
                long sum = 0;
                foreach (char letter in text)
                {
                    sum += letter;
                }
                return sum;
            }
            case object[] items:
            {
                long first = ((StormNode)items[0]).Tag;
                long sum = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    StormNode item = (StormNode)items[i];
                    if (item.Tag != first + i)
                    {
                        return -1;
                    }
                    sum += item.Tag;
                }
                return sum;
            }
            case StormFinalizable finalizable:
                return finalizable.Payload[0] == finalizable.Tag ? finalizable.Tag : -1;
            case byte[] bytes:
            {
                if (bytes.Length > 3000)
                {
                    return bytes[0] + bytes[^1] * 256L + bytes.Length;
                }
                long sum = 0;
                foreach (byte item in bytes)
                {
                    sum += item;
                }
                return sum;
            }
            case long boxed:
                return boxed;
            default:
                return -2;
        }
    }

    // Threads allocate objects of many kinds in a tight loop and verify the objects that they keep in a ring.
    // A controller thread runs reference-counting pauses, concurrent-mark pairs and full collections while they run.
    private static void VerifyAllocationStorm(Collect collect)
    {
        const int RingSize = 48;
        int threadCount = Math.Max(2, Math.Min(8, Environment.ProcessorCount));
        int failures = 0;
        long operations = 0;
        bool stop = false;
        StormFinalizable.s_created = 0;
        StormFinalizable.s_finalized = 0;
        Thread[] workers = new Thread[threadCount];
        for (int index = 0; index < threadCount; index++)
        {
            int id = index;
            workers[index] = new Thread(() =>
            {
                Random random = new(9000 + id);
                object?[] ring = new object?[RingSize];
                long[] sums = new long[RingSize];
                long tag = id * 1_000_000L;
                long local = 0;
                while (!Volatile.Read(ref stop))
                {
                    int slot = random.Next(RingSize);
                    if (ring[slot] is not null && StormChecksum(ring[slot]!) != sums[slot])
                    {
                        Interlocked.Increment(ref failures);
                    }
                    tag++;
                    ring[slot] = MakeStormObject(random, tag, out long checksum);
                    sums[slot] = checksum;
                    // A reference that only the stack holds, to a fresh object, across the next allocation.
                    if (random.Next(4) == 0)
                    {
                        long[] fresh = new long[1 + random.Next(100)];
                        fresh[^1] = tag;
                        ref long last = ref fresh[^1];
                        object? probe = MakeStormObject(random, tag + 7, out _);
                        if (last != tag || probe is null)
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                    local++;
                }
                for (int slot = 0; slot < RingSize; slot++)
                {
                    if (ring[slot] is not null && StormChecksum(ring[slot]!) != sums[slot])
                    {
                        Interlocked.Increment(ref failures);
                    }
                }
                Interlocked.Add(ref operations, local);
            });
            workers[index].Start();
        }
        Stopwatch watch = Stopwatch.StartNew();
        int rounds = 0;
        int marks = 0;
        while (watch.ElapsedMilliseconds < 12000)
        {
            Thread.Sleep(1);
            rounds++;
            if (collect(1) != 0)
            {
                Interlocked.Increment(ref failures);
                break;
            }
            if (rounds % 25 == 0)
            {
                if (collect(2) != 0)
                {
                    Interlocked.Increment(ref failures);
                    break;
                }
                Thread.Sleep(3);
                if (collect(3) != 0)
                {
                    Interlocked.Increment(ref failures);
                    break;
                }
                marks++;
            }
            if (rounds % 400 == 0)
            {
                GC.Collect();
            }
        }
        Volatile.Write(ref stop, true);
        foreach (Thread worker in workers)
        {
            worker.Join();
        }
        Check(Volatile.Read(ref failures) == 0, "An object of the allocation storm lost its content, or a pause failed.");
        Check(rounds >= 20 && marks >= 1, $"The controller ran only {rounds} pauses and {marks} trace pairs.");
        for (int attempt = 0; attempt < 40 && Volatile.Read(ref StormFinalizable.s_finalized) < StormFinalizable.s_created; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Check(Volatile.Read(ref StormFinalizable.s_finalized) == StormFinalizable.s_created,
            $"Finalizable objects of the storm: {StormFinalizable.s_finalized} finalized of {StormFinalizable.s_created}.");
        Console.WriteLine($"alloc-storm threads={threadCount} operations={operations} pauses={rounds} traces={marks} " +
            $"finalizable={StormFinalizable.s_created}");
    }

    // Strings and arrays of many sizes, also around the size of a line (256 bytes) and of a block (32 KiB). They are
    // kept across reference-counting pauses and checked, and the allocated-byte counter has to follow the requests.
    private static void VerifyAllocationSizes(Collect collect)
    {
        List<(object Value, long Sum)> kept = new();
        long requested = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        int[] interesting = { 0, 1, 2, 7, 8, 9, 100, 101, 112, 113, 120, 121, 128, 129, 230, 231, 232, 233, 240, 241, 248, 249,
            250, 251, 252, 253, 254, 255, 256, 300, 1000, 4000, 8000, 16000, 20000, 32000, 32700, 32744, 32768, 40000 };
        for (int round = 0; round < 12; round++)
        {
            foreach (int size in interesting)
            {
                int length = size + round;
                byte[] bytes = new byte[length];
                long sum = 0;
                for (int i = 0; i < bytes.Length; i += 1 + length / 50)
                {
                    bytes[i] = (byte)(length + i);
                    sum += bytes[i];
                }
                string text = new string((char)('a' + length % 26), length / 2 + 1);
                requested += 24 + length + 22 + 2L * text.Length;
                if ((size + round) % 3 == 0)
                {
                    kept.Add((bytes, sum));
                    kept.Add((text, text.Length * (long)text[0]));
                }
            }
            if (collect(1) != 0)
            {
                throw new InvalidOperationException("A reference-counting pause failed.");
            }
            int bad = 0;
            foreach ((object value, long sum) in kept)
            {
                if (value is byte[] bytes)
                {
                    long check = 0;
                    for (int i = 0; i < bytes.Length; i += 1 + bytes.Length / 50)
                    {
                        check += bytes[i];
                    }
                    bad += check == sum ? 0 : 1;
                }
                else if (value is string text)
                {
                    bad += text.Length * (long)text[0] == sum ? 0 : 1;
                }
            }
            Check(bad == 0, $"A kept array or string changed across pause {round}.");
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated >= requested && allocated < requested * 2 + (1 << 20),
            $"The allocated-byte counter ({allocated}) does not follow the {requested} requested bytes.");
        Console.WriteLine($"alloc-sizes kept={kept.Count} requested={requested} allocated={allocated}");
    }
}

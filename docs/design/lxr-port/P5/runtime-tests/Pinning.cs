// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// P17 S7: pinning without a flag for every object. A pause keeps a list of its pinned roots (stack slots of `fixed`
// and handles of GCHandleType.Pinned), and objects of the pinned object heap lie in blocks that no pause copies from.
// These scenarios run with natural collections (no explicit collection while the pins are checked) and check what a
// program can see: an address stays while the pin lives, and the content of the pinned object is intact.
internal static partial class Program
{
    private sealed class PinnedRecord
    {
        internal byte[] Buffer = [];
        internal GCHandle Handle;
        internal nint Address;
        internal long Sum;
    }

    private static long FillPattern(Span<byte> buffer, int seed)
    {
        long sum = 0;
        for (int index = 0; index < buffer.Length; index++)
        {
            buffer[index] = (byte)(seed * 31 + index * 7);
            sum += buffer[index];
        }
        return sum;
    }

    private static long SumOf(ReadOnlySpan<byte> buffer)
    {
        long sum = 0;
        foreach (byte value in buffer)
        {
            sum += value;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PinnedRecord MakePinnedRecord(int seed, int size)
    {
        PinnedRecord record = new() { Buffer = new byte[size] };
        record.Sum = FillPattern(record.Buffer, seed);
        record.Handle = GCHandle.Alloc(record.Buffer, GCHandleType.Pinned);
        record.Address = record.Handle.AddrOfPinnedObject();
        return record;
    }

    private static int VerifyPinnedRecords(IEnumerable<PinnedRecord> records, string where)
    {
        int count = 0;
        foreach (PinnedRecord record in records)
        {
            Check(record.Handle.AddrOfPinnedObject() == record.Address,
                $"A pinned buffer moved ({where}).");
            Check(SumOf(record.Buffer) == record.Sum, $"The content of a pinned buffer changed ({where}).");
            count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AllocateGarbage(int count, int size)
    {
        long total = 0;
        for (int index = 0; index < count; index++)
        {
            byte[] garbage = new byte[size + index % 97];
            garbage[0] = (byte)index;
            total += garbage.Length + garbage[0];
        }
        return total;
    }

    private static int NaturalRounds(int collections, int garbageCount = 4000, int garbageSize = 200)
    {
        int start = GC.CollectionCount(0);
        for (int round = 0; round < 200_000 && GC.CollectionCount(0) < start + collections; round++)
        {
            AllocateGarbage(garbageCount, garbageSize);
        }
        return GC.CollectionCount(0) - start;
    }

    // Pinned handles of many sizes, replaced all the time, while garbage forces natural collections.
    private static void VerifyPinnedHandleChurn()
    {
        const int Count = 1500;
        PinnedRecord[] records = new PinnedRecord[Count];
        for (int index = 0; index < Count; index++)
        {
            records[index] = MakePinnedRecord(index, 64 + index * 37 % 6000);
        }
        int start = GC.CollectionCount(0);
        long garbage = 0;
        for (int round = 0; round < 600; round++)
        {
            garbage += AllocateGarbage(2000, 120);
            if (round % 4 == 0)
            {
                for (int step = 0; step < 100; step++)
                {
                    int slot = (round * 13 + step * 17) % Count;
                    records[slot].Handle.Free();
                    records[slot] = MakePinnedRecord(round * 1000 + step, 64 + (round + step) * 53 % 7000);
                }
            }
            if (round % 50 == 0)
            {
                VerifyPinnedRecords(records, "during the churn");
            }
        }
        int collections = GC.CollectionCount(0) - start + NaturalRounds(2);
        Check(collections >= 5, $"Only {collections} natural collections ran during the pinned-handle churn.");
        Check(VerifyPinnedRecords(records, "after the churn") == Count, "A pinned record is missing.");
        Console.WriteLine($"pin-handles-churn collections={collections} garbage={garbage}");
        foreach (PinnedRecord record in records)
        {
            record.Handle.Free();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe long FixedUnderPressure(int seed, int size, int rounds)
    {
        byte[] buffer = new byte[size];
        long expected = FillPattern(buffer, seed);
        long sum = 0;
        fixed (byte* start = buffer)
        fixed (byte* middle = &buffer[size / 2])
        {
            byte* startBefore = start;
            byte* middleBefore = middle;
            for (int round = 0; round < rounds; round++)
            {
                AllocateGarbage(300, 150);
                Check(start == startBefore && middle == middleBefore && middle - start == size / 2,
                    "A `fixed` pointer changed while its object was pinned.");
                sum += start[(round * 131) % size] + middle[0];
            }
            Check(SumOf(new ReadOnlySpan<byte>(start, size)) == expected, "A `fixed` buffer lost its content.");
            fixed (byte* again = buffer)
            {
                Check(again == startBefore, "A second `fixed` of a pinned buffer gave another address.");
            }
        }
        return sum;
    }

    // `fixed` locals in the stack of several threads (pinned roots of the stack scan), and interior pointers into the
    // pinned buffer, while natural collections run.
    private static void VerifyFixedUnderPressure()
    {
        int start = GC.CollectionCount(0);
        long total = 0;
        Thread[] threads = new Thread[4];
        Exception? failure = null;
        for (int thread = 0; thread < threads.Length; thread++)
        {
            int id = thread;
            threads[thread] = new Thread(() =>
            {
                try
                {
                    long local = 0;
                    for (int iteration = 0; iteration < 60; iteration++)
                    {
                        local += FixedUnderPressure(id * 1000 + iteration, 100 + iteration * 211 % 9000, 120);
                    }
                    Interlocked.Add(ref total, local);
                }
                catch (Exception error)
                {
                    Interlocked.CompareExchange(ref failure, error, null);
                }
            });
            threads[thread].Start();
        }
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
        if (failure is not null)
        {
            throw new InvalidOperationException("A `fixed` thread failed: " + failure.Message, failure);
        }
        int collections = GC.CollectionCount(0) - start;
        Check(collections >= 5, $"Only {collections} natural collections ran during the `fixed` scenario.");
        Console.WriteLine($"fixed-pressure collections={collections} checksum={total}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe nint DataAddress(byte[] array) => (nint)Unsafe.AsPointer(ref array[0]);

    // Arrays of the pinned object heap: long-lived ones keep their address and content, short-lived ones come and go
    // by the hundred thousand. Each short-lived array is garbage for the next pause, so the pinned blocks must be
    // released and reused, or the heap limit of this cell ends the run.
    private static void VerifyPinnedObjectHeapChurn()
    {
        const int LongLived = 300;
        byte[][] longLived = new byte[LongLived][];
        nint[] addresses = new nint[LongLived];
        long[] sums = new long[LongLived];
        for (int index = 0; index < LongLived; index++)
        {
            longLived[index] = GC.AllocateArray<byte>(100 + index * 53 % 9000, pinned: true);
            sums[index] = FillPattern(longLived[index], index);
            addresses[index] = DataAddress(longLived[index]);
        }
        byte[][] ring = new byte[64][];
        int start = GC.CollectionCount(0);
        long bytes = 0;
        for (int iteration = 0; iteration < 120_000; iteration++)
        {
            int size = 50 + iteration * 97 % 20000;
            byte[] array = GC.AllocateArray<byte>(size, pinned: true);
            array[0] = (byte)iteration;
            array[size - 1] = (byte)(iteration >> 3);
            ring[iteration % ring.Length] = array;
            bytes += size;
            if (iteration % 8 == 0)
            {
                AllocateGarbage(200, 100);
            }
            if (iteration % 20_000 == 0)
            {
                for (int index = 0; index < LongLived; index++)
                {
                    Check(DataAddress(longLived[index]) == addresses[index] && SumOf(longLived[index]) == sums[index],
                        "A long-lived pinned-object-heap array moved or changed.");
                }
            }
        }
        int collections = GC.CollectionCount(0) - start;
        Check(collections >= 5, $"Only {collections} natural collections ran during the pinned-object-heap churn.");
        for (int index = 0; index < LongLived; index++)
        {
            Check(DataAddress(longLived[index]) == addresses[index] && SumOf(longLived[index]) == sums[index],
                "A long-lived pinned-object-heap array moved or changed at the end.");
        }
        for (int index = 0; index < ring.Length; index++)
        {
            int iteration = 120_000 - 1 - (120_000 - 1 - index) % ring.Length;
            byte[] array = ring[index];
            Check(array[0] == (byte)iteration && array[^1] == (byte)(iteration >> 3),
                "A short-lived pinned-object-heap array lost its content.");
        }
        Console.WriteLine($"poh-churn collections={collections} allocated_bytes={bytes}");
        GC.KeepAlive(longLived);
    }

    private sealed class PinnedOwner
    {
        internal static int s_finalized;
        internal static int s_damaged;
        internal readonly byte[] _buffer;
        internal readonly long _sum;
        internal GCHandle _handle;

        internal PinnedOwner(int seed, int size)
        {
            _buffer = new byte[size];
            _sum = FillPattern(_buffer, seed);
            _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
        }

        ~PinnedOwner()
        {
            if (SumOf(_buffer) != _sum)
            {
                Interlocked.Increment(ref s_damaged);
            }
            _handle.Free();
            Interlocked.Increment(ref s_finalized);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreatePinnedOwners(
        List<PinnedOwner> kept, List<nint> keptAddresses, List<WeakReference<byte[]>> weak, int count, int keepEvery)
    {
        for (int index = 0; index < count; index++)
        {
            PinnedOwner owner = new(index, 100 + index * 41 % 3000);
            weak.Add(new WeakReference<byte[]>(owner._buffer));
            if (index % keepEvery == 0)
            {
                kept.Add(owner);
                keptAddresses.Add(owner._handle.AddrOfPinnedObject());
            }
        }
    }

    // Finalizable owners of pinned buffers, and weak references to the buffers. The pinned handle keeps the buffer
    // alive until the finalizer frees it, so a weak reference clears only after the finalizer has run.
    private static void VerifyPinnedFinalizersAndWeak()
    {
        const int Total = 4000;
        const int KeepEvery = 10;
        List<PinnedOwner> kept = [];
        List<nint> keptAddresses = [];
        List<WeakReference<byte[]>> weak = [];
        PinnedOwner.s_finalized = 0;
        CreatePinnedOwners(kept, keptAddresses, weak, Total, KeepEvery);
        Check(NaturalRounds(4) >= 4, "The natural collections of the finalizer scenario did not run.");
        for (int index = 0; index < kept.Count; index++)
        {
            Check(kept[index]._handle.AddrOfPinnedObject() == keptAddresses[index] &&
                SumOf(kept[index]._buffer) == kept[index]._sum, "A kept pinned owner moved or lost its buffer.");
        }
        int expected = Total - kept.Count;
        for (int attempt = 0; attempt < 40 && Volatile.Read(ref PinnedOwner.s_finalized) < expected; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Check(Volatile.Read(ref PinnedOwner.s_finalized) == expected,
            $"Finalizers of the dropped owners ran {PinnedOwner.s_finalized} times, expected {expected}.");
        GC.Collect();
        int cleared = 0;
        for (int index = 0; index < Total; index++)
        {
            bool isKept = index % KeepEvery == 0;
            bool alive = weak[index].TryGetTarget(out _);
            Check(!isKept || alive, "A weak reference to the buffer of a kept owner was cleared.");
            cleared += !isKept && !alive ? 1 : 0;
        }
        Check(cleared == expected, $"{cleared} of {expected} weak references to dropped buffers were cleared.");
        for (int index = 0; index < kept.Count; index++)
        {
            Check(kept[index]._handle.AddrOfPinnedObject() == keptAddresses[index], "A kept pinned owner moved at the end.");
        }
        kept.Clear();
        for (int attempt = 0; attempt < 40 && Volatile.Read(ref PinnedOwner.s_finalized) < Total; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Check(Volatile.Read(ref PinnedOwner.s_finalized) == Total && Volatile.Read(ref PinnedOwner.s_damaged) == 0,
            $"Each owner must finalize exactly once with its buffer intact: {PinnedOwner.s_finalized} of {Total}, damaged {PinnedOwner.s_damaged}.");
    }

    // Pins that are added while a concurrent trace runs, and pins that live across the initial and the final mark.
    private static void VerifyPinsDuringConcurrentTrace(Collect collect)
    {
        List<PinnedRecord> before = [];
        for (int index = 0; index < 300; index++)
        {
            before.Add(MakePinnedRecord(index, 80 + index * 61 % 5000));
        }
        Node? head = null;
        for (int index = 0; index < 5000; index++)
        {
            head = new Node(index) { Next = head };
        }
        Check(collect(1) == 0, "The RC pause before the trace failed.");
        Check(collect(2) == 0, "InitialMark with pins failed.");
        List<PinnedRecord> during = [];
        for (int round = 0; round < 30; round++)
        {
            for (int step = 0; step < 10; step++)
            {
                during.Add(MakePinnedRecord(10_000 + round * 10 + step, 64 + (round * 10 + step) * 29 % 4000));
            }
            AllocateGarbage(3000, 100);
            Check(collect(1) == 0, "An RC pause during the trace failed.");
            Check(VerifyPinnedRecords(before, "during the trace") == before.Count, "A pre-trace pin is missing.");
        }
        Check(collect(3) == 0, "FinalMark with pins failed.");
        Check(VerifyPinnedRecords(before, "after the final mark") == 300 &&
            VerifyPinnedRecords(during, "after the final mark") == during.Count, "A pin is missing after the final mark.");
        Check(collect(0) == 0, "A full collection with pins failed.");
        Check(VerifyPinnedRecords(before, "after a full collection") == 300 &&
            VerifyPinnedRecords(during, "after a full collection") == during.Count, "A pin is missing after a full collection.");
        int count = 0;
        for (Node? node = head; node is not null; node = node.Next)
        {
            Check(node.Value == 4999 - count, "The ordinary list lost an element or changed order.");
            count++;
        }
        Check(count == 5000, "The ordinary list lost elements.");
        foreach (PinnedRecord record in before)
        {
            record.Handle.Free();
        }
        foreach (PinnedRecord record in during)
        {
            record.Handle.Free();
        }
    }
}

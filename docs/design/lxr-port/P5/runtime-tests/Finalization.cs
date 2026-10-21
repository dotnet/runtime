// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;

// P17 S6: finalization and weak references with the finalizer registry. Weak handles and finalizable objects are
// handled only in tracing pauses, so these scenarios check what must hold at any time (no finalizer is lost or run
// twice, a suppressed finalizer never runs, a live referent is never cleared, resurrection keeps the object intact)
// and what must hold after an explicit collection (an explicit collection is a full pause: weak handles are cleared
// and finalizers are queued at once).
internal static partial class Program
{
    private sealed class ChurnObject
    {
        internal static int[] s_runs = [];
        internal static int s_total;
        internal static int s_allocatingDepth;
        internal readonly int _id;
        internal readonly long _payload;
        internal ChurnObject? _next;

        internal ChurnObject(int id)
        {
            _id = id;
            _payload = id * 31L + 7;
        }

        ~ChurnObject()
        {
            Interlocked.Increment(ref s_runs[_id]);
            Interlocked.Increment(ref s_total);
            if (_payload != _id * 31L + 7)
            {
                Interlocked.Increment(ref s_damaged);
            }
            if (s_allocatingDepth > 0)
            {
                // A finalizer that allocates (also finalizable objects) while the collector runs.
                ChurnObject child = new(_id);
                child._next = new ChurnObject(_id);
                GC.SuppressFinalize(child);
                GC.SuppressFinalize(child._next);
            }
        }

        internal void Dispose() => GC.SuppressFinalize(this);
    }

    private static int s_damaged;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateChurn(int first, int count, int disposeEvery)
    {
        for (int index = first; index < first + count; index++)
        {
            ChurnObject value = new(index);
            if (disposeEvery > 0 && index % disposeEvery == 0)
            {
                value.Dispose();
            }
        }
    }

    private static void FinishFinalizers(Func<bool> done)
    {
        for (int attempt = 0; attempt < 40 && !done(); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // Millions of short-lived finalizable objects, with and without Dispose (SuppressFinalize) and with finalizers
    // that allocate. Natural collections run while the objects are created; the final explicit collections must
    // find every finalizer that was not suppressed, exactly once.
    private static void VerifyFinalizerChurn(GetMetric? metric)
    {
        const int Count = 1_000_000;
        foreach ((int disposeEvery, int allocating) in new[] { (0, 0), (2, 0), (3, 1) })
        {
            ChurnObject.s_runs = new int[Count];
            Volatile.Write(ref ChurnObject.s_total, 0);
            Volatile.Write(ref ChurnObject.s_allocatingDepth, allocating);
            Volatile.Write(ref s_damaged, 0);
            AllocateChurn(0, Count, disposeEvery);
            int expected = disposeEvery == 0 ? Count : Count - (Count + disposeEvery - 1) / disposeEvery;
            FinishFinalizers(() => Volatile.Read(ref ChurnObject.s_total) >= expected);
            Check(Volatile.Read(ref ChurnObject.s_total) == expected,
                $"The number of finalizers differs (disposeEvery={disposeEvery}): {ChurnObject.s_total} of {expected}.");
            int[] runs = ChurnObject.s_runs;
            int wrong = 0;
            for (int index = 0; index < Count; index++)
            {
                int want = disposeEvery > 0 && index % disposeEvery == 0 ? 0 : 1;
                if (runs[index] != want)
                {
                    wrong++;
                }
            }
            Check(wrong == 0, $"A finalizer was lost, duplicated or run for a suppressed object ({wrong} objects).");
            Check(Volatile.Read(ref s_damaged) == 0, "A finalized object had been reused or damaged.");
            Volatile.Write(ref ChurnObject.s_allocatingDepth, 0);
            Console.WriteLine($"finalizer-churn disposeEvery={disposeEvery} allocating={allocating} finalized={expected}" +
                (metric is null ? "" : $" registered={metric(451)} decided={metric(454)} dropped={metric(455)} taken={metric(456)}"));
        }
        ChurnObject.s_runs = [];
    }

    // Four threads register objects at the same time, some of them suppress and some re-register.
    private static void VerifyConcurrentFinalizerChurn()
    {
        const int Threads = 4;
        const int PerThread = 250_000;
        ChurnObject.s_runs = new int[Threads * PerThread];
        Volatile.Write(ref ChurnObject.s_total, 0);
        Volatile.Write(ref ChurnObject.s_allocatingDepth, 0);
        Volatile.Write(ref s_damaged, 0);
        Thread[] workers = new Thread[Threads];
        for (int index = 0; index < Threads; index++)
        {
            int first = index * PerThread;
            workers[index] = new Thread(() => AllocateChurn(first, PerThread, 4));
            workers[index].Start();
        }
        foreach (Thread worker in workers)
        {
            worker.Join();
        }
        int expected = Threads * (PerThread - (PerThread + 3) / 4);
        FinishFinalizers(() => Volatile.Read(ref ChurnObject.s_total) >= expected);
        Check(Volatile.Read(ref ChurnObject.s_total) == expected,
            $"Concurrent registration lost or duplicated finalizers: {ChurnObject.s_total} of {expected}.");
        int wrong = 0;
        for (int index = 0; index < Threads * PerThread; index++)
        {
            int want = index % PerThread % 4 == 0 ? 0 : 1;
            if (ChurnObject.s_runs[index] != want)
            {
                wrong++;
            }
        }
        Check(wrong == 0 && Volatile.Read(ref s_damaged) == 0, $"Concurrent registration damaged {wrong} objects.");
        ChurnObject.s_runs = [];
    }

    private sealed class WeakTarget
    {
        internal readonly int _value;
        internal WeakTarget(int value) => _value = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateWeakChurn(
        int count, List<WeakReference<WeakTarget>> shortWeak, List<WeakReference<WeakTarget>> longWeak,
        ConditionalWeakTable<WeakTarget, WeakTarget> table, List<WeakTarget> alive)
    {
        for (int index = 0; index < count; index++)
        {
            WeakTarget target = new(index);
            shortWeak.Add(new WeakReference<WeakTarget>(target));
            longWeak.Add(new WeakReference<WeakTarget>(target, trackResurrection: true));
            WeakTarget value = new(-index);
            table.Add(target, value);
            if (index % 1000 == 0)
            {
                alive.Add(target);
            }
        }
    }

    // Many weak references and dependent handles to short-lived targets. A target that is still referenced is never
    // cleared. After an explicit collection (a full pause) every dead target is cleared at once.
    private static void VerifyWeakChurn()
    {
        const int Count = 300_000;
        List<WeakReference<WeakTarget>> shortWeak = new(Count);
        List<WeakReference<WeakTarget>> longWeak = new(Count);
        ConditionalWeakTable<WeakTarget, WeakTarget> table = new();
        List<WeakTarget> alive = new();
        AllocateWeakChurn(Count, shortWeak, longWeak, table, alive);
        int aliveCount = alive.Count;
        for (int round = 0; round < 3; round++)
        {
            CreateAcyclicGarbage(20000);
            for (int index = 0; index < aliveCount; index++)
            {
                Check(shortWeak[index * 1000].TryGetTarget(out WeakTarget? target) && ReferenceEquals(target, alive[index]),
                    "A weak reference to a live target was cleared.");
                Check(table.TryGetValue(alive[index], out WeakTarget? value) && value._value == -alive[index]._value,
                    "A dependent handle lost the secondary of a live primary.");
            }
        }
        ForceFull();
        int survivors = 0;
        int survivorsLong = 0;
        for (int index = 0; index < Count; index++)
        {
            bool isAlive = index % 1000 == 0;
            if (shortWeak[index].TryGetTarget(out _))
            {
                if (!isAlive)
                {
                    survivors++;
                }
            }
            else
            {
                Check(!isAlive, "An explicit collection cleared a weak reference to a live target.");
            }
            if (longWeak[index].TryGetTarget(out _) && !isAlive)
            {
                survivorsLong++;
            }
        }
        Check(survivors == 0 && survivorsLong == 0,
            $"An explicit collection left weak references to dead targets: short={survivors} long={survivorsLong}.");
        GC.KeepAlive(alive);
        GC.KeepAlive(table);
    }

    private sealed class Resurrecting
    {
        internal static readonly ConcurrentQueue<Resurrecting> s_queue = new();
        internal static int s_runs;
        internal static int s_damaged;
        internal readonly int _value;
        internal readonly Node _child;
        internal int _lives;

        internal Resurrecting(int value, int lives)
        {
            _value = value;
            _child = new Node(value ^ 0x5a5a);
            _lives = lives;
        }

        ~Resurrecting()
        {
            Interlocked.Increment(ref s_runs);
            if (_child.Value != (_value ^ 0x5a5a))
            {
                Interlocked.Increment(ref s_damaged);
            }
            s_queue.Enqueue(this);
        }
    }

    private sealed class ResurrectingCritical : CriticalFinalizerObject
    {
        internal static int s_runs;
        ~ResurrectingCritical() => Interlocked.Increment(ref s_runs);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateResurrecting(int first, int count, int lives)
    {
        for (int index = first; index < first + count; index++)
        {
            GC.KeepAlive(new Resurrecting(index, lives));
            GC.KeepAlive(new ResurrectingCritical());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DrainResurrected()
    {
        int finalized = 0;
        while (Resurrecting.s_queue.TryDequeue(out Resurrecting? value))
        {
            finalized++;
            Check(value._child.Value == (value._value ^ 0x5a5a), "A resurrected object lost its child.");
            if (--value._lives > 0)
            {
                GC.ReRegisterForFinalize(value);
            }
        }
        return finalized;
    }

    // A finalizer resurrects its object into a queue. The test re-registers the object (several lives) or lets it die.
    // The object and its child must stay intact, and each life must run exactly one finalizer.
    private static void VerifyResurrection()
    {
        const int Count = 3000;
        const int Lives = 4;
        Resurrecting.s_runs = 0;
        Resurrecting.s_damaged = 0;
        ResurrectingCritical.s_runs = 0;
        CreateResurrecting(0, Count, Lives);
        int finalized = 0;
        for (int round = 0; round < Lives + 2 && finalized < Count * Lives; round++)
        {
            ForceFull();
            finalized += DrainResurrected();
            CreateAcyclicGarbage(5000);
        }
        for (int attempt = 0; attempt < 40 && finalized < Count * Lives; attempt++)
        {
            ForceFull();
            finalized += DrainResurrected();
        }
        Check(Volatile.Read(ref Resurrecting.s_runs) == Count * Lives,
            $"Re-registration ran {Resurrecting.s_runs} finalizers, expected {Count * Lives}.");
        Check(Volatile.Read(ref Resurrecting.s_damaged) == 0, "A resurrected object was damaged.");
        FinishFinalizers(() => Volatile.Read(ref ResurrectingCritical.s_runs) >= Count);
        Check(Volatile.Read(ref ResurrectingCritical.s_runs) == Count, "A critical finalizer was lost or duplicated.");
    }

    private sealed class Prompt
    {
        internal static int s_runs;
        ~Prompt() => Interlocked.Increment(ref s_runs);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<WeakTarget>[] CreatePrompt(int count)
    {
        WeakReference<WeakTarget>[] weak = new WeakReference<WeakTarget>[count];
        for (int index = 0; index < count; index++)
        {
            new Prompt();
            weak[index] = new WeakReference<WeakTarget>(new WeakTarget(index));
        }
        return weak;
    }

    // An explicit collection is a full pause: it clears weak handles and queues the finalizers at once, however many
    // natural collections have run before.
    private static void VerifyPromptExplicitCollect()
    {
        for (int round = 0; round < 200; round++)
        {
            int before = Volatile.Read(ref Prompt.s_runs);
            WeakReference<WeakTarget>[] weak = CreatePrompt(100);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Check(Volatile.Read(ref Prompt.s_runs) == before + 100,
                $"An explicit collection did not finalize every object at once (round {round}).");
            foreach (WeakReference<WeakTarget> reference in weak)
            {
                Check(!reference.TryGetTarget(out _), "An explicit collection did not clear a weak reference.");
            }
        }
    }

    private sealed class Pooled
    {
        internal static int s_runs;
        internal readonly int _id;
        internal Pooled(int id) => _id = id;
        ~Pooled() => Interlocked.Increment(ref s_runs);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CyclePooled(Pooled[] pool, int rounds)
    {
        for (int round = 0; round < rounds; round++)
        {
            foreach (Pooled item in pool)
            {
                // A pooled object suppresses its finalizer when it is returned and registers it again when it is rented.
                GC.SuppressFinalize(item);
                GC.ReRegisterForFinalize(item);
            }
            CreateAcyclicGarbage(2000);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropPool(ref Pooled[]? pool) => pool = null;

    // Natural collections (no explicit collection) while registered objects are alive: the pauses count the new entries
    // and move the objects, and no reference-counting pause may decide anything.
    private static int RunNaturalCollections(int count)
    {
        int start = GC.CollectionCount(0);
        for (int round = 0; round < 100_000 && GC.CollectionCount(0) < start + count; round++)
        {
            CreateAcyclicGarbage(20000);
        }
        return GC.CollectionCount(0) - start;
    }

    // Suppress and register again, many times, on live objects: registration stays idempotent (one finalizer per
    // object at the end, none while the object is alive), and the registry does not collect entries without limit.
    private static void VerifyPooledReregistration(GetMetric? metric)
    {
        const int Objects = 500;
        Pooled[]? pool = new Pooled[Objects];
        for (int index = 0; index < Objects; index++)
        {
            pool[index] = new Pooled(index);
        }
        Pooled.s_runs = 0;
        CyclePooled(pool, 400);
        Check(RunNaturalCollections(5) >= 5, "The natural collections of the pooled scenario did not run.");
        CyclePooled(pool, 20);
        Check(Volatile.Read(ref Pooled.s_runs) == 0, "A natural collection finalized a live pooled object.");
        ForceFull();
        Check(Volatile.Read(ref Pooled.s_runs) == 0, "A live pooled object was finalized.");
        if (metric is not null && metric(450) != ulong.MaxValue)
        {
            // The registry metrics exist only in the S6 collector; an older collector reports the invalid-metric value.
            ulong counted = metric(450);
            Console.WriteLine($"pooled-reregister counted_entries={counted} duplicates_merged={metric(461)}");
            Check(counted < 20000, $"The registry kept {counted} entries for {Objects} live pooled objects.");
        }
        DropPool(ref pool);
        FinishFinalizers(() => Volatile.Read(ref Pooled.s_runs) >= Objects);
        Check(Volatile.Read(ref Pooled.s_runs) == Objects,
            $"Each pooled object must run its finalizer exactly once: {Pooled.s_runs} of {Objects}.");
    }}

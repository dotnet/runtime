// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TestLibrary;
using Xunit;

// ControlledExecution.Run is the API that aborts a thread
#pragma warning disable SYSLIB0046

// A thread abort raised on Windows is unwound by the OS while the thread is preemptive. A GC in that window must not
// leave a stale object reference in the context that the second pass dispatches from.
// That context belongs to ControlledExecution.Run, which catches the abort. The references at risk are the ones that
// Run's caller keeps in callee-saved registers that Run does not save itself, and which those are depends on how
// CoreLib was compiled. So the holder keeps a movable object in every integer callee-saved register across the call.
// Run never returns normally after an abort, so the holder cannot check its objects afterwards: a stale register is
// caught when a GC reports it, during the dispatch or at the forced collections at the end.
// Under DOTNET_GCStress=0x3 the allocations of other threads collect in the window. Without GC stress a collector
// thread allocates to drive real compacting GCs.
// AbortAtEpilogStart spins in two IL methods that tail-call each other with an explicit tail. prefix, so some
// redirects land on the first instruction of an epilog. The OS finds no handler for that frame and dispatches its
// caller instead.
// AbortAtEpilogStart alone puts a catcher between its holder and Run, so the holder runs on after every abort and
// detects a stale register by validation; the other sub-tests detect one only through a GC that reports it.
public class Runtime_137606
{
    private const int Iterations = 20;

    // The epilog window is a single instruction, so that sub-test needs many more aborts.
    private const int EpilogIterations = 200;

    private static readonly TimeSpan s_budget = TimeSpan.FromSeconds(60);

    private static volatile int s_counter;
    // Written so that the garbage is allocated on the heap, never read.
    internal static object s_garbage;
    private static int s_validationFailures;
    private static Action s_body;
    private static CancellationToken s_token;
    // The GC updates these slots, so a register that missed an update no longer matches its slot.
    private static readonly Node[] s_expected = new Node[12];
    private static OperationCanceledException s_caught;

    private sealed class Node
    {
        public readonly int Value;
        public readonly long Magic;
        public readonly Node Next;

        public Node(int value, Node next)
        {
            Value = value;
            Magic = MagicOf(value);
            Next = next;
        }

        public static long MagicOf(int value) => unchecked((long)((ulong)value * 0x9E3779B97F4A7C15UL ^ 0x5DEECE66DUL));
    }

    // Garbage below each object leaves room for a compacting GC to move it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Node Make(int value, Node next)
    {
        for (int i = 0; i < 8; i++)
        {
            s_garbage = new byte[48];
        }

        return new Node(value, next);
    }

    // Keeps twelve young objects live across ControlledExecution.Run, more than there are integer callee-saved
    // registers: eight on x64 (rbx, rbp, rsi, rdi, r12-r15; rbp is free because the method has no exception handling
    // and needs no frame pointer) and ten on arm64 (x19-x28). The body and the token are read from statics right
    // before the call, so no register is kept for anything else. Tier0 code would keep the objects on the stack.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static void HoldAcrossRun(int k)
    {
        Node n0 = Make(k, null);
        Node n1 = Make(k + 1, n0);
        Node n2 = Make(k + 2, n1);
        Node n3 = Make(k + 3, n2);
        Node n4 = Make(k + 4, n3);
        Node n5 = Make(k + 5, n4);
        Node n6 = Make(k + 6, n5);
        Node n7 = Make(k + 7, n6);
        Node n8 = Make(k + 8, n7);
        Node n9 = Make(k + 9, n8);
        Node n10 = Make(k + 10, n9);
        Node n11 = Make(k + 11, n10);

        ControlledExecution.Run(s_body, s_token);

        // Not reached when the abort lands. Keeps every object live across the call.
        Check(n0, null);
        Check(n1, n0);
        Check(n2, n1);
        Check(n3, n2);
        Check(n4, n3);
        Check(n5, n4);
        Check(n6, n5);
        Check(n7, n6);
        Check(n8, n7);
        Check(n9, n8);
        Check(n10, n9);
        Check(n11, n10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Check(Node node, Node next)
    {
        if (node is null || node.GetType() != typeof(Node) || node.Magic != Node.MagicOf(node.Value)
            || !ReferenceEquals(node.Next, next))
        {
            s_validationFailures++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Node MakeExpected(int index, int value, Node next)
    {
        Node node = Make(value, next);
        s_expected[index] = node;
        return node;
    }

    // Catches the OperationCanceledException that ControlledExecution.Run throws after it has caught the abort, so the
    // holder runs on and checks its objects. Holds nothing across the call; its handler makes it save rbp on x64 and
    // fp and lr on arm64, so the holder's other registers reach Run, and come back from it, unsaved here.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static void RunAndCatch()
    {
        try
        {
            ControlledExecution.Run(s_body, s_token);
        }
        catch (OperationCanceledException e)
        {
            s_caught = e;
        }
    }

    // HoldAcrossRun's layout around RunAndCatch, so that it resumes after every abort. Each object is compared with its
    // GC-updated slot in s_expected, with no dereference, so a register the refresh missed after a GC moved the object
    // fails the check rather than crashing. One failing abort counts once.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static void HoldAcrossCatchingRun(int k)
    {
        Node n0 = MakeExpected(0, k, null);
        Node n1 = MakeExpected(1, k + 1, n0);
        Node n2 = MakeExpected(2, k + 2, n1);
        Node n3 = MakeExpected(3, k + 3, n2);
        Node n4 = MakeExpected(4, k + 4, n3);
        Node n5 = MakeExpected(5, k + 5, n4);
        Node n6 = MakeExpected(6, k + 6, n5);
        Node n7 = MakeExpected(7, k + 7, n6);
        Node n8 = MakeExpected(8, k + 8, n7);
        Node n9 = MakeExpected(9, k + 9, n8);
        Node n10 = MakeExpected(10, k + 10, n9);
        Node n11 = MakeExpected(11, k + 11, n10);

        RunAndCatch();

        Node[] expected = s_expected;
        bool same = ReferenceEquals(n0, expected[0]) & ReferenceEquals(n1, expected[1])
            & ReferenceEquals(n2, expected[2]) & ReferenceEquals(n3, expected[3])
            & ReferenceEquals(n4, expected[4]) & ReferenceEquals(n5, expected[5])
            & ReferenceEquals(n6, expected[6]) & ReferenceEquals(n7, expected[7])
            & ReferenceEquals(n8, expected[8]) & ReferenceEquals(n9, expected[9])
            & ReferenceEquals(n10, expected[10]) & ReferenceEquals(n11, expected[11]);
        if (!same)
        {
            s_validationFailures++;
        }
    }

    // Spins with no calls, so the method is fully interruptible and the abort arrives through a redirect.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SpinWithoutCalls(ManualResetEventSlim ready)
    {
        ready.Set();
        while (true)
        {
            s_counter++;
        }
    }

    // Spins through calls, so the loop is partially interruptible and the abort can arrive through a return-address
    // hijack as well as a redirect. This is the loop of GitHub_135530's CancelInGenericFrames.
    private static class GenericLoop<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Run(ManualResetEventSlim ready)
        {
            ready.Set();
            while (true)
            {
                Frame();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Frame()
        {
            Span<int> values = stackalloc int[32];
            values[0] = typeof(T).GetHashCode();
            Consume(values);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Span<int> values)
    {
        s_counter = values[0];
    }

    // PopSpin and PopSpin2, in test137606tailcalls.il, tail-call each other with a frame that saves callee-saved
    // registers. Not optimized, so this calls PopSpin rather than tail-calling it.
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static void SpinAtEpilogStart(ManualResetEventSlim ready)
    {
        ready.Set();
        TailCalls137606.PopSpin(Make(1, null), Make(2, null), Make(3, null), 0);
    }

    private enum Spin
    {
        WithoutCalls,
        ThroughCalls,
        AtEpilogStart,
    }

    private static void AbortOnce(int k, Spin spin)
    {
        using var ready = new ManualResetEventSlim();
        using var cts = new CancellationTokenSource();
        Task cancellation = Task.Run(() =>
        {
            ready.Wait();
            Thread.Sleep(1);
            cts.Cancel();
        });

        try
        {
            s_body = spin switch
            {
                Spin.ThroughCalls => () => GenericLoop<string>.Run(ready),
                Spin.AtEpilogStart => () => SpinAtEpilogStart(ready),
                _ => () => SpinWithoutCalls(ready),
            };
            s_token = cts.Token;
            OperationCanceledException exception;
            if (spin == Spin.AtEpilogStart)
            {
                s_caught = null;
                HoldAcrossCatchingRun(k);
                exception = s_caught;
                Assert.NotNull(exception);
            }
            else
            {
                exception = Assert.Throws<OperationCanceledException>(() => HoldAcrossRun(k));
            }

            Assert.Equal(cts.Token, exception.CancellationToken);
        }
        finally
        {
            s_body = null;
            s_token = default;
            s_caught = null;

            // Release the worker if the spin was never reached, and wait for it before ready and cts are disposed.
            ready.Set();
            cancellation.GetAwaiter().GetResult();
        }
    }

    private static Thread StartCollector(CancellationToken stop)
    {
        var thread = new Thread(() =>
        {
            object[] live = new object[256];
            var random = new Random(12345);
            for (int i = 0; !stop.IsCancellationRequested; i++)
            {
                live[i & 255] = new byte[random.Next(16, 4096)];
                if ((i & 0x3ff) == 0)
                {
                    live[random.Next(256)] = new byte[90_000];
                }
            }
        });
        thread.IsBackground = true;
        thread.Start();
        return thread;
    }

    private static void AbortRepeatedly(Spin spin)
    {
        int iterations = spin == Spin.AtEpilogStart ? EpilogIterations : Iterations;

        // GCStress's transition mode forces no GC until the first one has run.
        GC.Collect();
        s_validationFailures = 0;

        // Under GC stress the allocations of other threads already collect, so the collector is left out there.
        using var stop = new CancellationTokenSource();
        // CoreClrConfigurationDetection is qualified because Xunit also defines the name through the using above.
        Thread collector = TestLibrary.CoreClrConfigurationDetection.IsGCStress ? null : StartCollector(stop.Token);
        int collections = GC.CollectionCount(0);
        var elapsed = Stopwatch.StartNew();
        int aborts = 0;
        try
        {
            while (aborts < iterations && (aborts == 0 || elapsed.Elapsed < s_budget))
            {
                AbortOnce(aborts * 16, spin);
                aborts++;
            }
        }
        finally
        {
            stop.Cancel();
            collector?.Join();
        }

        string name = spin switch
        {
            Spin.ThroughCalls => nameof(AbortThroughHijack),
            Spin.AtEpilogStart => nameof(AbortAtEpilogStart),
            _ => nameof(AbortThroughRedirect),
        };
        int gcs = GC.CollectionCount(0) - collections;
        if (spin == Spin.AtEpilogStart)
        {
            // Printed before the forced collections, so a crash there still leaves the validation count.
            Console.WriteLine($"{name}: {aborts} aborts, {s_validationFailures} validation failures, {gcs} GCs");
        }
        else
        {
            Console.WriteLine($"{name}: {aborts} aborts, {gcs} GCs");
        }

        // A stale root reported during the dispatch corrupts the heap; make the next GCs walk it.
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Assert.Equal(0, s_validationFailures);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void AbortThroughRedirect()
    {
        AbortRepeatedly(Spin.WithoutCalls);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void AbortThroughHijack()
    {
        AbortRepeatedly(Spin.ThroughCalls);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void AbortAtEpilogStart()
    {
        AbortRepeatedly(Spin.AtEpilogStart);
    }
}

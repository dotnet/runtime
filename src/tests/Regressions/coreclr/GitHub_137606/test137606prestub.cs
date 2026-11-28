// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

// A managed exception raised with no thread abort: the JIT cannot compile a callee because an assembly it uses is
// missing, so the prestub raises the exception under a PrestubMethodFrame while the thread is preemptive, and the
// Windows OS unwinder copies the callee-saved registers in that window. A compacting GC on another thread must not
// leave a stale copy in the context that the second pass resumes the catching method with.
// The references at risk are the ones that the catching method's caller keeps in callee-saved registers, so that
// caller keeps a movable object in every integer callee-saved register across the call and checks them afterwards.
public class Runtime_137606_Prestub
{
    private const int Iterations = 1000;

    private const string DependencyFileName = "test137606dependency.dll";

    private static readonly TimeSpan s_budget = TimeSpan.FromSeconds(20);

    // Written so that the garbage is allocated on the heap, never read.
    internal static object s_garbage;
    private static int s_base;
    private static int s_validationFailures;
    private static string s_firstFailure;

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
    private static Node AllocateWithGarbage(int value, Node next)
    {
        for (int i = 0; i < 16; i++)
        {
            s_garbage = new byte[64];
        }

        return new Node(value, next);
    }

    // The JIT fails to compile this method because the dependency is missing, so every call goes back to the prestub.
    // AggressiveOptimization also keeps it out of ReadyToRun images, so it is always compiled by the JIT.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static int UseDependency()
    {
        return new Dependency137606.DependencyType().GetValue();
    }

    // Keeps six more objects live across the failing call, as the reproducing app does.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static int HoldAcrossFailingCall()
    {
        Node a = AllocateWithGarbage(1, null);
        Node b = AllocateWithGarbage(2, a);
        Node c = AllocateWithGarbage(3, b);
        Node d = AllocateWithGarbage(4, null);
        Node e = AllocateWithGarbage(5, d);
        Node f = AllocateWithGarbage(6, e);

        int value = UseDependency();

        return value + a.Value + b.Next.Value + c.Next.Next.Value + d.Value + e.Next.Value + f.Next.Next.Value;
    }

    // The second pass resumes this method's catch with the callee-saved registers its caller left in place.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CallThroughCatch()
    {
        try
        {
            HoldAcrossFailingCall();
            return false;
        }
        catch (FileNotFoundException)
        {
            return true;
        }
    }

    // Keeps twelve young objects live across the call, more than there are integer callee-saved registers: eight on
    // x64 (rbx, rbp, rsi, rdi, r12-r15; rbp is free because the method has no exception handling and needs no frame
    // pointer) and ten on arm64 (x19-x28). The values come from a static that is read again after the call, so no
    // integer is kept across it. Tier0 code would keep the objects on the stack.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static bool HoldAcrossCatch()
    {
        int k = s_base;
        Node n0 = AllocateWithGarbage(k, null);
        Node n1 = AllocateWithGarbage(k + 1, n0);
        Node n2 = AllocateWithGarbage(k + 2, n1);
        Node n3 = AllocateWithGarbage(k + 3, n2);
        Node n4 = AllocateWithGarbage(k + 4, n3);
        Node n5 = AllocateWithGarbage(k + 5, n4);
        Node n6 = AllocateWithGarbage(k + 6, n5);
        Node n7 = AllocateWithGarbage(k + 7, n6);
        Node n8 = AllocateWithGarbage(k + 8, n7);
        Node n9 = AllocateWithGarbage(k + 9, n8);
        Node n10 = AllocateWithGarbage(k + 10, n9);
        Node n11 = AllocateWithGarbage(k + 11, n10);

        bool caught = CallThroughCatch();

        k = s_base;
        Validate(n0, k, null);
        Validate(n1, k + 1, n0);
        Validate(n2, k + 2, n1);
        Validate(n3, k + 3, n2);
        Validate(n4, k + 4, n3);
        Validate(n5, k + 5, n4);
        Validate(n6, k + 6, n5);
        Validate(n7, k + 7, n6);
        Validate(n8, k + 8, n7);
        Validate(n9, k + 9, n8);
        Validate(n10, k + 10, n9);
        Validate(n11, k + 11, n10);
        return caught;
    }

    // A stale register points at whatever was at the object's old address, so the exact type, the value, the value
    // derived from it and the link to the previous object are all checked.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Validate(Node node, int value, Node next)
    {
        string failure = null;
        if (node is null)
        {
            failure = "null";
        }
        else if (node.GetType() != typeof(Node))
        {
            failure = "type " + node.GetType();
        }
        else if (node.Value != value)
        {
            failure = "Value " + node.Value;
        }
        else if (node.Magic != Node.MagicOf(value))
        {
            failure = "Magic mismatch";
        }
        else if (!ReferenceEquals(node.Next, next))
        {
            failure = "Next link differs";
        }

        if (failure != null && s_validationFailures++ == 0)
        {
            s_firstFailure = $"Node {value}: {failure}";
        }
    }

    // The project compiles against the dependency but does not copy it to the output folder.
    private static void CheckDependencyIsMissing()
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DependencyFileName);
        Assert.False(File.Exists(path), path + " was deployed");

        // Without this the test would pass without ever reaching the prestub's raise.
        Assert.Throws<FileNotFoundException>(() => { UseDependency(); });
    }

    private static Thread StartCollector(CancellationToken stop)
    {
        var thread = new Thread(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            }
        });
        thread.IsBackground = true;
        thread.Start();
        return thread;
    }

    private static void FailRepeatedly()
    {
        CheckDependencyIsMissing();
        GC.Collect();
        s_validationFailures = 0;
        s_firstFailure = null;

        using var stop = new CancellationTokenSource();
        Thread collector = StartCollector(stop.Token);
        int collections = GC.CollectionCount(0);
        var elapsed = Stopwatch.StartNew();
        int iterations = 0;
        int caught = 0;
        try
        {
            while (iterations < Iterations && s_validationFailures == 0 && (iterations == 0 || elapsed.Elapsed < s_budget))
            {
                s_base = iterations * 64;
                if (HoldAcrossCatch())
                {
                    caught++;
                }

                iterations++;
            }
        }
        finally
        {
            stop.Cancel();
            collector.Join();
        }

        string name = nameof(FailingJitWithForcedCollections);
        Console.WriteLine($"{name}: {iterations} iterations, {caught} caught, {GC.CollectionCount(0) - collections} GCs");

        // A stale root reported during the dispatch corrupts the heap; make the next GCs walk it.
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Assert.True(s_validationFailures == 0, s_firstFailure);
        Assert.Equal(iterations, caught);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void FailingJitWithForcedCollections()
    {
        FailRepeatedly();
    }
}

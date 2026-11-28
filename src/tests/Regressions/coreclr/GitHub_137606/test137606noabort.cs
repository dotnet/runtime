// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TestLibrary;
using Xunit;

// A managed exception raised through the Windows OS unwinder with no thread abort: the runtime's virtual stub dispatch
// resolver for an IDynamicInterfaceCastable whose GetInterfaceImplementation throws raises the exception under a
// StubDispatchFrame while the thread is preemptive, and the OS unwinder copies the callee-saved registers in that
// window. A compacting GC on another thread must not leave a stale copy in the context the second pass resumes the
// catching method with.
// The references at risk are the ones the catching method's caller keeps in callee-saved registers that the catching
// method does not save. So an outer holder keeps twelve movable objects across the call, more than there are integer
// callee-saved registers, and validates them after it.
public class Runtime_137606_NoAbort
{
    private const int Iterations = 40000;

    private static readonly TimeSpan s_budget = TimeSpan.FromSeconds(20);

    // Written so that the garbage is allocated on the heap, never read.
    internal static object s_garbage;
    private static object s_target;
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

    private sealed class ExpectedException : Exception
    {
    }

    public interface IGetValue
    {
        int GetValue();
    }

    // The interface call is resolved by the virtual stub dispatch worker under a StubDispatchFrame.
    // GetInterfaceImplementation throws, so the resolver raises the exception through RaiseTheExceptionInternalOnly.
    private sealed class ThrowingCastable : IDynamicInterfaceCastable
    {
        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented) => true;

        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType) => throw new ExpectedException();
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

    // The interface call the resolver throws from. AggressiveOptimization here and on CallThroughCatch, so tiering
    // cannot change their frames mid-run and the register layout read once holds for the whole run.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static int ThrowingCall(IGetValue target)
    {
        return target.GetValue();
    }

    // The second pass resumes this method's catch with the callee-saved registers its caller left in place. It has an
    // exception handler, so it saves only the frame pointer; the caller's objects in the other registers are kept.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static bool CallThroughCatch(object target)
    {
        try
        {
            ThrowingCall((IGetValue)target);
            return false;
        }
        catch (ExpectedException)
        {
            return true;
        }
    }

    // Keeps twelve young objects live across the call, more than there are integer callee-saved registers: eight on
    // x64 (rbx, rbp, rsi, rdi, r12-r15; rbp is free because the method has no exception handling and needs no frame
    // pointer) and ten on arm64 (x19-x28). The values come from a static read again after the call, so no integer is
    // kept across it. Tier0 code would keep the objects on the stack.
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static bool OuterHolder()
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

        bool caught = CallThroughCatch(s_target);

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

    // A collector thread runs compacting gen0 GCs; under GC stress it is left out.
    private static Thread StartCollector(CancellationToken stop)
    {
        var thread = new Thread(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                GC.Collect(0, GCCollectionMode.Forced, blocking: true, compacting: true);
            }
        });
        thread.IsBackground = true;
        thread.Start();
        return thread;
    }

    private static void ThrowRepeatedly()
    {
        // GCStress's transition mode forces no GC until the first one has run.
        GC.Collect();
        s_validationFailures = 0;
        s_firstFailure = null;
        s_target = new ThrowingCastable();

        // The first throw's one-time costs are paid before the collector starts: with a PDB beside the dll (a local
        // build; CI payloads strip it), that throw otherwise waits behind the forced collections for minutes.
        _ = OuterHolder();
        Assert.True(s_validationFailures == 0, s_firstFailure);

        // CoreClrConfigurationDetection is qualified because Xunit also defines the name through the using above.
        using var stop = new CancellationTokenSource();
        Thread collector = TestLibrary.CoreClrConfigurationDetection.IsGCStress ? null : StartCollector(stop.Token);
        var elapsed = Stopwatch.StartNew();
        int iterations = 0;
        int caught = 0;
        try
        {
            while (iterations < Iterations && s_validationFailures == 0 && (iterations == 0 || elapsed.Elapsed < s_budget))
            {
                s_base = iterations * 64;
                if (OuterHolder())
                {
                    caught++;
                }

                iterations++;
            }
        }
        finally
        {
            stop.Cancel();
            collector?.Join();
        }

        Console.WriteLine($"{nameof(DynamicInterfaceCastableThrows)}: {iterations} iterations, {caught} caught");

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Assert.True(s_validationFailures == 0, s_firstFailure);
        Assert.Equal(iterations, caught);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public static void DynamicInterfaceCastableThrows()
    {
        ThrowRepeatedly();
    }
}

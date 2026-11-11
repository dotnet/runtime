// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// These portable correctness tests target Wasm GC-reference home selection.
// Tests a variety of scenarios with locals live and dead across different GC safepoints,
// including in exceptional throwing paths.

// Forced compacting collections DO NOT guarantee movement. GC stress, where
// supported, would strengthen coverage of allocation and throw helpers.
public class WasmGCLiveness
{
    private sealed class Box
    {
        public int Value;
        public int Other;
        public Box Next;
    }

    public enum Scenario
    {
        Call,
        InteriorByref,
        DeadBefore,
        BornAfter,
        Mixed,
        LastUseArgument,
        Redefined,
        Finally,
        Allocation,
        WriteBarrier
    }

    [Theory]
    [InlineData(Scenario.Call, 101)]
    [InlineData(Scenario.InteriorByref, 202)]
    [InlineData(Scenario.DeadBefore, 303)]
    [InlineData(Scenario.BornAfter, 404)]
    [InlineData(Scenario.Mixed, 507)]
    [InlineData(Scenario.LastUseArgument, 601)]
    [InlineData(Scenario.Redefined, 702)]
    [InlineData(Scenario.Finally, 901)]
    [InlineData(Scenario.Allocation, 1457)]
    [InlineData(Scenario.WriteBarrier, 1301)]
    public static void CheckScenario(Scenario scenario, int expected)
    {
        int actual = scenario switch
        {
            Scenario.Call => AcrossCall(),
            Scenario.InteriorByref => InteriorByrefAcrossCall(),
            Scenario.DeadBefore => DeadBeforeCall(),
            Scenario.BornAfter => BornAfterCall(),
            Scenario.Mixed => MixedHomes(),
            Scenario.LastUseArgument => LastUseArgument(),
            Scenario.Redefined => RedefinedByReturn(),
            Scenario.Finally => AcrossFinally(),
            Scenario.Allocation => AcrossAllocation(),
            Scenario.WriteBarrier => AcrossWriteBarrier(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        Assert.Equal(expected, actual);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Box Make(int value)
    {
        return new Box { Value = value, Other = 1 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AcrossCall()
    {
        Box x = Make(101);
        Collect();
        return x.Value;
    }

    // The interior byref survives collection even though the source local need not.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InteriorByrefAcrossCall()
    {
        ref int p = ref Make(201).Value;
        Collect();
        p++;
        return p;
    }

    // Only the scalar result survives collection; x never crosses a safe point.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DeadBeforeCall()
    {
        Box x = Make(303);
        int result = x.Value;
        Collect();
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BornAfterCall()
    {
        Collect();
        Box x = Make(404);
        return x.Value;
    }

    // survivor needs protection, but shortLived is a Wasm-only home candidate.
    // Two distinct field reads keep shortLived from becoming just a call result.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MixedHomes()
    {
        Box survivor = Make(500);
        Box shortLived = Make(6);
        int value = shortLived.Value + shortLived.Other;
        Collect();
        return survivor.Value + value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CollectThenRead(Box argument)
    {
        Collect();
        return argument.Value;
    }

    // x dies at its read, but the argument value still needs protection.
    // Inspect fgWasmSpillRefs as well as local liveness for this case.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LastUseArgument()
    {
        Box x = Make(601);
        return CollectThenRead(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Box CollectAndReturn(Box argument)
    {
        Collect();
        argument.Value++;
        return argument;
    }

    // Old x dies before the call; new x is defined after it. The named local
    // may disappear during optimization, so check the actual LIR shape.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RedefinedByReturn()
    {
        Box x = Make(701);
        x = CollectAndReturn(x);
        return x.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AcrossFinally()
    {
        Box x = Make(901);
        try
        {
            x.Value++;
        }
        finally
        {
            Collect();
        }

        return x.Value - 1;
    }

    // GC stress can collect inside these allocations. The explicit collection
    // verifies preservation afterward, but does not prove an allocation collected.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AcrossAllocation()
    {
        Box x = Make(1201);
        Box head = null;
        for (int i = 0; i < 256; i++)
        {
            head = new Box { Value = i, Next = head };
        }

        Collect();
        int count = 0;
        for (Box node = head; node is not null; node = node.Next)
        {
            count++;
        }

        return x.Value + count;
    }

    // x crosses a write barrier, not a collecting operation. Its local dies
    // before Collect; the object remains reachable through destination.Next.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AcrossWriteBarrier()
    {
        Box destination = Make(0);
        Box x = Make(1301);
        destination.Next = x;
        int value = x.Value;
        Collect();
        return destination.Next.Value == value ? value : -1;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AcrossJoin(bool collect)
    {
        Box x = Make(1001);
        if (collect)
        {
            Collect();
        }

        Assert.Equal(1001, x.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AcrossLoop(int iterations)
    {
        Box x = Make(1101);
        for (int i = 0; i < iterations; i++)
        {
            Collect();
            x.Value++;
        }

        Assert.Equal(1101 + iterations, x.Value);
    }

    public enum ThrowingCheck
    {
        Null,
        Bounds,
        Overflow,
        Division
    }

    [Theory]
    [InlineData(ThrowingCheck.Null, 0, 0, typeof(NullReferenceException))]
    [InlineData(ThrowingCheck.Bounds, 1, 0, typeof(IndexOutOfRangeException))]
    [InlineData(ThrowingCheck.Overflow, int.MaxValue, 1, typeof(OverflowException))]
    [InlineData(ThrowingCheck.Division, 1, 0, typeof(DivideByZeroException))]
    [InlineData(ThrowingCheck.Division, int.MinValue, -1, typeof(OverflowException))]
    public static void CheckThrowingPath(ThrowingCheck kind, int a, int b, Type expected)
    {
        Assert.Equal(801, AcrossThrowingCheck(kind, null, new int[1], a, b, expected));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CollectInFilter(Exception exception, Type expected)
    {
        if (exception.GetType() != expected)
        {
            return false;
        }

        Collect();
        return true;
    }

    // Keep the failing operation in the method owning survivor. Moving it into
    // a helper would only exercise an ordinary call in this method. Parameters
    // prevent constant folding of the failing checks.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AcrossThrowingCheck(
        ThrowingCheck kind, Box obj, int[] array, int a, int b, Type expected)
    {
        Box survivor = Make(801);
        try
        {
            int result = kind switch
            {
                ThrowingCheck.Null => obj.Value,
                ThrowingCheck.Bounds => array[a],
                ThrowingCheck.Overflow => checked(a + b),
                ThrowingCheck.Division => a / b,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            return result;
        }
        catch (Exception exception) when (CollectInFilter(exception, expected))
        {
            return survivor.Value;
        }
    }
}

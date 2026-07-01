// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using TestLibrary;
using Xunit;

enum AllocationKind
{
    Heap,
    Stack,
    Undefined
}

delegate int Test();

public class LocallocStackAlloc
{
    static bool GCStressEnabled()
    {
        return Environment.GetEnvironmentVariable("DOTNET_GCStress") != null;
    }

    static AllocationKind StackAllocation()
    {
        AllocationKind expectedAllocationKind = AllocationKind.Stack;
        if (!OperatingSystem.IsWindows() || GCStressEnabled())
        {
            Console.WriteLine("Allocation kind is not predictable");
            expectedAllocationKind = AllocationKind.Undefined;
        }
        return expectedAllocationKind;
    }

    static AllocationKind HeapAllocation()
    {
        AllocationKind expectedAllocationKind = AllocationKind.Heap;
        if (!OperatingSystem.IsWindows() || GCStressEnabled())
        {
            Console.WriteLine("Allocation kind is not predictable");
            expectedAllocationKind = AllocationKind.Undefined;
        }
        return expectedAllocationKind;
    }

    static int CallTestAndVerifyAllocation(Test test, int expectedResult, AllocationKind expectedAllocationsKind, bool throws = false)
    {
        string methodName = test.Method.Name;
        try
        {
            long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
            int testResult = test();
            long allocatedBytesAfter = GC.GetAllocatedBytesForCurrentThread();

            if (throws)
            {
                Console.WriteLine($"FAILURE ({methodName}): expected exception, got {testResult}");
                return -1;
            }

            if (testResult != expectedResult)
            {
                Console.WriteLine($"FAILURE ({methodName}): expected {expectedResult}, got {testResult}");
                return -1;
            }

            if ((expectedAllocationsKind == AllocationKind.Stack) && (allocatedBytesBefore != allocatedBytesAfter))
            {
                Console.WriteLine($"FAILURE ({methodName}): unexpected allocation of {allocatedBytesAfter - allocatedBytesBefore} bytes");
                return -1;
            }

            if ((expectedAllocationsKind == AllocationKind.Heap) && (allocatedBytesBefore == allocatedBytesAfter))
            {
                Console.WriteLine($"FAILURE ({methodName}): unexpected stack allocation");
                return -1;
            }

            Console.WriteLine($"SUCCESS ({methodName})");
            return 100;
        }
        catch (Exception e)
        {
            if (throws)
            {
                Console.WriteLine($"SUCCESS ({methodName}) caught {e.GetType().Name}");
                return 100;
            }
            Console.WriteLine($"FAILURE ({methodName}): unexpected {e.GetType().Name}: {e.Message}");
            return -1;
        }
    }

    // Keep JIT from constant-folding the length.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int OpaqueLength(int n) => n;

    // Variable-length stack-allocated int[] within the localloc threshold.
    // Sums the elements after writing them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VariableLengthSmall()
    {
        int n = OpaqueLength(8);
        int[] array = new int[n];
        int sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = i + 1;
        }
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }
        return sum + array.Length;
    }

    // Variable-length newarr that exceeds the stack-alloc threshold; should be
    // routed to the heap helper at runtime instead of corrupting the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VariableLengthLarge()
    {
        int n = OpaqueLength(100_000);
        int[] array = new int[n];
        int sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = 1;
        }
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }
        return sum;
    }

    // Negative length must throw OverflowException via the heap helper
    // even when the localloc dispatch path is selected.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VariableLengthNegative()
    {
        int n = OpaqueLength(-1);
        int[] array = new int[n];
        return array.Length;
    }

    // int.MinValue length must also throw OverflowException; this is the case
    // where signed totalSize wraps to a small value if not guarded properly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VariableLengthIntMin()
    {
        int n = OpaqueLength(int.MinValue);
        int[] array = new int[n];
        return array.Length;
    }

    // Length near INT32_MAX with large element size: elemSize * length overflows.
    // Helper should raise OutOfMemoryException; no stack corruption.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VariableLengthHuge()
    {
        int n = OpaqueLength(int.MaxValue);
        long[] array = new long[n];
        return array.Length;
    }

    // Repeatedly allocate a small variable-length array within a single
    // method invocation. The per-frame budget caps total localloc bytes, so
    // after enough iterations the remaining allocations must fall back to
    // the heap rather than growing the frame without bound.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VariableLengthFrameBudget()
    {
        int sum = 0;
        for (int iter = 0; iter < 200; iter++)
        {
            int n = OpaqueLength(64);
            int[] array = new int[n];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i + 1;
            }
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
        }
        return sum;
    }

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestSmall()
    {
        VariableLengthSmall();
        return CallTestAndVerifyAllocation(VariableLengthSmall, 8 + (1 + 2 + 3 + 4 + 5 + 6 + 7 + 8), StackAllocation());
    }

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestLarge()
    {
        VariableLengthLarge();
        return CallTestAndVerifyAllocation(VariableLengthLarge, 100_000, HeapAllocation());
    }

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestNegative() => CallTestAndVerifyAllocation(VariableLengthNegative, 0, AllocationKind.Undefined, throws: true);

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestIntMin() => CallTestAndVerifyAllocation(VariableLengthIntMin, 0, AllocationKind.Undefined, throws: true);

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestHuge() => CallTestAndVerifyAllocation(VariableLengthHuge, 0, AllocationKind.Undefined, throws: true);

    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static int TestFrameBudget()
    {
        VariableLengthFrameBudget();
        return CallTestAndVerifyAllocation(VariableLengthFrameBudget, 200 * ((64 * 65) / 2), HeapAllocation());
    }
}

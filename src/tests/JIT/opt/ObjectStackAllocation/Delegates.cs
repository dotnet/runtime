// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

enum AllocationKind
{
    Heap,
    Stack,
    Undefined
}

delegate int Test();

public class Delegates
{
    static bool GCStressEnabled()
    {
        return Environment.GetEnvironmentVariable("DOTNET_GCStress") != null;
    }

    static AllocationKind StackAllocation()
    {
        AllocationKind expectedAllocationKind = AllocationKind.Stack;
        if (GCStressEnabled())
        {
            Console.WriteLine("GCStress is enabled");
            expectedAllocationKind = AllocationKind.Undefined;
        }
        return expectedAllocationKind;
    }

    static AllocationKind HeapAllocation()
    {
        AllocationKind expectedAllocationKind = AllocationKind.Heap;
        if (GCStressEnabled())
        {
            Console.WriteLine("GCStress is enabled");
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
            else
            {
                Console.WriteLine($"SUCCESS ({methodName})");
                return 100;
            }
        }
        catch
        {
            if (throws)
            {
                Console.WriteLine($"SUCCESS ({methodName})");
                return 100;
            }
            else
            {
                return -1;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoTest0(int a)
    {
        var f = (int x) => x + 1;
        return f(a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunTest0() => DoTest0(100);

    [Fact]
    public static int Test0() 
    {
        RunTest0();
        return CallTestAndVerifyAllocation(RunTest0, 101, StackAllocation());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoTest1(int[] a)
    {
        var f = (int x) => x + 1;
        int sum = 0;

        foreach (int i in a)
        {
            sum += f(i);
        }

        return sum;
    }

    static int[] s_a = new int[100];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunTest1() => DoTest1(s_a);

    [Fact]
    public static int Test1()
    {
        RunTest1();
        return CallTestAndVerifyAllocation(RunTest1, 100, StackAllocation());
    }

    // Here the delegate gets stack allocated, but not the closure.
    // With PGO the delegate is also inlined.
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunTest2Inner(int a) => InvokeFunc(x => x + a);

    static int InvokeFunc(Func<int, int> func) => func(101);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunTest2() => RunTest2Inner(-1);

    [Fact]
    public static int Test2()
    {
        return CallTestAndVerifyAllocation(RunTest2, 100, HeapAllocation());
    }
}

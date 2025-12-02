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

public class X
{
    public X() { }
    public X? a;
    public X? b;
    public int y;
}

public class F
{
    public X x;
    ~F()
    {
        if (x != null)
        {
            Console.WriteLine($"F destroyed: {x.y}");
            x = null;
        }
    }
}

public class Fields
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(X x)
    {
        // Do nothing
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

    // --------------- NO ESCAPE TESTS (once fields are fully enabled) ------------------

    // Local objects that refer to one another

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunStack0()
    {
        X x1 = new X();
        x1.y = 3;
        x1.a = new X();
        x1.a.y = 4;
        x1.b = x1.a;

        return x1.y + x1.a.y + x1.b.y;
    }

    [Fact]
    public static int Stack0()
    {
        RunStack0();
        return CallTestAndVerifyAllocation(RunStack0, 11, HeapAllocation());
    }

    // Field refers to both non-escaping and other objects

    static X s_x = new X();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunStack2()
    {
        X x = new X();
        x.y = 3;
        x.a = new X();
        x.b = s_x;

        return x.y + x.a.y + x.b.y;
    }

    [Fact]
    public static int Stack2()
    {
        RunStack2();
        return CallTestAndVerifyAllocation(RunStack2, 3, HeapAllocation());
    }

    // Array refers to both non-escaping and other objects

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunStack3()
    {
        X[] x = new X[2];

        x[0] = new X();
        x[1] = s_x;
        x[0].y = 77;

        return x[0].y + x[1].y;
    }

    [Fact]
    public static int Stack3()
    {
        return CallTestAndVerifyAllocation(RunStack3, 77, HeapAllocation());
    }

    // Local objects that refer to one another (including array)

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunStack4()
    {
        X[] a = new X[10];
        a[0] = new X();
        a[1] = new X();

        a[0].y = 3;
        a[1].y = 4;

        GC.Collect();

        return a[1].y + a[0].y;
    }

    [Fact]
    public static int Stack4()
    {
        return CallTestAndVerifyAllocation(RunStack4, 7, HeapAllocation());
    }

    // Local objects that refer to one another (including array)

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunStack5()
    {
        X[] a = new X[10];
        a[0] = new X();

        a[1] = a[0];

        a[0].y = 3;
        a[1].y = 4;

        GC.Collect();

        return a[1].y + a[0].y;
    }

    [Fact]
    public static int Stack5()
    {
        return CallTestAndVerifyAllocation(RunStack5, 8, HeapAllocation());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunStack6()
    {
        // Array of structs of arrays + strings.

        var fullInput = new[]
        {
            new { utf8Bytes = new byte[] { 0x40 }, output = "@" },
            new { utf8Bytes = new byte[] { 0xC3, 0x85 }, output = "[00C5]" },
        };

        int result = 0;

        foreach (var f in fullInput)
        {
            if (result == 0)
            {
                GC.Collect();
            }
            result += f.utf8Bytes.Length + f.output.Length;
        }

        return result;
    }
    
    [Fact]
    public static int Stack6()
    {
        return CallTestAndVerifyAllocation(RunStack6, 10, HeapAllocation());
    }

    // --------------- ESCAPE TESTS ------------------

    // Field escapes via return

    [MethodImpl(MethodImplOptions.NoInlining)]
    static X DoHeap0()
    {
        X x1 = new X();
        x1.y = 3;
        x1.a = new X();
        x1.a.y = 4;
        x1.b = x1.a;

        return x1.b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap0()
    {
        X x = DoHeap0();

        return x.y;
    }

    [Fact]
    public static int Heap0()
    {
        return CallTestAndVerifyAllocation(RunHeap0, 4, HeapAllocation());
    }

    // Field escapes via param

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoHeap1(ref X xe)
    {
        X x1 = new X();
        x1.y = 3;
        x1.a = new X();
        x1.a.y = 4;
        x1.b = x1.a;

        xe = x1.a;

        return x1.y + x1.a.y + x1.b.y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap1()
    {
        X x = null;
        return DoHeap1(ref x);
    }

    [Fact]
    public static int Heap1()
    {
        return CallTestAndVerifyAllocation(RunHeap1, 11, HeapAllocation());
    }

    // Field escapes via assignment to param field

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoHeap2(X x)
    {
        X x1 = new X();
        x1.y = 3;
        x1.a = new X();
        x1.a.y = 8;
        x1.b = x1.a;

        x.b = x1.b;

        return x1.y + x1.a.y + x1.b.y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap2()
    {
        return DoHeap2(new X());
    }

    [Fact]
    public static int Heap2()
    {
        return CallTestAndVerifyAllocation(RunHeap2, 19, HeapAllocation());
    }

    // Object escapes via assignment to param field

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoHeap3(X x)
    {
        X x1 = new X();
        x.b = x1;
        x1.y = 11;

        return x1.y + x.b.y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap3()
    {
        return DoHeap3(new X());
    }

    [Fact]
    public static int Heap3()
    {
        return CallTestAndVerifyAllocation(RunHeap3, 22, HeapAllocation());
    }

    // Field escapes via call

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap4()
    {
        X x1 = new X();
        x1.y = 3;
        x1.a = new X();
        x1.a.y = 4;
        x1.b = x1.a;

        Consume(x1.b);

        return x1.y + x1.a.y + x1.b.y;
    }

    [Fact]
    public static int Heap4()
    {
        return CallTestAndVerifyAllocation(RunHeap4, 11, HeapAllocation());
    }

    // Array field escapes via return

    [MethodImpl(MethodImplOptions.NoInlining)]
    static X DoHeap5()
    {
        X[] x = new X[2];
        x[0] = new X();
        x[1] = s_x;

        x[0].y = 33;

        return x[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap5()
    {
        return DoHeap5().y;
    }

    [Fact]
    public static int Heap5()
    {
        RunHeap5();
        return CallTestAndVerifyAllocation(RunHeap5, 33, HeapAllocation());
    }

    // Array field escapes via param

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoHeap6(ref X xe)
    {
        X[] x = new X[2];
        x[0] = new X();
        x[1] = s_x;

        x[0].y = 44;

        xe = x[1];

        return x[0].y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap6()
    {
        X x = null;
        return DoHeap6(ref x);
    }

    [Fact]
    public static int Heap6()
    {
        return CallTestAndVerifyAllocation(RunHeap6, 44, HeapAllocation());
    }

    // Array field escapes via param field

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoHeap7(X xe)
    {
        X[] x = new X[2];
        x[0] = new X();
        x[1] = s_x;

        x[0].y = 45;

        xe.a = x[1];

        return x[0].y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap7()
    {
        X x = new X();
        return DoHeap7(x);
    }

    [Fact]
    public static int Heap7()
    {
        return CallTestAndVerifyAllocation(RunHeap7, 45, HeapAllocation());
    }

    // Delegate and closure
    // Delegate doesn't escape, but closure does

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoHeap8(int[] a)
    {
        int y = 1;
        var f = (int x) => x + y;
        int sum = 0;

        foreach (int i in a)
        {
            sum += f(i);
        }

        return sum;
    }

    static int[] s_a = new int[100];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap8() => DoHeap8(s_a);

    [Fact]
    public static int Heap8()
    {
        RunHeap8();
        return CallTestAndVerifyAllocation(RunHeap8, 100, HeapAllocation());
    }

    // "non-escaping" finalizable object with GC fields
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunHeap9()
    {
        F f = new F();
        f.x = new X();
        f.x.y = 1;
        return 100;
    }

    [Fact]
    public static int Heap9()
    {
        int result = CallTestAndVerifyAllocation(RunHeap9, 100, HeapAllocation());
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return result;
    }
}

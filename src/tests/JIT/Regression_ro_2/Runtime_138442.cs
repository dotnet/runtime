// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_138442;

// For an unconstrained T, "value == null" is "box T; ldnull; ceq", "value != null" is
// "box T; ldnull; cgt.un" and "if (value == null)" is "box T; brtrue". When T is a
// Nullable<> these are hasValue checks and should not allocate, also when the
// comparison is inlined into a loop. For other value types they are constants.
public class Runtime_138442
{
    private struct S<T>
    {
        public T Value;
    }

    private static int s_calls;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNull<T>(T value) => value == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNotNull<T>(T value) => value != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NullToOne<T>(T value)
    {
        if (value == null)
        {
            return 1;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Get<T>(T value)
    {
        s_calls++;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int CountNulls<T>(T[] values)
    {
        int n = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (IsNull(values[i]))
            {
                n++;
            }
        }
        return n;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int CountNonNulls<T>(T[] values)
    {
        int n = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (IsNotNull(values[i]))
            {
                n++;
            }
        }
        return n;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumNullToOne<T>(T[] values)
    {
        int n = 0;
        for (int i = 0; i < values.Length; i++)
        {
            n += NullToOne(values[i]);
        }
        return n;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Branches<T>(T value)
    {
        int result = 0;
        if (value == null)
        {
            result |= 1;
        }
        if (value != null)
        {
            result |= 2;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int CallsEvaluatedOnce<T>(T present, T absent)
    {
        s_calls = 0;
        int result = 0;
        if (IsNull(Get(present)))
        {
            result |= 1;
        }
        if (IsNotNull(Get(absent)))
        {
            result |= 2;
        }
        return result + (s_calls << 2);
    }

    private static void Test<T>(T present, T absent)
    {
        Assert.False(IsNull(present));
        Assert.True(IsNull(absent));
        Assert.True(IsNotNull(present));
        Assert.False(IsNotNull(absent));
        Assert.Equal(2, Branches(present));
        Assert.Equal(1, Branches(absent));
        Assert.Equal(2 << 2, CallsEvaluatedOnce(present, absent));

        T[] values = new T[] { present, absent, present, present, absent };
        Assert.Equal(2, CountNulls(values));
        Assert.Equal(3, CountNonNulls(values));
        Assert.Equal(2, SumNullToOne(values));
    }

    private static void TestNotNull<T>(T value)
    {
        Assert.False(IsNull(value));
        Assert.True(IsNotNull(value));
        Assert.Equal(0, NullToOne(value));
        Assert.Equal(2, Branches(value));
        Assert.Equal(2 + (2 << 2), CallsEvaluatedOnce(value, value));
    }

    private static void TestShared<U>(U value) => Test<S<U>?>(new S<U> { Value = value }, null);

    [Fact]
    public static void TestEntryPoint()
    {
        Test<int?>(42, null);
        Test<long?>(-1, null);
        Test<Guid?>(Guid.NewGuid(), null);
        Test<S<string>?>(new S<string> { Value = "a" }, null);
        TestShared("b");
        TestShared<object>(null);
        Test<string>("c", null);
        TestNotNull(42);
        TestNotNull(new S<string> { Value = "d" });
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    [SkipOnCoreClr("Requires optimized code", RuntimeTestModes.AnyJitStress)]
    public static void TestNoAllocation()
    {
        int?[] values = new int?[100];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (i % 10 == 0) ? null : i;
        }

        // Make sure the methods are compiled before measuring.
        CountNulls(values);
        CountNonNulls(values);
        SumNullToOne(values);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int nulls = CountNulls(values);
        long allocated1 = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        int nonNulls = CountNonNulls(values);
        long allocated2 = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        int nullsToOne = SumNullToOne(values);
        long allocated3 = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(10, nulls);
        Assert.Equal(90, nonNulls);
        Assert.Equal(10, nullsToOne);
        Assert.Equal(0, allocated1);
        Assert.Equal(0, allocated2);
        Assert.Equal(0, allocated3);
    }
}

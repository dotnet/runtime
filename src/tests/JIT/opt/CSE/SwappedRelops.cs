// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// CSE should treat a relop and its swapped form (for example "a < b" and "b > a")
// as the same value, so only one compare is emitted for both.
// See https://github.com/dotnet/runtime/issues/138063.
public class SwappedRelops
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(0, -1)]
    [InlineData(-1, 0)]
    [InlineData(-1, int.MaxValue)]
    [InlineData(int.MinValue, 0)]
    [InlineData(int.MinValue, int.MaxValue)]
    [InlineData(int.MaxValue, int.MinValue)]
    [InlineData(int.MinValue, int.MinValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public static void IntRelops(int a, int b)
    {
        int order = a.CompareTo(b);

        LessThanSwapped(a, b, out bool first, out bool second);
        Assert.Equal(order < 0, first);
        Assert.Equal(order < 0, second);

        LessOrEqualSwapped(a, b, out first, out second);
        Assert.Equal(order <= 0, first);
        Assert.Equal(order <= 0, second);

        EqualSwapped(a, b, out first, out second);
        Assert.Equal(order == 0, first);
        Assert.Equal(order == 0, second);

        NotEqualSwapped(a, b, out first, out second);
        Assert.Equal(order != 0, first);
        Assert.Equal(order != 0, second);

        int unsignedOrder = ((uint)a).CompareTo((uint)b);
        UnsignedLessThanSwapped((uint)a, (uint)b, out first, out second);
        Assert.Equal(unsignedOrder < 0, first);
        Assert.Equal(unsignedOrder < 0, second);
    }

    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(0L, -1L)]
    [InlineData(-1L, 0L)]
    [InlineData(long.MinValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MinValue)]
    [InlineData(long.MinValue, long.MinValue)]
    [InlineData(long.MaxValue, long.MaxValue)]
    [InlineData((long)int.MaxValue + 1, int.MaxValue)]
    public static void LongRelops(long a, long b)
    {
        int order = a.CompareTo(b);

        LongLessThanSwapped(a, b, out bool first, out bool second);
        Assert.Equal(order < 0, first);
        Assert.Equal(order < 0, second);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(int.MinValue, int.MaxValue, 0)]
    [InlineData(int.MaxValue, int.MinValue, 0)]
    [InlineData(int.MaxValue, int.MaxValue, 1)]
    public static void NestedSwappedBranches(int a, int b, int expected) =>
        Assert.Equal(expected, NestedSwapped(a, b));

    [Theory]
    [InlineData(1.0, 2.0)]
    [InlineData(2.0, 1.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(-0.0, 0.0)]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(1.0, double.NaN)]
    [InlineData(double.NaN, double.NaN)]
    public static void DoubleRelops(double a, double b)
    {
        bool unordered = double.IsNaN(a) || double.IsNaN(b);
        int order = a.CompareTo(b);

        DoubleLessThanSwapped(a, b, out bool first, out bool second);
        Assert.Equal(!unordered && (order < 0), first);
        Assert.Equal(!unordered && (order < 0), second);

        // Unordered compares are true when either operand is NaN.
        DoubleUnorderedLessThanSwapped(a, b, out first, out second);
        Assert.Equal(unordered || (order < 0), first);
        Assert.Equal(unordered || (order < 0), second);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LessThanSwapped(int a, int b, out bool first, out bool second)
    {
        // X64: cmp {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: cmp {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: cmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a < b;
        second = b > a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LessOrEqualSwapped(int a, int b, out bool first, out bool second)
    {
        // X64: cmp {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: cmp {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: cmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a <= b;
        second = b >= a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EqualSwapped(int a, int b, out bool first, out bool second)
    {
        // X64: cmp {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: cmp {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: cmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a == b;
        second = b == a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NotEqualSwapped(int a, int b, out bool first, out bool second)
    {
        // X64: cmp {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: cmp {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: cmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a != b;
        second = b != a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnsignedLessThanSwapped(uint a, uint b, out bool first, out bool second)
    {
        // X64: cmp {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: cmp {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: cmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a < b;
        second = b > a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LongLessThanSwapped(long a, long b, out bool first, out bool second)
    {
        // X64: cmp {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: cmp {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: cmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a < b;
        second = b > a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DoubleLessThanSwapped(double a, double b, out bool first, out bool second)
    {
        // X64: {{v?u?comisd}} {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: {{v?u?comisd}} {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: fcmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: fcmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = a < b;
        second = b > a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DoubleUnorderedLessThanSwapped(double a, double b, out bool first, out bool second)
    {
        // X64: {{v?u?comisd}} {{.*}}
        // X64: set{{[a-z]+}} {{.*}}
        // X64-NOT: {{v?u?comisd}} {{.*}}
        // X64-NOT: set{{[a-z]+}} {{.*}}
        // ARM64: fcmp {{.*}}
        // ARM64: cset {{.*}}
        // ARM64-NOT: fcmp {{.*}}
        // ARM64-NOT: cset {{.*}}
        first = !(a >= b);
        second = !(b <= a);
    }

    // Redundant branch opts should still turn this into a single "a == b" when the
    // inner relop has its operands in the other order.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NestedSwapped(int a, int b)
    {
        // X64: cmp {{.*}}
        // X64-NOT: cmp {{.*}}
        // ARM64: cmp {{.*}}
        // ARM64-NOT: cmp {{.*}}
        if (a >= b)
        {
            if (b >= a)
            {
                return 1;
            }
        }

        return 0;
    }
}

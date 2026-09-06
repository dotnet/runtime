using System;
using System.Runtime.CompilerServices;
using Xunit;

public class RangeCheck_Overflow
{
    [Fact]
    public static int TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => DescendingOverflow(Span<byte>.Empty, int.MinValue + 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DescendingSliceOverflow(Span<byte>.Empty, int.MinValue + 1, 1));
        return Overflow([10, 0, 20, 0, 30, 0, 40]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DescendingOverflow(Span<byte> a, int start)
    {
        // CHECK: CORINFO_HELP_RNGCHKFAIL

        int sum = 0;
        for (int i = start; i > int.MinValue;)
        {
            i -= 2;
            sum += a[i];
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DescendingSliceOverflow(Span<byte> a, int start, int iterations)
    {
        // CHECK: ThrowArgumentOutOfRangeException

        int sum = 0;
        for (int i = start; i > int.MinValue;)
        {
            i -= 2;
            sum += a.Slice(i, 2).Length;
            if (--iterations == 0)
            {
                return sum;
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Overflow(Span<byte> a)
    {
        // CHECK: CORINFO_HELP_RNGCHKFAIL

        int sum = 0;
        for (int i = 0; i < a.Length; i += 2)
        {
            sum += a[i];
        }

        return sum;
    }
}

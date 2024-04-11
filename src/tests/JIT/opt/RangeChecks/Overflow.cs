using System;
using System.Runtime.CompilerServices;
using Xunit;

public class RangeCheck_Overflow
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Overflow([10, 0, 20, 0, 30, 0, 40]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Overflow(Span<byte> a)
    {
		// We should have two compares in this function: one for the loop test, and one for the bounds check.
        // X64: cmp
        // X64: cmp

        int sum = 0;
        for (int i = 0; i < a.Length; i += 2)
        {
            sum += a[i];
        }

        return sum;
    }
}

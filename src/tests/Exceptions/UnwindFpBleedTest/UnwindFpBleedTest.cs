using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        double a0 = 1.0;
        double a1 = 2.0;
        double a2 = 3.0;
        double a3 = 4.0;
        double a4 = 5.0;
        double a5 = 6.0;
        double a6 = 7.0;
        double a7 = 8.0;
        double a8 = 9.0;
        double a9 = 10.0;

        for (int i = 1; i < 10; i++)
        {
            a0 *= 1.0;
            a1 *= 2.0;
            a2 *= 3.0;
            a3 *= 4.0;
            a4 *= 5.0;
            a5 *= 6.0;
            a6 *= 7.0;
            a7 *= 8.0;
            a8 *= 9.0;
            a9 *= 10.0;
        }

        EHMethod();

        bool isExpectedValue =
            a0 == 1.0
            && a1 == Math.Pow(2, 10)
            && a2 == Math.Pow(3, 10)
            && a3 == Math.Pow(4, 10)
            && a4 == Math.Pow(5, 10)
            && a5 == Math.Pow(6, 10)
            && a6 == Math.Pow(7, 10)
            && a7 == Math.Pow(8, 10)
            && a8 == Math.Pow(9, 10)
            && a9 == Math.Pow(10, 10);

        return isExpectedValue ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void EHMethod()
    {
        try
        {
            FloatManipulationMethod();
        }
        catch (Exception) { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void FloatManipulationMethod()
    {
        // Enough locals to try to get the JIT to touch at least some of the non-volatile
        // registers (some spilling might happen, but hopefully the lack of EH prioritizes
        // registers).
        double a0 = 1, a1 = 2, a2 = 3, a3 = 4, a4 = 5, a5 = 6, a6 = 7, a7 = 8;
        double a8 = 1, a9 = 2, a10 = 3, a11 = 4, a12 = 5, a13 = 6, a14 = 7, a15 = 8;
        double a16 = 1, a17 = 2, a18 = 3, a19 = 4, a20 = 5, a21 = 6, a22 = 7, a23 = 8;
        double a24 = 1, a25 = 2, a26 = 3, a27 = 4, a28 = 5, a29 = 6, a30 = 7, a31 = 8;

        // Some busy math to prevent easy optimizations and folding.
        for (int i = 0; i < 5; i++)
        {
            a0 -= 1; a1 -= 2; a2 -= 3; a3 -= 4; a4 -= 5; a5 -= 6; a6 -= 7; a7 -= 8;
            a8 -= 9; a9 -= 10; a10 -= 11; a11 -= 12; a12 -= 13; a13 -= 14; a14 -= 15; a15 -= 16;
            a16 -= 17; a17 -= 18; a18 -= 19; a19 -= 20; a20 -= 21; a21 -= 22; a22 -= 23; a23 -= 24;
            a24 -= 25; a25 -= 26; a26 -= 27; a27 -= 28; a28 -= 29; a29 -= 30; a30 -= 31; a31 -= 32;
        }

        throw new Exception();
    }
}

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    static sbyte[] s_7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void issue1()
    {
        var vr9 = Vector64.Create<float>(0);
        if ((2147483647 == (-(int)AdvSimd.Extract(vr9, 1))))
        {
            s_7 = s_7;
        }
    }

    static int[][] s_2;
    static bool s_3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void issue2()
    {
        s_3 ^= (2021486855 != (-(long)s_2[0][0]));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Checking for successful compilation
        issue1();
        issue2();
        return 100;
    }
}
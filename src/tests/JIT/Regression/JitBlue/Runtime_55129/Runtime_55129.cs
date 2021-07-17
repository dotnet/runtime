// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public class Runtime_55129
{
    public static int Main()
    {
        int result = 100;
        if (!Runtime_55129_1.Run())
            result |= 1;
        if (!Runtime_55129_2.Run())
            result |= 2;
        return result;
    }
}

// These tests failed because of a missing zero extension because a peephole
// did not handle that 'movsxd' would sign extend.
public class Runtime_55129_1
{
    static I s_i = new C();
    static short s_7;
    static sbyte[][] s_10 = new sbyte[][]{new sbyte[]{-1}};
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Run()
    {
        var vr59 = (uint)M6(s_10[0][0]);
        return (long)vr59 == uint.MaxValue;
    }

    static ulong M6(sbyte arg0)
    {
        return (ulong)arg0;
        ref short var1 = ref s_7;
        s_i.Foo(var1);
    }
}

interface I
{
    void Foo<T>(T val);
}

class C : I
{
    public void Foo<T>(T val) { }
}

struct S0
{
    public long F5;
    public S0(int f0, byte f1, ulong f2, byte f3, uint f4, long f5, int f6, int f7) : this()
    {
    }
}

class C0
{
    public long F0;
}

class C1
{
    public ulong F1;
}

public class Runtime_55129_2
{
    static int[] s_2 = new int[] { -1 };
    static C0 s_4 = new C0();
    static S0 s_5;
    static C1[][] s_47 = new C1[][] { new C1[] { new C1() } };
    static bool s_result;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Run()
    {
        s_5.F5 = s_2[0];
        C1 vr4 = s_47[0][0];
        var vr6 = vr4.F1;
        M6(vr6);
        return s_result;
    }

    static void M6(ulong arg0)
    {
        arg0 >>= 0;
        if (-1 < (uint)(0U | M7(ref s_4.F0, new S0[][,] { new S0[,] { { new S0(-10, 1, 0, 178, 1671790506U, -2L, 1, -2147483648) } }, new S0[,] { { new S0(1330389305, 255, 1297834355652867458UL, 0, 1777203966U, 4402572156859115751L, -1597826478, 1) } }, new S0[,] { { new S0(2147483646, 15, 18446744073709551614UL, 9, 1089668776U, 8629324174561266356L, 2124906017, -1883510008) } } }, 1, new sbyte[] { -37, -21, 0, 0, 0, 0 }, new S0[] { new S0(219671235, 22, 11763641210444381762UL, 0, 2568868236U, -7432636731544997849L, 1623417447, -479936755), new S0(-2147483647, 108, 0, 1, 4294967294U, 9223372036854775807L, 539462011, 1), new S0(1, 0, 15733997012901423027UL, 212, 4294967294U, 4663434921694141184L, -2147483647, 1196938120), new S0(1, 68, 0, 14, 653907833U, -6962955672558660864L, 1966270988, -378944819) })))
        {
            s_result = true;
        }
    }

    static short M7(ref long arg0, S0[][,] arg1, ushort arg2, sbyte[] arg3, S0[] arg4)
    {
        long vr20 = s_5.F5;
        return (short)vr20;
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test: optOptimizePostLayout could assert "found use of a node that
// is not in the LIR sequence" on arm32 when LSRA inserted a RELOAD on top of
// a SETCC result under a JTRUE. gtReverseCond then returned a newly-allocated
// EQ(reload, 0) tree whose CNS_INT 0 child was never threaded into LIR.

namespace Runtime_127745;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_127745
{
    public static IRuntime127745 s_rt;
    public static short[][][] s_15;
    public static uint[,] s_34;
    public static byte[] s_53;
    public static int s_62;
    public static long[][] s_90;
    public static int s_108;
    public static int[] s_110;
    public static long s_114;
    public static ushort s_130;

    [Fact]
    public static void TestEntryPoint()
    {
        // Calling M96 forces the JIT to compile it. The regression was a
        // compile-time assertion rather than wrong output. The static fields are
        // all null, so execution throws NullReferenceException immediately.
        try
        {
            RunM96();
        }
        catch (NullReferenceException)
        {
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RunM96()
    {
        M96(s_90);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ref byte M96(long[][] arg0)
    {
        byte lvar0 = default(byte);
        do
        {
            s_rt.WriteLine("c_677", 0);
        }
        while (++lvar0 < 252);
        try
        {
            var vr2 = new short[] { 1 };
            M100(ref arg0[0][0], ref arg0[0][0], vr2);
        }
        finally
        {
            arg0[0] = arg0[0];
        }

        var vr1 = s_15[0][0][0];
        if ((1 <= s_114) | (0 > M97(vr1)))
        {
            arg0[0][0] >>= 1;
        }
        else
        {
            s_62 = s_110[0];
        }

        s_rt.WriteLine("c_715", arg0[0][0]);
        s_rt.WriteLine("c_716", s_108);
        return ref s_53[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort M97(short arg0)
    {
        bool vr0 = 0 > s_34[0, 0];
        return s_130;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M100(ref long arg0, ref long arg1, short[] arg2)
    {
    }
}

public interface IRuntime127745
{
    void WriteLine<T>(string site, T value);
}

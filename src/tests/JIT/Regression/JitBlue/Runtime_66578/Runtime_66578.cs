// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In below test case, would keep around unreachable blocks which would wrongly keep the
//       variables alive and we end up generating false refpositions. This leads to not marking
//       an interval as spilled.
using System;
using System.Runtime.CompilerServices;
using Xunit;

public interface I0
{
}

public interface I1
{
}

public class C0 : I0, I1
{
    public sbyte F0;
    public bool F1;
    public uint F2;
    public ushort F5;
    public C0(sbyte f0, bool f1, uint f2, ushort f5)
    {
        F0 = f0;
        F1 = f1;
        F2 = f2;
        F5 = f5;
    }
}

public class Program2
{
    public static I1[][] s_18;
    //= { new I1[] { null }, };

    public static ushort[][] s_43;
    public static I1 s_64;
    public static I0 s_88;

    [Fact]
    public static int TestEntryPoint()
    {
        var vr6 = new C0(0, false, 0, 0);
        try
        {
            M52(vr6);
        } catch (NullReferenceException ex)
        {
            return 100;
        }
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M52(C0 argThis)
    {
        I1 vr9 = s_18[0][0];
        if (argThis.F1)
        {
            return;
        }

        if (argThis.F1)
        {
            try
            {
                s_43 = new ushort[][] { new ushort[] { 0 } };
            }
            finally
            {
                for (int var8 = 0; var8 < 2; var8++)
                {
                    s_64 = argThis;
                }

                C0 vr10 = new C0(0, true, 0, 0);
                s_88 = new C0(-1, true, 0, 0);
            }

            I1 vr12 = s_18[0][0];
        }
    }
}

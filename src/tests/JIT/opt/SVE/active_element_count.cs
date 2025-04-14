// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class TestGetActiveElementCount
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            bool fail = false;

            Vector<byte> v0 = Vector.Create<byte>(1);
            Vector<byte> v1 = Vector.Create<byte>(4);
            if (DifferentVector(v0, v1, 0) != 16)
            {
                fail = true;
            }
            
            if (SameVector(v0, 0) != 16)
            {
                fail = true;
            }

            Vector<double> vDouble0 = Vector.Create<double>(1);
            Vector<double> vDouble1 = Vector.Create<double>(4);
            if (DifferentVectorDouble(vDouble0, vDouble1, 0) != 2)
            {
                fail = true;
            }
            
            if (SameVectorDouble(vDouble0, 0) != 2)
            {
                fail = true;
            }

            if (fail)
            {
                return 101;
            }
        }
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DifferentVector(Vector<byte> v0, Vector<byte> v1, ulong total)
    {
        //ARM64-FULL-LINE: cntp {{x[0-9]+}}, {{p[0-9]+}}, {{p[0-9]+}}.b
        //ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
        total += Sve.GetActiveElementCount(v0, v1);
        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SameVector(Vector<byte> v0, ulong total)
    {
        //ARM64-FULL-LINE: incp {{x[0-9]+}}, {{p[0-9]+}}.b
        total += Sve.GetActiveElementCount(v0, v0);
        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DifferentVectorDouble(Vector<double> v0, Vector<double> v1, ulong total)
    {
        //ARM64-FULL-LINE: cntp {{x[0-9]+}}, {{p[0-9]+}}, {{p[0-9]+}}.b
        //ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
        total += Sve.GetActiveElementCount(v0, v1);
        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SameVectorDouble(Vector<double> v0, ulong total)
    {
        //ARM64-FULL-LINE: incp {{x[0-9]+}}, {{p[0-9]+}}.b
        total += Sve.GetActiveElementCount(v0, v0);
        return total;
    }
}

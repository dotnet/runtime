// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class AcrossAndCselToAcross
{

    [Theory]
    [InlineData(1, 42)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<long> addAcross_sbyte(sbyte op1, sbyte op2)
    {
        if (Sve.IsSupported)
        {
            //ARM64-FULL-LINE: cmpne {{p[0-9]+.b}}, {{p[0-9]+/z}}, {{z[0-9]+.b}}, #0
            //ARM64-FULL-LINE-NEXT: saddv {{d[0-9]+}}, {{p[0-9]+}}, {{z[0-9]+.b}}
            var vec = Vector.Create(op2);
            var mask = Vector.Create(op1);
            return Sve.AddAcross(Sve.ConditionalSelect(mask, vec, Vector<sbyte>.Zero));
        }
        else
        {
            return Vector<long>.Zero;
        }
    }

    [Theory]
    [InlineData(1, 42, 43)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<double> addSequentialAcross_double(double op1, double op2, double op3)
    {
        if (Sve.IsSupported)
        {
            //ARM64-FULL-LINE: cmpne {{p[0-9]+.d}}, {{p[0-9]+/z}}, {{z[0-9]+.d}}, #0
            //ARM64-FULL-LINE-NEXT: fadda {{v[0-9]+.d}}, {{p[0-9]+}}, {{v[0-9]+.d}}, {{z[0-9]+.d}}
            var vec1 = Vector.Create(op2);
            var vec2 = Vector.Create(op3);
            var mask = Vector.Create(op1);
            return Sve.AddSequentialAcross(vec1, Sve.ConditionalSelect(mask, vec2, Vector<double>.Zero));
        }
        else
        {
            return Vector<double>.Zero;
        }
    }

    [Theory]
    [InlineData(UInt16.MaxValue, 42)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<ushort> maxAcross_ushort(ushort op1, ushort op2)
    {
        if (Sve.IsSupported)
        {
            //ARM64-FULL-LINE: cmpne {{p[0-9]+.h}}, {{p[0-9]+/z}}, {{z[0-9]+.h}}, #0
            //ARM64-FULL-LINE-NEXT: umaxv {{v[0-9]+.h}}, {{p[0-9]+}}, {{z[0-9]+.h}}
            var vec = Vector.Create(op2);
            var mask = Vector.Create(op1);
            return Sve.MaxAcross(Sve.ConditionalSelect(mask, vec, Vector<ushort>.Zero));
        }
        else
        {
            return Vector<ushort>.Zero;
        }
    }

    [Theory]
    [InlineData(1, 42)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<int> orAcross_int(int op1, int op2)
    {
        if (Sve.IsSupported)
        {
            //ARM64-FULL-LINE: cmpne {{p[0-9]+.s}}, {{p[0-9]+/z}}, {{z[0-9]+.s}}, #0
            //ARM64-FULL-LINE-NEXT: orv {{v[0-9]+.s}}, {{p[0-9]+}}, {{z[0-9]+.s}}
            var vec = Vector.Create(op2);
            var mask = Vector.Create(op1);
            return Sve.OrAcross(Sve.ConditionalSelect(mask, vec, Vector<int>.Zero));
        }
        else
        {
            return Vector<int>.Zero;
        }
    }

    [Theory]
    [InlineData(1, 42)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<ulong> xorAcross_ushort(ulong op1, ulong op2)
    {
        if (Sve.IsSupported)
        {
            //ARM64-FULL-LINE: cmpne {{p[0-9]+.d}}, {{p[0-9]+/z}}, {{z[0-9]+.d}}, #0
            //ARM64-FULL-LINE-NEXT: eorv {{v[0-9]+.d}}, {{p[0-9]+}}, {{z[0-9]+.d}}
            var vec = Vector.Create(op2);
            var mask = Vector.Create(op1);
            return Sve.XorAcross(Sve.ConditionalSelect(mask, vec, Vector<ulong>.Zero));
        }
        else
        {
            return Vector<ulong>.Zero;
        }
    }
}

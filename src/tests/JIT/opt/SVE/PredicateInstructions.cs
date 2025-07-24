// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class PredicateInstructions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static void TestPredicateInstructions()
    {
        if (Sve.IsSupported)
        {
            ZipLow();
            ZipHigh();
            UnzipOdd();
            UnzipEven();
            TransposeOdd();
            TransposeEven();
            ReverseElement();
            And();
            BitwiseClear();
            Xor();
            Or();
            ConditionalSelect();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ZipLow()
    {
        return Sve.ZipLow(Vector<short>.Zero, Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<uint> ZipHigh()
    {
        return Sve.ZipHigh(Sve.CreateTrueMaskUInt32(), Sve.CreateTrueMaskUInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<sbyte> UnzipEven()
    {
        return Sve.UnzipEven(Sve.CreateTrueMaskSByte(), Vector<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipOdd()
    {
        return Sve.UnzipOdd(Sve.CreateTrueMaskInt16(), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<long> TransposeEven()
    {
        return Sve.TransposeEven(Sve.CreateFalseMaskInt64(), Sve.CreateTrueMaskInt64());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeOdd()
    {
        return Sve.TransposeOdd(Vector<short>.Zero, Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ReverseElement()
    {
        return Sve.ReverseElement(Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> And()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.And(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> BitwiseClear()
    {
        return Sve.ConditionalSelect(
            Sve.CreateFalseMaskInt16(),
            Sve.BitwiseClear(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> Xor()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt32(),
            Sve.Xor(Sve.CreateTrueMaskInt32(), Sve.CreateTrueMaskInt32()),
            Vector<int>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> Or()
    {
        return Sve.ConditionalSelect(
            Sve.CreateTrueMaskInt16(),
            Sve.Or(Sve.CreateTrueMaskInt16(), Sve.CreateTrueMaskInt16()),
            Vector<short>.Zero
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelect()
    {
        return Sve.ConditionalSelect(
            Vector<int>.Zero,
            Sve.CreateFalseMaskInt32(),
            Sve.CreateTrueMaskInt32()
        );
    }
}
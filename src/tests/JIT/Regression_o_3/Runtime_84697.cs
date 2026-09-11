// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_84697;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_84697
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ok = 0;

        byte[] bytes = new byte[10];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)i;

        if (SumByteArrayNE(bytes) == 45) ok++;
        if (SumByteArrayLT(bytes) == 45) ok++;
        if (SumByteArrayNE(Array.Empty<byte>()) == 0) ok++;
        if (SumStringNE("hello") == 'h' + 'e' + 'l' + 'l' + 'o') ok++;
        if (SumSpanNE(new int[] { 1, 2, 3, 4, 5 }) == 15) ok++;
        if (SumNEDecreasing(bytes) == 45) ok++;
        if (SumNEStride2(bytes) == 0 + 2 + 4 + 6 + 8) ok++;
        if (SumNEFromOffset(bytes, 3) == 3 + 4 + 5 + 6 + 7 + 8 + 9) ok++;
        if (SumNEFromOffset(bytes, bytes.Length) == 0) ok++;

        // Soundness: when init > length and stride is +1, the NE loop would wrap
        // around without bounds checks if loop cloning got the entry guard wrong.
        // The slow path must run and throw IndexOutOfRangeException.
        bool threw = false;
        try
        {
            SumNEFromOffset(bytes, 100);
        }
        catch (IndexOutOfRangeException)
        {
            threw = true;
        }
        if (threw) ok++;

        // Soundness: a decreasing "i != someLen" loop indexing a different array.
        // The loop visits init, init-1, ... and could OOB-read accessArr if a
        // fast clone bypasses the access array's bounds checks. Whether or not
        // the loop is cloned, the expected behavior is throwing IndexOutOfRange.
        bool threw2 = false;
        try
        {
            SumDecreasingNETwoArrays(new byte[1], new byte[5], 1000);
        }
        catch (IndexOutOfRangeException)
        {
            threw2 = true;
        }
        if (threw2) ok++;

        return ok == 11 ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumByteArrayNE(byte[] src)
    {
        int sum = 0;
        for (int i = 0; i != src.Length; i++)
            sum += src[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumByteArrayLT(byte[] src)
    {
        int sum = 0;
        for (int i = 0; i < src.Length; i++)
            sum += src[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumStringNE(string s)
    {
        int sum = 0;
        for (int i = 0; i != s.Length; i++)
            sum += s[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumSpanNE(ReadOnlySpan<int> sp)
    {
        int sum = 0;
        for (int i = 0; i != sp.Length; i++)
            sum += sp[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumNEDecreasing(byte[] src)
    {
        int sum = 0;
        for (int i = src.Length - 1; i != -1; i--)
            sum += src[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumNEStride2(byte[] src)
    {
        int sum = 0;
        for (int i = 0; i != src.Length; i += 2)
            sum += src[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumNEFromOffset(byte[] src, int start)
    {
        int sum = 0;
        for (int i = start; i != src.Length; i++)
            sum += src[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumDecreasingNETwoArrays(byte[] limitArr, byte[] accessArr, int startIdx)
    {
        int sum = 0;
        for (int i = startIdx; i != limitArr.Length; i--)
            sum += accessArr[i];
        return sum;
    }
}

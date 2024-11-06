// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for the local masks optimization

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class AcrossAndCselToAcross
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T, T2>(T value, T2 value2) { }


    // Create a mask. Use it as a mask.
    // Conversion of mask1 will be removed.
    [Fact]
    [method: MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseMaskAsMask()
    {
        if (Sve.IsSupported)
        {
            Vector<ulong> mask1 = Sve.CreateWhileLessThanMask64Bit(2, 9); // Create lcl mask
            Vector<ulong> vec1  = Vector.Create<ulong>(5);
            Vector<ulong> vec2  = Sve.Compact(mask1, vec1); // Use as mask
            Consume(vec2);
        }
    }

    // Create a mask. Use it as a vector.
    // No conversions will be changed: Mask->Vector is optimal.
    [Fact]
    [method: MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseMaskAsVector()
    {
        if (Sve.IsSupported)
        {
            Vector<short> mask1 = Sve.CreateFalseMaskInt16(); // Create lcl mask
            Vector<short> vec1  = Vector.Create<short>(9);
            Vector<short> vec2  = Sve.Add(vec1, mask1); // Use as vector
            Consume(vec2);
        }
    }

    // Create a mask. Use it as a mask, then use as a vector.
    // Mask1 conversions will be switched.
    [Fact]
    [method: MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseMaskAsMaskAndVector()
    {
        if (Sve.IsSupported)
        {
            Vector<byte> mask1 = Sve.CreateWhileLessThanOrEqualMask8Bit(2, 9); // Create lcl mask
            Vector<byte> vec1  = Vector.Create<byte>(3);
            Vector<byte> vec2  = Vector.Create<byte>(4);
            Vector<byte> vec3  = Sve.ConditionalExtractAfterLastActiveElement(mask1, vec1, vec2); // Use as mask
            Vector<byte> vec4  = Sve.PopCount(mask1); // Use as vector
            Consume(vec3, vec4);
        }
    }

    [Fact]
    // Create a mask. Use it as a mask, then use as a vector inside a loop.
    // No conversions will be changed: vector use inside the loop dominates.
    [method: MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseMaskAsMaskAndVectorInsideLoop()
    {
        if (Sve.IsSupported)
        {
            Vector<short> mask1 = Sve.CreateFalseMaskInt16(); // Create lcl mask
            Vector<short> vec1  = Vector.Create<short>(3);
            Vector<short> vec2  = Vector.Create<short>(4);
            Vector<short> vec3  = Sve.Splice(mask1, vec1, vec2); // Use as mask

            for (int i = 0; i < 100; i++)
            {
                Vector<short> vec4 = Sve.ReverseElement8(mask1); // Use as vector
                Consume(vec3, vec4);
            }
        }
    }

}
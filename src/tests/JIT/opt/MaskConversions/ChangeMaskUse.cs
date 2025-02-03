// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for the masks conversion optimization
// Uses vectors as masks and vice versa.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using Xunit;

public class ChangeMaskUse
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T, T2>(T value, T2 value2) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeAddressExposed<T>(ref Vector<T> value) {}

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            Vector<int> vec = Vector.Create<int>(77);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 200; j++)
                {
                    UseMaskAsVector();
                    UseMaskAsMaskAndVector();
                    UseMaskAsMaskAndVectorInsideLoop();
                    UseMaskAsVectorAndMaskInsideLoop();
                    CastMaskUseAsVector();
                    CastMaskUseAsMask();
                    UseMaskAsMaskAndRef();
                    UseParamAsMask(vec);
                }

                Thread.Sleep(100);
            }
            UseMaskAsVector();
            UseMaskAsMaskAndVector();
            UseMaskAsMaskAndVectorInsideLoop();
            UseMaskAsVectorAndMaskInsideLoop();
            CastMaskUseAsVector();
            CastMaskUseAsMask();
            UseMaskAsMaskAndRef();
            UseParamAsMask(vec);
        }
    }

    // Create a mask. Use it as a vector.
    // No conversions will be changed: use as vector.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMaskAsVector()
    {
        Vector<short> mask1 = Sve.CreateFalseMaskInt16(); // Create lcl mask
        Vector<short> vec1  = Vector.Create<short>(9);
        Vector<short> vec2  = Sve.Add(vec1, mask1); // Use as vector
        Consume(vec2);
    }

    // Create a mask. Use it as a mask, then use as a vector.
    // No conversions will be changed: use as vector.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMaskAsMaskAndVector()
    {
        Vector<byte> mask1 = Sve.CreateWhileLessThanOrEqualMask8Bit(2, 9); // Create lcl mask
        Vector<byte> vec1  = Vector.Create<byte>(3);
        Vector<byte> vec2  = Vector.Create<byte>(4);
        Vector<byte> vec3  = Sve.ConditionalExtractAfterLastActiveElement(mask1, vec1, vec2); // Use as mask
        Vector<byte> vec4  = Sve.PopCount(mask1); // Use as vector
        Consume(vec3, vec4);
    }

    // Create a mask. Use it as a mask, then use as a vector inside a loop.
    // No conversions will be changed: use as vector.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMaskAsMaskAndVectorInsideLoop()
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

    // Create a mask. Use it as a vector, then use as a mask inside a loop.
    // No conversions will be changed: use as vector.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMaskAsVectorAndMaskInsideLoop()
    {
        Vector<int> vec1 = Vector.Create<int>(7); // Create lcl vector
        Vector<int> vec2 = Vector.Create<int>(3);
        Vector<int> vec3  = Sve.Add(vec1, vec2); // Use as vector

        for (int i = 0; i < 100; i++)
        {
            Vector<int> vec4  = Sve.Compact(vec1, vec3); // Use as mask
            Consume(vec3, vec4);
        }
    }

    // Create a mask, potentially bitcasting it. Use it as a vector.
    // No conversion due to the bitcasting.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CastMaskUseAsVector()
    {
        Vector<int> mask1;
        if (Environment.TickCount % 2 == 0)
          mask1 = Sve.CreateTrueMaskInt32();
        else
          mask1 = Unsafe.BitCast<Vector<uint>, Vector<int>>(Sve.CreateTrueMaskUInt32());
        Consume(mask1); // Use as vector
    }

    // Create a mask, potentially bitcasting it. Use it as a mask.
    // No conversion due to the bitcasting.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CastMaskUseAsMask()
    {
        Vector<int> mask1;
        if (Environment.TickCount % 2 == 0)
          mask1 = Sve.CreateTrueMaskInt32();
        else
          mask1 = Unsafe.BitCast<Vector<uint>, Vector<int>>(Sve.CreateTrueMaskUInt32());

        Vector<int> vec1  = Vector.Create<int>(25);
        Vector<int> vec2  = Sve.Compact(mask1, vec1); // Use as mask
        Consume(vec2);
    }

    // Create a mask. Use it as a mask and a reference.
    // No conversion due to the reference.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMaskAsMaskAndRef()
    {
        Vector<double> mask1 = Sve.CreateFalseMaskDouble(); // Create lcl mask
        Vector<double> vec1  = Vector.Create<double>(1.3);
        ConsumeAddressExposed(ref mask1); // Use as ref
        Vector<double> vec2  = Sve.Compact(mask1, vec1); // Use as mask
        Consume(vec2);
    }

    // Take a vector parameter. Use it as a mask
    // No conversion due to it being a parameter.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseParamAsMask(Vector<int> vecParam)
    {
        Vector<int> vec1 = Vector.Create<int>(73);
        Vector<int> vec2 = Sve.Compact(vecParam, vec1); // Use as mask
        Consume(vec2);
    }
}

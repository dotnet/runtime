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
    private static void ConsumrAddressExposed<T>(ref Vector<T> value) {}

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
                    UseMask1();
                    UseMask2();
                }

                Thread.Sleep(100);
            }
            UseMask1();
            UseMask2();
        }
    }

    // Create a mask and use it as a mask.
    // Mask will be used without going to local. Conversions will be optimised during lowering.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMask1()
    {
        Vector<ulong> mask1 = Sve.CreateWhileLessThanMask64Bit(2, 9); // Create local mask
        Vector<ulong> vec1  = Vector.Create<ulong>(5);
        Vector<ulong> vec2  = Sve.Compact(mask1, vec1); // Use as mask
        Consume(vec2);
    }

    // Create a mask and use it as a mask twice.
    // Will be switched to store as mask.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UseMask2()
    {
        Vector<int> mask1 = Sve.CreateTrueMaskInt32(SveMaskPattern.LargestMultipleOf3); // Create local mask
        Vector<int> vec1  = Vector.Create<int>(5);
        Vector<int> vec2  = Sve.Compact(mask1, vec1); // Use as mask
        Vector<int> vec3  = Sve.ConditionalExtractAfterLastActiveElement(mask1, vec1, vec1); // Use as mask
        Consume(vec2, vec3);
    }


}

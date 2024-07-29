// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using Xunit;

// Test folding of addressing expressions

public class Program
{
    struct S
    {
        public float f0;
        public float f1;
        public float f2;
        public float f3;
        public float f4;
        public float f5;
        public float f6;
        public float f7;
        public float f8;
        public float f9;
        public float f10;
        public float f11;
        public float f12;
        public float f13;
        public float f14;
        public float f15;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe int Test(ref S s, Vector128<float> v, int offset)
    {
        int returnVal = 100;

        if (Sse2.IsSupported)
        {
            fixed (float* p = &s.f0)
            {
                // We need an address aligned on 16 bytes, so we need to add a *float* offset to get there.
                int alignmentOffset = (0x10 - ((int)p & 0xc)) >> 2;
                try
                {
                    // This is the aligned case.
                    // We're going to store a scalar at an offset of 2 from the aligned location.
                    // As it happens, we know that the struct has been initialized to all zeros,
                    // and the vector passed in was all ones, so now we have a one at offset 2.
                    Sse2.StoreScalar(p + alignmentOffset + 2, Sse2.Subtract(v, Sse2.LoadAlignedVector128(p + offset + alignmentOffset + 4)));

                    // Now do a load from the aligned location.
                    // That should give us {0, 0, 1, 0}.
                    Vector128<float> v2;
                    if (Sse41.IsSupported)
                    {
                        v2 = Sse41.LoadAlignedVector128NonTemporal((byte*)(p + alignmentOffset)).AsSingle();
                    }
                    else
                    {
                        v2 = Sse2.LoadVector128((byte*)(p + alignmentOffset)).AsSingle();
                    }
                    if (!v2.Equals(Vector128.Create(0.0F, 0.0F, 1.0F, 0.0F)))
                    {
                        Console.WriteLine("Aligned case FAILED: v2 = " + v2);
                        returnVal = -1;
                    }

                    // This is the unaligned case. The value we're loading to subtract is one element earlier than what we just stored.
                    // So we're doing { 1, 1, 1, 1 } - { 0, 1, 0, 0 } = { 1, 0, 1, 1 }
                    Sse2.Store(p + alignmentOffset + 1, Sse2.Subtract(v, Sse2.LoadVector128(p + offset + alignmentOffset + 1)));
                    // Now do an unaligned load from that location.
                    v2 = Sse2.LoadVector128(p + alignmentOffset + 1);
                    if (!v2.Equals(Vector128.Create(1.0F, 0.0F, 1.0F, 1.0F)))
                    {
                        Console.WriteLine("Unaligned case FAILED: v2 = " + v2);
                        returnVal = -1;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception: " + e.Message);
                    returnVal = -1;
                }
            }
        }
        return returnVal;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe int Test256(ref S s, Vector256<float> v, int offset)
    {
        int returnVal = 100;
        if (Avx.IsSupported)
        {
            // offset must be a multiple of the vector size in floats.
            offset &= ~3;
            fixed (float* p = &s.f0)
            {
                try
                {
                    Avx.Store(p + 1, Avx.Subtract(v, Avx.LoadVector256(p + offset + 1)));
                    Vector256<float> v2 = Avx.LoadVector256(p + 1);
                    if (!v2.Equals(v))
                    {
                        Console.WriteLine("Vector256 case FAILED: v = " + v + ", v2 = " + v2);
                        returnVal = -1;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception: " + e.Message);
                    returnVal = -1;
                }
            }
        }
        return returnVal;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S s = new S();
        Vector128<float> v = Vector128.Create(1.0F);
        int returnVal = Test(ref s, v, 0);
        if (returnVal != 100)
        {
            Console.WriteLine("Vector128 test failed.");
        }

        // Get a new vector initialized to zeros.
        S s2 = new S();
        Vector256<float> v2 = Vector256.Create(1.0F);
        if (Test256(ref s2, v2, 4) != 100)
        {
            Console.WriteLine("Vector256 test failed.");
            returnVal = -1;
        }
        return returnVal;
    }
}

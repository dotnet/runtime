// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using static System.Runtime.Intrinsics.X86.Avx2;
using Xunit;

public class GitHub_25039
{
    static ReadOnlySpan<byte> PermTable => new byte[]
    {
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
        0, 1, 2, 3, 4, 5, 6, 7, /* 0*/
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe Vector256<int> GetPermutation(byte* pBase, int pvbyte)
    {
        Debug.Assert(pvbyte >= 0);
        Debug.Assert(pvbyte < 255);
        Debug.Assert(pBase != null);
        return ConvertToVector256Int32(pBase + pvbyte * 8);
    }

    [Fact]
    public static unsafe int TestEntryPoint()
    {
        if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
        {
            try
            {
                var src = new int[1024];
                fixed (int* pSrc = &src[0])
                fixed (byte* pBase = &PermTable[0])
                {

                    for (var i = 0; i < 100; i++)
                    {
                        var srcv = LoadDquVector256(pSrc + i);
                        var pe = i & 0x7;
                        var permuted = PermuteVar8x32(srcv, GetPermutation(pBase, (int)pe));
                        Store(pSrc + i, permuted);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed with exception " + e.Message);
                return -1;
            }
        }
        return 100;
    }
}

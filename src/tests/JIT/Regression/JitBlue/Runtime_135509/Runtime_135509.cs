// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public static class Runtime_135509
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (!AdvSimd.Arm64.IsSupported)
        {
            return 100;
        }

        var a = new byte[64 * 16];
        for (int i = 0; i < a.Length; i++)
        {
            a[i] = (byte)(i * 7 + 3);
        }
        ulong result = Test(a, 3);
        Console.WriteLine(result);
        return result == 4292821915693441957UL ? 100 : 1;
    }

    static Vector128<byte> L(byte[] a, int i) => Vector128.Create(a, i * 16);

    // Keep the vectors live across all table lookups to force consecutive-register copies and spills.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ulong Test(byte[] a, int k)
    {
        Vector128<byte> v0 = L(a, 0);
        Vector128<byte> v1 = L(a, 1);
        Vector128<byte> v2 = L(a, 2);
        Vector128<byte> v3 = L(a, 3);
        Vector128<byte> v4 = L(a, 4);
        Vector128<byte> v5 = L(a, 5);
        Vector128<byte> v6 = L(a, 6);
        Vector128<byte> v7 = L(a, 7);
        Vector128<byte> v8 = L(a, 8);
        Vector128<byte> v9 = L(a, 9);
        Vector128<byte> v10 = L(a, 10);
        Vector128<byte> v11 = L(a, 11);
        Vector128<byte> v12 = L(a, 12);
        Vector128<byte> v13 = L(a, 13);
        Vector128<byte> v14 = L(a, 14);
        Vector128<byte> v15 = L(a, 15);
        Vector128<byte> v16 = L(a, 16);
        Vector128<byte> v17 = L(a, 17);
        Vector128<byte> v18 = L(a, 18);
        Vector128<byte> v19 = L(a, 19);
        Vector128<byte> v20 = L(a, 20);
        Vector128<byte> v21 = L(a, 21);
        Vector128<byte> v22 = L(a, 22);
        Vector128<byte> v23 = L(a, 23);
        Vector128<byte> v24 = L(a, 24);
        Vector128<byte> v25 = L(a, 25);
        Vector128<byte> v26 = L(a, 26);
        Vector128<byte> idx = L(a, 30) & Vector128.Create((byte)63);
        Vector128<byte> r = Vector128<byte>.Zero;
        for (int i = 0; i < k; i++)
        {
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v0, (v1, v2, v3, v4), idx);
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v4, (v5, v6, v7, v9), idx + v8);
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v9, (v10, v11, v12, v14), idx + v13);
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v14, (v15, v16, v17, v19), idx + v18);
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v19, (v20, v21, v22, v24), idx + v23);
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v24, (v25, v26, v1, v3), idx + v2);
            r ^= AdvSimd.Arm64.VectorTableLookupExtension(v26, (v0, v25, v1, v5), idx + v3);
        }
        r = r ^ v0 ^ v1 ^ v2 ^ v3 ^ v4 ^ v5 ^ v6 ^ v7 ^ v8 ^ v9 ^ v10 ^ v11 ^ v12 ^ v13 ^ v14 ^ v15 ^ v16 ^ v17 ^ v18 ^ v19 ^ v20 ^ v21 ^ v22 ^ v23 ^ v24 ^ v25 ^ v26;
        return r.AsUInt64().GetElement(0);
    }
}

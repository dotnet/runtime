// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Hashing
{
    public sealed partial class Adler32 : NonCryptographicHashAlgorithm
    {
        // Commented out until an Avx2 implementation is available.
        // [MethodImpl(MethodImplOptions.NoInlining)]
        // private static uint UpdateScalarAvx2(uint adler, ReadOnlySpan<byte> buf)
        // {
        //     return adler;
        // }

        // Commented out until an Ssse3 based implementation is available.
        // [MethodImpl(MethodImplOptions.NoInlining)]
        // private static uint UpdateScalarSse(uint adler, ReadOnlySpan<byte> buf)
        // {
        //     return adler;
        // }

        // Commented out until an Arm based implementation is available.
        // [MethodImpl(MethodImplOptions.NoInlining)]
        // private static uint UpdateScalarArm(uint adler, ReadOnlySpan<byte> buf)
        // {
        //     return adler;
        // }
    }
}

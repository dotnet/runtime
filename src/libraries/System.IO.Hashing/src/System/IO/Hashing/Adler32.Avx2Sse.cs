// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Hashing
{
    public sealed partial class Adler32 : NonCryptographicHashAlgorithm
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateScalarAvx2(uint adler, ReadOnlySpan<byte> buf)
        {
            // Placeholder implementation until a Avx2 implementation is provided.
            return adler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateScalarSse(uint adler, ReadOnlySpan<byte> buf)
        {
            // Placeholder implementation until a Sse implementation is provided.
            return adler;
        }
    }
}

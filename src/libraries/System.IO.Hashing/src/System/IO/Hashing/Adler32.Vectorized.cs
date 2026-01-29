// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Hashing
{
    public sealed partial class Adler32 : NonCryptographicHashAlgorithm
    {
        // We check for little endian byte order here in case we're ever on ARM in big endian mode.
        // All of these checks except the length check are elided by JIT, so the JITted implementation
        // will be either a return false or a length check against a constant. This means this method
        // should be inlined into the caller.
        private static bool CanBeVectorized(ReadOnlySpan<byte> source) =>
            BitConverter.IsLittleEndian
            && VectorHelper.IsSupported
            && source.Length > Vector128<byte>.Count;
    }
}

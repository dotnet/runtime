// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.IO.Hashing
{
    /// <summary>
    ///   Provides an implementation of the Adler-32 algorithm, as used in
    ///   RFC1950.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The Adler-32 algorithm is designed for fast, lightweight integrity checking and is commonly used in
    ///     data compression and transmission scenarios. This class is not suitable for cryptographic purposes.
    ///   </para>
    ///   <para>
    ///     Adler-32 is not as robust as other checksum algorithms like CRC32, but it is faster to compute.
    ///     It also originally comes from zlib.
    ///   </para>
    ///   <para>
    ///     The Adler-32 checksum is stored as s2*65536 + s1 in most-significant-byte first(network) order.
    ///   </para>
    /// </remarks>
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateVectorized(uint adler, ReadOnlySpan<byte> source)
        {
            // Placeholder implementation until a vectorized implementation is provided.
            return adler;
        }
    }
}

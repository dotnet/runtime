// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Encodings.Web
{
    internal sealed partial class OptimizedInboxTextEncoder
    {
        /// <summary>
        /// Reads 32 bits of data (machine-endian) from <paramref name="ptr"/> and returns a
        /// <see cref="Vector128{Byte}"/> whose low 32 bits contain that data. The other bits of
        /// the returned vector are not guaranteed to contain useful data.
        /// </summary>
        /// <remarks>
        /// The pointer does not need to be aligned.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<byte> LoadScalar32OfByteFrom(void* ptr)
        {
            // !! NOTE !!
            // Pointer may be unaligned, only call instructions that can deal with unaligned accesses.

            if (Sse2.IsSupported)
            {
                return Sse2.LoadScalarVector128((uint*)ptr).AsByte();
            }
            else
            {
                return Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<uint>(ptr)).AsByte();
            }
        }

        /// <summary>
        /// Reads 64 bits of data (machine-endian) from <paramref name="ptr"/> and returns a
        /// <see cref="Vector128{Byte}"/> whose low 64 bits contain that data. The other bits of
        /// the returned vector are not guaranteed to contain useful data.
        /// </summary>
        /// <remarks>
        /// The pointer does not need to be aligned.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<byte> LoadScalar64OfByteFrom(void* ptr)
        {
            // !! NOTE !!
            // Pointer may be unaligned, only call instructions that can deal with unaligned accesses.

            if (Sse2.IsSupported)
            {
                return Sse2.LoadScalarVector128((ulong*)ptr).AsByte();
            }
            else
            {
                return Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<ulong>(ptr)).AsByte();
            }
        }

        private unsafe partial struct AllowedAsciiCodePoints
        {
            [FieldOffset(0)]
            internal Vector128<byte> AsVector;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types

#if TARGET_64BIT
using nint_t = System.Int64;
using nuint_t = System.UInt64;
#else
using nint_t = System.Int32;
using nuint_t = System.UInt32;
#endif

namespace System.Buffers.Binary
{
    /// <summary>
    /// Reads bytes as primitives with specific endianness
    /// </summary>
    /// <remarks>
    /// Use these helpers when you need to read specific endianness.
    /// </remarks>
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// This is a no-op and added only for consistency.
        /// This allows the caller to read a struct of numeric primitives and reverse each field
        /// rather than having to skip sbyte fields.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReverseEndianness(sbyte value) => value;

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReverseEndianness(short value) => (short)ReverseEndianness((ushort)value);

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReverseEndianness(int value) => (int)ReverseEndianness((uint)value);

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReverseEndianness(long value) => (long)ReverseEndianness((ulong)value);

        /// <summary>Reverses a primitive value by performing an endianness swap of the specified <see cref="nint"/> value.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint ReverseEndianness(nint value) => (nint)ReverseEndianness((nint_t)value);

        /// <summary>Reverses a primitive value by performing an endianness swap of the specified <see cref="Int128"/> value.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 ReverseEndianness(Int128 value)
        {
            return new Int128(
                ReverseEndianness(value.Lower),
                ReverseEndianness(value.Upper)
            );
        }

        /// <summary>
        /// This is a no-op and added only for consistency.
        /// This allows the caller to read a struct of numeric primitives and reverse each field
        /// rather than having to skip byte fields.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReverseEndianness(byte value) => value;

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReverseEndianness(ushort value)
        {
            // Don't need to AND with 0xFF00 or 0x00FF since the final
            // cast back to ushort will clear out all bits above [ 15 .. 00 ].
            // This is normally implemented via "movzx eax, ax" on the return.
            // Alternatively, the compiler could elide the movzx instruction
            // entirely if it knows the caller is only going to access "ax"
            // instead of "eax" / "rax" when the function returns.

            return (ushort)((value >> 8) + (value << 8));
        }

        /// <summary>
        /// Reverses a 16-bit character value - performs an endianness swap
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ReverseEndianness(char value) => (char)ReverseEndianness((ushort)value);

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReverseEndianness(uint value)
        {
            // This takes advantage of the fact that the JIT can detect
            // ROL32 / ROR32 patterns and output the correct intrinsic.
            //
            // Input: value = [ ww xx yy zz ]
            //
            // First line generates : [ ww xx yy zz ]
            //                      & [ 00 FF 00 FF ]
            //                      = [ 00 xx 00 zz ]
            //             ROR32(8) = [ zz 00 xx 00 ]
            //
            // Second line generates: [ ww xx yy zz ]
            //                      & [ FF 00 FF 00 ]
            //                      = [ ww 00 yy 00 ]
            //             ROL32(8) = [ 00 yy 00 ww ]
            //
            //                (sum) = [ zz yy xx ww ]
            //
            // Testing shows that throughput increases if the AND
            // is performed before the ROL / ROR.

            return BitOperations.RotateRight(value & 0x00FF00FFu, 8) // xx zz
                + BitOperations.RotateLeft(value & 0xFF00FF00u, 8); // ww yy
        }

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary>
        [CLSCompliant(false)]
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReverseEndianness(ulong value)
        {
            // Operations on 32-bit values have higher throughput than
            // operations on 64-bit values, so decompose.

            return ((ulong)ReverseEndianness((uint)value) << 32)
                + ReverseEndianness((uint)(value >> 32));
        }

        /// <summary>Reverses a primitive value by performing an endianness swap of the specified <see cref="nuint"/> value.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint ReverseEndianness(nuint value) => (nuint)ReverseEndianness((nuint_t)value);

        /// <summary>Reverses a primitive value by performing an endianness swap of the specified <see cref="UInt128"/> value.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 ReverseEndianness(UInt128 value)
        {
            return new UInt128(
                ReverseEndianness(value.Lower),
                ReverseEndianness(value.Upper)
            );
        }
    }
}

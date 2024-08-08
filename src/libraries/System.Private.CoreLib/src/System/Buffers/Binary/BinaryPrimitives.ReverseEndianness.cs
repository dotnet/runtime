// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="sbyte" /> value, which effectively does nothing for an <see cref="sbyte" />.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The passed-in value, unmodified.</returns>
        /// <remarks>This method effectively does nothing and was added only for consistency.</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReverseEndianness(sbyte value) => value;

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="short" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReverseEndianness(short value) => (short)ReverseEndianness((ushort)value);

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="int" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReverseEndianness(int value) => (int)ReverseEndianness((uint)value);

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="long" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReverseEndianness(long value) => (long)ReverseEndianness((ulong)value);

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="nint" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint ReverseEndianness(nint value) => (nint)ReverseEndianness((nint_t)value);

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="Int128" /> value.
        /// </summary>
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
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="byte" /> value, which effectively does nothing for an <see cref="byte" />.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The passed-in value, unmodified.</returns>
        /// <remarks>This method effectively does nothing and was added only for consistency.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReverseEndianness(byte value) => value;

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="ushort" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
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
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="char" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ReverseEndianness(char value) => (char)ReverseEndianness((ushort)value);

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="uint" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
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
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="ulong" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
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

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="nuint" /> value.
        /// </summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>The reversed value.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint ReverseEndianness(nuint value) => (nuint)ReverseEndianness((nuint_t)value);

        /// <summary>
        /// Reverses a primitive value by performing an endianness swap of the specified <see cref="UInt128" /> value.
        /// </summary>
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

        /// <summary>Copies every primitive value from <paramref name="source"/> to <paramref name="destination"/>, reversing each primitive by performing an endianness swap as part of writing each.</summary>
        /// <param name="source">The source span to copy.</param>
        /// <param name="destination">The destination to which the source elements should be copied.</param>
        /// <remarks>The source and destination spans may overlap. The same span may be passed as both the source and the destination in order to reverse each element's endianness in place.</remarks>
        /// <exception cref="ArgumentException">The <paramref name="destination"/>'s length is smaller than that of the <paramref name="source"/>.</exception>
        [CLSCompliant(false)]
        public static void ReverseEndianness(ReadOnlySpan<ushort> source, Span<ushort> destination) =>
            ReverseEndianness<short, Int16EndiannessReverser>(MemoryMarshal.Cast<ushort, short>(source), MemoryMarshal.Cast<ushort, short>(destination));

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        public static void ReverseEndianness(ReadOnlySpan<short> source, Span<short> destination) =>
            ReverseEndianness<short, Int16EndiannessReverser>(source, destination);

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        [CLSCompliant(false)]
        public static void ReverseEndianness(ReadOnlySpan<uint> source, Span<uint> destination) =>
            ReverseEndianness<int, Int32EndiannessReverser>(MemoryMarshal.Cast<uint, int>(source), MemoryMarshal.Cast<uint, int>(destination));

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        public static void ReverseEndianness(ReadOnlySpan<int> source, Span<int> destination) =>
            ReverseEndianness<int, Int32EndiannessReverser>(source, destination);

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        [CLSCompliant(false)]
        public static void ReverseEndianness(ReadOnlySpan<ulong> source, Span<ulong> destination) =>
            ReverseEndianness<long, Int64EndiannessReverser>(MemoryMarshal.Cast<ulong, long>(source), MemoryMarshal.Cast<ulong, long>(destination));

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        public static void ReverseEndianness(ReadOnlySpan<long> source, Span<long> destination) =>
            ReverseEndianness<long, Int64EndiannessReverser>(source, destination);

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        [CLSCompliant(false)]
        public static void ReverseEndianness(ReadOnlySpan<nuint> source, Span<nuint> destination) =>
#if TARGET_64BIT
            ReverseEndianness<long, Int64EndiannessReverser>(MemoryMarshal.Cast<nuint, long>(source), MemoryMarshal.Cast<nuint, long>(destination));
#else
            ReverseEndianness<int, Int32EndiannessReverser>(MemoryMarshal.Cast<nuint, int>(source), MemoryMarshal.Cast<nuint, int>(destination));
#endif

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        public static void ReverseEndianness(ReadOnlySpan<nint> source, Span<nint> destination) =>
#if TARGET_64BIT
            ReverseEndianness<long, Int64EndiannessReverser>(MemoryMarshal.Cast<nint, long>(source), MemoryMarshal.Cast<nint, long>(destination));
#else
            ReverseEndianness<int, Int32EndiannessReverser>(MemoryMarshal.Cast<nint, int>(source), MemoryMarshal.Cast<nint, int>(destination));
#endif

        private readonly struct Int16EndiannessReverser : IEndiannessReverser<short>
        {
            public static short Reverse(short value) =>
                ReverseEndianness(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<short> Reverse(Vector128<short> vector) =>
                Vector128.ShiftLeft(vector, 8) | Vector128.ShiftRightLogical(vector, 8);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<short> Reverse(Vector256<short> vector) =>
                Vector256.ShiftLeft(vector, 8) | Vector256.ShiftRightLogical(vector, 8);
        }

        private readonly struct Int32EndiannessReverser : IEndiannessReverser<int>
        {
            public static int Reverse(int value) =>
                ReverseEndianness(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<int> Reverse(Vector128<int> vector) =>
                Vector128.Shuffle(vector.AsByte(), Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)).AsInt32();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<int> Reverse(Vector256<int> vector) =>
                Vector256.Shuffle(vector.AsByte(), Vector256.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12, 19, 18, 17, 16, 23, 22, 21, 20, 27, 26, 25, 24, 31, 30, 29, 28)).AsInt32();
        }

        private readonly struct Int64EndiannessReverser : IEndiannessReverser<long>
        {
            public static long Reverse(long value) =>
                ReverseEndianness(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<long> Reverse(Vector128<long> vector) =>
                Vector128.Shuffle(vector.AsByte(), Vector128.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)).AsInt64();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<long> Reverse(Vector256<long> vector) =>
                Vector256.Shuffle(vector.AsByte(), Vector256.Create((byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)).AsInt64();
        }

        private static void ReverseEndianness<T, TReverser>(ReadOnlySpan<T> source, Span<T> destination)
            where T : struct
            where TReverser : IEndiannessReverser<T>
        {
            if (destination.Length < source.Length)
            {
                ThrowDestinationTooSmall();
            }

            ref T sourceRef = ref MemoryMarshal.GetReference(source);
            ref T destRef = ref MemoryMarshal.GetReference(destination);

            if (Unsafe.AreSame(in sourceRef, in destRef) ||
                !source.Overlaps(destination, out int elementOffset) ||
                elementOffset < 0)
            {
                // Either there's no overlap between the source and the destination, or there's overlap but the
                // destination starts at or before the source.  That means we can safely iterate from beginning
                // to end of the source and not have to worry about writing into the destination and clobbering
                // source data we haven't yet read.

                int i = 0;

                if (Vector256.IsHardwareAccelerated)
                {
                    while (i <= source.Length - Vector256<T>.Count)
                    {
                        Vector256.StoreUnsafe(TReverser.Reverse(Vector256.LoadUnsafe(ref sourceRef, (uint)i)), ref destRef, (uint)i);
                        i += Vector256<T>.Count;
                    }
                }

                if (Vector128.IsHardwareAccelerated)
                {
                    while (i <= source.Length - Vector128<T>.Count)
                    {
                        Vector128.StoreUnsafe(TReverser.Reverse(Vector128.LoadUnsafe(ref sourceRef, (uint)i)), ref destRef, (uint)i);
                        i += Vector128<T>.Count;
                    }
                }

                while (i < source.Length)
                {
                    Unsafe.Add(ref destRef, i) = TReverser.Reverse(Unsafe.Add(ref sourceRef, i));
                    i++;
                }
            }
            else
            {
                // There's overlap between the source and the destination, and the source starts before the destination.
                // That means if we were to iterate from beginning to end, reading from the source and writing to the
                // destination, we'd overwrite source elements not yet read.  To avoid that, we iterate from end to beginning.

                int i = source.Length;

                if (Vector256.IsHardwareAccelerated)
                {
                    while (i >= Vector256<T>.Count)
                    {
                        i -= Vector256<T>.Count;
                        Vector256.StoreUnsafe(TReverser.Reverse(Vector256.LoadUnsafe(ref sourceRef, (uint)i)), ref destRef, (uint)i);
                    }
                }

                if (Vector128.IsHardwareAccelerated)
                {
                    while (i >= Vector128<T>.Count)
                    {
                        i -= Vector128<T>.Count;
                        Vector128.StoreUnsafe(TReverser.Reverse(Vector128.LoadUnsafe(ref sourceRef, (uint)i)), ref destRef, (uint)i);
                    }
                }

                while (i > 0)
                {
                    i--;
                    Unsafe.Add(ref destRef, i) = TReverser.Reverse(Unsafe.Add(ref sourceRef, i));
                }
            }
        }

        private interface IEndiannessReverser<T> where T : struct
        {
            static abstract T Reverse(T value);
            static abstract Vector128<T> Reverse(Vector128<T> vector);
            static abstract Vector256<T> Reverse(Vector256<T> vector);
        }

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        [CLSCompliant(false)]
        public static void ReverseEndianness(ReadOnlySpan<UInt128> source, Span<UInt128> destination) =>
            ReverseEndianness(MemoryMarshal.Cast<UInt128, Int128>(source), MemoryMarshal.Cast<UInt128, Int128>(destination));

        /// <inheritdoc cref="ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})" />
        public static void ReverseEndianness(ReadOnlySpan<Int128> source, Span<Int128> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowDestinationTooSmall();
            }

            if (Unsafe.AreSame(in MemoryMarshal.GetReference(source), in MemoryMarshal.GetReference(destination)) ||
                !source.Overlaps(destination, out int elementOffset) ||
                elementOffset < 0)
            {
                // Iterate from beginning to end
                for (int i = 0; i < source.Length; i++)
                {
                    destination[i] = ReverseEndianness(source[i]);
                }
            }
            else
            {
                // Iterate from end to beginning
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    destination[i] = ReverseEndianness(source[i]);
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowDestinationTooSmall() =>
            throw new ArgumentException(SR.Arg_BufferTooSmall, "destination");
    }
}

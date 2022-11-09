// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
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

            if (Unsafe.AreSame(ref sourceRef, ref destRef) ||
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

            if (Unsafe.AreSame(ref MemoryMarshal.GetReference(source), ref MemoryMarshal.GetReference(destination)) ||
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

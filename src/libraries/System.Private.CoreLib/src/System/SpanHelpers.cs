// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System
{
    internal static partial class SpanHelpers
    {
        public static unsafe void ClearWithReferences(ref IntPtr ip, nuint pointerSizeLength)
        {
            Debug.Assert(Unsafe.IsOpportunisticallyAligned(ref ip, (uint)sizeof(IntPtr)), "Should've been aligned on natural word boundary.");

            // First write backward 8 natural words at a time.
            // Writing backward allows us to get away with only simple modifications to the
            // mov instruction's base and index registers between loop iterations.

            for (; pointerSizeLength >= 8; pointerSizeLength -= 8)
            {
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -4) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -5) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -6) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -7) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -8) = default;
            }

            Debug.Assert(pointerSizeLength <= 7);

            // The logic below works by trying to minimize the number of branches taken for any
            // given range of lengths. For example, the lengths [ 4 .. 7 ] are handled by a single
            // branch, [ 2 .. 3 ] are handled by a single branch, and [ 1 ] is handled by a single
            // branch.
            //
            // We can write both forward and backward as a perf improvement. For example,
            // the lengths [ 4 .. 7 ] can be handled by zeroing out the first four natural
            // words and the last 3 natural words. In the best case (length = 7), there are
            // no overlapping writes. In the worst case (length = 4), there are three
            // overlapping writes near the middle of the buffer. In perf testing, the
            // penalty for performing duplicate writes is less expensive than the penalty
            // for complex branching.

            if (pointerSizeLength >= 4)
            {
                goto Write4To7;
            }
            else if (pointerSizeLength >= 2)
            {
                goto Write2To3;
            }
            else if (pointerSizeLength > 0)
            {
                goto Write1;
            }
            else
            {
                return; // nothing to write
            }

        Write4To7:
            Debug.Assert(pointerSizeLength >= 4);

            // Write first four and last three.
            Unsafe.Add(ref ip, 2) = default;
            Unsafe.Add(ref ip, 3) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = default;

        Write2To3:
            Debug.Assert(pointerSizeLength >= 2);

            // Write first two and last one.
            Unsafe.Add(ref ip, 1) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = default;

        Write1:
            Debug.Assert(pointerSizeLength >= 1);

            // Write only element.
            ip = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse<T>(Span<T> span)
        {
            Debug.Assert(span.Length > 1);

            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    ReverseValueType<T, byte>(span);
                    return;
                }
                if (sizeof(T) == sizeof(ushort))
                {
                    ReverseValueType<T, ushort>(span);
                    return;
                }
                if (sizeof(T) == sizeof(uint))
                {
                    ReverseValueType<T, uint>(span);
                    return;
                }
                if (sizeof(T) == sizeof(ulong))
                {
                    ReverseValueType<T, ulong>(span);
                    return;
                }
            }

            while (span.Length > 1)
            {
                T first = span[0];
                span[0] = span[span.Length - 1];
                span[span.Length - 1] = first;
                span = span.Slice(1);
                span = span.Slice(0, span.Length - 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReverseValueType<T, TElem>(Span<T> span)
            where TElem : struct
        {
            if (span.Length >= 4)
            {
                // TElem may be more strictly aligned than T, so the reinterpreted span is only accessed via
                // IChunkReverser, which doesn't require alignment.
                int reversed = ReverseChunks(Unsafe.BitCast<Span<T>, Span<TElem>>(span));
                span = span.Slice(reversed);
                span = span.Slice(0, span.Length - reversed);
            }

            while (span.Length > 1)
            {
                // Hold the temporary as TElem to avoid field-by-field copies of T; elements are still accessed as T.
                TElem first = Unsafe.BitCast<T, TElem>(span[0]);
                span[0] = span[span.Length - 1];
                span[span.Length - 1] = Unsafe.BitCast<TElem, T>(first);
                span = span.Slice(1);
                span = span.Slice(0, span.Length - 1);
            }
        }

        // Reverses chunks from both ends, from the widest to the narrowest reverser, and returns
        // the number of elements reversed at each end.
        private static int ReverseChunks<TElem>(Span<TElem> span)
            where TElem : struct
        {
            Span<TElem> remaining = ReverseChunks<TElem, Vector512Reverser<TElem>>(span);
            remaining = ReverseChunks<TElem, Vector256Reverser<TElem>>(remaining);
            remaining = ReverseChunks<TElem, Vector128Reverser<TElem>>(remaining);
            remaining = ReverseChunks<TElem, UInt64Reverser<TElem>>(remaining);
            remaining = ReverseChunks<TElem, UInt32Reverser<TElem>>(remaining);
            return (span.Length - remaining.Length) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<TElem> ReverseChunks<TElem, TReverser>(Span<TElem> span)
            where TReverser : IChunkReverser<TElem>
        {
            if (TReverser.IsSupported)
            {
                while (span.Length >= 2 * TReverser.Count)
                {
                    Span<TElem> last = span.Slice(span.Length - TReverser.Count);
                    TReverser.ReverseAndSwap(span, last);
                    span = span.Slice(TReverser.Count);
                    span = span.Slice(0, span.Length - TReverser.Count);
                }

                if (TReverser.CanOverlap && span.Length >= TReverser.Count)
                {
                    // Both overlapping chunks are read before either is written.
                    TReverser.ReverseAndSwap(span, span.Slice(span.Length - TReverser.Count));
                    return default;
                }
            }
            return span;
        }

        private interface IChunkReverser<TElem>
        {
            static abstract bool IsSupported { get; }

            static abstract int Count { get; }

            // Whether the last two chunks may overlap. Worth it only for bytes: for wider elements, the scalar tail
            // is short and partially overlapping stores can cause store-forwarding stalls.
            static abstract bool CanOverlap { get; }

            // Reverses the first Count elements of both spans and swaps them.
            static abstract void ReverseAndSwap(Span<TElem> first, Span<TElem> last);
        }

        private readonly struct Vector512Reverser<TElem> : IChunkReverser<TElem>
        {
            public static bool IsSupported => Vector512.IsHardwareAccelerated;

            public static int Count => Vector512<TElem>.Count;

            public static bool CanOverlap => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ReverseAndSwap(Span<TElem> first, Span<TElem> last)
            {
                Vector512<TElem> a = Vector512.Create<TElem>(first);
                Vector512<TElem> b = Vector512.Create<TElem>(last);
                ReverseElements(b).CopyTo(first);
                ReverseElements(a).CopyTo(last);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<TElem> ReverseElements(Vector512<TElem> vector)
            {
                if (typeof(TElem) == typeof(byte))
                {
                    return Vector512.Shuffle(vector.AsByte(), Vector512.Create(
                        (byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48,
                        47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32,
                        31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)).As<byte, TElem>();
                }
                if (typeof(TElem) == typeof(ushort))
                {
                    return Vector512.Shuffle(vector.AsUInt16(), Vector512.Create(
                        (ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)).As<ushort, TElem>();
                }
                if (typeof(TElem) == typeof(uint))
                {
                    return Vector512.Shuffle(vector.AsUInt32(), Vector512.Create(15u, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)).As<uint, TElem>();
                }
                Debug.Assert(typeof(TElem) == typeof(ulong));
                return Vector512.Shuffle(vector.AsUInt64(), Vector512.Create(7ul, 6, 5, 4, 3, 2, 1, 0)).As<ulong, TElem>();
            }
        }

        private readonly struct Vector256Reverser<TElem> : IChunkReverser<TElem>
        {
            public static bool IsSupported => Vector256.IsHardwareAccelerated;

            public static int Count => Vector256<TElem>.Count;

            public static bool CanOverlap => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ReverseAndSwap(Span<TElem> first, Span<TElem> last)
            {
                Vector256<TElem> a = Vector256.Create<TElem>(first);
                Vector256<TElem> b = Vector256.Create<TElem>(last);
                ReverseElements(b).CopyTo(first);
                ReverseElements(a).CopyTo(last);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<TElem> ReverseElements(Vector256<TElem> vector)
            {
                if (typeof(TElem) == typeof(byte))
                {
                    return Vector256.Shuffle(vector.AsByte(), Vector256.Create(
                        (byte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)).As<byte, TElem>();
                }
                if (typeof(TElem) == typeof(ushort))
                {
                    return Vector256.Shuffle(vector.AsUInt16(), Vector256.Create((ushort)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)).As<ushort, TElem>();
                }
                if (typeof(TElem) == typeof(uint))
                {
                    return Vector256.Shuffle(vector.AsUInt32(), Vector256.Create(7u, 6, 5, 4, 3, 2, 1, 0)).As<uint, TElem>();
                }
                Debug.Assert(typeof(TElem) == typeof(ulong));
                return Vector256.Shuffle(vector.AsUInt64(), Vector256.Create(3ul, 2, 1, 0)).As<ulong, TElem>();
            }
        }

        private readonly struct Vector128Reverser<TElem> : IChunkReverser<TElem>
        {
            public static bool IsSupported => Vector128.IsHardwareAccelerated;

            public static int Count => Vector128<TElem>.Count;

            public static bool CanOverlap => false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ReverseAndSwap(Span<TElem> first, Span<TElem> last)
            {
                Vector128<TElem> a = Vector128.Create<TElem>(first);
                Vector128<TElem> b = Vector128.Create<TElem>(last);
                ReverseElements(b).CopyTo(first);
                ReverseElements(a).CopyTo(last);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<TElem> ReverseElements(Vector128<TElem> vector)
            {
                if (typeof(TElem) == typeof(byte))
                {
                    return Vector128.Shuffle(vector.AsByte(), Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)).As<byte, TElem>();
                }
                if (typeof(TElem) == typeof(ushort))
                {
                    return Vector128.Shuffle(vector.AsUInt16(), Vector128.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0)).As<ushort, TElem>();
                }
                if (typeof(TElem) == typeof(uint))
                {
                    return Vector128.Shuffle(vector.AsUInt32(), Vector128.Create(3u, 2, 1, 0)).As<uint, TElem>();
                }
                Debug.Assert(typeof(TElem) == typeof(ulong));
                return Vector128.Shuffle(vector.AsUInt64(), Vector128.Create(1ul, 0)).As<ulong, TElem>();
            }
        }

        private readonly struct UInt64Reverser<TElem> : IChunkReverser<TElem>
            where TElem : struct
        {
            public static bool IsSupported => sizeof(TElem) < sizeof(ulong);

            public static int Count => sizeof(ulong) / sizeof(TElem);

            public static bool CanOverlap => typeof(TElem) == typeof(byte);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ReverseAndSwap(Span<TElem> first, Span<TElem> last)
            {
                Span<byte> a = MemoryMarshal.AsBytes(first.Slice(0, Count));
                Span<byte> b = MemoryMarshal.AsBytes(last);
                ulong x = BinaryPrimitives.ReadUInt64LittleEndian(a);
                ulong y = BinaryPrimitives.ReadUInt64LittleEndian(b);
                BinaryPrimitives.WriteUInt64LittleEndian(a, ReverseElements(y));
                BinaryPrimitives.WriteUInt64LittleEndian(b, ReverseElements(x));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong ReverseElements(ulong value)
            {
                if (typeof(TElem) == typeof(byte))
                {
                    return BinaryPrimitives.ReverseEndianness(value);
                }
                value = ulong.RotateLeft(value, 32);
                if (typeof(TElem) == typeof(ushort))
                {
                    value = ((value >> 16) & 0x0000FFFF0000FFFF) | ((value & 0x0000FFFF0000FFFF) << 16);
                }
                return value;
            }
        }

        private readonly struct UInt32Reverser<TElem> : IChunkReverser<TElem>
            where TElem : struct
        {
            public static bool IsSupported => sizeof(TElem) < sizeof(uint);

            public static int Count => sizeof(uint) / sizeof(TElem);

            public static bool CanOverlap => typeof(TElem) == typeof(byte);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ReverseAndSwap(Span<TElem> first, Span<TElem> last)
            {
                Span<byte> a = MemoryMarshal.AsBytes(first.Slice(0, Count));
                Span<byte> b = MemoryMarshal.AsBytes(last);
                uint x = BinaryPrimitives.ReadUInt32LittleEndian(a);
                uint y = BinaryPrimitives.ReadUInt32LittleEndian(b);
                BinaryPrimitives.WriteUInt32LittleEndian(a, ReverseElements(y));
                BinaryPrimitives.WriteUInt32LittleEndian(b, ReverseElements(x));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint ReverseElements(uint value) =>
                typeof(TElem) == typeof(byte) ? BinaryPrimitives.ReverseEndianness(value) : uint.RotateLeft(value, 16);
        }
    }
}

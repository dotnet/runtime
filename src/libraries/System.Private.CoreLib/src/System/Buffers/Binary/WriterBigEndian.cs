// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Writes a <see cref="double" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 8 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="double" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleBigEndian(Span<byte> destination, double value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Writes a <see cref="Half" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 2 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="Half" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalfBigEndian(Span<byte> destination, Half value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = ReverseEndianness(BitConverter.HalfToInt16Bits(value));
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Writes an Int16 into a span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16BigEndian(Span<byte> destination, short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Writes an Int32 into a span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32BigEndian(Span<byte> destination, int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Writes an Int64 into a span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64BigEndian(Span<byte> destination, long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Writes a <see cref="float" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 4 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="float" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleBigEndian(Span<byte> destination, float value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = ReverseEndianness(BitConverter.SingleToInt32Bits(value));
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Write a UInt16 into a span of bytes as big endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16BigEndian(Span<byte> destination, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ushort tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Write a UInt32 into a span of bytes as big endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32BigEndian(Span<byte> destination, uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                uint tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Write a UInt64 into a span of bytes as big endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64BigEndian(Span<byte> destination, ulong value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ulong tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, ref tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, ref value);
            }
        }

        /// <summary>
        /// Writes a <see cref="double" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="double" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 8 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteDoubleBigEndian(Span<byte> destination, double value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Writes a <see cref="Half" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="Half" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 2 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteHalfBigEndian(Span<byte> destination, Half value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = ReverseEndianness(BitConverter.HalfToInt16Bits(value));
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Writes an Int16 into a span of bytes as big endian.
        /// </summary>
        /// <returns>If the span is too small to contain the value, return false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt16BigEndian(Span<byte> destination, short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Writes an Int32 into a span of bytes as big endian.
        /// </summary>
        /// <returns>If the span is too small to contain the value, return false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt32BigEndian(Span<byte> destination, int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Writes an Int64 into a span of bytes as big endian.
        /// </summary>
        /// <returns>If the span is too small to contain the value, return false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt64BigEndian(Span<byte> destination, long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Writes a <see cref="float" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="float" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 4 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteSingleBigEndian(Span<byte> destination, float value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = ReverseEndianness(BitConverter.SingleToInt32Bits(value));
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Write a UInt16 into a span of bytes as big endian.
        /// </summary>
        /// <returns>If the span is too small to contain the value, return false.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt16BigEndian(Span<byte> destination, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ushort tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Write a UInt32 into a span of bytes as big endian.
        /// </summary>
        /// <returns>If the span is too small to contain the value, return false.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt32BigEndian(Span<byte> destination, uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                uint tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }

        /// <summary>
        /// Write a UInt64 into a span of bytes as big endian.
        /// </summary>
        /// <returns>If the span is too small to contain the value, return false.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt64BigEndian(Span<byte> destination, ulong value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ulong tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, ref tmp);
            }

            return MemoryMarshal.TryWrite(destination, ref value);
        }
    }
}

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
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
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
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="short" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 2 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="short" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16BigEndian(Span<byte> destination, short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="int" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 4 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="int" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32BigEndian(Span<byte> destination, int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="long" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 8 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="long" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64BigEndian(Span<byte> destination, long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="Int128" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 16 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="Int128" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt128BigEndian(Span<byte> destination, Int128 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                Int128 tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="nint" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 4 bytes on 32-bit platforms -or- 8 bytes on 64-bit platforms to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="nint" />.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteIntPtrBigEndian(Span<byte> destination, nint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                nint tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
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
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Write a <see cref="ushort" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 2 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="ushort" />.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16BigEndian(Span<byte> destination, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ushort tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Write a <see cref="uint" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 4 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="uint" />.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32BigEndian(Span<byte> destination, uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                uint tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Write a <see cref="ulong" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 8 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="ulong" />.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64BigEndian(Span<byte> destination, ulong value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ulong tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="UInt128" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 16 bytes to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="UInt128" />.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt128BigEndian(Span<byte> destination, UInt128 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                UInt128 tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
            }
        }

        /// <summary>
        /// Writes a <see cref="nuint" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <remarks>Writes exactly 4 bytes on 32-bit platforms -or- 8 bytes on 64-bit platforms to the beginning of the span.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="destination" /> is too small to contain a <see cref="nuint" />.
        /// </exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUIntPtrBigEndian(Span<byte> destination, nuint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                nuint tmp = ReverseEndianness(value);
                MemoryMarshal.Write(destination, in tmp);
            }
            else
            {
                MemoryMarshal.Write(destination, in value);
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
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
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
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="short" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="short" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 2 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt16BigEndian(Span<byte> destination, short value)
        {
            if (BitConverter.IsLittleEndian)
            {
                short tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="int" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="int" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 4 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt32BigEndian(Span<byte> destination, int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                int tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="long" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="long" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 8 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt64BigEndian(Span<byte> destination, long value)
        {
            if (BitConverter.IsLittleEndian)
            {
                long tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="Int128" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="Int128" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 16 bytes to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt128BigEndian(Span<byte> destination, Int128 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                Int128 tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="nint" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="nint" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 4 bytes on 32-bit platforms -or- 8 bytes on 64-bit platforms to the beginning of the span.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteIntPtrBigEndian(Span<byte> destination, nint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                nint tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
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
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Write a <see cref="ushort" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="ushort" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 2 bytes to the beginning of the span.</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt16BigEndian(Span<byte> destination, ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ushort tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Write a <see cref="uint" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="uint" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 4 bytes to the beginning of the span.</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt32BigEndian(Span<byte> destination, uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                uint tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Write a <see cref="ulong" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="ulong" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 8 bytes to the beginning of the span.</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt64BigEndian(Span<byte> destination, ulong value)
        {
            if (BitConverter.IsLittleEndian)
            {
                ulong tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="UInt128" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="UInt128" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 16 bytes to the beginning of the span.</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt128BigEndian(Span<byte> destination, UInt128 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                UInt128 tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }

        /// <summary>
        /// Writes a <see cref="nuint" /> into a span of bytes, as big endian.
        /// </summary>
        /// <param name="destination">The span of bytes where the value is to be written, as big endian.</param>
        /// <param name="value">The value to write into the span of bytes.</param>
        /// <returns>
        /// <see langword="true" /> if the span is large enough to contain a <see cref="nuint" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>Writes exactly 4 bytes on 32-bit platforms -or- 8 bytes on 64-bit platforms to the beginning of the span.</remarks>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUIntPtrBigEndian(Span<byte> destination, nuint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                nuint tmp = ReverseEndianness(value);
                return MemoryMarshal.TryWrite(destination, in tmp);
            }

            return MemoryMarshal.TryWrite(destination, in value);
        }
    }
}

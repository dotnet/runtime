// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Reads an Int16 out of a read-only span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16BigEndian(ReadOnlySpan<byte> source)
        {
            short result = MemoryMarshal.Read<short>(source);
            if (BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads an Int32 out of a read-only span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32BigEndian(ReadOnlySpan<byte> source)
        {
            int result = MemoryMarshal.Read<int>(source);
            if (BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads an Int64 out of a read-only span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64BigEndian(ReadOnlySpan<byte> source)
        {
            long result = MemoryMarshal.Read<long>(source);
            if (BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads a UInt16 out of a read-only span of bytes as big endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
        {
            ushort result = MemoryMarshal.Read<ushort>(source);
            if (BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads a UInt32 out of a read-only span of bytes as big endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source)
        {
            uint result = MemoryMarshal.Read<uint>(source);
            if (BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads a UInt64 out of a read-only span of bytes as big endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source)
        {
            ulong result = MemoryMarshal.Read<ulong>(source);
            if (BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads an Int16 out of a read-only span of bytes as big endian.
        /// <returns>If the span is too small to contain an Int16, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadInt16BigEndian(ReadOnlySpan<byte> source, out short value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads an Int32 out of a read-only span of bytes as big endian.
        /// <returns>If the span is too small to contain an Int32, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadInt32BigEndian(ReadOnlySpan<byte> source, out int value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads an Int64 out of a read-only span of bytes as big endian.
        /// <returns>If the span is too small to contain an Int64, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadInt64BigEndian(ReadOnlySpan<byte> source, out long value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads a UInt16 out of a read-only span of bytes as big endian.
        /// <returns>If the span is too small to contain a UInt16, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> source, out ushort value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads a UInt32 out of a read-only span of bytes as big endian.
        /// <returns>If the span is too small to contain a UInt32, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUInt32BigEndian(ReadOnlySpan<byte> source, out uint value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads a UInt64 out of a read-only span of bytes as big endian.
        /// <returns>If the span is too small to contain a UInt64, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUInt64BigEndian(ReadOnlySpan<byte> source, out ulong value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }
    }
}


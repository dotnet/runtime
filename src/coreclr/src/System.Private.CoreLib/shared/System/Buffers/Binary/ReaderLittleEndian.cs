// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Reads an Int16 out of a read-only span of bytes as little endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16LittleEndian(ReadOnlySpan<byte> source)
        {
            short result = MemoryMarshal.Read<short>(source);
            if (!BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads an Int32 out of a read-only span of bytes as little endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32LittleEndian(ReadOnlySpan<byte> source)
        {
            int result = MemoryMarshal.Read<int>(source);
            if (!BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads an Int64 out of a read-only span of bytes as little endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64LittleEndian(ReadOnlySpan<byte> source)
        {
            long result = MemoryMarshal.Read<long>(source);
            if (!BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads a UInt16 out of a read-only span of bytes as little endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source)
        {
            ushort result = MemoryMarshal.Read<ushort>(source);
            if (!BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads a UInt32 out of a read-only span of bytes as little endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source)
        {
            uint result = MemoryMarshal.Read<uint>(source);
            if (!BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads a UInt64 out of a read-only span of bytes as little endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> source)
        {
            ulong result = MemoryMarshal.Read<ulong>(source);
            if (!BitConverter.IsLittleEndian)
            {
                result = ReverseEndianness(result);
            }
            return result;
        }

        /// <summary>
        /// Reads an Int16 out of a read-only span of bytes as little endian.
        /// <returns>If the span is too small to contain an Int16, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadInt16LittleEndian(ReadOnlySpan<byte> source, out short value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads an Int32 out of a read-only span of bytes as little endian.
        /// <returns>If the span is too small to contain an Int32, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadInt32LittleEndian(ReadOnlySpan<byte> source, out int value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads an Int64 out of a read-only span of bytes as little endian.
        /// <returns>If the span is too small to contain an Int64, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadInt64LittleEndian(ReadOnlySpan<byte> source, out long value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads a UInt16 out of a read-only span of bytes as little endian.
        /// <returns>If the span is too small to contain a UInt16, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUInt16LittleEndian(ReadOnlySpan<byte> source, out ushort value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads a UInt32 out of a read-only span of bytes as little endian.
        /// <returns>If the span is too small to contain a UInt32, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUInt32LittleEndian(ReadOnlySpan<byte> source, out uint value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }

        /// <summary>
        /// Reads a UInt64 out of a read-only span of bytes as little endian.
        /// <returns>If the span is too small to contain a UInt64, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUInt64LittleEndian(ReadOnlySpan<byte> source, out ulong value)
        {
            bool success = MemoryMarshal.TryRead(source, out value);
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }
    }
}

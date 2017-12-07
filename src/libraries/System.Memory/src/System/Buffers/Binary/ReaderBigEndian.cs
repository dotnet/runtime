// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Reads an Int16 out of a read-only span of bytes as big endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16BigEndian(ReadOnlySpan<byte> buffer)
        {
            short result = ReadMachineEndian<short>(buffer);
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
        public static int ReadInt32BigEndian(ReadOnlySpan<byte> buffer)
        {
            int result = ReadMachineEndian<int>(buffer);
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
        public static long ReadInt64BigEndian(ReadOnlySpan<byte> buffer)
        {
            long result = ReadMachineEndian<long>(buffer);
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
        public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> buffer)
        {
            ushort result = ReadMachineEndian<ushort>(buffer);
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
        public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> buffer)
        {
            uint result = ReadMachineEndian<uint>(buffer);
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
        public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> buffer)
        {
            ulong result = ReadMachineEndian<ulong>(buffer);
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
        public static bool TryReadInt16BigEndian(ReadOnlySpan<byte> buffer, out short value)
        {
            bool success = TryReadMachineEndian(buffer, out value);
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
        public static bool TryReadInt32BigEndian(ReadOnlySpan<byte> buffer, out int value)
        {
            bool success = TryReadMachineEndian(buffer, out value);
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
        public static bool TryReadInt64BigEndian(ReadOnlySpan<byte> buffer, out long value)
        {
            bool success = TryReadMachineEndian(buffer, out value);
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
        public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> buffer, out ushort value)
        {
            bool success = TryReadMachineEndian(buffer, out value);
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
        public static bool TryReadUInt32BigEndian(ReadOnlySpan<byte> buffer, out uint value)
        {
            bool success = TryReadMachineEndian(buffer, out value);
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
        public static bool TryReadUInt64BigEndian(ReadOnlySpan<byte> buffer, out ulong value)
        {
            bool success = TryReadMachineEndian(buffer, out value);
            if (BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return success;
        }
    }
}


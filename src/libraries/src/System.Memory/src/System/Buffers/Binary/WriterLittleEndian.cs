// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Writes an Int16 into a span of bytes as little endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16LittleEndian(Span<byte> buffer, short value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            WriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Writes an Int32 into a span of bytes as little endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32LittleEndian(Span<byte> buffer, int value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            WriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Writes an Int64 into a span of bytes as little endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64LittleEndian(Span<byte> buffer, long value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            WriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Write a UInt16 into a span of bytes as little endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16LittleEndian(Span<byte> buffer, ushort value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            WriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Write a UInt32 into a span of bytes as little endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32LittleEndian(Span<byte> buffer, uint value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            WriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Write a UInt64 into a span of bytes as little endian.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64LittleEndian(Span<byte> buffer, ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            WriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Writes an Int16 into a span of bytes as little endian.
        /// <returns>If the span is too small to contain the value, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt16LittleEndian(Span<byte> buffer, short value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return TryWriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Writes an Int32 into a span of bytes as little endian.
        /// <returns>If the span is too small to contain the value, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt32LittleEndian(Span<byte> buffer, int value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return TryWriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Writes an Int64 into a span of bytes as little endian.
        /// <returns>If the span is too small to contain the value, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteInt64LittleEndian(Span<byte> buffer, long value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return TryWriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Write a UInt16 into a span of bytes as little endian.
        /// <returns>If the span is too small to contain the value, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt16LittleEndian(Span<byte> buffer, ushort value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return TryWriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Write a UInt32 into a span of bytes as little endian.
        /// <returns>If the span is too small to contain the value, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt32LittleEndian(Span<byte> buffer, uint value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return TryWriteMachineEndian(buffer, ref value);
        }

        /// <summary>
        /// Write a UInt64 into a span of bytes as little endian.
        /// <returns>If the span is too small to contain the value, return false.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt64LittleEndian(Span<byte> buffer, ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseEndianness(value);
            }
            return TryWriteMachineEndian(buffer, ref value);
        }
    }
}

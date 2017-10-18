// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Buffers.Binary
{
    /// <summary>
    /// Reads bytes as primitives with specific endianness
    /// </summary>
    /// <remarks>
    /// For native formats, SpanExtensions.Read&lt;T&gt; should be used.
    /// Use these helpers when you need to read specific endinanness.
    /// </remarks>
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// This is a no-op and added only for consistency.
        /// This allows the caller to read a struct of numeric primitives and reverse each field
        /// rather than having to skip sbyte fields.
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReverseEndianness(sbyte value)
        {
            return value;
        }

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReverseEndianness(short value)
        {
            return (short)((value & 0x00FF) << 8 | (value & 0xFF00) >> 8);
        }

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReverseEndianness(int value) => (int)ReverseEndianness((uint)value);

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReverseEndianness(long value) => (long)ReverseEndianness((ulong)value);

        /// <summary>
        /// This is a no-op and added only for consistency.
        /// This allows the caller to read a struct of numeric primitives and reverse each field
        /// rather than having to skip byte fields.
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReverseEndianness(byte value)
        {
            return value;
        }

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReverseEndianness(ushort value)
        {
            return (ushort)((value & 0x00FFU) << 8 | (value & 0xFF00U) >> 8);
        }

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReverseEndianness(uint value)
        {
            value = (value << 16) | (value >> 16);
            value = (value & 0x00FF00FF) << 8 | (value & 0xFF00FF00) >> 8;
            return value;
        }

        /// <summary>
        /// Reverses a primitive value - performs an endianness swap
        /// </summary> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReverseEndianness(ulong value)
        {
            value = (value << 32) | (value >> 32);
            value = (value & 0x0000FFFF0000FFFF) << 16 | (value & 0xFFFF0000FFFF0000) >> 16;
            value = (value & 0x00FF00FF00FF00FF) << 8 | (value & 0xFF00FF00FF00FF00) >> 8;
            return value;
        }

        /// <summary>
        /// Reads a structure of type T out of a read-only span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadMachineEndian<T>(ReadOnlySpan<byte> buffer)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeWithPointersNotSupported, typeof(T)));
            }
#endif
            if (Unsafe.SizeOf<T>() > buffer.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            return Unsafe.ReadUnaligned<T>(ref buffer.DangerousGetPinnableReference());
        }

        /// <summary>
        /// Reads a structure of type T out of a span of bytes.
        /// <returns>If the span is too small to contain the type T, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadMachineEndian<T>(ReadOnlySpan<byte> buffer, out T value)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeWithPointersNotSupported, typeof(T)));
            }
#endif
            if (Unsafe.SizeOf<T>() > (uint)buffer.Length)
            {
                value = default;
                return false;
            }
            value = Unsafe.ReadUnaligned<T>(ref buffer.DangerousGetPinnableReference());
            return true;
        }
    }
}

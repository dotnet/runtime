// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Contains helpers for manipulating spans so that we can keep unsafe code out of the common path.
    /// </summary>
    internal static class SpanUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex<T>(ReadOnlySpan<T> span, int index)
        {
            return ((uint)index < (uint)span.Length) ? true : false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex<T>(Span<T> span, int index)
        {
            return ((uint)index < (uint)span.Length) ? true : false;
        }

        /// <summary>
        /// Tries writing a 64-bit value as little endian to the span. If success, returns true. If
        /// the span is not large enough to hold 8 bytes, leaves the span unchanged and returns false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt64LittleEndian(Span<byte> span, int offset, ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            return MemoryMarshal.TryWrite(span.Slice(offset), ref value);
        }
    }
}

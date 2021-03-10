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
        /// Tries writing four bytes to the span. If success, returns true. If the span is not large
        /// enough to hold four bytes, leaves the span unchanged and returns false.
        /// </summary>
        /// <remarks>
        /// Parameters are intended to be constant values.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> span, byte a, byte b, byte c, byte d)
        {
            if (span.Length >= 4)
            {
                uint value;
                if (BitConverter.IsLittleEndian)
                {
                    value = ((uint)d << 24) | ((uint)c << 16) | ((uint)b << 8) | a;
                }
                else
                {
                    value = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
                }
                Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(span), value);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries writing five bytes to the span. If success, returns true. If the span is not large
        /// enough to hold five bytes, leaves the span unchanged and returns false.
        /// </summary>
        /// <remarks>
        /// Parameters are intended to be constant values.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> span, byte a, byte b, byte c, byte d, byte e)
        {
            if (span.Length >= 5)
            {
                uint value;
                if (BitConverter.IsLittleEndian)
                {
                    value = ((uint)d << 24) | ((uint)c << 16) | ((uint)b << 8) | a;
                }
                else
                {
                    value = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
                }
                Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(span), value);
                Unsafe.Add(ref MemoryMarshal.GetReference(span), 4) = e;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries writing six bytes to the span. If success, returns true. If the span is not large
        /// enough to hold six bytes, leaves the span unchanged and returns false.
        /// </summary>
        /// <remarks>
        /// Parameters are intended to be constant values.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBytes(Span<byte> span, byte a, byte b, byte c, byte d, byte e, byte f)
        {
            if (span.Length >= 6)
            {
                uint hi;
                uint lo;
                if (BitConverter.IsLittleEndian)
                {
                    hi = ((uint)d << 24) | ((uint)c << 16) | ((uint)b << 8) | a;
                    lo = ((uint)f << 8) | e;
                }
                else
                {
                    hi = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
                    lo = ((uint)e << 8) | f;
                }
                Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(span), hi);
                Unsafe.WriteUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), 4), (ushort)lo);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries writing four chars to the span. If success, returns true. If the span is not large
        /// enough to hold four chars, leaves the span unchanged and returns false.
        /// </summary>
        /// <remarks>
        /// Parameters are intended to be constant values.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteChars(Span<char> span, char a, char b, char c, char d)
        {
            if (span.Length >= 4)
            {
                ulong value;
                if (BitConverter.IsLittleEndian)
                {
                    value = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
                }
                else
                {
                    value = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
                }
                Unsafe.WriteUnaligned<ulong>(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span)), value);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries writing five chars to the span. If success, returns true. If the span is not large
        /// enough to hold five chars, leaves the span unchanged and returns false.
        /// </summary>
        /// <remarks>
        /// Parameters are intended to be constant values.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteChars(Span<char> span, char a, char b, char c, char d, char e)
        {
            if (span.Length >= 5)
            {
                ulong value;
                if (BitConverter.IsLittleEndian)
                {
                    value = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
                }
                else
                {
                    value = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
                }
                Unsafe.WriteUnaligned<ulong>(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span)), value);
                Unsafe.Add(ref MemoryMarshal.GetReference(span), 4) = e;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries writing six chars to the span. If success, returns true. If the span is not large
        /// enough to hold six chars, leaves the span unchanged and returns false.
        /// </summary>
        /// <remarks>
        /// Parameters are intended to be constant values.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteChars(Span<char> span, char a, char b, char c, char d, char e, char f)
        {
            if (span.Length >= 6)
            {
                ulong value64;
                uint value32;
                if (BitConverter.IsLittleEndian)
                {
                    value64 = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
                    value32 = ((uint)f << 16) | e;
                }
                else
                {
                    value64 = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
                    value32 = ((uint)e << 16) | f;
                }
                Unsafe.WriteUnaligned<ulong>(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span)), value64);
                Unsafe.WriteUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), 4)), value32);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries writing a 64-bit value as little endian to the span. If success, returns true. If
        /// the span is not large enough to hold 8 bytes, leaves the span unchanged and returns false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteUInt64LittleEndian(Span<byte> span, int offset, ulong value)
        {
            if (AreValidIndexAndLength(span.Length, offset, sizeof(ulong)))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }
                Unsafe.WriteUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)offset), value);
                return true;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreValidIndexAndLength(int spanRealLength, int requestedOffset, int requestedLength)
        {
            // Logic here is copied from Span<T>.Slice.
            if (IntPtr.Size == 4)
            {
                if ((uint)requestedOffset > (uint)spanRealLength) { return false; }
                if ((uint)requestedLength > (uint)(spanRealLength - requestedOffset)) { return false; }
            }
            else
            {
                if ((ulong)(uint)spanRealLength < (ulong)(uint)requestedOffset + (ulong)(uint)requestedLength) { return false; }
            }
            return true;
        }
    }
}

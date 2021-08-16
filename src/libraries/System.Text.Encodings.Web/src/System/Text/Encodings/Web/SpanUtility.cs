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
                uint abcd32;
                if (BitConverter.IsLittleEndian)
                {
                    abcd32 = ((uint)d << 24) | ((uint)c << 16) | ((uint)b << 8) | a;
                }
                else
                {
                    abcd32 = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
                }
                Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(span), abcd32);
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
                uint abcd32;
                if (BitConverter.IsLittleEndian)
                {
                    abcd32 = ((uint)d << 24) | ((uint)c << 16) | ((uint)b << 8) | a;
                }
                else
                {
                    abcd32 = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
                }
                ref byte rDest = ref MemoryMarshal.GetReference(span);
                Unsafe.WriteUnaligned<uint>(ref rDest, abcd32);
                Unsafe.Add(ref rDest, 4) = e;
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
                uint abcd32;
                uint ef16;
                if (BitConverter.IsLittleEndian)
                {
                    abcd32 = ((uint)d << 24) | ((uint)c << 16) | ((uint)b << 8) | a;
                    ef16 = ((uint)f << 8) | e;
                }
                else
                {
                    abcd32 = ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
                    ef16 = ((uint)e << 8) | f;
                }
                ref byte rDest = ref MemoryMarshal.GetReference(span);
                Unsafe.WriteUnaligned<uint>(ref rDest, abcd32);
                Unsafe.WriteUnaligned<ushort>(ref Unsafe.Add(ref rDest, 4), (ushort)ef16);
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
                ulong abcd64;
                if (BitConverter.IsLittleEndian)
                {
                    abcd64 = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
                }
                else
                {
                    abcd64 = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
                }
                Unsafe.WriteUnaligned<ulong>(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span)), abcd64);
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
                ulong abcd64;
                if (BitConverter.IsLittleEndian)
                {
                    abcd64 = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
                }
                else
                {
                    abcd64 = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
                }
                ref char rDest = ref MemoryMarshal.GetReference(span);
                Unsafe.WriteUnaligned<ulong>(ref Unsafe.As<char, byte>(ref rDest), abcd64);
                Unsafe.Add(ref rDest, 4) = e;
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
                ulong abcd64;
                uint ef32;
                if (BitConverter.IsLittleEndian)
                {
                    abcd64 = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
                    ef32 = ((uint)f << 16) | e;
                }
                else
                {
                    abcd64 = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
                    ef32 = ((uint)e << 16) | f;
                }
                ref byte rDest = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span));
                Unsafe.WriteUnaligned<ulong>(ref rDest, abcd64);
                Unsafe.WriteUnaligned<uint>(ref Unsafe.AddByteOffset(ref rDest, (nint)sizeof(ulong)), ef32);
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

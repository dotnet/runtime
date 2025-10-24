// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text
{
    /// <summary>Provides downlevel polyfills for span-based Encoding APIs.</summary>
    internal static class EncodingPolyfills
    {
        public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
        {
            fixed (char* charsPtr = &GetNonNullPinnableReference(chars))
            {
                return encoding.GetByteCount(charsPtr, chars.Length);
            }
        }

        public static unsafe int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            fixed (byte* bytesPtr = &GetNonNullPinnableReference(bytes))
            {
                return encoding.GetCharCount(bytesPtr, bytes.Length);
            }
        }

        public static int GetBytes(this Encoding encoding, string str, Span<byte> bytes)
        {
            return GetBytes(encoding, str.AsSpan(), bytes);
        }

        public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            fixed (char* charsPtr = &GetNonNullPinnableReference(chars))
            fixed (byte* bytesPtr = &GetNonNullPinnableReference(bytes))
            {
                return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }

        public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            fixed (byte* bytesPtr = &GetNonNullPinnableReference(bytes))
            fixed (char* charsPtr = &GetNonNullPinnableReference(chars))
            {
                return encoding.GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length);
            }
        }

        public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            fixed (byte* bytesPtr = &GetNonNullPinnableReference(bytes))
            {
                return encoding.GetString(bytesPtr, bytes.Length);
            }
        }

        public static bool TryGetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten)
        {
            int charCount = encoding.GetCharCount(bytes);

            if (charCount > chars.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = encoding.GetChars(bytes, chars);
            Debug.Assert(charCount == charsWritten);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ref readonly T GetNonNullPinnableReference<T>(ReadOnlySpan<T> buffer)
        {
            // Based on the internal implementation from MemoryMarshal.
            return ref buffer.Length != 0 ? ref MemoryMarshal.GetReference(buffer) : ref Unsafe.AsRef<T>((void*)1);
        }
    }
}

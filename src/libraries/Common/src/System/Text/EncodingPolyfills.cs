// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text
{
    /// <summary>Provides downlevel polyfills for span-based Encoding APIs.</summary>
    internal static class EncodingPolyfills
    {
        public static int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
        {
            if (chars.IsEmpty)
            {
                // Ensure a non-null pointer is obtained, even though the expected answer is 0.
                chars = string.Empty.AsSpan();
            }

            unsafe
            {
                fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
                {
                    return encoding.GetByteCount(charsPtr, chars.Length);
                }
            }
        }

        public static int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
            {
                // Ensure a non-null pointer is obtained, even though the expected answer is 0.
                bytes = Array.Empty<byte>();
            }

            unsafe
            {
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                {
                    return encoding.GetCharCount(bytesPtr, bytes.Length);
                }
            }
        }

        public static int GetBytes(this Encoding encoding, string str, Span<byte> bytes)
        {
            return GetBytes(encoding, str.AsSpan(), bytes);
        }

        public static int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            if (chars.IsEmpty)
            {
                // Ensure a non-null pointer is obtained.
                chars = string.Empty.AsSpan();
            }

            if (bytes.IsEmpty)
            {
                // Ensure a non-null pointer is obtained.
                bytes = Array.Empty<byte>();
            }

            unsafe
            {
                fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                {
                    return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
                }
            }
        }

        public static int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            if (bytes.IsEmpty)
            {
                // Ensure a non-null pointer is obtained.
                bytes = Array.Empty<byte>();
            }

            if (chars.IsEmpty)
            {
                // Ensure a non-null pointer is obtained.
                chars = Array.Empty<char>();
            }

            unsafe
            {
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
                {
                    return encoding.GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length);
                }
            }
        }

        public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                {
                    return encoding.GetString(bytesPtr, bytes.Length);
                }
            }
        }

        public static bool TryGetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten)
        {
            if (bytes.Length == 0)
            {
                charsWritten = 0;
                return true;
            }

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
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Returns the index of the first non-ASCII byte in a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to scan.</param>
        /// <returns>The index in <paramref name="buffer"/> where the first non-ASCII
        /// byte appears, or -1 if the buffer contains only ASCII bytes.</returns>
        internal static unsafe int GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return -1;
            }

            nuint bufferLength = (uint)buffer.Length;
            fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                nuint idxOfFirstNonAsciiElement = GetIndexOfFirstNonAsciiByte(pBuffer, bufferLength);
                Debug.Assert(idxOfFirstNonAsciiElement <= bufferLength);
                return (idxOfFirstNonAsciiElement == bufferLength) ? -1 : (int)idxOfFirstNonAsciiElement;
            }
        }

        /// <summary>
        /// Returns the index of the first non-ASCII char in a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to scan.</param>
        /// <returns>The index in <paramref name="buffer"/> where the first non-ASCII
        /// char appears, or -1 if the buffer contains only ASCII char.</returns>
        internal static unsafe int GetIndexOfFirstNonAsciiChar(ReadOnlySpan<char> buffer)
        {
            if (buffer.IsEmpty)
            {
                return -1;
            }

            nuint bufferLength = (uint)buffer.Length;
            fixed (char* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                nuint idxOfFirstNonAsciiElement = GetIndexOfFirstNonAsciiChar(pBuffer, bufferLength);
                Debug.Assert(idxOfFirstNonAsciiElement <= bufferLength);
                return (idxOfFirstNonAsciiElement == bufferLength) ? -1 : (int)idxOfFirstNonAsciiElement;
            }
        }

        /// <summary>
        /// Determines whether the provided value contains only ASCII bytes.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII bytes or is
        /// empty; False otherwise.</returns>
        public static unsafe bool IsValid(ReadOnlySpan<byte> value) => GetIndexOfFirstNonAsciiByte(value) < 0;

        /// <summary>
        /// Determines whether the provided value contains only ASCII chars.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII chars or is
        /// empty; False otherwise.</returns>
        public static unsafe bool IsValid(ReadOnlySpan<char> value) => GetIndexOfFirstNonAsciiChar(value) < 0;

        /// <summary>
        /// Determines whether the provided value is ASCII byte.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> is ASCII, False otherwise.</returns>
        public static unsafe bool IsValid(byte value) => value <= 127;

        /// <summary>
        /// Determines whether the provided value is ASCII char.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> is ASCII, False otherwise.</returns>
        public static unsafe bool IsValid(char value) => value <= 127;
    }
}

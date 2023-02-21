// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Determines whether the provided value contains only ASCII bytes.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII bytes or is
        /// empty; False otherwise.</returns>
        public static unsafe bool IsValid(ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                return true;
            }

            nuint bufferLength = (uint)value.Length;
            fixed (byte* pBuffer = &MemoryMarshal.GetReference(value))
            {
                nuint idxOfFirstNonAsciiElement = GetIndexOfFirstNonAsciiByte(pBuffer, bufferLength);
                Debug.Assert(idxOfFirstNonAsciiElement <= bufferLength);
                return idxOfFirstNonAsciiElement == bufferLength;
            }
        }

        /// <summary>
        /// Determines whether the provided value contains only ASCII chars.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII chars or is
        /// empty; False otherwise.</returns>
        public static unsafe bool IsValid(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return true;
            }

            nuint bufferLength = (uint)value.Length;
            fixed (char* pBuffer = &MemoryMarshal.GetReference(value))
            {
                nuint idxOfFirstNonAsciiElement = GetIndexOfFirstNonAsciiChar(pBuffer, bufferLength);
                Debug.Assert(idxOfFirstNonAsciiElement <= bufferLength);
                return idxOfFirstNonAsciiElement == bufferLength;
            }
        }

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System
{
    internal static class FixedBufferExtensions
    {
        /// <summary>
        /// Returns a string from the given span, terminating the string at null if present.
        /// </summary>
        internal unsafe static string GetStringFromFixedBuffer(this ReadOnlySpan<char> span)
        {
            fixed (char* c = &MemoryMarshal.GetReference(span))
            {
                return new string(c, 0, span.GetFixedBufferStringLength());
            }
        }

        /// <summary>
        /// Gets the null-terminated string length of the given span.
        /// </summary>
        internal unsafe static int GetFixedBufferStringLength(this ReadOnlySpan<char> span)
        {
            int length = span.IndexOf('\0');
            return length < 0 ? span.Length : length;
        }

        /// <summary>
        /// Returns true if the given string equals the given span.
        /// The span's logical length is to the first null if present.
        /// </summary>
        internal unsafe static bool FixedBufferEqualsString(this ReadOnlySpan<char> span, string value)
        {
            if (value == null || value.Length > span.Length)
                return false;

            int i = 0;
            for (; i < value.Length; i++)
            {
                // Strings with embedded nulls can never match as the fixed buffer always null terminates.
                if (value[i] == '\0' || value[i] != span[i])
                    return false;
            }

            // If we've maxed out the buffer or reached the
            // null terminator, we're equal.
            return i == span.Length || span[i] == '\0';
        }
    }
}

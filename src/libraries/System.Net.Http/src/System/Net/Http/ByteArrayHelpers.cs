// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    internal static class ByteArrayHelpers
    {
        // TODO: https://github.com/dotnet/runtime/issues/28230
        // Use Ascii.Equals* when it's available.

        internal static bool EqualsOrdinalAsciiIgnoreCase(string left, ReadOnlySpan<byte> right)
        {
            Debug.Assert(left != null, "Expected non-null string");

            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                uint charA = left[i];
                uint charB = right[i];

                // We're only interested in ASCII characters here.
                if ((charA - 'a') <= ('z' - 'a'))
                    charA -= ('a' - 'A');
                if ((charB - 'a') <= ('z' - 'a'))
                    charB -= ('a' - 'A');

                if (charA != charB)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool EqualsOrdinalAscii(string left, ReadOnlySpan<byte> right)
        {
            Debug.Assert(left != null, "Expected non-null string");

            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

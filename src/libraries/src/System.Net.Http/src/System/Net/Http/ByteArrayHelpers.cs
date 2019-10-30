// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static class ByteArrayHelpers
    {
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

                unchecked
                {
                    // We're only interested in ASCII characters here.
                    if ((charA - 'a') <= ('z' - 'a'))
                        charA -= ('a' - 'A');
                    if ((charB - 'a') <= ('z' - 'a'))
                        charB -= ('a' - 'A');
                }

                if (charA != charB)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

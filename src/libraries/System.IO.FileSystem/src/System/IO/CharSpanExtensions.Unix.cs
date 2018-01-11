// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class CharSpanExtensions
    {
        internal static unsafe bool EqualsOrdinal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, bool ignoreCase = false)
        {
            if (first.Length != second.Length)
                return false;

            if (!ignoreCase)
                return first.SequenceEqual(second);

            fixed (char* fp = &MemoryMarshal.GetReference(first))
            fixed (char* sp = &MemoryMarshal.GetReference(second))
            {
                char* f = fp;
                char* s = sp;

                for (int i = 0; i < first.Length; i++)
                {
                    if (*f != *s && char.ToUpperInvariant(*f) != char.ToUpperInvariant(*s))
                        return false;
                    f++;
                    s++;
                }
            }

            return true;
        }
    }
}

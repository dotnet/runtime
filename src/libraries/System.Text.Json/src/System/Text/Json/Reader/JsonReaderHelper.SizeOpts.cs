// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        // https://tools.ietf.org/html/rfc8259
        // Does the span contain '"', '\',  or any control characters (i.e. 0 to 31)
        // IndexOfAny(34, 92, < 32)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                int value = span[i];
                if (value == JsonConstants.Quote || value == JsonConstants.BackSlash || JsonConstants.Space > value)
                    return i;
            }

            return -1;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class CharSpanExtensions
    {
        public static bool EndsWithOrdinal(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, bool ignoreCase = false)
        {
            if (value.Length == 0)
                return true;
            else if (value.Length > span.Length)
                return false;

            span = span.Slice(span.Length - value.Length);

            if (ignoreCase == false)
                return span.SequenceEqual(value);

            return EqualsOrdinal(span, value, ignoreCase);
        }
    }
}

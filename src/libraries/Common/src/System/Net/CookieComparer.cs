// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    internal static class CookieComparer
    {
        internal static bool Equals(Cookie left, Cookie right)
        {
            if (!string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!EqualDomains(left.Domain, right.Domain))
            {
                return false;
            }

            // NB: Only the path is case sensitive as per spec. However, many Windows applications assume
            //     case-insensitivity.
            return string.Equals(left.Path, right.Path, StringComparison.Ordinal);
        }

        internal static bool EqualDomains(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.StartsWith('.')) left = left.Slice(1);
            if (right.StartsWith('.')) right = right.Slice(1);

            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }
    }
}

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

            if (!EqualsDomain(left.Domain, right.Domain))
            {
                return false;
            }

            // NB: Only the path is case sensitive as per spec. However, many Windows applications assume
            //     case-insensitivity.
            return string.Equals(left.Path, right.Path, StringComparison.Ordinal);
        }

        internal static bool EqualsDomain(string left, string right)
        {
            if (left.Length == 0)
            {
                return right.Length == 0;
            }

            int indexLeft = GetComparisonStartIndex(left);
            int indexRight = GetComparisonStartIndex(right);

            if (left.Length - indexLeft != right.Length - indexRight)
            {
                return false;
            }

            return string.Equals(left.Substring(indexLeft), right.Substring(indexRight), StringComparison.OrdinalIgnoreCase);
        }

        private static int GetComparisonStartIndex(string domain)
            => domain.Length != 0 && domain[0] == '.' ? 1 : 0;
    }
}

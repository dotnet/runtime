// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class CookieComparer
    {
        internal static int Compare(Cookie left, Cookie right)
        {
            int result;

            if ((result = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)) != 0)
            {
                return result;
            }

            if ((result = string.Compare(left.Domain, right.Domain, StringComparison.OrdinalIgnoreCase)) != 0)
            {
                return result;
            }

            // NB: Only the path is case sensitive as per spec. However, many Windows applications assume
            //     case-insensitivity.
            return string.Compare(left.Path, right.Path, StringComparison.Ordinal);
        }
    }
}

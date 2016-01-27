// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static class StringExtensions
    {
        internal static string SubstringTrim(this string value, int startIndex)
        {
            return SubstringTrim(value, startIndex, value.Length - startIndex);
        }

        internal static string SubstringTrim(this string value, int startIndex, int length)
        {
            Debug.Assert(value != null, "string must be non-null");
            Debug.Assert(startIndex >= 0, "startIndex must be non-negative");
            Debug.Assert(length >= 0, "length must be non-negative");
            Debug.Assert(startIndex <= value.Length - length, "startIndex + length must be <= value.Length");

            if (length == 0)
            {
                return string.Empty;
            }

            int endIndex = startIndex + length - 1;

            while (startIndex <= endIndex && char.IsWhiteSpace(value[startIndex]))
            {
                startIndex++;
            }

            while (endIndex >= startIndex && char.IsWhiteSpace(value[endIndex]))
            {
                endIndex--;
            }

            int newLength = endIndex - startIndex + 1;
            Debug.Assert(newLength >= 0 && newLength <= value.Length, "Expected resulting length to be within value's length");

            return
                newLength == 0 ? string.Empty :
                newLength == value.Length ? value :
                value.Substring(startIndex, newLength);
        }
    }
}

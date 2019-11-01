// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System
{
    internal static class FormattingHelpers
    {
        public static string ToStringInvariant<T>(this T value) where T : IConvertible
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringInvariant<T>(this T value, string format) where T : IFormattable
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}

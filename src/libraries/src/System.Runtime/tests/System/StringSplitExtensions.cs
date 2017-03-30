// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Tests
{
    // Allows the string.Split tests to run on targets that *do not* have the new string.Split overloads.
    internal static class StringSplitExtensions
    {
        public static string[] Split(this string value, char separator, StringSplitOptions options = StringSplitOptions.None) =>
            value.Split(new[] { separator }, options);

        public static string[] Split(this string value, char separator, int count, StringSplitOptions options = StringSplitOptions.None) =>
            value.Split(new[] { separator }, count, options);

        public static string[] Split(this string value, string separator, StringSplitOptions options = StringSplitOptions.None) =>
            value.Split(new[] { separator }, options);

        public static string[] Split(this string value, string separator, int count, StringSplitOptions options = StringSplitOptions.None) =>
            value.Split(new[] { separator }, count, options);
    }
}

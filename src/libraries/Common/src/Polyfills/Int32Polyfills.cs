// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="int"/>.</summary>
internal static class Int32Polyfills
{
    extension(int)
    {
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out int result) =>
            int.TryParse(s.ToString(), style, provider, out result);
    }
}

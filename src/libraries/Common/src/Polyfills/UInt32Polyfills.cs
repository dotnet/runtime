// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="uint"/>.</summary>
internal static class UInt32Polyfills
{
    extension(uint)
    {
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out uint result) =>
            uint.TryParse(s.ToString(), style, provider, out result);
    }
}

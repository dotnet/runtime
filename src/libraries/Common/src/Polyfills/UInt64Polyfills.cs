// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="ulong"/>.</summary>
internal static class UInt64Polyfills
{
    extension(ulong)
    {
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ulong result) =>
            ulong.TryParse(s.ToString(), style, provider, out result);

        public static ulong PopCount(ulong value)
        {
            ulong count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }
    }
}

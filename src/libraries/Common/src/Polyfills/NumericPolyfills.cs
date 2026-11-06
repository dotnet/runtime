// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System;

internal static class NumericPolyfills
{
    extension(uint)
    {
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out uint result) =>
            uint.TryParse(s.ToString(), style, provider, out result);
    }

    extension(ulong)
    {
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ulong result) =>
            ulong.TryParse(s.ToString(), style, provider, out result);
    }
}

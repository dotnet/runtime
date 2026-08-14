// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="decimal"/>.</summary>
internal static class DecimalPolyfills
{
    extension(decimal)
    {
        public static int GetBits(decimal value, Span<int> destination)
        {
            int[] bits = decimal.GetBits(value);
            bits.AsSpan().CopyTo(destination);
            return bits.Length;
        }
    }
}

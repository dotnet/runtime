// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="decimal"/>.</summary>
internal static class DecimalPolyfills
{
    extension(decimal)
    {
        public static void GetBits(decimal d, Span<int> destination)
        {
            decimal.GetBits(d).CopyTo(destination);
        }
    }
}

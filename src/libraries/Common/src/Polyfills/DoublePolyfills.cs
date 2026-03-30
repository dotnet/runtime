// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Provides downlevel polyfills for static methods on <see cref="double"/>.</summary>
    internal static class DoublePolyfills
    {
        extension(double)
        {
            public static bool IsFinite(double value) =>
                !(double.IsNaN(value) || double.IsInfinity(value));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Provides downlevel polyfills for static methods on <see cref="float"/>.</summary>
    internal static class SinglePolyfills
    {
        extension(float)
        {
            public static bool IsFinite(float value) =>
                !(float.IsNaN(value) || float.IsInfinity(value));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Provides downlevel polyfills for static methods on <see cref="OperatingSystem"/>.</summary>
    internal static class OperatingSystemPolyfills
    {
        extension(OperatingSystem)
        {
            public static bool IsAndroid() => false;
            public static bool IsBrowser() => false;
            public static bool IsIOS() => false;
            public static bool IsMacCatalyst() => false;
            public static bool IsTvOS() => false;
            public static bool IsWasi() => false;
        }
    }
}

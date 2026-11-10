// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETFRAMEWORK
using System.Runtime.InteropServices;
#endif

namespace System;

/// <summary>Provides downlevel polyfills for static methods on <see cref="OperatingSystem"/>.</summary>
internal static class OperatingSystemPolyfills
{
    extension(OperatingSystem)
    {
        public static bool IsWindows() =>
#if NETFRAMEWORK
            Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
    }
}

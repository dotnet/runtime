// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateAutoreleasePool")]
        internal static extern IntPtr CreateAutoreleasePool();

        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_DrainAutoreleasePool")]
        internal static extern void DrainAutoreleasePool(IntPtr ptr);
    }
}

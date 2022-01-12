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
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateAutoreleasePool")]
        internal static partial IntPtr CreateAutoreleasePool();

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_DrainAutoreleasePool")]
        internal static partial void DrainAutoreleasePool(IntPtr ptr);
    }
}

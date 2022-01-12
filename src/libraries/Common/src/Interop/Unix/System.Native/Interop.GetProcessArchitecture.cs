// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [SuppressGCTransition]
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetProcessArchitecture")]
        internal static partial int GetProcessArchitecture();
    }
}

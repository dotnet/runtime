// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetErrNo")]
        [SuppressGCTransition]
        internal static partial int GetErrNo();

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_SetErrNo")]
        [SuppressGCTransition]
        internal static partial void SetErrNo(int errorCode);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetDomainSocketSizes")]
        [SuppressGCTransition]
        internal static extern void GetDomainSocketSizes(out int pathOffset, out int pathSize, out int addressSize);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPeerUserName", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial string GetPeerUserName(SafeHandle socket);
    }
}

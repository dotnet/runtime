// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SendFile", SetLastError = true)]
        internal static partial Error SendFile(SafeHandle out_fd, SafeHandle in_fd, long offset, long count, out long sent);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SendFile", SetLastError = true)]
        internal static extern unsafe Error SendFile(SafeHandle out_fd, SafeHandle in_fd, long offset, long count, out long sent);
    }
}

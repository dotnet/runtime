// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum SeekWhence
        {
            SEEK_SET = 0,
            SEEK_CUR = 1,
            SEEK_END = 2
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LSeek", SetLastError = true)]
        internal static extern long LSeek(SafeFileHandle fd, long offset, SeekWhence whence);
    }
}

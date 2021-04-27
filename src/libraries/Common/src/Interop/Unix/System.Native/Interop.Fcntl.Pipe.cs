// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static partial class Fcntl
        {
            internal static readonly bool CanGetSetPipeSz = (FcntlCanGetSetPipeSz() != 0);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlGetPipeSz", SetLastError=true)]
            internal static extern int GetPipeSz(SafePipeHandle fd);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlSetPipeSz", SetLastError=true)]
            internal static extern int SetPipeSz(SafePipeHandle fd, int size);

            [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FcntlCanGetSetPipeSz")]
            [SuppressGCTransition]
            private static extern int FcntlCanGetSetPipeSz();
        }
    }
}

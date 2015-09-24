// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static class Fcntl
        {
            internal static readonly bool CanGetSetPipeSz = (FcntlCanGetSetPipeSz() != 0);

            [DllImport(Libraries.SystemNative, EntryPoint="FcntlGetPipeSz", SetLastError=true)]
            internal static extern int GetPipeSz(int fd);

            [DllImport(Libraries.SystemNative, EntryPoint="FcntlSetPipeSz", SetLastError=true)]
            internal static extern int SetPipeSz(int fd, int size);

            [DllImport(Libraries.SystemNative)]
            private static extern int FcntlCanGetSetPipeSz();

            [DllImport(Libraries.SystemNative, EntryPoint="FcntlGetIsNonBlocking", SetLastError=true)]
            internal static extern int GetIsNonBlocking(int fd);

            [DllImport(Libraries.SystemNative, EntryPoint="FcntlSetIsNonBlocking", SetLastError=true)]
            internal static extern int SetIsNonBlocking(int fd, int isNonBlocking);
        }
    }
}

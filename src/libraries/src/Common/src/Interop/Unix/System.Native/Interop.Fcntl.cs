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
            [DllImport(Libraries.SystemNative, EntryPoint="FcntlSetIsNonBlocking", SetLastError=true)]
            internal static extern int SetIsNonBlocking(IntPtr fd, int isNonBlocking);
        }
    }
}

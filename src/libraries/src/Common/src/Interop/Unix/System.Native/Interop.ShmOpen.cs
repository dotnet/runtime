// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int ShmOpen(string name, OpenFlags flags, int mode);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int ShmUnlink(string name);
    }
}

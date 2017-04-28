// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace System.IO
{
    // Available in 14393 (RS1) and later
    [ComImport]
    [Guid("5CA296B2-2C25-4D22-B785-B885C8201E6A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IStorageItemHandleAccess
    {
        [PreserveSig]
        int Create(
            HANDLE_ACCESS_OPTIONS accessOptions,
            HANDLE_SHARING_OPTIONS sharingOptions,
            HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler,
            out SafeFileHandle interopHandle);
    }
}

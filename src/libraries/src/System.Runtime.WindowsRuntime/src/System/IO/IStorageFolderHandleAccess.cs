// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace System.IO
{
    // Available in 14393 (RS1) and later
    [ComImport]
    [Guid("DF19938F-5462-48A0-BE65-D2A3271A08D6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IStorageFolderHandleAccess
    {
        [PreserveSig]
        int Create(
            [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            HANDLE_CREATION_OPTIONS creationOptions,
            HANDLE_ACCESS_OPTIONS accessOptions,
            HANDLE_SHARING_OPTIONS sharingOptions,
            HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler,
            out SafeFileHandle interopHandle);
    }
}

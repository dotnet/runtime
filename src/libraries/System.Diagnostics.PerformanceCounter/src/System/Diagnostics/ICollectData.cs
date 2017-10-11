// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    /// <internalonly/>
    [ComImport, Guid("73386977-D6FD-11D2-BED5-00C04F79E3AE"), System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICollectData
    {
        [return: MarshalAs(UnmanagedType.I4)]
        void CollectData(
            [In, MarshalAs(UnmanagedType.I4 )]
             int id,
            [In, MarshalAs(UnmanagedType.SysInt )]
             IntPtr valueName,
            [In, MarshalAs(UnmanagedType.SysInt )]
             IntPtr data,
            [In, MarshalAs(UnmanagedType.I4 )]
             int totalBytes,
            [Out, MarshalAs(UnmanagedType.SysInt)]
             out IntPtr res);

        void CloseData();
    }
}

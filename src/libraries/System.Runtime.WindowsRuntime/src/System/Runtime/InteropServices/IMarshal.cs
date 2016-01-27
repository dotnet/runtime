// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices
{
    [Guid("00000003-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    /// <summary>
    /// Managed declaration of the IMarshal COM interface.
    /// </summary>
    internal interface IMarshal
    {
        void GetUnmarshalClass([In] ref Guid riid, IntPtr pv, UInt32 dwDestContext, IntPtr pvDestContext, UInt32 mshlFlags, out Guid pCid);

        void GetMarshalSizeMax([In] ref Guid riid, IntPtr pv, UInt32 dwDestContext, IntPtr pvDestContext, UInt32 mshlflags, out UInt32 pSize);

        void MarshalInterface(IntPtr pStm, [In] ref Guid riid, IntPtr pv, UInt32 dwDestContext, IntPtr pvDestContext, UInt32 mshlflags);

        void UnmarshalInterface(IntPtr pStm, [In] ref Guid riid, out IntPtr ppv);

        void ReleaseMarshalData(IntPtr pStm);

        void DisconnectObject(UInt32 dwReserved);
    }  // interface IMarshal
}  // namespace 

// IMarshal.cs

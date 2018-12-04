// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Managed declaration of the IMarshal COM interface.
    /// </summary>
    [Guid("00000003-0000-0000-c000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IMarshal
    {
        void GetUnmarshalClass([In] ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint mshlFlags, out Guid pCid);

        void GetMarshalSizeMax([In] ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint mshlflags, out uint pSize);

        void MarshalInterface(IntPtr pStm, [In] ref Guid riid, IntPtr pv, uint dwDestContext, IntPtr pvDestContext, uint mshlflags);

        void UnmarshalInterface(IntPtr pStm, [In] ref Guid riid, out IntPtr ppv);

        void ReleaseMarshalData(IntPtr pStm);

        void DisconnectObject(uint dwReserved);
    }  // interface IMarshal
}  // namespace 

// IMarshal.cs

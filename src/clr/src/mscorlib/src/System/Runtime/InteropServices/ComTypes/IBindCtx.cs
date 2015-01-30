// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: IBindCtx interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    [StructLayout(LayoutKind.Sequential)]

    public struct BIND_OPTS 
    {
        public int cbStruct;
        public int grfFlags;
        public int grfMode;
        public int dwTickCountDeadline;
    }

    [Guid("0000000e-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IBindCtx 
    {
        void RegisterObjectBound([MarshalAs(UnmanagedType.Interface)] Object punk);
        void RevokeObjectBound([MarshalAs(UnmanagedType.Interface)] Object punk);
        void ReleaseBoundObjects();
        void SetBindOptions([In()] ref BIND_OPTS pbindopts);
        void GetBindOptions(ref BIND_OPTS pbindopts);
        void GetRunningObjectTable(out IRunningObjectTable pprot);
        void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr)] String pszKey, [MarshalAs(UnmanagedType.Interface)] Object punk);
        void GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] String pszKey, [MarshalAs(UnmanagedType.Interface)] out Object ppunk);
        void EnumObjectParam(out IEnumString ppenum);
        [PreserveSig]
        int RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] String pszKey);
    }
}

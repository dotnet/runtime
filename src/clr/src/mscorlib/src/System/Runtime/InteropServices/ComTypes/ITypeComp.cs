// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: ITypeComp interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices.ComTypes
{
    using System;
 
    [Serializable]
    public enum DESCKIND
    {
        DESCKIND_NONE               = 0,
        DESCKIND_FUNCDESC           = DESCKIND_NONE + 1,
        DESCKIND_VARDESC            = DESCKIND_FUNCDESC + 1,
        DESCKIND_TYPECOMP           = DESCKIND_VARDESC + 1,
        DESCKIND_IMPLICITAPPOBJ     = DESCKIND_TYPECOMP + 1,
        DESCKIND_MAX                = DESCKIND_IMPLICITAPPOBJ + 1
    }

    [StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]

    public struct BINDPTR
    {
        [FieldOffset(0)]
        public IntPtr lpfuncdesc;
        [FieldOffset(0)]
        public IntPtr lpvardesc;
        [FieldOffset(0)]
        public IntPtr lptcomp;
    }

    [Guid("00020403-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ITypeComp
    {
        void Bind([MarshalAs(UnmanagedType.LPWStr)] String szName, int lHashVal, Int16 wFlags, out ITypeInfo ppTInfo, out DESCKIND pDescKind, out BINDPTR pBindPtr);
        void BindType([MarshalAs(UnmanagedType.LPWStr)] String szName, int lHashVal, out ITypeInfo ppTInfo, out ITypeComp ppTComp);
    }
}

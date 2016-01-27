// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMITypeComp interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;
 
    [Obsolete("Use System.Runtime.InteropServices.ComTypes.DESCKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
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

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.BINDPTR instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
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

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.ITypeComp instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("00020403-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMITypeComp
    {
        void Bind([MarshalAs(UnmanagedType.LPWStr)] String szName, int lHashVal, Int16 wFlags, out UCOMITypeInfo ppTInfo, out DESCKIND pDescKind, out BINDPTR pBindPtr);
        void BindType([MarshalAs(UnmanagedType.LPWStr)] String szName, int lHashVal, out UCOMITypeInfo ppTInfo, out UCOMITypeComp ppTComp);
    }
}

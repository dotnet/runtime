// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    public enum DESCKIND
    {
        DESCKIND_NONE = 0,
        DESCKIND_FUNCDESC = DESCKIND_NONE + 1,
        DESCKIND_VARDESC = DESCKIND_FUNCDESC + 1,
        DESCKIND_TYPECOMP = DESCKIND_VARDESC + 1,
        DESCKIND_IMPLICITAPPOBJ = DESCKIND_TYPECOMP + 1,
        DESCKIND_MAX = DESCKIND_IMPLICITAPPOBJ + 1
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
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
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ITypeComp
    {
        void Bind([MarshalAs(UnmanagedType.LPWStr)] string szName, int lHashVal, short wFlags, out ITypeInfo ppTInfo, out DESCKIND pDescKind, out BINDPTR pBindPtr);
        void BindType([MarshalAs(UnmanagedType.LPWStr)] string szName, int lHashVal, out ITypeInfo ppTInfo, out ITypeComp ppTComp);
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: ITypeLib interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    [Serializable]
    public enum SYSKIND
    {
        SYS_WIN16               = 0,
        SYS_WIN32               = SYS_WIN16 + 1,
        SYS_MAC                 = SYS_WIN32 + 1,
        SYS_WIN64               = SYS_MAC   + 1
    }

[Serializable]
[Flags()]
    public enum LIBFLAGS : short
    {
        LIBFLAG_FRESTRICTED     = 0x1,
        LIBFLAG_FCONTROL        = 0x2,
        LIBFLAG_FHIDDEN         = 0x4,
        LIBFLAG_FHASDISKIMAGE   = 0x8
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    [Serializable]
    public struct TYPELIBATTR
    { 
        public Guid guid;
        public int lcid;
        public SYSKIND syskind; 
        public Int16 wMajorVerNum;
        public Int16 wMinorVerNum;
        public LIBFLAGS wLibFlags;
    }

    [Guid("00020402-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ITypeLib
    {
        [PreserveSig]
        int GetTypeInfoCount();
        void GetTypeInfo(int index, out ITypeInfo ppTI);
        void GetTypeInfoType(int index, out TYPEKIND pTKind);       
        void GetTypeInfoOfGuid(ref Guid guid, out ITypeInfo ppTInfo);        
        void GetLibAttr(out IntPtr ppTLibAttr);        
        void GetTypeComp(out ITypeComp ppTComp);        
        void GetDocumentation(int index, out String strName, out String strDocString, out int dwHelpContext, out String strHelpFile);
        [return : MarshalAs(UnmanagedType.Bool)] 
        bool IsName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal);
        void FindName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray), Out] ITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray), Out] int[] rgMemId, ref Int16 pcFound);
        [PreserveSig]
        void ReleaseTLibAttr(IntPtr pTLibAttr);
    }
}

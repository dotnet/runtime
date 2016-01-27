// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMITypeLib interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.SYSKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Serializable]
    public enum SYSKIND
    {
        SYS_WIN16               = 0,
        SYS_WIN32               = SYS_WIN16 + 1,
        SYS_MAC                 = SYS_WIN32 + 1
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.LIBFLAGS instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
[Serializable]
[Flags()]
    public enum LIBFLAGS : short
    {
        LIBFLAG_FRESTRICTED     = 0x1,
        LIBFLAG_FCONTROL        = 0x2,
        LIBFLAG_FHIDDEN         = 0x4,
        LIBFLAG_FHASDISKIMAGE   = 0x8
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPELIBATTR instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
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

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.ITypeLib instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("00020402-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMITypeLib
    {
        [PreserveSig]
        int GetTypeInfoCount();
        void GetTypeInfo(int index, out UCOMITypeInfo ppTI);
        void GetTypeInfoType(int index, out TYPEKIND pTKind);       
        void GetTypeInfoOfGuid(ref Guid guid, out UCOMITypeInfo ppTInfo);        
        void GetLibAttr(out IntPtr ppTLibAttr);        
        void GetTypeComp(out UCOMITypeComp ppTComp);        
        void GetDocumentation(int index, out String strName, out String strDocString, out int dwHelpContext, out String strHelpFile);
        [return : MarshalAs(UnmanagedType.Bool)] 
        bool IsName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal);
        void FindName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray), Out] UCOMITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray), Out] int[] rgMemId, ref Int16 pcFound);
        [PreserveSig]
        void ReleaseTLibAttr(IntPtr pTLibAttr);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    public enum SYSKIND
    {
        SYS_WIN16 = 0,
        SYS_WIN32 = SYS_WIN16 + 1,
        SYS_MAC = SYS_WIN32 + 1,
        SYS_WIN64 = SYS_MAC + 1
    }

    [Flags]
    public enum LIBFLAGS : short
    {
        LIBFLAG_FRESTRICTED = 0x1,
        LIBFLAG_FCONTROL = 0x2,
        LIBFLAG_FHIDDEN = 0x4,
        LIBFLAG_FHASDISKIMAGE = 0x8
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TYPELIBATTR
    {
        public Guid guid;
        public int lcid;
        public SYSKIND syskind;
        public short wMajorVerNum;
        public short wMinorVerNum;
        public LIBFLAGS wLibFlags;
    }

    [Guid("00020402-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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
        void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal);
        void FindName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray), Out] ITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray), Out] int[] rgMemId, ref short pcFound);
        [PreserveSig]
        void ReleaseTLibAttr(IntPtr pTLibAttr);
    }
}

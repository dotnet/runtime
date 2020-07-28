// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Data.Common
{
    internal static class NativeMethods
    {
        [Guid("0c733a1e-2a1c-11ce-ade5-00aa0044773d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
        internal interface ISourcesRowset
        {
            [PreserveSig]
            System.Data.OleDb.OleDbHResult GetSourcesRowset(
                [In] IntPtr pUnkOuter,
                [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [In] int cPropertySets,
                [In] IntPtr rgProperties,
                [Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);
        }

        [Guid("0C733A5E-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
        internal interface ITransactionJoin
        {
            [Obsolete("not used", true)]
            [PreserveSig]
            int GetOptionsObject(IntPtr ppOptions);

            void JoinTransaction(
                [In, MarshalAs(UnmanagedType.Interface)] object? punkTransactionCoord,
                [In] int isoLevel,
                [In] int isoFlags,
                [In] IntPtr pOtherOptions);
        }

        [DllImport(Interop.Libraries.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);

        // OpenFileMappingA contains a security venerability, in the unicode->ansi conversion
        // Its possible to spoof the directory and construct ../ sequeences,  See FxCop Warrning
        // Specify marshaling for pinvoke string arguments
        [DllImport(Interop.Libraries.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //        [DllImport(Interop.Libraries.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Ansi)]
        internal static extern IntPtr OpenFileMappingA(int dwDesiredAccess, bool bInheritHandle, [MarshalAs(UnmanagedType.LPStr)] string lpName);

        // CreateFileMappingA contains a security venerability, in the unicode->ansi conversion
        // Its possible to spoof the directory and construct ../ sequeences,  See FxCop Warrning
        // Specify marshaling for pinvoke string arguments
        [DllImport(Interop.Libraries.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //        [DllImport(Interop.Libraries.Kernel32, CharSet=System.Runtime.InteropServices.CharSet.Ansi)]
        internal static extern IntPtr CreateFileMappingA(IntPtr hFile, IntPtr pAttr, int flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, [MarshalAs(UnmanagedType.LPStr)] string lpName);

        [DllImport(Interop.Libraries.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport(Interop.Libraries.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool AllocateAndInitializeSid(
            IntPtr pIdentifierAuthority, // authority
            byte nSubAuthorityCount,                        // count of subauthorities
            int dwSubAuthority0,                          // subauthority 0
            int dwSubAuthority1,                          // subauthority 1
            int dwSubAuthority2,                          // subauthority 2
            int dwSubAuthority3,                          // subauthority 3
            int dwSubAuthority4,                          // subauthority 4
            int dwSubAuthority5,                          // subauthority 5
            int dwSubAuthority6,                          // subauthority 6
            int dwSubAuthority7,                          // subauthority 7
            ref IntPtr pSid);                                   // SID

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern int GetLengthSid(
                    IntPtr pSid);   // SID to query

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool InitializeAcl(
            IntPtr pAcl,            // ACL
            int nAclLength,     // size of ACL
            int dwAclRevision);  // revision level of ACL

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool AddAccessDeniedAce(
            IntPtr pAcl,            // access control list
            int dwAceRevision,  // ACL revision level
            int AccessMask,     // access mask
            IntPtr pSid);           // security identifier

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool AddAccessAllowedAce(
            IntPtr pAcl,            // access control list
            int dwAceRevision,  // ACL revision level
            uint AccessMask,     // access mask
            IntPtr pSid);           // security identifier

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool InitializeSecurityDescriptor(
            IntPtr pSecurityDescriptor, // SD
            int dwRevision);                         // revision level
        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern bool SetSecurityDescriptorDacl(
            IntPtr pSecurityDescriptor, // SD
            bool bDaclPresent,                        // DACL presence
            IntPtr pDacl,                               // DACL
            bool bDaclDefaulted);                       // default DACL

        [DllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr FreeSid(
            IntPtr pSid);   // SID to free
    }
}

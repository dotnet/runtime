// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Data.Common
{
    internal static partial class NativeMethods
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

        [GeneratedDllImport(Interop.Libraries.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static partial IntPtr MapViewOfFile(IntPtr hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);

        [GeneratedDllImport(Interop.Libraries.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static partial bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [GeneratedDllImport(Interop.Libraries.Kernel32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool CloseHandle(IntPtr handle);

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool AllocateAndInitializeSid(
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

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial int GetLengthSid(
                    IntPtr pSid);   // SID to query

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool InitializeAcl(
            IntPtr pAcl,            // ACL
            int nAclLength,     // size of ACL
            int dwAclRevision);  // revision level of ACL

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool AddAccessDeniedAce(
            IntPtr pAcl,            // access control list
            int dwAceRevision,  // ACL revision level
            int AccessMask,     // access mask
            IntPtr pSid);           // security identifier

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool AddAccessAllowedAce(
            IntPtr pAcl,            // access control list
            int dwAceRevision,  // ACL revision level
            uint AccessMask,     // access mask
            IntPtr pSid);           // security identifier

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool InitializeSecurityDescriptor(
            IntPtr pSecurityDescriptor, // SD
            int dwRevision);                         // revision level
        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial bool SetSecurityDescriptorDacl(
            IntPtr pSecurityDescriptor, // SD
            bool bDaclPresent,                        // DACL presence
            IntPtr pDacl,                               // DACL
            bool bDaclDefaulted);                       // default DACL

        [GeneratedDllImport(Interop.Libraries.Advapi32, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        internal static partial IntPtr FreeSid(
            IntPtr pSid);   // SID to free
    }
}

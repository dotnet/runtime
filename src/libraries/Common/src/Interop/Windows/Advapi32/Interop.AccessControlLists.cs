// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool InitializeAcl(nint pAcl, int nAclLength, int dwAclRevision);

        [LibraryImport(Libraries.Advapi32, EntryPoint = "SetEntriesInAclW", SetLastError = true)]
        internal static unsafe partial int SetEntriesInAcl(
            int cCountOfExplicitEntries,
            EXPLICIT_ACCESS* pListOfExplicitEntries,
            nint OldAcl,
            out SafeLocalAllocHandle NewAcl);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetAce(ACL* pAcl, int dwAceIndex, out ACE* pAce);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AddMandatoryAce(
            nint pAcl,
            int dwAceRevision,
            int AceFlags,
            int MandatoryPolicy,
            nint pLabelSid);

        internal const int ACL_REVISION = 2;

        // Values for ACE_HEADER.AceType
        internal const byte ACCESS_ALLOWED_ACE_TYPE = 0x0;

        // Values for MandatoryPolicy in AddMandatoryAce()
        internal const int SYSTEM_MANDATORY_LABEL_NO_WRITE_UP = 0x1;
        internal const int SYSTEM_MANDATORY_LABEL_NO_READ_UP = 0x2;

        // Values for the RID portion of a mandatory label SID, which indicates the integrity level
        internal const uint SECURITY_MANDATORY_MEDIUM_RID = 0x2000;

        [StructLayout(LayoutKind.Sequential)]
        internal struct ACL
        {
            public byte AclRevision;
            public byte Sbz1;
            public ushort AclSize;
            public ushort AceCount;
            public ushort Sbz2;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ACE
        {
            public ACE_HEADER Header;
            public uint Mask;
            public uint SidStart;

            public static int SizeOfSidPortionInAce => sizeof(uint);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ACE_HEADER
        {
            public byte AceType;
            public byte AceFlags;
            public ushort AceSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct EXPLICIT_ACCESS
        {
            public int grfAccessPermissions;
            public ACCESS_MODE grfAccessMode;
            public int grfInheritance;
            public TRUSTEE Trustee;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRUSTEE
        {
            public nint pMultipleTrustee;
            public int MultipleTrusteeOperation;
            public TRUSTEE_FORM TrusteeForm;
            public TRUSTEE_TYPE TrusteeType;
            public nint ptstrName;
        }

        internal enum ACCESS_MODE
        {
            NOT_USED_ACCESS,
            GRANT_ACCESS,
            SET_ACCESS,
            DENY_ACCESS,
            REVOKE_ACCESS,
            SET_AUDIT_SUCCESS,
            SET_AUDIT_FAILURE
        }

        // Constants for EXPLICIT_ACCESS.grfInheritance
        internal const int EXPLICIT_ACCESS_NO_INHERITANCE = 0;

        internal enum TRUSTEE_FORM
        {
            TRUSTEE_IS_SID,
            TRUSTEE_IS_NAME,
            TRUSTEE_BAD_FORM,
            TRUSTEE_IS_OBJECTS_AND_SID,
            TRUSTEE_IS_OBJECTS_AND_NAME
        }

        internal enum TRUSTEE_TYPE
        {
            TRUSTEE_IS_UNKNOWN,
            TRUSTEE_IS_USER,
            TRUSTEE_IS_GROUP,
            TRUSTEE_IS_DOMAIN,
            TRUSTEE_IS_ALIAS,
            TRUSTEE_IS_WELL_KNOWN_GROUP,
            TRUSTEE_IS_DELETED,
            TRUSTEE_IS_INVALID,
            TRUSTEE_IS_COMPUTER
        }
    }
}

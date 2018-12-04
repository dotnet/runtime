// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//NOTE: 
//Structures containing collection of another structures are defined without
//embedded structure which will ensure proper marshalling in case collection
//count is zero and there is no embedded structure. Marshalling code read the
//embedded structure appropriately for non-zero collection count.
//
//E.g.
//Unmanaged structure DS_REPL_CURSORS_3 is defind as
//typedef struct {
//    DWORD cNumCursors;
//    DWORD dwEnumerationContext;
//    DS_REPL_CURSOR_3 rgCursor[1];
//} DS_REPL_CURSORS_3;
//
//Here it has been defined as (without embedded structure DS_REPL_CURSOR_3)
//
//internal sealed class DS_REPL_CURSORS_3
//{  
//    public int cNumCursors;
//    public int dwEnumerationContext;
//}

using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.DirectoryServices.ActiveDirectory
{

    internal enum DS_REPL_INFO_TYPE
    {
        DS_REPL_INFO_NEIGHBORS = 0,
        DS_REPL_INFO_CURSORS_FOR_NC = 1,
        DS_REPL_INFO_METADATA_FOR_OBJ = 2,
        DS_REPL_INFO_KCC_DSA_CONNECT_FAILURES = 3,
        DS_REPL_INFO_KCC_DSA_LINK_FAILURES = 4,
        DS_REPL_INFO_PENDING_OPS = 5,
        DS_REPL_INFO_METADATA_FOR_ATTR_VALUE = 6,
        DS_REPL_INFO_CURSORS_2_FOR_NC = 7,
        DS_REPL_INFO_CURSORS_3_FOR_NC = 8,
        DS_REPL_INFO_METADATA_2_FOR_OBJ = 9,
        DS_REPL_INFO_METADATA_2_FOR_ATTR_VALUE = 10
    }

    public enum ReplicationOperationType
    {
        Sync = 0,
        Add = 1,
        Delete = 2,
        Modify = 3,
        UpdateReference = 4
    }

    internal enum DS_NAME_ERROR
    {
        DS_NAME_NO_ERROR = 0,
        DS_NAME_ERROR_RESOLVING = 1,
        DS_NAME_ERROR_NOT_FOUND = 2,
        DS_NAME_ERROR_NOT_UNIQUE = 3,
        DS_NAME_ERROR_NO_MAPPING = 4,
        DS_NAME_ERROR_DOMAIN_ONLY = 5,
        DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING = 6,
        DS_NAME_ERROR_TRUST_REFERRAL = 7
    }

    [Flags]
    internal enum DS_DOMAINTRUST_FLAG
    {
        DS_DOMAIN_IN_FOREST = 0x0001,
        DS_DOMAIN_DIRECT_OUTBOUND = 0x0002,
        DS_DOMAIN_TREE_ROOT = 0x0004,
        DS_DOMAIN_PRIMARY = 0x0008,
        DS_DOMAIN_NATIVE_MODE = 0x0010,
        DS_DOMAIN_DIRECT_INBOUND = 0x0020
    }

    internal enum LSA_FOREST_TRUST_RECORD_TYPE
    {
        ForestTrustTopLevelName,
        ForestTrustTopLevelNameEx,
        ForestTrustDomainInfo,
        ForestTrustRecordTypeLast
    }

    public enum ForestTrustCollisionType
    {
        TopLevelName,
        Domain,
        Other
    }

    [Flags]
    public enum TopLevelNameCollisionOptions
    {
        None = 0,
        NewlyCreated = 1,
        DisabledByAdmin = 2,
        DisabledByConflict = 4
    }

    [Flags]
    public enum DomainCollisionOptions
    {
        None = 0,
        SidDisabledByAdmin = 1,
        SidDisabledByConflict = 2,
        NetBiosNameDisabledByAdmin = 4,
        NetBiosNameDisabledByConflict = 8
    }

    /*
    typedef enum 
    {
        DsRole_RoleStandaloneWorkstation,
        DsRole_RoleMemberWorkstation,
        DsRole_RoleStandaloneServer,
        DsRole_RoleMemberServer,
        DsRole_RoleBackupDomainController,
        DsRole_RolePrimaryDomainController,
        DsRole_WorkstationWithSharedAccountDomain,
        DsRole_ServerWithSharedAccountDomain,
        DsRole_MemberWorkstationWithSharedAccountDomain,
        DsRole_MemberServerWithSharedAccountDomain
    }DSROLE_MACHINE_ROLE;
    */

    internal enum DSROLE_MACHINE_ROLE
    {
        DsRole_RoleStandaloneWorkstation,
        DsRole_RoleMemberWorkstation,
        DsRole_RoleStandaloneServer,
        DsRole_RoleMemberServer,
        DsRole_RoleBackupDomainController,
        DsRole_RolePrimaryDomainController,
        DsRole_WorkstationWithSharedAccountDomain,
        DsRole_ServerWithSharedAccountDomain,
        DsRole_MemberWorkstationWithSharedAccountDomain,
        DsRole_MemberServerWithSharedAccountDomain
    }

    /*
    typedef enum 
    {
        DsRolePrimaryDomainInfoBasic,
        DsRoleUpgradeStatus,
        DsRoleOperationState,
        DsRolePrimaryDomainInfoBasicEx
    }DSROLE_PRIMARY_DOMAIN_INFO_LEVEL;
    */

    internal enum DSROLE_PRIMARY_DOMAIN_INFO_LEVEL
    {
        DsRolePrimaryDomainInfoBasic = 1,
        DsRoleUpgradeStatus = 2,
        DsRoleOperationState = 3,
        DsRolePrimaryDomainInfoBasicEx = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class FileTime
    {
        public int lower;
        public int higher;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class SystemTime
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    //Without embedded structure DS_REPL_CURSOR_3.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_CURSORS_3
    {
        public int cNumCursors;
        public int dwEnumerationContext;
    }

    //Without embedded structure DS_REPL_CURSOR.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_CURSORS
    {
        public int cNumCursors;
        public int reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_CURSOR_3
    {
        public Guid uuidSourceDsaInvocationID;
        public long usnAttributeFilter;
        public long ftimeLastSyncSuccess;
        public IntPtr pszSourceDsaDN;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_CURSOR
    {
        public Guid uuidSourceDsaInvocationID;
        public long usnAttributeFilter;
    }

    //Without embedded structure DS_REPL_OP.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_PENDING_OPS
    {
        public long ftimeCurrentOpStarted;
        public int cNumPendingOps;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal sealed class DS_REPL_OP
    {
        public long ftimeEnqueued;
        public int ulSerialNumber;
        public int ulPriority;
        public ReplicationOperationType OpType;
        public int ulOptions;
        public IntPtr pszNamingContext;
        public IntPtr pszDsaDN;
        public IntPtr pszDsaAddress;
        public Guid uuidNamingContextObjGuid;
        public Guid uuidDsaObjGuid;
    }

    //Without embedded structure DS_REPL_NEIGHBOR.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_NEIGHBORS
    {
        public int cNumNeighbors;
        public int dwReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_NEIGHBOR
    {
        public IntPtr pszNamingContext;
        public IntPtr pszSourceDsaDN;
        public IntPtr pszSourceDsaAddress;
        public IntPtr pszAsyncIntersiteTransportDN;
        public int dwReplicaFlags;
        public int dwReserved;
        public Guid uuidNamingContextObjGuid;
        public Guid uuidSourceDsaObjGuid;
        public Guid uuidSourceDsaInvocationID;
        public Guid uuidAsyncIntersiteTransportObjGuid;
        public long usnLastObjChangeSynced;
        public long usnAttributeFilter;
        public long ftimeLastSyncSuccess;
        public long ftimeLastSyncAttempt;
        public int dwLastSyncResult;
        public int cNumConsecutiveSyncFailures;
    }

    //Without embedded structure DS_REPL_KCC_DSA_FAILURE.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_KCC_DSA_FAILURES
    {
        public int cNumEntries;
        public int dwReserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal sealed class DS_REPL_KCC_DSA_FAILURE
    {
        public IntPtr pszDsaDN;
        public Guid uuidDsaObjGuid;
        public long ftimeFirstFailure;
        public int cNumFailures;
        public int dwLastResult;
    }

    //Without embedded structure DS_REPL_ATTR_META_DATA_2.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_OBJ_META_DATA_2
    {
        public int cNumEntries;
        public int dwReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_ATTR_META_DATA_2
    {
        public IntPtr pszAttributeName;
        public int dwVersion;
        // using two int to replace long to prevent managed code packing it
        public int ftimeLastOriginatingChange1;
        public int ftimeLastOriginatingChange2;
        public Guid uuidLastOriginatingDsaInvocationID;
        public long usnOriginatingChange;
        public long usnLocalChange;
        public IntPtr pszLastOriginatingDsaDN;
    }

    //Without embedded structure DS_REPL_ATTR_META_DATA.
    //See NOTE at the top of this file for more details
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_OBJ_META_DATA
    {
        public int cNumEntries;
        public int dwReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPL_ATTR_META_DATA
    {
        public IntPtr pszAttributeName;
        public int dwVersion;
        // using two int to replace long to prevent managed code packing it
        public int ftimeLastOriginatingChange1;
        public int ftimeLastOriginatingChange2;
        public Guid uuidLastOriginatingDsaInvocationID;
        public long usnOriginatingChange;
        public long usnLocalChange;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPSYNCALL_UPDATE
    {
        public SyncFromAllServersEvent eventType;
        public IntPtr pErrInfo;
        public IntPtr pSync;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPSYNCALL_ERRINFO
    {
        public IntPtr pszSvrId;
        public SyncFromAllServersErrorCategory error;
        public int dwWin32Err;
        public IntPtr pszSrcId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_REPSYNCALL_SYNC
    {
        public IntPtr pszSrcId;
        public IntPtr pszDstId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_NAME_RESULT_ITEM
    {
        public DS_NAME_ERROR status;
        public IntPtr pDomain;
        public IntPtr pName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_NAME_RESULT
    {
        public int cItems;
        public IntPtr rItems;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DS_DOMAIN_TRUSTS
    {
        public IntPtr NetbiosDomainName;
        public IntPtr DnsDomainName;
        public int Flags;
        public int ParentIndex;
        public int TrustType;
        public int TrustAttributes;
        public IntPtr DomainSid;
        public Guid DomainGuid;
    }

    internal sealed class TrustObject
    {
        public string NetbiosDomainName;
        public string DnsDomainName;
        public int Flags;
        public int ParentIndex;
        public TrustType TrustType;
        public int TrustAttributes;
        public int OriginalIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_FOREST_TRUST_INFORMATION
    {
        public int RecordCount;
        public IntPtr Entries;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal sealed class LSA_FOREST_TRUST_RECORD
    {
        [FieldOffset(0)]
        public int Flags;
        [FieldOffset(4)]
        public LSA_FOREST_TRUST_RECORD_TYPE ForestTrustType;
        [FieldOffset(8)]
        public LARGE_INTEGER Time;
        [FieldOffset(16)]
        public LSA_UNICODE_STRING TopLevelName;
        [FieldOffset(16)]
        public LSA_FOREST_TRUST_BINARY_DATA Data;
        [FieldOffset(16)]
        public LSA_FOREST_TRUST_DOMAIN_INFO DomainInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LARGE_INTEGER
    {
        public int lowPart;
        public int highPart;

        public LARGE_INTEGER()
        {
            lowPart = 0;
            highPart = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_UNICODE_STRING
    {
        public short Length;
        public short MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_FOREST_TRUST_DOMAIN_INFO
    {
        public IntPtr sid;
        public short DNSNameLength;
        public short DNSNameMaximumLength;
        public IntPtr DNSNameBuffer;
        public short NetBIOSNameLength;
        public short NetBIOSNameMaximumLength;
        public IntPtr NetBIOSNameBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_FOREST_TRUST_BINARY_DATA
    {
        public int Length;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_OBJECT_ATTRIBUTES
    {
        internal int Length;
        private IntPtr _rootDirectory;
        private IntPtr _objectName;
        internal int Attributes;
        private IntPtr _securityDescriptor;
        private IntPtr _securityQualityOfService;

        public LSA_OBJECT_ATTRIBUTES()
        {
            Length = 0;
            _rootDirectory = (IntPtr)0;
            _objectName = (IntPtr)0;
            Attributes = 0;
            _securityDescriptor = (IntPtr)0;
            _securityQualityOfService = (IntPtr)0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class TRUSTED_DOMAIN_INFORMATION_EX
    {
        public LSA_UNICODE_STRING Name;
        public LSA_UNICODE_STRING FlatName;
        public IntPtr Sid;
        public int TrustDirection;
        public int TrustType;
        public TRUST_ATTRIBUTE TrustAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_FOREST_TRUST_COLLISION_INFORMATION
    {
        public int RecordCount;
        public IntPtr Entries;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_FOREST_TRUST_COLLISION_RECORD
    {
        public int Index;
        public ForestTrustCollisionType Type;
        public int Flags;
        public LSA_UNICODE_STRING Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class NETLOGON_INFO_2
    {
        public int netlog2_flags;

        //
        // If NETLOGON_VERIFY_STATUS_RETURNED bit is set in
        //  netlog2_flags, the following field will return
        //  the status of trust verification. Otherwise,
        //  the field will return the status of the secure
        //  channel to the primary domain of the machine
        //  (useful for BDCs only).
        //
        public int netlog2_pdc_connection_status;
        public IntPtr netlog2_trusted_dc_name;
        public int netlog2_tc_connection_status;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class TRUSTED_DOMAIN_AUTH_INFORMATION
    {
        public int IncomingAuthInfos;
        public IntPtr IncomingAuthenticationInformation;
        public IntPtr IncomingPreviousAuthenticationInformation;
        public int OutgoingAuthInfos;
        public IntPtr OutgoingAuthenticationInformation;
        public IntPtr OutgoingPreviousAuthenticationInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LSA_AUTH_INFORMATION
    {
        public LARGE_INTEGER LastUpdateTime;
        public int AuthType;
        public int AuthInfoLength;
        public IntPtr AuthInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class POLICY_DNS_DOMAIN_INFO
    {
        public LSA_UNICODE_STRING Name;
        public LSA_UNICODE_STRING DnsDomainName;
        public LSA_UNICODE_STRING DnsForestName;
        public Guid DomainGuid;
        public IntPtr Sid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class TRUSTED_POSIX_OFFSET_INFO
    {
        internal int Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class TRUSTED_DOMAIN_FULL_INFORMATION
    {
        public TRUSTED_DOMAIN_INFORMATION_EX Information;
        internal TRUSTED_POSIX_OFFSET_INFO PosixOffset;
        public TRUSTED_DOMAIN_AUTH_INFORMATION AuthInformation;
    }

    /*
     typedef struct _DSROLE_PRIMARY_DOMAIN_INFO_BASIC {  
     DSROLE_MACHINE_ROLE MachineRole;  
     ULONG Flags;  
     LPWSTR DomainNameFlat;  
     LPWSTR DomainNameDns;  
     LPWSTR DomainForestName;  
     GUID DomainGuid;
     } DSROLE_PRIMARY_DOMAIN_INFO_BASIC,  *PDSROLE_PRIMARY_DOMAIN_INFO_BASIC;
     */

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class DSROLE_PRIMARY_DOMAIN_INFO_BASIC
    {
        public DSROLE_MACHINE_ROLE MachineRole;
        public uint Flags;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DomainNameFlat;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DomainNameDns;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DomainForestName;
        public Guid DomainGuid = new Guid();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class SID_AND_ATTR
    {
        public IntPtr pSid = IntPtr.Zero;
        public int attrs = 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class TOKEN_USER
    {
        public SID_AND_ATTR sidAndAttributes = new SID_AND_ATTR();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class SID_IDENTIFIER_AUTHORITY
    {
        public byte b1 = 0;
        public byte b2 = 0;
        public byte b3 = 0;
        public byte b4 = 0;
        public byte b5 = 0;
        public byte b6 = 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class POLICY_ACCOUNT_DOMAIN_INFO
    {
        public LSA_UNICODE_STRING domainName = new LSA_UNICODE_STRING();
        public IntPtr domainSid = IntPtr.Zero;
    }

    internal class UnsafeNativeMethods
    {
        public delegate int DsReplicaConsistencyCheck([In]IntPtr handle, int taskID, int flags);

        public delegate int DsReplicaGetInfo2W(IntPtr handle, int type, [MarshalAs(UnmanagedType.LPWStr)] string objectPath, IntPtr sourceGUID, string attributeName, string value, int flag, int context, ref IntPtr info);

        public delegate int DsReplicaGetInfoW(IntPtr handle, int type, [MarshalAs(UnmanagedType.LPWStr)] string objectPath, IntPtr sourceGUID, ref IntPtr info);

        public delegate int DsReplicaFreeInfo(int type, IntPtr value);

        public delegate int DsReplicaSyncW(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string partition, IntPtr uuid, int option);

        public delegate int DsReplicaSyncAllW(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string partition, int flags, SyncReplicaFromAllServersCallback callback, IntPtr data, ref IntPtr error);

        [DllImport("kernel32.dll", EntryPoint = "LocalFree")]
        public static extern int LocalFree(IntPtr mem);

        [DllImport("activeds.dll", EntryPoint = "ADsEncodeBinaryData", CharSet = CharSet.Unicode)]
        public static extern int ADsEncodeBinaryData(byte[] data, int length, ref IntPtr result);

        [DllImport("activeds.dll", EntryPoint = "FreeADsMem")]
        public static extern bool FreeADsMem(IntPtr pVoid);

        [DllImport("netapi32.dll", EntryPoint = "DsGetSiteNameW", CharSet = CharSet.Unicode)]
        public static extern int DsGetSiteName(string dcName, ref IntPtr ptr);

        public delegate int DsListDomainsInSiteW(IntPtr handle, [MarshalAs(UnmanagedType.LPWStr)] string site, ref IntPtr info);

        public delegate void DsFreeNameResultW(IntPtr result);

        [DllImport("Netapi32.dll", EntryPoint = "DsEnumerateDomainTrustsW", CharSet = CharSet.Unicode)]
        public static extern int DsEnumerateDomainTrustsW(string serverName, int flags, out IntPtr domains, out int count);

        [DllImport("Netapi32.dll", EntryPoint = "NetApiBufferFree")]
        public static extern int NetApiBufferFree(IntPtr buffer);

        [DllImport("Advapi32.dll", EntryPoint = "LogonUserW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int LogonUserW(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("Advapi32.dll", EntryPoint = "ImpersonateLoggedOnUser", SetLastError = true)]
        public static extern int ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("Advapi32.dll", EntryPoint = "RevertToSelf", SetLastError = true)]
        public static extern int RevertToSelf();

        [DllImport("Advapi32.dll", EntryPoint = "ConvertSidToStringSidW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int ConvertSidToStringSidW(IntPtr pSid, ref IntPtr stringSid);

        [DllImport("Advapi32.dll", EntryPoint = "ConvertStringSidToSidW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int ConvertStringSidToSidW(IntPtr stringSid, ref IntPtr pSid);

        [DllImport("Advapi32.dll", EntryPoint = "LsaSetForestTrustInformation")]
        public static extern int LsaSetForestTrustInformation(PolicySafeHandle handle, LSA_UNICODE_STRING target, IntPtr forestTrustInfo, int checkOnly, out IntPtr collisionInfo);

        [DllImport("Advapi32.dll", EntryPoint = "LsaOpenPolicy")]
        public static extern int LsaOpenPolicy(LSA_UNICODE_STRING target, LSA_OBJECT_ATTRIBUTES objectAttributes, int access, out IntPtr handle);

        [DllImport("Advapi32.dll", EntryPoint = "LsaClose")]
        public static extern int LsaClose(IntPtr handle);

        [DllImport("Advapi32.dll", EntryPoint = "LsaQueryForestTrustInformation")]
        public static extern int LsaQueryForestTrustInformation(PolicySafeHandle handle, LSA_UNICODE_STRING target, ref IntPtr ForestTrustInfo);

        [DllImport("Advapi32.dll", EntryPoint = "LsaQueryTrustedDomainInfoByName")]
        public static extern int LsaQueryTrustedDomainInfoByName(PolicySafeHandle handle, LSA_UNICODE_STRING trustedDomain, TRUSTED_INFORMATION_CLASS infoClass, ref IntPtr buffer);

        [DllImport("Advapi32.dll", EntryPoint = "LsaNtStatusToWinError")]
        public static extern int LsaNtStatusToWinError(int status);

        [DllImport("Advapi32.dll", EntryPoint = "LsaFreeMemory")]
        public static extern int LsaFreeMemory(IntPtr ptr);

        [DllImport("Advapi32.dll", EntryPoint = "LsaSetTrustedDomainInfoByName")]
        public static extern int LsaSetTrustedDomainInfoByName(PolicySafeHandle handle, LSA_UNICODE_STRING trustedDomain, TRUSTED_INFORMATION_CLASS infoClass, IntPtr buffer);

        [DllImport("Advapi32.dll", EntryPoint = "LsaOpenTrustedDomainByName")]
        public static extern int LsaOpenTrustedDomainByName(PolicySafeHandle policyHandle, LSA_UNICODE_STRING trustedDomain, int access, ref IntPtr trustedDomainHandle);

        [DllImport("Advapi32.dll", EntryPoint = "LsaDeleteTrustedDomain")]
        public static extern int LsaDeleteTrustedDomain(PolicySafeHandle handle, IntPtr pSid);

        [DllImport("netapi32.dll", EntryPoint = "I_NetLogonControl2", CharSet = CharSet.Unicode)]
        public static extern int I_NetLogonControl2(string serverName, int FunctionCode, int QueryLevel, IntPtr data, out IntPtr buffer);

        [DllImport("Kernel32.dll", EntryPoint = "GetSystemTimeAsFileTime")]
        public static extern void GetSystemTimeAsFileTime(IntPtr fileTime);

        [DllImport("Advapi32.dll", EntryPoint = "LsaQueryInformationPolicy")]
        public static extern int LsaQueryInformationPolicy(PolicySafeHandle handle, int infoClass, out IntPtr buffer);

        [DllImport("Advapi32.dll", EntryPoint = "LsaCreateTrustedDomainEx")]
        public static extern int LsaCreateTrustedDomainEx(PolicySafeHandle handle, TRUSTED_DOMAIN_INFORMATION_EX domainEx, TRUSTED_DOMAIN_AUTH_INFORMATION authInfo, int classInfo, out IntPtr domainHandle);

        [DllImport("Kernel32.dll", EntryPoint = "OpenThread", SetLastError = true)]
        public static extern IntPtr OpenThread(uint desiredAccess, bool inheirted, int threadID);

        [DllImport("Kernel32.dll", EntryPoint = "GetCurrentThreadId")]
        public static extern int GetCurrentThreadId();

        [DllImport("Advapi32.dll", EntryPoint = "ImpersonateAnonymousToken", SetLastError = true)]
        public static extern int ImpersonateAnonymousToken(IntPtr token);

        [DllImport("Kernel32.dll", EntryPoint = "CloseHandle")]
        public static extern int CloseHandle(IntPtr handle);

        [DllImport("ntdll.dll", EntryPoint = "RtlInitUnicodeString")]
        public static extern int RtlInitUnicodeString(LSA_UNICODE_STRING result, IntPtr s);

        [DllImport("Kernel32.dll", EntryPoint = "LoadLibraryW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public extern static uint FreeLibrary(IntPtr libName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, BestFitMapping = false)]
        public extern static IntPtr GetProcAddress(LoadLibrarySafeHandle hModule, string entryPoint);

        /*
        DWORD DsRoleGetPrimaryDomainInformation(
          LPCWSTR lpServer,
          DSROLE_PRIMARY_DOMAIN_INFO_LEVEL InfoLevel,
          PBYTE* Buffer
        ); */

        [DllImport("Netapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "DsRoleGetPrimaryDomainInformation", CharSet = CharSet.Unicode)]
        public static extern int DsRoleGetPrimaryDomainInformation(
            [MarshalAs(UnmanagedType.LPTStr)] string lpServer,
            [In] DSROLE_PRIMARY_DOMAIN_INFO_LEVEL InfoLevel,
            out IntPtr Buffer);

        [DllImport("Netapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "DsRoleGetPrimaryDomainInformation", CharSet = CharSet.Unicode)]
        public static extern int DsRoleGetPrimaryDomainInformation(
            [In] IntPtr lpServer,
            [In] DSROLE_PRIMARY_DOMAIN_INFO_LEVEL InfoLevel,
            out IntPtr Buffer);

        [DllImport("advapi32.dll")]
        public static extern int GetLengthSid(IntPtr sid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool IsValidSid(IntPtr sid);

        [DllImport("advapi32.dll")]
        public static extern IntPtr GetSidIdentifierAuthority(IntPtr sid);

        [DllImport("advapi32.dll")]
        public static extern IntPtr GetSidSubAuthority(IntPtr sid, int index);

        [DllImport("advapi32.dll")]
        public static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);

        [DllImport("advapi32.dll")]
        public static extern bool EqualDomainSid(IntPtr pSid1, IntPtr pSid2, ref bool equal);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CopySid(int destinationLength, IntPtr pSidDestination, IntPtr pSidSource);

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "OpenThreadToken", CharSet = CharSet.Unicode)]
        static extern public bool OpenThreadToken(
                                        IntPtr threadHandle,
                                        int desiredAccess,
                                        bool openAsSelf,
                                        ref IntPtr tokenHandle
                                        );

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "OpenProcessToken", CharSet = CharSet.Unicode)]
        static extern public bool OpenProcessToken(
                                        IntPtr processHandle,
                                        int desiredAccess,
                                        ref IntPtr tokenHandle
                                        );

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetCurrentThread", CharSet = CharSet.Unicode)]
        static extern public IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetCurrentProcess", CharSet = CharSet.Unicode)]
        static extern public IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "GetTokenInformation", CharSet = CharSet.Unicode)]
        static extern public bool GetTokenInformation(
                                        IntPtr tokenHandle,
                                        int tokenInformationClass,
                                        IntPtr buffer,
                                        int bufferSize,
                                        ref int returnLength
                                        );

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaOpenPolicy", CharSet = CharSet.Unicode)]
        static extern public int LsaOpenPolicy(
                                        IntPtr lsaUnicodeString,
                                        IntPtr lsaObjectAttributes,
                                        int desiredAccess,
                                        ref IntPtr policyHandle);

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaQueryInformationPolicy", CharSet = CharSet.Unicode)]
        static extern public int LsaQueryInformationPolicy(
                                        IntPtr policyHandle,
                                        int policyInformationClass,
                                        ref IntPtr buffer
                                        );

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaLookupSids", CharSet = CharSet.Unicode)]
        public static extern int LsaLookupSids(
                                        IntPtr policyHandle,
                                        int count,
                                        IntPtr[] sids,
                                        out IntPtr referencedDomains,
                                        out IntPtr names
                                        );

        /*
        void DsRoleFreeMemory(
          PVOID Buffer
        );
        */
        [DllImport("Netapi32.dll")]
        public static extern int DsRoleFreeMemory(
            [In] IntPtr buffer);
    }
}


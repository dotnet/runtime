// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file defines many COM dual interfaces which are legacy and,
// cannot be changed.  Tolerate possible obsoletion.
//
#pragma warning disable CS0618 // Type or member is obsolete

namespace System.DirectoryServices.AccountManagement
{
    using System.Runtime.InteropServices;
    using System;
    using System.Security;
    using System.Text;

    internal class Constants
    {
        private Constants() { }
        internal static byte[] GUID_USERS_CONTAINER_BYTE = new byte[] { 0xa9, 0xd1, 0xca, 0x15, 0x76, 0x88, 0x11, 0xd1, 0xad, 0xed, 0x00, 0xc0, 0x4f, 0xd8, 0xd5, 0xcd };
        internal static byte[] GUID_COMPUTRS_CONTAINER_BYTE = new byte[] { 0xaa, 0x31, 0x28, 0x25, 0x76, 0x88, 0x11, 0xd1, 0xad, 0xed, 0x00, 0xc0, 0x4f, 0xd8, 0xd5, 0xcd };
        internal static byte[] GUID_FOREIGNSECURITYPRINCIPALS_CONTAINER_BYTE = new byte[] { 0x22, 0xb7, 0x0c, 0x67, 0xd5, 0x6e, 0x4e, 0xfb, 0x91, 0xe9, 0x30, 0x0f, 0xca, 0x3d, 0xc1, 0xaa };
    }

    internal class SafeNativeMethods
    {
        // To stop the compiler from autogenerating a constructor for this class
        private SafeNativeMethods() { }

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetCurrentThreadId", CharSet = CharSet.Unicode)]
        static extern public int GetCurrentThreadId();

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaNtStatusToWinError", CharSet = CharSet.Unicode)]
        static extern public int LsaNtStatusToWinError(int ntStatus);
    }

    internal class UnsafeNativeMethods
    {
        // To stop the compiler from autogenerating a constructor for this class
        private UnsafeNativeMethods() { }

        [DllImport(ExternDll.Activeds, ExactSpelling = true, EntryPoint = "ADsOpenObject", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int IntADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject);
        public static int ADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject)
        {
            try
            {
                return IntADsOpenObject(path, userName, password, flags, ref iid, out ppObject);
            }
            catch (EntryPointNotFoundException)
            {
                throw new InvalidOperationException(SR.AdsiNotInstalled);
            }
        }

        //
        // ADSI Interopt
        //

        internal enum ADS_PASSWORD_ENCODING_ENUM
        {
            ADS_PASSWORD_ENCODE_REQUIRE_SSL = 0,
            ADS_PASSWORD_ENCODE_CLEAR = 1
        }

        internal enum ADS_OPTION_ENUM
        {
            ADS_OPTION_SERVERNAME = 0,
            ADS_OPTION_REFERRALS = 1,
            ADS_OPTION_PAGE_SIZE = 2,
            ADS_OPTION_SECURITY_MASK = 3,
            ADS_OPTION_MUTUAL_AUTH_STATUS = 4,
            ADS_OPTION_QUOTA = 5,
            ADS_OPTION_PASSWORD_PORTNUMBER = 6,
            ADS_OPTION_PASSWORD_METHOD = 7,
            ADS_OPTION_ACCUMULATIVE_MODIFICATION = 8,
            ADS_OPTION_SKIP_SID_LOOKUP = 9
        }

        [ComImport, Guid("7E99C0A2-F935-11D2-BA96-00C04FB6D0D1"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IADsDNWithBinary
        {
            object BinaryValue { get; set; }
            string DNString { get; set; }
        }

        [ComImport, Guid("9068270b-0939-11D1-8be1-00c04fd8d503"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IADsLargeInteger
        {
            int HighPart { get; set; }
            int LowPart { get; set; }
        }

        [ComImport, Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
        public class ADsLargeInteger
        {
        }

        [ComImport, Guid("46f14fda-232b-11d1-a808-00c04fd8d5a8"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IAdsObjectOptions
        {
            [return: MarshalAs(UnmanagedType.Struct)]
            object GetOption(
                [In]
                int option);

            void PutOption(
                [In]
                int option,
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProp);
        }

        [ComImport, Guid("FD8256D0-FD15-11CE-ABC4-02608C9E7553"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IADs
        {
            string Name
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string Class
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string GUID
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string ADsPath
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string Parent
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string Schema
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            void GetInfo();

            void SetInfo();

            [return: MarshalAs(UnmanagedType.Struct)]
            object Get(
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName);

            void Put(
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName,
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProp);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetEx(
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName);

            void PutEx(
                [In, MarshalAs(UnmanagedType.U4)]
                int lnControlCode,
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName,
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProp);

            void GetInfoEx(
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProperties,
                [In, MarshalAs(UnmanagedType.U4)]
                int lnReserved);
        }

        [ComImport, Guid("27636b00-410f-11cf-b1ff-02608c9e7553"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IADsGroup
        {
            string Name
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string Class
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string GUID
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string ADsPath
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string Parent
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string Schema
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            void GetInfo();

            void SetInfo();

            [return: MarshalAs(UnmanagedType.Struct)]
            object Get(
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName);

            void Put(
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName,
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProp);

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetEx(
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName);

            void PutEx(
                [In, MarshalAs(UnmanagedType.U4)]
                int lnControlCode,
                [In, MarshalAs(UnmanagedType.BStr)]
                string bstrName,
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProp);

            void GetInfoEx(
                [In, MarshalAs(UnmanagedType.Struct)]
                object vProperties,
                [In, MarshalAs(UnmanagedType.U4)]
                int lnReserved);

            string Description
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            IADsMembers Members();

            bool IsMember([In, MarshalAs(UnmanagedType.BStr)] string bstrMember);

            void Add([In, MarshalAs(UnmanagedType.BStr)] string bstrNewItem);

            void Remove([In, MarshalAs(UnmanagedType.BStr)] string bstrItemToBeRemoved);
        }

        [ComImport, Guid("451a0030-72ec-11cf-b03b-00aa006e0975"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IADsMembers
        {
            int Count
            {
                [return: MarshalAs(UnmanagedType.U4)]
                get;
            }

            object _NewEnum
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                get;
            }

            object Filter
            {
                [return: MarshalAs(UnmanagedType.Struct)]
                get;
                [param: MarshalAs(UnmanagedType.Struct)]
                set;
            }
        }

        [ComImport, Guid("080d0d78-f421-11d0-a36e-00c04fb950dc")]
        public class Pathname
        {
        }

        [ComImport, Guid("d592aed4-f420-11d0-a36e-00c04fb950dc"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
        public interface IADsPathname
        {
            void Set(
                [In, MarshalAs(UnmanagedType.BStr)] string bstrADsPath,
                [In, MarshalAs(UnmanagedType.U4)]  int lnSetType
                );

            void SetDisplayType(
                [In, MarshalAs(UnmanagedType.U4)] int lnDisplayType
                );

            [return: MarshalAs(UnmanagedType.BStr)]
            string Retrieve(
                [In, MarshalAs(UnmanagedType.U4)] int lnFormatType
                );

            [return: MarshalAs(UnmanagedType.U4)]
            int GetNumElements();

            [return: MarshalAs(UnmanagedType.BStr)]
            string
            GetElement(
                [In, MarshalAs(UnmanagedType.U4)]  int lnElementIndex
                );

            void AddLeafElement(
                [In, MarshalAs(UnmanagedType.BStr)] string bstrLeafElement
                );

            void RemoveLeafElement();

            [return: MarshalAs(UnmanagedType.Struct)]
            object CopyPath();

            [return: MarshalAs(UnmanagedType.BStr)]
            string GetEscapedElement(
                [In, MarshalAs(UnmanagedType.U4)] int lnReserved,
                [In, MarshalAs(UnmanagedType.BStr)] string bstrInStr
                );

            int EscapedMode
            {
                [return: MarshalAs(UnmanagedType.U4)]
                get;
                [param: MarshalAs(UnmanagedType.U4)]
                set;
            }
        }

        //
        // DSInteropt
        //

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

        public enum DSROLE_MACHINE_ROLE
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

        public enum DSROLE_PRIMARY_DOMAIN_INFO_LEVEL
        {
            DsRolePrimaryDomainInfoBasic = 1,
            DsRoleUpgradeStatus = 2,
            DsRoleOperationState = 3,
            DsRolePrimaryDomainInfoBasicEx = 4
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
        public sealed class DSROLE_PRIMARY_DOMAIN_INFO_BASIC
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

        /*
        DWORD DsRoleGetPrimaryDomainInformation(
          LPCWSTR lpServer,
          DSROLE_PRIMARY_DOMAIN_INFO_LEVEL InfoLevel,
          PBYTE* Buffer
        ); */

        [DllImport("dsrole.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "DsRoleGetPrimaryDomainInformation", CharSet = CharSet.Unicode)]
        public static extern int DsRoleGetPrimaryDomainInformation(
            [MarshalAs(UnmanagedType.LPTStr)] string lpServer,
            [In] DSROLE_PRIMARY_DOMAIN_INFO_LEVEL InfoLevel,
            out IntPtr Buffer);

        /*typedef struct _DOMAIN_CONTROLLER_INFO {
            LPTSTR DomainControllerName;
            LPTSTR DomainControllerAddress;
            ULONG DomainControllerAddressType;
            GUID DomainGuid;
            LPTSTR DomainName;
            LPTSTR DnsForestName;
            ULONG Flags;
            LPTSTR DcSiteName;
            LPTSTR ClientSiteName;
        } DOMAIN_CONTROLLER_INFO, *PDOMAIN_CONTROLLER_INFO; */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class DomainControllerInfo
        {
            public string DomainControllerName = null;
            public string DomainControllerAddress = null;
            public int DomainControllerAddressType = 0;
            public Guid DomainGuid = new Guid();
            public string DomainName = null;
            public string DnsForestName = null;
            public int Flags = 0;
            public string DcSiteName = null;
            public string ClientSiteName = null;
        }

        /*
        void DsRoleFreeMemory(
          PVOID Buffer
        );
        */
        [DllImport("dsrole.dll")]
        public static extern int DsRoleFreeMemory(
            [In] IntPtr buffer);

        /*DWORD DsGetDcName(
            LPCTSTR ComputerName, 
            LPCTSTR DomainName, 
            GUID* DomainGuid, 
            LPCTSTR SiteName, 
            ULONG Flags, 
            PDOMAIN_CONTROLLER_INFO* DomainControllerInfo
        );*/
        [DllImport("logoncli.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "DsGetDcNameW", CharSet = CharSet.Unicode)]
        public static extern int DsGetDcName(
            [In] string computerName,
            [In] string domainName,
            [In] IntPtr domainGuid,
            [In] string siteName,
            [In] int flags,
            [Out] out IntPtr domainControllerInfo);

        /* typedef struct _WKSTA_INFO_100 {
                DWORD wki100_platform_id;
                LMSTR wki100_computername;
                LMSTR wki100_langroup;
                DWORD wki100_ver_major;
                DWORD wki100_ver_minor;
        } WKSTA_INFO_100, *PWKSTA_INFO_100; */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class WKSTA_INFO_100
        {
            public int wki100_platform_id = 0;
            public string wki100_computername = null;
            public string wki100_langroup = null;
            public int wki100_ver_major = 0;
            public int wki100_ver_minor = 0;
        };

        [DllImport("wkscli.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "NetWkstaGetInfo", CharSet = CharSet.Unicode)]
        public static extern int NetWkstaGetInfo(string server, int level, ref IntPtr buffer);

        [DllImport("netutils.dll")]
        public static extern int NetApiBufferFree(
            [In] IntPtr buffer);

        //
        // SID
        //

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "ConvertSidToStringSidW", CharSet = CharSet.Unicode)]
        public static extern bool ConvertSidToStringSid(IntPtr sid, ref string stringSid);

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "ConvertStringSidToSidW", CharSet = CharSet.Unicode)]
        public static extern bool ConvertStringSidToSid(string stringSid, ref IntPtr sid);

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

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr ptr);

        [DllImport("Credui.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "CredUIParseUserNameW", CharSet = CharSet.Unicode)]
        public static extern unsafe int CredUIParseUserName(
            string pszUserName,
            char* pszUser,
            uint ulUserMaxChars,
            char* pszDomain,
            uint ulDomainMaxChars);

        // These contants were taken from the wincred.h file
        public const int CRED_MAX_USERNAME_LENGTH = 514;
        public const int CRED_MAX_DOMAIN_TARGET_LENGTH = 338;

        //
        // AuthZ functions
        //

        internal sealed class AUTHZ_RM_FLAG
        {
            private AUTHZ_RM_FLAG() { }
            public static int AUTHZ_RM_FLAG_NO_AUDIT = 0x1;
            public static int AUTHZ_RM_FLAG_INITIALIZE_UNDER_IMPERSONATION = 0x2;
            public static int AUTHZ_VALID_RM_INIT_FLAGS = (AUTHZ_RM_FLAG_NO_AUDIT | AUTHZ_RM_FLAG_INITIALIZE_UNDER_IMPERSONATION);
        }

        [DllImport("authz.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzInitializeResourceManager", CharSet = CharSet.Unicode)]
        static extern public bool AuthzInitializeResourceManager(
                                        int flags,
                                        IntPtr pfnAccessCheck,
                                        IntPtr pfnComputeDynamicGroups,
                                        IntPtr pfnFreeDynamicGroups,
                                        string name,
                                        out IntPtr rm
                                        );

        /*
        BOOL WINAPI AuthzInitializeContextFromSid(
            DWORD Flags,
            PSID UserSid,
            AUTHZ_RESOURCE_MANAGER_HANDLE AuthzResourceManager,
            PLARGE_INTEGER pExpirationTime,
            LUID Identifier,
            PVOID DynamicGroupArgs,
            PAUTHZ_CLIENT_CONTEXT_HANDLE pAuthzClientContext
        );
        */
        [DllImport("authz.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzInitializeContextFromSid", CharSet = CharSet.Unicode)]
        static extern public bool AuthzInitializeContextFromSid(
                                        int Flags,
                                        IntPtr UserSid,
                                        IntPtr AuthzResourceManager,
                                        IntPtr pExpirationTime,
                                        LUID Identitifier,
                                        IntPtr DynamicGroupArgs,
                                        out IntPtr pAuthzClientContext
                                        );

        /*
                [DllImport("authz.dll", SetLastError=true, CallingConvention=CallingConvention.StdCall, EntryPoint="AuthzInitializeContextFromToken", CharSet=CharSet.Unicode)]				
                static extern public bool AuthzInitializeContextFromToken(
                                                int Flags,
                                                IntPtr TokenHandle,
                                                IntPtr AuthzResourceManager,
                                                IntPtr pExpirationTime,
                                                LUID Identitifier,
                                                IntPtr DynamicGroupArgs,
                                                out IntPtr pAuthzClientContext
                                                );
        */
        [DllImport("authz.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzGetInformationFromContext", CharSet = CharSet.Unicode)]
        static extern public bool AuthzGetInformationFromContext(
                                        IntPtr hAuthzClientContext,
                                        int InfoClass,
                                        int BufferSize,
                                        out int pSizeRequired,
                                        IntPtr Buffer
                                        );

        [DllImport("authz.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzFreeContext", CharSet = CharSet.Unicode)]
        static extern public bool AuthzFreeContext(
                                        IntPtr AuthzClientContext
                                        );

        [DllImport("authz.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "AuthzFreeResourceManager", CharSet = CharSet.Unicode)]
        static extern public bool AuthzFreeResourceManager(
                                        IntPtr rm
                                        );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LUID
        {
            public int low;
            public int high;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class TOKEN_GROUPS
        {
            public int groupCount = 0;
            public IntPtr groups = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class SID_AND_ATTR
        {
            public IntPtr pSid = IntPtr.Zero;
            public int attrs = 0;
        }

        //
        // Token
        //

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class TOKEN_USER
        {
            public SID_AND_ATTR sidAndAttributes = new SID_AND_ATTR();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class SID_IDENTIFIER_AUTHORITY
        {
            public byte b1 = 0;
            public byte b2 = 0;
            public byte b3 = 0;
            public byte b4 = 0;
            public byte b5 = 0;
            public byte b6 = 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class LSA_OBJECT_ATTRIBUTES
        {
            public int length = 0;
            public IntPtr rootDirectory = IntPtr.Zero;
            public IntPtr objectName = IntPtr.Zero;
            public int attributes = 0;
            public IntPtr securityDescriptor = IntPtr.Zero;
            public IntPtr securityQualityOfService = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class POLICY_ACCOUNT_DOMAIN_INFO
        {
            public LSA_UNICODE_STRING domainName = new LSA_UNICODE_STRING();
            public IntPtr domainSid = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class LSA_UNICODE_STRING
        {
            public ushort length = 0;
            public ushort maximumLength = 0;
            public IntPtr buffer = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class LSA_UNICODE_STRING_Managed
        {
            public ushort length = 0;
            public ushort maximumLength = 0;
            public string buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class LSA_TRANSLATED_NAME
        {
            public int use = 0;
            public LSA_UNICODE_STRING name = new LSA_UNICODE_STRING();
            public int domainIndex = 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class LSA_REFERENCED_DOMAIN_LIST
        {
            // To stop the compiler from autogenerating a constructor for this class
            private LSA_REFERENCED_DOMAIN_LIST() { }

            public int entries = 0;
            public IntPtr domains = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public sealed class LSA_TRUST_INFORMATION
        {
            public LSA_UNICODE_STRING name = new LSA_UNICODE_STRING();
            private IntPtr _pSid = IntPtr.Zero;
        }

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

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "CloseHandle", CharSet = CharSet.Unicode)]
        static extern public bool CloseHandle(IntPtr handle);

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

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaFreeMemory", CharSet = CharSet.Unicode)]
        static extern public int LsaFreeMemory(IntPtr buffer);

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "LsaClose", CharSet = CharSet.Unicode)]
        static extern public int LsaClose(IntPtr policyHandle);

        //
        // Impersonation
        //

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "LogonUserW", CharSet = CharSet.Unicode)]
        static extern public int LogonUser(
                                    string lpszUsername,
                                    string lpszDomain,
                                    string lpszPassword,
                                    int dwLogonType,
                                    int dwLogonProvider,
                                    ref IntPtr phToken);

        [DllImport("advapi32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "ImpersonateLoggedOnUser", CharSet = CharSet.Unicode)]
        static extern public int ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("Advapi32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "RevertToSelf", CharSet = CharSet.Unicode)]
        static extern public int RevertToSelf();
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

// This file defines many COM dual interfaces which are legacy and,
// cannot be changed.  Tolerate possible obsoletion.
#pragma warning disable CS0618 // Type or member is obsolete

namespace System.DirectoryServices.AccountManagement
{
    internal static class Constants
    {
        internal static byte[] GUID_FOREIGNSECURITYPRINCIPALS_CONTAINER_BYTE = new byte[] { 0x22, 0xb7, 0x0c, 0x67, 0xd5, 0x6e, 0x4e, 0xfb, 0x91, 0xe9, 0x30, 0x0f, 0xca, 0x3d, 0xc1, 0xaa };
    }

    internal static class UnsafeNativeMethods
    {
        public static int ADsOpenObject(string path, string userName, string password, int flags, [In, Out] ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppObject)
        {
            try
            {
                int hr = Interop.Activeds.ADsOpenObject(path, userName, password, flags, ref iid, out IntPtr ppObjPtr);
                ppObject = Marshal.GetObjectForIUnknown(ppObjPtr);
                return hr;
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
            public Guid DomainGuid;
        }

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
            public string DomainControllerName;
            public string DomainControllerAddress;
            public int DomainControllerAddressType;
            public Guid DomainGuid;
            public string DomainName;
            public string DnsForestName;
            public int Flags;
            public string DcSiteName;
            public string ClientSiteName;
        }

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
            public int wki100_platform_id;
            public string wki100_computername;
            public string wki100_langroup;
            public int wki100_ver_major;
            public int wki100_ver_minor;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct POLICY_ACCOUNT_DOMAIN_INFO
        {
            public Interop.UNICODE_INTPTR_STRING DomainName;
            public IntPtr DomainSid;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{

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
    internal sealed class DomainControllerInfo
    {
        public string DomainControllerName = null!;
        public string? DomainControllerAddress;
        public int DomainControllerAddressType;
        public Guid DomainGuid;
        public string? DomainName;
        public string? DnsForestName;
        public int Flags;
        public string? DcSiteName;
        public string? ClientSiteName;
    }

    /*typedef struct {
         LPTSTR NetbiosName;
        LPTSTR DnsHostName;
        LPTSTR SiteName;
        LPTSTR SiteObjectName;
        LPTSTR ComputerObjectName;
        LPTSTR ServerObjectName;
        LPTSTR NtdsaObjectName;
        BOOL fIsPdc;
        BOOL fDsEnabled;
        BOOL fIsGc;
        GUID SiteObjectGuid;
        GUID ComputerObjectGuid;
        GUID ServerObjectGuid;
        GUID NtdsDsaObjectGuid;
    } DS_DOMAIN_CONTROLLER_INFO_2, *PDS_DOMAIN_CONTROLLER_INFO_2;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class DsDomainControllerInfo2
    {
        public string? netBiosName;
        public string? dnsHostName;
        public string? siteName;
        public string? siteObjectName;
        public string? computerObjectName;
        public string? serverObjectName;
        public string? ntdsaObjectName;
        public bool isPdc;
        public bool dsEnabled;
        public bool isGC;
        public Guid siteObjectGuid;
        public Guid computerObjectGuid;
        public Guid serverObjectGuid;
        public Guid ntdsDsaObjectGuid;
    }

    /*typedef struct {
        LPTSTR NetbiosName;
        LPTSTR DnsHostName;
        LPTSTR SiteName;
        LPTSTR SiteObjectName;
        LPTSTR ComputerObjectName;
        LPTSTR ServerObjectName;
        LPTSTR NtdsaObjectName;
        BOOL fIsPdc;
        BOOL fDsEnabled;
        BOOL fIsGc;
        BOOL fIsRodc;
        GUID SiteObjectGuid;
        GUID ComputerObjectGuid;
        GUID ServerObjectGuid;
        GUID NtdsDsaObjectGuid;
    } DS_DOMAIN_CONTROLLER_INFO_3, *PDS_DOMAIN_CONTROLLER_INFO_3;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class DsDomainControllerInfo3
    {
        public string? netBiosName;
        public string? dnsHostName;
        public string? siteName;
        public string? siteObjectName;
        public string? computerObjectName;
        public string? serverObjectName;
        public string? ntdsaObjectName;
        public bool isPdc;
        public bool dsEnabled;
        public bool isGC;
        public bool isRodc;
        public Guid siteObjectGuid;
        public Guid computerObjectGuid;
        public Guid serverObjectGuid;
        public Guid ntdsDsaObjectGuid;
    }

    /*typedef struct {
        DWORD cItems;
        PDS_NAME_RESULT_ITEM rItems;
    } DS_NAME_RESULT, *PDS_NAME_RESULT;*/
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DsNameResult
    {
        public int itemCount;
        public IntPtr items;
    }

    /*typedef struct  {
        DWORD status;
        LPTSTR pDomain;
        LPTSTR pName;
    } DS_NAME_RESULT_ITEM, *PDS_NAME_RESULT_ITEM;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class DsNameResultItem
    {
        public int status;
        public string? domain;
        public string? name;
    }

    /*typedef struct _DnsRecord {
        struct _DnsRecord * pNext;
        LPTSTR              pName;
        WORD                wType;
        WORD                wDataLength; // Not referenced for DNS record
        //types defined above.
        union {
            DWORD               DW;      // flags as DWORD
            DNS_RECORD_FLAGS    S;       // flags as structure
        } Flags;

        DWORD               dwTtl;
        DWORD               dwReserved;

        // Record Data
        union {
            DNS_A_DATA      A;
            DNS_SOA_DATA    SOA, Soa;
            DNS_PTR_DATA    PTR, Ptr,
                            NS, Ns,
                            CNAME, Cname,
                            MB, Mb,
                            MD, Md,
                            MF, Mf,
                            MG, Mg,
                            MR, Mr;
            DNS_MINFO_DATA  MINFO, Minfo,
                            RP, Rp;
            DNS_MX_DATA     MX, Mx,
                            AFSDB, Afsdb,
                            RT, Rt;
            DNS_TXT_DATA    HINFO, Hinfo,
                            ISDN, Isdn,
                            TXT, Txt,
                            X25;
            DNS_NULL_DATA   Null;
            DNS_WKS_DATA    WKS, Wks;
            DNS_AAAA_DATA   AAAA;
            DNS_KEY_DATA    KEY, Key;
            DNS_SIG_DATA    SIG, Sig;
            DNS_ATMA_DATA   ATMA, Atma;
            DNS_NXT_DATA    NXT, Nxt;
            DNS_SRV_DATA    SRV, Srv;
            DNS_TKEY_DATA   TKEY, Tkey;
            DNS_TSIG_DATA   TSIG, Tsig;
            DNS_WINS_DATA   WINS, Wins;
            DNS_WINSR_DATA  WINSR, WinsR, NBSTAT, Nbstat;
        } Data;
    }DNS_RECORD, *PDNS_RECORD;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class DnsRecord
    {
        public IntPtr next;
        public string? name;
        public short type;
        public short dataLength;
        public int flags;
        public int ttl;
        public int reserved;
        public DnsSrvData data = null!;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class PartialDnsRecord
    {
        public IntPtr next;
        public string? name;
        public short type;
        public short dataLength;
        public int flags;
        public int ttl;
        public int reserved;
        public IntPtr data;
    }

    /*typedef struct {
        LPTSTR      pNameTarget;
        WORD        wPriority;
        WORD        wWeight;
        WORD        wPort;
        WORD        Pad;            // keep ptrs DWORD aligned
    }DNS_SRV_DATA, *PDNS_SRV_DATA;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class DnsSrvData
    {
        public string targetName = null!;
        public short priority;
        public short weight;
        public short port;
        public short pad;
    }

    /*typedef struct _OSVERSIONINFOEX {
        DWORD dwOSVersionInfoSize;
        DWORD dwMajorVersion;
        DWORD dwMinorVersion;
        DWORD dwBuildNumber;
        DWORD dwPlatformId;
        TCHAR szCSDVersion[ 128 ];
        WORD wServicePackMajor;
        WORD wServicePackMinor;
        WORD wSuiteMask;
        BYTE wProductType;
        BYTE wReserved;
        } OSVERSIONINFOEX, *POSVERSIONINFOEX, *LPOSVERSIONINFOEX;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class OSVersionInfoEx
    {
        public OSVersionInfoEx()
        {
            osVersionInfoSize = (int)Marshal.SizeOf(this);
        }

        // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
        public int osVersionInfoSize;
        public int majorVersion;
        public int minorVersion;
        public int buildNumber;
        public int platformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? csdVersion = null;
        public short servicePackMajor;
        public short servicePackMinor;
        public short suiteMask;
        public byte productType;
        public byte reserved;
    }

    /*typedef struct _NEGOTIATE_CALLER_NAME_REQUEST {
            ULONG       MessageType ;
            LUID        LogonId ;
    } NEGOTIATE_CALLER_NAME_REQUEST, *PNEGOTIATE_CALLER_NAME_REQUEST ;*/
    [StructLayout(LayoutKind.Sequential)]
    internal struct NegotiateCallerNameRequest
    {
        public int messageType;
        public global::Interop.LUID logonId;
    }

    /*typedef struct _NEGOTIATE_CALLER_NAME_RESPONSE {
            ULONG       MessageType ;
            PWSTR       CallerName ;
        } NEGOTIATE_CALLER_NAME_RESPONSE, * PNEGOTIATE_CALLER_NAME_RESPONSE ;*/
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class NegotiateCallerNameResponse
    {
        public int messageType;
        public string? callerName;
    }

    internal sealed partial class NativeMethods
    {
        // disable public constructor
        private NativeMethods() { }

        internal const int VER_PLATFORM_WIN32_NT = 2;
        internal const int ERROR_INVALID_DOMAIN_NAME_FORMAT = 1212;
        internal const int ERROR_NO_SUCH_DOMAIN = 1355;
        internal const int ERROR_NOT_ENOUGH_MEMORY = 8;
        internal const int ERROR_INVALID_FLAGS = 1004;
        internal const int DS_NAME_NO_ERROR = 0;
        internal const int ERROR_NO_MORE_ITEMS = 259;
        internal const int ERROR_FILE_MARK_DETECTED = 1101;
        internal const int DNS_ERROR_RCODE_NAME_ERROR = 9003;
        internal const int ERROR_NO_SUCH_LOGON_SESSION = 1312;

        internal const int DS_NAME_FLAG_SYNTACTICAL_ONLY = 1;
        internal const int DS_FQDN_1779_NAME = 1;
        internal const int DS_CANONICAL_NAME = 7;
        internal const int DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING = 6;

        internal const int STATUS_QUOTA_EXCEEDED = unchecked((int)0xC0000044);

        /*DWORD DsGetDcName(
                LPCTSTR ComputerName,
                LPCTSTR DomainName,
                GUID* DomainGuid,
                LPCTSTR SiteName,
                ULONG Flags,
                PDOMAIN_CONTROLLER_INFO* DomainControllerInfo
                );*/
        [LibraryImport(global::Interop.Libraries.Netapi32, EntryPoint = "DsGetDcNameW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsGetDcName(
            string? computerName,
            string? domainName,
            IntPtr domainGuid,
            string? siteName,
            int flags,
            out IntPtr domainControllerInfo);

        /* DWORD WINAPI DsGetDcOpen(
                         LPCTSTR DnsName,
                         ULONG OptionFlags,
                         LPCTSTR SiteName,
                         GUID* DomainGuid,
                         LPCTSTR DnsForestName,
                         ULONG DcFlags,
                         PHANDLE RetGetDcContext
                         );*/
        [LibraryImport(global::Interop.Libraries.Netapi32, EntryPoint = "DsGetDcOpenW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsGetDcOpen(
            string? dnsName,
            int optionFlags,
            string? siteName,
            IntPtr domainGuid,
            string? dnsForestName,
            int dcFlags,
            out IntPtr retGetDcContext);

        /*DWORD WINAPI DsGetDcNext(
                        HANDLE GetDcContextHandle,
                        PULONG SockAddressCount,
                        LPSOCKET_ADDRESS* SockAddresses,
                        LPTSTR* DnsHostName
                        );*/
        [LibraryImport(global::Interop.Libraries.Netapi32, EntryPoint = "DsGetDcNextW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DsGetDcNext(
            IntPtr getDcContextHandle,
            ref IntPtr sockAddressCount,
            out IntPtr sockAddresses,
            out IntPtr dnsHostName);

        /*void WINAPI DsGetDcClose(
                        HANDLE GetDcContextHandle
                        );*/
        [LibraryImport(global::Interop.Libraries.Netapi32, EntryPoint = "DsGetDcCloseW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void DsGetDcClose(
            IntPtr getDcContextHandle);

        /*NET_API_STATUS NetApiBufferFree(
                LPVOID Buffer
                );*/
        [LibraryImport(global::Interop.Libraries.Netapi32)]
        internal static partial int NetApiBufferFree(
            IntPtr buffer);

        internal const int DsDomainControllerInfoLevel2 = 2;
        internal const int DsDomainControllerInfoLevel3 = 3;

        internal const int DsNameNoError = 0;

        internal const int DnsSrvData = 33;
        internal const int DnsQueryBypassCache = 8;

        /*DNS_STATUS WINAPI DnsQuery (
            LPSTR lpstrName,
            WORD wType,
            DWORD fOptions,
            PIP4_ARRAY aipServers,
            PDNS_RECORD *ppQueryResultsSet,
            PVOID *pReserved
            );*/
        [LibraryImport(global::Interop.Libraries.Dnsapi, EntryPoint = "DnsQuery_W", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DnsQuery(
            string recordName,
            short recordType,
            int options,
            IntPtr servers,
            out IntPtr dnsResultList,
            IntPtr reserved);

        /*VOID WINAPI DnsRecordListFree(
            PDNS_RECORD pRecordList,
            DNS_FREE_TYPE FreeType
            );*/
        [LibraryImport(global::Interop.Libraries.Dnsapi)]
        internal static partial void DnsRecordListFree(
            IntPtr dnsResultList,
            [MarshalAs(UnmanagedType.Bool)] bool dnsFreeType);

        /*NTSTATUS LsaConnectUntrusted(
              PHANDLE LsaHandle
            );*/
        [LibraryImport(global::Interop.Libraries.Secur32)]
        internal static partial uint LsaConnectUntrusted(
             out LsaLogonProcessSafeHandle lsaHandle);

        internal const int NegGetCallerName = 1;

        /*NTSTATUS LsaCallAuthenticationPackage(
              HANDLE LsaHandle,
              ULONG AuthenticationPackage,
              PVOID ProtocolSubmitBuffer,
              ULONG SubmitBufferLength,
              PVOID* ProtocolReturnBuffer,
              PULONG ReturnBufferLength,
              PNTSTATUS ProtocolStatus
            );*/
        [LibraryImport(global::Interop.Libraries.Secur32)]
        internal static partial uint LsaCallAuthenticationPackage(
            LsaLogonProcessSafeHandle lsaHandle,
            int authenticationPackage,
            in NegotiateCallerNameRequest protocolSubmitBuffer,
            int submitBufferLength,
            out IntPtr protocolReturnBuffer,
            out int returnBufferLength,
            out uint protocolStatus);

        /*NTSTATUS LsaFreeReturnBuffer(
              PVOID Buffer
            );*/
        [LibraryImport(global::Interop.Libraries.Secur32)]
        internal static partial uint LsaFreeReturnBuffer(
            IntPtr buffer);

        /*NTSTATUS LsaDeregisterLogonProcess(
              HANDLE LsaHandle
            );*/
        [LibraryImport(global::Interop.Libraries.Secur32)]
        internal static partial int LsaDeregisterLogonProcess(
            IntPtr lsaHandle);

        /*int CompareString(LCID Locale,
            DWORD dwCmpFlags,
            DWORD lpString1,
            DWORD cchCount1,
            DWORD lpString2,
            DWORD cchCount2
            );*/
        [LibraryImport(global::Interop.Libraries.Kernel32, EntryPoint = "CompareStringW",  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int CompareString(
            uint locale,
            uint dwCmpFlags,
            IntPtr lpString1,
            int cchCount1,
            IntPtr lpString2,
            int cchCount2);
    }

    internal sealed class NativeComInterfaces
    {
        /*typedef enum {
           ADS_SETTYPE_FULL=1,
           ADS_SETTYPE_PROVIDER=2,
           ADS_SETTYPE_SERVER=3,
           ADS_SETTYPE_DN=4
        } ADS_SETTYPE_ENUM;

        typedef enum {
           ADS_FORMAT_WINDOWS=1,
           ADS_FORMAT_WINDOWS_NO_SERVER=2,
           ADS_FORMAT_WINDOWS_DN=3,
           ADS_FORMAT_WINDOWS_PARENT=4,
           ADS_FORMAT_X500=5,
           ADS_FORMAT_X500_NO_SERVER=6,
           ADS_FORMAT_X500_DN=7,
           ADS_FORMAT_X500_PARENT=8,
           ADS_FORMAT_SERVER=9,
           ADS_FORMAT_PROVIDER=10,
           ADS_FORMAT_LEAF=11
        } ADS_FORMAT_ENUM;

        typedef enum {
           ADS_ESCAPEDMODE_DEFAULT=1,
           ADS_ESCAPEDMODE_ON=2,
           ADS_ESCAPEDMODE_OFF=3,
           ADS_ESCAPEDMODE_OFF_EX=4
        } ADS_ESCAPE_MODE_ENUM;*/

        internal const int ADS_SETTYPE_DN = 4;
        internal const int ADS_FORMAT_X500_DN = 7;
        internal const int ADS_ESCAPEDMODE_ON = 2;
        internal const int ADS_ESCAPEDMODE_OFF_EX = 4;
        internal const int ADS_FORMAT_LEAF = 11;

        //
        // Pathname as a co-class that implements the IAdsPathname interface
        //
        [ComImport, Guid("080d0d78-f421-11d0-a36e-00c04fb950dc")]
        internal class Pathname
        {
        }

        [ComImport, Guid("D592AED4-F420-11D0-A36E-00C04FB950DC")]
        internal interface IAdsPathname
        {
            // HRESULT Set([in] BSTR bstrADsPath,  [in] long lnSetType);
            int Set([In, MarshalAs(UnmanagedType.BStr)] string bstrADsPath, [In, MarshalAs(UnmanagedType.U4)] int lnSetType);

            // HRESULT SetDisplayType([in] long lnDisplayType);
            int SetDisplayType([In, MarshalAs(UnmanagedType.U4)] int lnDisplayType);

            // HRESULT Retrieve([in] long lnFormatType,  [out, retval] BSTR* pbstrADsPath);
            [return: MarshalAs(UnmanagedType.BStr)]
            string Retrieve([In, MarshalAs(UnmanagedType.U4)] int lnFormatType);

            // HRESULT GetNumElements([out, retval] long* plnNumPathElements);
            [return: MarshalAs(UnmanagedType.U4)]
            int GetNumElements();

            // HRESULT GetElement([in]  long lnElementIndex,  [out, retval] BSTR* pbstrElement);
            [return: MarshalAs(UnmanagedType.BStr)]
            string GetElement([In, MarshalAs(UnmanagedType.U4)] int lnElementIndex);

            // HRESULT AddLeafElement([in] BSTR bstrLeafElement);
            void AddLeafElement([In, MarshalAs(UnmanagedType.BStr)] string bstrLeafElement);

            // HRESULT RemoveLeafElement();
            void RemoveLeafElement();

            // HRESULT CopyPath([out, retval] IDispatch** ppAdsPath);
            [return: MarshalAs(UnmanagedType.Interface)]
            object CopyPath();

            // HRESULT GetEscapedElement([in] long lnReserved, [in] BSTR bstrInStr, [out, retval] BSTR*  pbstrOutStr );
            [return: MarshalAs(UnmanagedType.BStr)]
            string GetEscapedElement([In, MarshalAs(UnmanagedType.U4)] int lnReserved, [In, MarshalAs(UnmanagedType.BStr)] string bstrInStr);

            int EscapedMode
            {
                get;
                set;
            }
        }

        [ComImport, Guid("C8F93DD3-4AE0-11CF-9E73-00AA004A5691")]
        internal interface IAdsProperty
        {
            //
            // Need to also include the IAds interface definition here
            //

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

            object Get([In, MarshalAs(UnmanagedType.BStr)] string bstrName);

            void Put([In, MarshalAs(UnmanagedType.BStr)] string bstrName,
                        [In] object vProp);

            object GetEx([In, MarshalAs(UnmanagedType.BStr)] string bstrName);

            void PutEx([In, MarshalAs(UnmanagedType.U4)] int lnControlCode,
                        [In, MarshalAs(UnmanagedType.BStr)] string bstrName,
                        [In] object vProp);

            void GetInfoEx([In] object vProperties,
                        [In, MarshalAs(UnmanagedType.U4)] int lnReserved);

            //
            // IAdsProperty definition starts here
            //

            string OID
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string Syntax
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            int MaxRange
            {
                [return: MarshalAs(UnmanagedType.U4)]
                get;
                [param: MarshalAs(UnmanagedType.U4)]
                set;
            }

            int MinRange
            {
                [return: MarshalAs(UnmanagedType.U4)]
                get;
                [param: MarshalAs(UnmanagedType.U4)]
                set;
            }

            bool MultiValued
            {
                get;
                set;
            }
            object Qualifiers();
        }

        [ComImport, Guid("C8F93DD0-4AE0-11CF-9E73-00AA004A5691")]
        internal interface IAdsClass
        {
            //
            // Need to also include the IAds interface definition here
            //

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

            object Get([In, MarshalAs(UnmanagedType.BStr)] string bstrName);

            void Put([In, MarshalAs(UnmanagedType.BStr)] string bstrName,
                        [In] object vProp);

            object GetEx([In, MarshalAs(UnmanagedType.BStr)] string bstrName);

            void PutEx([In, MarshalAs(UnmanagedType.U4)] int lnControlCode,
                        [In, MarshalAs(UnmanagedType.BStr)] string bstrName,
                        [In] object vProp);

            void GetInfoEx([In] object vProperties,
                        [In, MarshalAs(UnmanagedType.U4)] int lnReserved);

            //
            // IAdsClass definition starts here
            //

            string PrimaryInterface
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
            }

            string CLSID
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            string OID
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            bool Abstract { get; set; }

            bool Auxiliary { get; set; }

            object MandatoryProperties
            {
                get;
                set;
            }

            object OptionalProperties
            {
                get;
                set;
            }

            object NamingProperties { get; set; }

            object DerivedFrom
            {
                get;
                set;
            }

            object AuxDerivedFrom
            {
                get;
                set;
            }

            object PossibleSuperiors
            {
                get;
                set;
            }

            object Containment { get; set; }

            bool Container { get; set; }

            string HelpFileName
            {
                [return: MarshalAs(UnmanagedType.BStr)]
                get;
                [param: MarshalAs(UnmanagedType.BStr)]
                set;
            }

            int HelpFileContext
            {
                [return: MarshalAs(UnmanagedType.U4)]
                get;
                [param: MarshalAs(UnmanagedType.U4)]
                set;
            }

            [return: MarshalAs(UnmanagedType.Interface)]
            object Qualifiers();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security;

namespace System.DirectoryServices.Protocols
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class Luid
    {
        private int _lowPart;
        private int _highPart;

        public int LowPart => _lowPart;
        public int HighPart => _highPart;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class SEC_WINNT_AUTH_IDENTITY_EX
    {
        public int version;
        public int length;
        public string user;
        public int userLength;
        public string domain;
        public int domainLength;
        public string password;
        public int passwordLength;
        public int flags;
        public string packageList;
        public int packageListLength;
    }

    internal enum BindMethod : uint
    {
        LDAP_AUTH_OTHERKIND = 0x86,
        LDAP_AUTH_SICILY = LDAP_AUTH_OTHERKIND | 0x0200,
        LDAP_AUTH_MSN = LDAP_AUTH_OTHERKIND | 0x0800,
        LDAP_AUTH_NTLM = LDAP_AUTH_OTHERKIND | 0x1000,
        LDAP_AUTH_DPA = LDAP_AUTH_OTHERKIND | 0x2000,
        LDAP_AUTH_NEGOTIATE = LDAP_AUTH_OTHERKIND | 0x0400,
        LDAP_AUTH_SSPI = LDAP_AUTH_NEGOTIATE,
        LDAP_AUTH_DIGEST = LDAP_AUTH_OTHERKIND | 0x4000,
        LDAP_AUTH_EXTERNAL = LDAP_AUTH_OTHERKIND | 0x0020
    }

    internal enum LdapOption
    {
        LDAP_OPT_DESC = 0x01,
        LDAP_OPT_DEREF = 0x02,
        LDAP_OPT_SIZELIMIT = 0x03,
        LDAP_OPT_TIMELIMIT = 0x04,
        LDAP_OPT_REFERRALS = 0x08,
        LDAP_OPT_RESTART = 0x09,
        LDAP_OPT_SSL = 0x0a,
        LDAP_OPT_REFERRAL_HOP_LIMIT = 0x10,
        LDAP_OPT_VERSION = 0x11,
        LDAP_OPT_API_FEATURE_INFO = 0x15,
        LDAP_OPT_HOST_NAME = 0x30,
        LDAP_OPT_ERROR_NUMBER = 0x31,
        LDAP_OPT_ERROR_STRING = 0x32,
        LDAP_OPT_SERVER_ERROR = 0x33,
        LDAP_OPT_SERVER_EXT_ERROR = 0x34,
        LDAP_OPT_HOST_REACHABLE = 0x3E,
        LDAP_OPT_PING_KEEP_ALIVE = 0x36,
        LDAP_OPT_PING_WAIT_TIME = 0x37,
        LDAP_OPT_PING_LIMIT = 0x38,
        LDAP_OPT_DNSDOMAIN_NAME = 0x3B,
        LDAP_OPT_GETDSNAME_FLAGS = 0x3D,
        LDAP_OPT_PROMPT_CREDENTIALS = 0x3F,
        LDAP_OPT_TCP_KEEPALIVE = 0x40,
        LDAP_OPT_FAST_CONCURRENT_BIND = 0x41,
        LDAP_OPT_SEND_TIMEOUT = 0x42,
        LDAP_OPT_REFERRAL_CALLBACK = 0x70,
        LDAP_OPT_CLIENT_CERTIFICATE = 0x80,
        LDAP_OPT_SERVER_CERTIFICATE = 0x81,
        LDAP_OPT_AUTO_RECONNECT = 0x91,
        LDAP_OPT_SSPI_FLAGS = 0x92,
        LDAP_OPT_SSL_INFO = 0x93,
        LDAP_OPT_SIGN = 0x95,
        LDAP_OPT_ENCRYPT = 0x96,
        LDAP_OPT_SASL_METHOD = 0x97,
        LDAP_OPT_AREC_EXCLUSIVE = 0x98,
        LDAP_OPT_SECURITY_CONTEXT = 0x99,
        LDAP_OPT_ROOTDSE_CACHE = 0x9a
    }

    internal enum ResultAll
    {
        LDAP_MSG_ALL = 1,
        LDAP_MSG_RECEIVED = 2,
        LDAP_MSG_POLLINGALL = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class LDAP_TIMEVAL
    {
        public int tv_sec;
        public int tv_usec;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class berval
    {
        public int bv_len = 0;
        public IntPtr bv_val = IntPtr.Zero;

        public berval() { }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class SafeBerval
    {
        public int bv_len = 0;
        public IntPtr bv_val = IntPtr.Zero;

        ~SafeBerval()
        {
            if (bv_val != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(bv_val);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class LdapControl
    {
        public IntPtr ldctl_oid = IntPtr.Zero;
        public berval ldctl_value = null;
        public bool ldctl_iscritical = false;

        public LdapControl() { }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct LdapReferralCallback
    {
        public int sizeofcallback;
        public QUERYFORCONNECTIONInternal query;
        public NOTIFYOFNEWCONNECTIONInternal notify;
        public DEREFERENCECONNECTIONInternal dereference;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CRYPTOAPI_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SecPkgContext_IssuerListInfoEx
    {
        public IntPtr aIssuers;
        public int cIssuers;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class LdapMod
    {
        public int type = 0;
        public IntPtr attribute = IntPtr.Zero;
        public IntPtr values = IntPtr.Zero;

        ~LdapMod()
        {
            if (attribute != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(attribute);
            }

            if (values != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(values);
            }
        }
    }

    internal class Wldap32
    {
        private const string Wldap32dll = "wldap32.dll";

        public const int SEC_WINNT_AUTH_IDENTITY_UNICODE = 0x2;
        public const int SEC_WINNT_AUTH_IDENTITY_VERSION = 0x200;
        public const string MICROSOFT_KERBEROS_NAME_W = "Kerberos";

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_bind_sW", CharSet = CharSet.Unicode)]
        public static extern int ldap_bind_s([In]ConnectionHandle ldapHandle, string dn, SEC_WINNT_AUTH_IDENTITY_EX credentials, BindMethod method);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_initW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_init(string hostName, int portNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "ldap_connect", CharSet = CharSet.Unicode)]
        public static extern int ldap_connect([In] ConnectionHandle ldapHandle, LDAP_TIMEVAL timeout);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "ldap_unbind", CharSet = CharSet.Unicode)]
        public static extern int ldap_unbind([In] IntPtr ldapHandle);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_int([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref int outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_int([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref int inValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_ptr([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref IntPtr outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_ptr([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref IntPtr inValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_sechandle([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref SecurityHandle outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_secInfo([In] ConnectionHandle ldapHandle, [In] LdapOption option, [In, Out] SecurityPackageContextConnectionInformation outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_referral([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref LdapReferralCallback outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_clientcert([In] ConnectionHandle ldapHandle, [In] LdapOption option, QUERYCLIENTCERT outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_servercert([In] ConnectionHandle ldapHandle, [In] LdapOption option, VERIFYSERVERCERT outValue);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LdapGetLastError")]
        public static extern int LdapGetLastError();

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "cldap_openW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr cldap_open(string hostName, int portNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_simple_bind_sW", CharSet = CharSet.Unicode)]
        public static extern int ldap_simple_bind_s([In] ConnectionHandle ldapHandle, string distinguishedName, string password);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_delete_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_delete_ext([In] ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_result", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int ldap_result([In] ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Mesage);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_resultW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_result([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_resultW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_result_referral([In] ConnectionHandle ldapHandle, [In] IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_memfreeW", CharSet = CharSet.Unicode)]
        public static extern void ldap_memfree([In] IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_value_freeW", CharSet = CharSet.Unicode)]
        public static extern int ldap_value_free([In] IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_controls_freeW", CharSet = CharSet.Unicode)]
        public static extern int ldap_controls_free([In] IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_abandon", CharSet = CharSet.Unicode)]
        public static extern int ldap_abandon([In] ConnectionHandle ldapHandle, [In] int messagId);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_start_tls_sW", CharSet = CharSet.Unicode)]
        public static extern int ldap_start_tls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_stop_tls_s", CharSet = CharSet.Unicode)]
        public static extern byte ldap_stop_tls(ConnectionHandle ldapHandle);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_rename_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_rename([In] ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_compare_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_compare([In] ConnectionHandle ldapHandle, string dn, string attributeName, string strValue, berval binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_add_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_add([In] ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_modify_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_modify([In] ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_extended_operationW", CharSet = CharSet.Unicode)]
        public static extern int ldap_extended_operation([In] ConnectionHandle ldapHandle, string oid, berval data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_extended_resultW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_extended_result([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_msgfree", CharSet = CharSet.Unicode)]
        public static extern int ldap_msgfree([In] IntPtr result);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_search_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_search([In] ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_first_entry", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_first_entry([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_next_entry", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_next_entry([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_first_reference", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_first_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_next_reference", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_next_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_dnW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_get_dn([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_first_attributeW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_first_attribute([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr address);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_next_attributeW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_next_attribute([In] ConnectionHandle ldapHandle, [In] IntPtr result, [In, Out] IntPtr address);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_free", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_free([In] IntPtr berelement, int option);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_values_lenW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_get_values_len([In] ConnectionHandle ldapHandle, [In] IntPtr result, string name);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_value_free_len", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_value_free_len([In] IntPtr berelement);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_referenceW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr referrals);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_alloc_t", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_alloc(int option);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_emptyarg(BerSafeHandle berElement, string format);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_int(BerSafeHandle berElement, string format, int value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_bytearray(BerSafeHandle berElement, string format, HGlobalMemHandle value, int length);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_printf", CharSet = CharSet.Unicode)]
        public static extern int ber_printf_berarray(BerSafeHandle berElement, string format, IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_flatten", CharSet = CharSet.Unicode)]
        public static extern int ber_flatten(BerSafeHandle berElement, ref IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_init", CharSet = CharSet.Unicode)]
        public static extern IntPtr ber_init(berval value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf(BerSafeHandle berElement, string format);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_int(BerSafeHandle berElement, string format, ref int value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_ptr(BerSafeHandle berElement, string format, ref IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_scanf", CharSet = CharSet.Unicode)]
        public static extern int ber_scanf_bitstring(BerSafeHandle berElement, string format, ref IntPtr value, ref int length);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_bvfree", CharSet = CharSet.Unicode)]
        public static extern int ber_bvfree(IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ber_bvecfree", CharSet = CharSet.Unicode)]
        public static extern int ber_bvecfree(IntPtr value);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_create_sort_controlW", CharSet = CharSet.Unicode)]
        public static extern int ldap_create_sort_control(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_control_freeW", CharSet = CharSet.Unicode)]
        public static extern int ldap_control_free(IntPtr control);

        [DllImport("Crypt32.dll", EntryPoint = "CertFreeCRLContext", CharSet = CharSet.Unicode)]
        public static extern int CertFreeCRLContext(IntPtr certContext);

        [DllImport(Wldap32dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_result2error", CharSet = CharSet.Unicode)]
        public static extern int ldap_result2error([In] ConnectionHandle ldapHandle, [In] IntPtr result, int freeIt);
    }
}

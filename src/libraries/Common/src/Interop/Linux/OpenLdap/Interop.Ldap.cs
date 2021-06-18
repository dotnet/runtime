// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.DirectoryServices.Protocols;

namespace System.DirectoryServices.Protocols
{
    /// <summary>
    /// Structure that will get passed into the Sasl interactive callback in case
    /// the authentication process emits challenges to validate information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SaslDefaultCredentials
    {
        public string mech;
        public string realm;
        public string authcid;
        public string passwd;
        public string authzid;
    }

    /// <summary>
    /// Structure that will represent a Sasl Interactive challenge during a
    /// Sasl interactive bind, which will contain the challenge and it is also
    /// where we will have to resolve the result.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal sealed class SaslInteractiveChallenge
    {
        public ulong saslChallengeType;
        public string challenge;
        public string prompt;
        public string defresult;
        public IntPtr result;
        public uint len;
    }

    internal enum SaslChallengeType
    {
        SASL_CB_LIST_END = 0,
        SASL_CB_GETOPT = 1,
        SASL_CB_LOG = 2,
        SASL_CB_GETPATH = 3,
        SASL_CB_VERIFYFILE = 4,
        SASL_CB_GETCONFPATH = 5,
        SASL_CB_USER = 0x4001,
        SASL_CB_AUTHNAME = 0x4002,
        SASL_CB_LANGUAGE = 0x4003,
        SASL_CB_PASS = 0x4004,
        SASL_CB_ECHOPROMPT = 0x4005,
        SASL_CB_NOECHOPROMPT = 0x4006,
        SASL_CB_CNONCE = 0x4007,
        SASL_CB_GETREALM = 0x4008,
        SASL_CB_PROXY_POLICY = 0x8001,
    }
}

internal delegate int LDAP_SASL_INTERACT_PROC(IntPtr ld, uint flags, IntPtr defaults, IntPtr interact);

internal static partial class Interop
{
    public const string LDAP_SASL_SIMPLE = null;

    internal static partial class Ldap
    {
        static Ldap()
        {
            // OpenLdap must be initialized on a single thread, once this is done it allows concurrent calls
            // By doing so in the static constructor we guarantee this is run before any other methods are called.

            // we call ldap_get_option_int to get an option and trigger the initialization as reccomended by
            // https://www.openldap.org/software//man.cgi?query=ldap_init
            int unused = 0;
            ldap_get_option_int(IntPtr.Zero, LdapOption.LDAP_OPT_DEBUG_LEVEL, ref unused);
        }

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_initialize", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int ldap_initialize(out IntPtr ld, string uri);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_unbind_ext_s", CharSet = CharSet.Ansi)]
        public static extern int ldap_unbind_ext_s(IntPtr ld, ref IntPtr serverctrls, ref IntPtr clientctrls);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_dn", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_get_dn([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_get_option_secInfo([In] ConnectionHandle ldapHandle, [In] LdapOption option, [In, Out] SecurityPackageContextConnectionInformation outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_get_option_sechandle([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref SecurityHandle outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        private static extern int ldap_get_option_int([In] IntPtr ldapHandle, [In] LdapOption option, ref int outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_get_option_int([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref int outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_get_option_ptr([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref IntPtr outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_values_len", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_get_values_len([In] ConnectionHandle ldapHandle, [In] IntPtr result, string name);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_result", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int ldap_result([In] ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Mesage);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_result2error", CharSet = CharSet.Ansi)]
        public static extern int ldap_result2error([In] ConnectionHandle ldapHandle, [In] IntPtr result, int freeIt);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_search_ext", CharSet = CharSet.Ansi)]
        public static extern int ldap_search([In] ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_set_option_clientcert([In] ConnectionHandle ldapHandle, [In] LdapOption option, QUERYCLIENTCERT outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_set_option_servercert([In] ConnectionHandle ldapHandle, [In] LdapOption option, VERIFYSERVERCERT outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int ldap_set_option_int([In] ConnectionHandle ld, [In] LdapOption option, ref int inValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_set_option_ptr([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref IntPtr inValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_set_option_string([In] ConnectionHandle ldapHandle, [In] LdapOption option, string inValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_set_option_referral([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref LdapReferralCallback outValue);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_start_tls_s", CharSet = CharSet.Ansi)]
        public static extern int ldap_start_tls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_result", CharSet = CharSet.Ansi)]
        public static extern int ldap_parse_result([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_result", CharSet = CharSet.Ansi)]
        public static extern int ldap_parse_result_referral([In] ConnectionHandle ldapHandle, [In] IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_extended_result", CharSet = CharSet.Ansi)]
        public static extern int ldap_parse_extended_result([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_reference", CharSet = CharSet.Ansi)]
        public static extern int ldap_parse_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr referrals, IntPtr ServerControls, byte freeIt);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_sasl_bind_s", CharSet = CharSet.Ansi)]
        internal static extern int ldap_sasl_bind([In] ConnectionHandle ld, string dn, string mechanism, berval cred, IntPtr serverctrls, IntPtr clientctrls, IntPtr servercredp);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_sasl_interactive_bind_s", CharSet = CharSet.Ansi)]
        internal static extern int ldap_sasl_interactive_bind([In] ConnectionHandle ld, string dn, string mechanism, IntPtr serverctrls, IntPtr clientctrls, uint flags, [MarshalAs(UnmanagedType.FunctionPtr)] LDAP_SASL_INTERACT_PROC proc, IntPtr defaults);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_err2string", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_err2string(int err);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_extended_operation", CharSet = CharSet.Ansi)]
        public static extern int ldap_extended_operation([In] ConnectionHandle ldapHandle, string oid, berval data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_first_attribute", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_first_attribute([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr address);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_first_entry", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_first_entry([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_first_reference", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_first_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_create_sort_control", CharSet = CharSet.Ansi)]
        public static extern int ldap_create_sort_control(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_control_free", CharSet = CharSet.Ansi)]
        public static extern int ldap_control_free(IntPtr control);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_controls_free", CharSet = CharSet.Ansi)]
        public static extern int ldap_controls_free([In] IntPtr value);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_value_free", CharSet = CharSet.Ansi)]
        public static extern int ldap_value_free([In] IntPtr value);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_value_free_len", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_value_free_len([In] IntPtr berelement);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_memfree", CharSet = CharSet.Ansi)]
        public static extern void ldap_memfree([In] IntPtr value);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_msgfree", CharSet = CharSet.Ansi)]
        public static extern void ldap_msgfree([In] IntPtr value);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_modify_ext", CharSet = CharSet.Ansi)]
        public static extern int ldap_modify([In] ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_next_attribute", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_next_attribute([In] ConnectionHandle ldapHandle, [In] IntPtr result, [In, Out] IntPtr address);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_next_entry", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_next_entry([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_next_reference", CharSet = CharSet.Ansi)]
        public static extern IntPtr ldap_next_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_abandon", CharSet = CharSet.Ansi)]
        public static extern int ldap_abandon([In] ConnectionHandle ldapHandle, [In] int messagId);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_add_ext", CharSet = CharSet.Ansi)]
        public static extern int ldap_add([In] ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_delete_ext", CharSet = CharSet.Ansi)]
        public static extern int ldap_delete_ext([In] ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_rename", CharSet = CharSet.Ansi)]
        public static extern int ldap_rename([In] ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_compare_ext", CharSet = CharSet.Ansi)]
        public static extern int ldap_compare([In] ConnectionHandle ldapHandle, string dn, string attributeName, berval binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);
    }
}

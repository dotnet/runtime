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

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_initialize", CharSet = CharSet.Ansi, SetLastError = true)]
        public static partial int ldap_initialize(out IntPtr ld, string uri);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_unbind_ext_s", CharSet = CharSet.Ansi)]
        public static partial int ldap_unbind_ext_s(IntPtr ld, ref IntPtr serverctrls, ref IntPtr clientctrls);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_dn", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_get_dn(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_get_option_bool(ConnectionHandle ldapHandle, LdapOption option, ref bool outValue);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] We need to manually convert SecurityPackageContextConnectionInformation to marshal differently as layout classes are not supported in generated interop.
        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_get_option_secInfo(ConnectionHandle ldapHandle, LdapOption option, [In, Out] SecurityPackageContextConnectionInformation outValue);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_get_option_sechandle(ConnectionHandle ldapHandle, LdapOption option, ref SecurityHandle outValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        private static partial int ldap_get_option_int(IntPtr ldapHandle, LdapOption option, ref int outValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_get_option_int(ConnectionHandle ldapHandle, LdapOption option, ref int outValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_get_option_ptr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr outValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_get_values_len", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_get_values_len(ConnectionHandle ldapHandle, IntPtr result, string name);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_result", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int ldap_result(ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Mesage);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_result2error", CharSet = CharSet.Ansi)]
        public static partial int ldap_result2error(ConnectionHandle ldapHandle, IntPtr result, int freeIt);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_search_ext", CharSet = CharSet.Ansi)]
        public static partial int ldap_search(ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi, SetLastError = true)]
        public static partial int ldap_set_option_bool(ConnectionHandle ld, LdapOption option, bool value);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_set_option_clientcert(ConnectionHandle ldapHandle, LdapOption option, QUERYCLIENTCERT outValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_set_option_servercert(ConnectionHandle ldapHandle, LdapOption option, VERIFYSERVERCERT outValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi, SetLastError = true)]
        public static partial int ldap_set_option_int(ConnectionHandle ld, LdapOption option, ref int inValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_set_option_ptr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr inValue);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static partial int ldap_set_option_string(ConnectionHandle ldapHandle, LdapOption option, string inValue);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", CharSet = CharSet.Ansi)]
        public static extern int ldap_set_option_referral(ConnectionHandle ldapHandle, LdapOption option, ref LdapReferralCallback outValue);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        // Note that ldap_start_tls_s has a different signature across Windows LDAP and OpenLDAP
        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_start_tls_s", CharSet = CharSet.Ansi)]
        public static partial int ldap_start_tls(ConnectionHandle ldapHandle, IntPtr serverControls, IntPtr clientControls);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_result", CharSet = CharSet.Ansi)]
        public static partial int ldap_parse_result(ConnectionHandle ldapHandle, IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_result", CharSet = CharSet.Ansi)]
        public static partial int ldap_parse_result_referral(ConnectionHandle ldapHandle, IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_extended_result", CharSet = CharSet.Ansi)]
        public static partial int ldap_parse_extended_result(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_reference", CharSet = CharSet.Ansi)]
        public static partial int ldap_parse_reference(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr referrals, IntPtr ServerControls, byte freeIt);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_sasl_bind_s", CharSet = CharSet.Ansi)]
        internal static extern int ldap_sasl_bind(ConnectionHandle ld, string dn, string mechanism, BerVal cred, IntPtr serverctrls, IntPtr clientctrls, IntPtr servercredp);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_sasl_interactive_bind_s", CharSet = CharSet.Ansi)]
        internal static partial int ldap_sasl_interactive_bind(ConnectionHandle ld, string dn, string mechanism, IntPtr serverctrls, IntPtr clientctrls, uint flags, LDAP_SASL_INTERACT_PROC proc, IntPtr defaults);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_err2string", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_err2string(int err);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_extended_operation", CharSet = CharSet.Ansi)]
        public static extern int ldap_extended_operation(ConnectionHandle ldapHandle, string oid, BerVal data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_first_attribute", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_first_attribute(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr address);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_first_entry", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_first_entry(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_first_reference", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_first_reference(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_create_sort_control", CharSet = CharSet.Ansi)]
        public static partial int ldap_create_sort_control(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_control_free", CharSet = CharSet.Ansi)]
        public static partial int ldap_control_free(IntPtr control);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_controls_free", CharSet = CharSet.Ansi)]
        public static partial int ldap_controls_free(IntPtr value);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_value_free", CharSet = CharSet.Ansi)]
        public static partial int ldap_value_free(IntPtr value);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_value_free_len", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_value_free_len(IntPtr berelement);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_memfree", CharSet = CharSet.Ansi)]
        public static partial void ldap_memfree(IntPtr value);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_msgfree", CharSet = CharSet.Ansi)]
        public static partial void ldap_msgfree(IntPtr value);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_modify_ext", CharSet = CharSet.Ansi)]
        public static partial int ldap_modify(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_next_attribute", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_next_attribute(ConnectionHandle ldapHandle, IntPtr result, IntPtr address);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_next_entry", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_next_entry(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_next_reference", CharSet = CharSet.Ansi)]
        public static partial IntPtr ldap_next_reference(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_abandon", CharSet = CharSet.Ansi)]
        public static partial int ldap_abandon(ConnectionHandle ldapHandle, int messagId);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_add_ext", CharSet = CharSet.Ansi)]
        public static partial int ldap_add(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_delete_ext", CharSet = CharSet.Ansi)]
        public static partial int ldap_delete_ext(ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.OpenLdap, EntryPoint = "ldap_rename", CharSet = CharSet.Ansi)]
        public static partial int ldap_rename(ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.OpenLdap, EntryPoint = "ldap_compare_ext", CharSet = CharSet.Ansi)]
        public static extern int ldap_compare(ConnectionHandle ldapHandle, string dn, string attributeName, BerVal binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    }
}

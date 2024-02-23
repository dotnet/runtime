// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices.Protocols;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

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
            Assembly currentAssembly = typeof(Ldap).Assembly;

            // Register callback that tries to load other libraries when the default library "libldap-2.5.so.0" not found
            AssemblyLoadContext.GetLoadContext(currentAssembly).ResolvingUnmanagedDll += (assembly, ldapName) =>
            {
                if (assembly != currentAssembly || ldapName != Libraries.OpenLdap)
                {
                    return IntPtr.Zero;
                }

                // Try load next (libldap-2.6.so.0) or previous (libldap-2.4.so.2) versions
                if (NativeLibrary.TryLoad("libldap-2.6.so.0", out IntPtr handle) ||
                    NativeLibrary.TryLoad("libldap-2.4.so.2", out handle))
                {
                    return handle;
                }

                return IntPtr.Zero;
            };

            // OpenLdap must be initialized on a single thread, once this is done it allows concurrent calls
            // By doing so in the static constructor we guarantee this is run before any other methods are called.

            // we call ldap_get_option_int to get an option and trigger the initialization as reccomended by
            // https://www.openldap.org/software//man.cgi?query=ldap_init
            int unused = 0;
            ldap_get_option_int(IntPtr.Zero, LdapOption.LDAP_OPT_DEBUG_LEVEL, ref unused);
        }

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_initialize", SetLastError = true)]
        public static partial int ldap_initialize(out IntPtr ld, [MarshalAs(UnmanagedType.LPUTF8Str)] string uri);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_unbind_ext_s")]
        public static partial int ldap_unbind_ext_s(IntPtr ld, ref IntPtr serverctrls, ref IntPtr clientctrls);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_dn")]
        public static partial IntPtr ldap_get_dn(ConnectionHandle ldapHandle, IntPtr result);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option")]
        public static partial int ldap_get_option_bool(ConnectionHandle ldapHandle, LdapOption option, [MarshalAs(UnmanagedType.Bool)] ref bool outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option")]
        public static unsafe partial int ldap_get_option_secInfo(ConnectionHandle ldapHandle, LdapOption option, void* outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option")]
        public static partial int ldap_get_option_sechandle(ConnectionHandle ldapHandle, LdapOption option, ref SecurityHandle outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option")]
        private static partial int ldap_get_option_int(IntPtr ldapHandle, LdapOption option, ref int outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option")]
        public static partial int ldap_get_option_int(ConnectionHandle ldapHandle, LdapOption option, ref int outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_option")]
        public static partial int ldap_get_option_ptr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_get_values_len")]
        public static partial IntPtr ldap_get_values_len(ConnectionHandle ldapHandle, IntPtr result, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_result", SetLastError = true)]
        public static partial int ldap_result(ConnectionHandle ldapHandle, int messageId, int all, in LDAP_TIMEVAL timeout, ref IntPtr Mesage);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_result2error")]
        public static partial int ldap_result2error(ConnectionHandle ldapHandle, IntPtr result, int freeIt);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_search_ext")]
        public static partial int ldap_search(
            ConnectionHandle ldapHandle,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dn,
            int scope,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string filter,
            IntPtr attributes,
            [MarshalAs(UnmanagedType.Bool)] bool attributeOnly,
            IntPtr servercontrol,
            IntPtr clientcontrol,
            in LDAP_TIMEVAL timelimit,
            int sizelimit,
            ref int messageNumber);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", SetLastError = true)]
        public static partial int ldap_set_option_bool(ConnectionHandle ld, LdapOption option, [MarshalAs(UnmanagedType.Bool)] bool value);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option")]
        public static partial int ldap_set_option_clientcert(ConnectionHandle ldapHandle, LdapOption option, QUERYCLIENTCERT outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option")]
        public static partial int ldap_set_option_servercert(ConnectionHandle ldapHandle, LdapOption option, VERIFYSERVERCERT outValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option", SetLastError = true)]
        public static partial int ldap_set_option_int(ConnectionHandle ld, LdapOption option, ref int inValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option")]
        public static partial int ldap_set_option_ptr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr inValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option")]
        public static partial int ldap_set_option_string(ConnectionHandle ldapHandle, LdapOption option, [MarshalAs(UnmanagedType.LPUTF8Str)] string inValue);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_set_option")]
        public static partial int ldap_set_option_referral(ConnectionHandle ldapHandle, LdapOption option, ref LdapReferralCallback outValue);

        // Note that ldap_start_tls_s has a different signature across Windows LDAP and OpenLDAP
        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_start_tls_s")]
        public static partial int ldap_start_tls(ConnectionHandle ldapHandle, IntPtr serverControls, IntPtr clientControls);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_result")]
        public static partial int ldap_parse_result(ConnectionHandle ldapHandle, IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_result")]
        public static partial int ldap_parse_result_referral(ConnectionHandle ldapHandle, IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_extended_result")]
        public static partial int ldap_parse_extended_result(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_parse_reference")]
        public static partial int ldap_parse_reference(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr referrals, IntPtr ServerControls, byte freeIt);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_sasl_bind_s")]
        internal static partial int ldap_sasl_bind(
            ConnectionHandle ld,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dn,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string mechanism,
            BerVal cred,
            IntPtr serverctrls,
            IntPtr clientctrls,
            IntPtr servercredp);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_sasl_interactive_bind_s")]
        internal static partial int ldap_sasl_interactive_bind(
            ConnectionHandle ld,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dn,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string mechanism,
            IntPtr serverctrls,
            IntPtr clientctrls,
            uint flags,
            LDAP_SASL_INTERACT_PROC proc,
            IntPtr defaults);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_err2string")]
        public static partial IntPtr ldap_err2string(int err);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_extended_operation")]
        public static partial int ldap_extended_operation(ConnectionHandle ldapHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string oid, BerVal data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_first_attribute")]
        public static partial IntPtr ldap_first_attribute(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr address);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_first_entry")]
        public static partial IntPtr ldap_first_entry(ConnectionHandle ldapHandle, IntPtr result);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_first_reference")]
        public static partial IntPtr ldap_first_reference(ConnectionHandle ldapHandle, IntPtr result);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_create_sort_control")]
        public static partial int ldap_create_sort_control(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_control_free")]
        public static partial int ldap_control_free(IntPtr control);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_controls_free")]
        public static partial int ldap_controls_free(IntPtr value);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_value_free")]
        public static partial int ldap_value_free(IntPtr value);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_value_free_len")]
        public static partial IntPtr ldap_value_free_len(IntPtr berelement);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_memfree")]
        public static partial void ldap_memfree(IntPtr value);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_msgfree")]
        public static partial void ldap_msgfree(IntPtr value);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_modify_ext")]
        public static partial int ldap_modify(ConnectionHandle ldapHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_next_attribute")]
        public static partial IntPtr ldap_next_attribute(ConnectionHandle ldapHandle, IntPtr result, IntPtr address);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_next_entry")]
        public static partial IntPtr ldap_next_entry(ConnectionHandle ldapHandle, IntPtr result);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_next_reference")]
        public static partial IntPtr ldap_next_reference(ConnectionHandle ldapHandle, IntPtr result);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_abandon")]
        public static partial int ldap_abandon(ConnectionHandle ldapHandle, int messagId);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_add_ext")]
        public static partial int ldap_add(ConnectionHandle ldapHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_delete_ext")]
        public static partial int ldap_delete_ext(ConnectionHandle ldapHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_rename")]
        public static partial int ldap_rename(
            ConnectionHandle ldapHandle,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dn,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string newRdn,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string newParentDn,
            int deleteOldRdn,
            IntPtr servercontrol,
            IntPtr clientcontrol,
            ref int messageNumber);

        [LibraryImport(Libraries.OpenLdap, EntryPoint = "ldap_compare_ext")]
        public static partial int ldap_compare(
            ConnectionHandle ldapHandle,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string dn,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string attributeName,
            BerVal binaryValue,
            IntPtr servercontrol,
            IntPtr clientcontrol,
            ref int messageNumber);
    }
}

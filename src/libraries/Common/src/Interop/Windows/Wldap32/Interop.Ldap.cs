// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices.Protocols;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ldap
    {
        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_bind_sW", CharSet = CharSet.Unicode)]
        public static extern int ldap_bind_s([In] ConnectionHandle ldapHandle, string dn, SEC_WINNT_AUTH_IDENTITY_EX credentials, BindMethod method);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_initW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_init(string hostName, int portNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "ldap_connect", CharSet = CharSet.Unicode)]
        public static extern int ldap_connect([In] ConnectionHandle ldapHandle, LDAP_TIMEVAL timeout);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "ldap_unbind", CharSet = CharSet.Unicode)]
        public static extern int ldap_unbind([In] IntPtr ldapHandle);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_int([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref int outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_int([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref int inValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_ptr([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref IntPtr outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_ptr([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref IntPtr inValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_sechandle([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref SecurityHandle outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_get_option_secInfo([In] ConnectionHandle ldapHandle, [In] LdapOption option, [In, Out] SecurityPackageContextConnectionInformation outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_referral([In] ConnectionHandle ldapHandle, [In] LdapOption option, ref LdapReferralCallback outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_clientcert([In] ConnectionHandle ldapHandle, [In] LdapOption option, QUERYCLIENTCERT outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_set_optionW", CharSet = CharSet.Unicode)]
        public static extern int ldap_set_option_servercert([In] ConnectionHandle ldapHandle, [In] LdapOption option, VERIFYSERVERCERT outValue);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LdapGetLastError")]
        public static extern int LdapGetLastError();

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "cldap_openW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr cldap_open(string hostName, int portNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_simple_bind_sW", CharSet = CharSet.Unicode)]
        public static extern int ldap_simple_bind_s([In] ConnectionHandle ldapHandle, string distinguishedName, string password);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_delete_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_delete_ext([In] ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_result", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int ldap_result([In] ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Mesage);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_resultW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_result([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_resultW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_result_referral([In] ConnectionHandle ldapHandle, [In] IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_memfreeW", CharSet = CharSet.Unicode)]
        public static extern void ldap_memfree([In] IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_value_freeW", CharSet = CharSet.Unicode)]
        public static extern int ldap_value_free([In] IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_controls_freeW", CharSet = CharSet.Unicode)]
        public static extern int ldap_controls_free([In] IntPtr value);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_abandon", CharSet = CharSet.Unicode)]
        public static extern int ldap_abandon([In] ConnectionHandle ldapHandle, [In] int messagId);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_start_tls_sW", CharSet = CharSet.Unicode)]
        public static extern int ldap_start_tls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_stop_tls_s", CharSet = CharSet.Unicode)]
        public static extern byte ldap_stop_tls(ConnectionHandle ldapHandle);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_rename_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_rename([In] ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_compare_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_compare([In] ConnectionHandle ldapHandle, string dn, string attributeName, string strValue, berval binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_add_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_add([In] ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_modify_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_modify([In] ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_extended_operationW", CharSet = CharSet.Unicode)]
        public static extern int ldap_extended_operation([In] ConnectionHandle ldapHandle, string oid, berval data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_extended_resultW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_extended_result([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_msgfree", CharSet = CharSet.Unicode)]
        public static extern int ldap_msgfree([In] IntPtr result);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_search_extW", CharSet = CharSet.Unicode)]
        public static extern int ldap_search([In] ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_first_entry", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_first_entry([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_next_entry", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_next_entry([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_first_reference", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_first_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_next_reference", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_next_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_dnW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_get_dn([In] ConnectionHandle ldapHandle, [In] IntPtr result);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_first_attributeW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_first_attribute([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr address);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_next_attributeW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_next_attribute([In] ConnectionHandle ldapHandle, [In] IntPtr result, [In, Out] IntPtr address);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_get_values_lenW", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_get_values_len([In] ConnectionHandle ldapHandle, [In] IntPtr result, string name);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_value_free_len", CharSet = CharSet.Unicode)]
        public static extern IntPtr ldap_value_free_len([In] IntPtr berelement);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_parse_referenceW", CharSet = CharSet.Unicode)]
        public static extern int ldap_parse_reference([In] ConnectionHandle ldapHandle, [In] IntPtr result, ref IntPtr referrals);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_create_sort_controlW", CharSet = CharSet.Unicode)]
        public static extern int ldap_create_sort_control(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_control_freeW", CharSet = CharSet.Unicode)]
        public static extern int ldap_control_free(IntPtr control);

        [DllImport("Crypt32.dll", EntryPoint = "CertFreeCRLContext", CharSet = CharSet.Unicode)]
        public static extern int CertFreeCRLContext(IntPtr certContext);

        [DllImport(Libraries.Wldap32, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ldap_result2error", CharSet = CharSet.Unicode)]
        public static extern int ldap_result2error([In] ConnectionHandle ldapHandle, [In] IntPtr result, int freeIt);
    }
}

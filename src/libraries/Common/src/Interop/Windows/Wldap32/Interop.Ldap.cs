// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.DirectoryServices.Protocols;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ldap
    {
        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_bind_sW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_bind_s(ConnectionHandle ldapHandle, string dn, in SEC_WINNT_AUTH_IDENTITY_EX credentials, BindMethod method);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_initW",  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_init(string hostName, int portNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_connect")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_connect(ConnectionHandle ldapHandle, in LDAP_TIMEVAL timeout);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_unbind")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_unbind(IntPtr ldapHandle);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_get_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_get_option_int(ConnectionHandle ldapHandle, LdapOption option, ref int outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_set_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_set_option_int(ConnectionHandle ldapHandle, LdapOption option, ref int inValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_get_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_get_option_ptr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_set_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_set_option_ptr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr inValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_get_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_get_option_sechandle(ConnectionHandle ldapHandle, LdapOption option, ref SecurityHandle outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_get_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe partial int ldap_get_option_secInfo(ConnectionHandle ldapHandle, LdapOption option, void* outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_set_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_set_option_referral(ConnectionHandle ldapHandle, LdapOption option, ref LdapReferralCallback outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_set_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_set_option_clientcert(ConnectionHandle ldapHandle, LdapOption option, QUERYCLIENTCERT outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_set_optionW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_set_option_servercert(ConnectionHandle ldapHandle, LdapOption option, VERIFYSERVERCERT outValue);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "LdapGetLastError")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int LdapGetLastError();

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "cldap_openW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr cldap_open(string hostName, int portNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_simple_bind_sW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_simple_bind_s(ConnectionHandle ldapHandle, string distinguishedName, string password);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_delete_extW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_delete_ext(ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_result", SetLastError = true)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_result(ConnectionHandle ldapHandle, int messageId, int all, in LDAP_TIMEVAL timeout, ref IntPtr Mesage);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_parse_resultW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_parse_result(ConnectionHandle ldapHandle, IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_parse_resultW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_parse_result_referral(ConnectionHandle ldapHandle, IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_memfreeW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ldap_memfree(IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_value_freeW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_value_free(IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_controls_freeW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_controls_free(IntPtr value);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_abandon")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_abandon(ConnectionHandle ldapHandle, int messagId);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_start_tls_sW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_start_tls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_stop_tls_s")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial byte ldap_stop_tls(ConnectionHandle ldapHandle);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_rename_extW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_rename(ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_compare_extW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_compare(ConnectionHandle ldapHandle, string dn, string attributeName, string strValue, BerVal binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_add_extW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_add(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_modify_extW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_modify(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_extended_operationW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_extended_operation(ConnectionHandle ldapHandle, string oid, BerVal data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_parse_extended_resultW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_parse_extended_result(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_msgfree")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_msgfree(IntPtr result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_search_extW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_search(ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, [MarshalAs(UnmanagedType.Bool)] bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_first_entry")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_first_entry(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_next_entry")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_next_entry(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_first_reference")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_first_reference(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_next_reference")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_next_reference(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_get_dnW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_get_dn(ConnectionHandle ldapHandle, IntPtr result);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_first_attributeW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_first_attribute(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr address);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_next_attributeW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_next_attribute(ConnectionHandle ldapHandle, IntPtr result, IntPtr address);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_get_values_lenW", StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_get_values_len(ConnectionHandle ldapHandle, IntPtr result, string name);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_value_free_len")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ldap_value_free_len(IntPtr berelement);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_parse_referenceW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_parse_reference(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr referrals);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_create_sort_controlW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_create_sort_control(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_control_freeW")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_control_free(IntPtr control);

        [GeneratedDllImport("Crypt32.dll", EntryPoint = "CertFreeCRLContext")]
        public static partial int CertFreeCRLContext(IntPtr certContext);

        [GeneratedDllImport(Libraries.Wldap32, EntryPoint = "ldap_result2error")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial int ldap_result2error(ConnectionHandle ldapHandle, IntPtr result, int freeIt);
    }
}

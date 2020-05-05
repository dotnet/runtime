// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    internal static class LdapPal
    {
        internal static void LdapAbandon(ConnectionHandle ldapHandle, int messagId) => Interop.ldap_abandon(ldapHandle, messagId);

        internal static int LdapAdd(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.ldap_add(ldapHandle, dn, attrs, servercontrol, clientcontrol, ref messageNumber);

        internal static int LdapCompare(ConnectionHandle ldapHandle, string dn, string attributeName, string strValue, berval binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.ldap_compare(ldapHandle, dn, attributeName, binaryValue, servercontrol, clientcontrol, ref messageNumber);

        internal static void LdapControlFree(IntPtr control) => Interop.ldap_control_free(control);

        internal static void LdapControlsFree(IntPtr value) => Interop.ldap_controls_free(value);

        internal static int LdapCreateSortControl(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control) => Interop.ldap_create_sort_control(handle, keys, critical, ref control);

        internal static int LdapDeleteExt(ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) => Interop.ldap_delete_ext(ldapHandle, dn, servercontrol, clientcontrol, ref messageNumber);

        internal static int LdapExtendedOperation(ConnectionHandle ldapHandle, string oid, berval data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.ldap_extended_operation(ldapHandle, oid, data, servercontrol, clientcontrol, ref messageNumber);

        internal static IntPtr LdapFirstAttribute(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr address) => Interop.ldap_first_attribute(ldapHandle, result, ref address);

        internal static IntPtr LdapFirstEntry(ConnectionHandle ldapHandle, IntPtr result) => Interop.ldap_first_entry(ldapHandle, result);

        internal static IntPtr LdapFirstReference(ConnectionHandle ldapHandle, IntPtr result) => Interop.ldap_first_reference(ldapHandle, result);

        internal static IntPtr LdapGetDn(ConnectionHandle ldapHandle, IntPtr result) => Interop.ldap_get_dn(ldapHandle, result);

        internal static int LdapGetLastError(ConnectionHandle ldapHandle)
        {
            int result = 0;
            Interop.ldap_get_option_int(ldapHandle, LdapOption.LDAP_OPT_ERROR_NUMBER, ref result);
            return result;
        }

        internal static int LdapGetOptionInt(ConnectionHandle ldapHandle, LdapOption option, ref int outValue) => Interop.ldap_get_option_int(ldapHandle, option, ref outValue);

        internal static int LdapGetOptionPtr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr outValue) => Interop.ldap_get_option_ptr(ldapHandle, option, ref outValue);

        internal static int LdapGetOptionSechandle(ConnectionHandle ldapHandle, LdapOption option, ref SecurityHandle outValue) => Interop.ldap_get_option_sechandle(ldapHandle, option, ref outValue);

        // This option is not supported on Linux, so it would most likely throw.
        internal static int LdapGetOptionSecInfo(ConnectionHandle ldapHandle, LdapOption option, SecurityPackageContextConnectionInformation outValue) => Interop.ldap_get_option_secInfo(ldapHandle, option, outValue);

        internal static IntPtr LdapGetValuesLen(ConnectionHandle ldapHandle, IntPtr result, string name) => Interop.ldap_get_values_len(ldapHandle, result, name);

        internal static void LdapMemfree(IntPtr outValue) => Interop.ldap_memfree(outValue);

        internal static int LdapModify(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.ldap_modify(ldapHandle, dn, attrs, servercontrol, clientcontrol, ref messageNumber);

        internal static IntPtr LdapNextAttribute(ConnectionHandle ldapHandle, IntPtr result, IntPtr address) => Interop.ldap_next_attribute(ldapHandle, result, address);

        internal static IntPtr LdapNextEntry(ConnectionHandle ldapHandle, IntPtr result) => Interop.ldap_next_entry(ldapHandle, result);

        internal static IntPtr LdapNextReference(ConnectionHandle ldapHandle, IntPtr result) => Interop.ldap_next_reference(ldapHandle, result);

        internal static int LdapParseExtendedResult(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt) => Interop.ldap_parse_extended_result(ldapHandle, result, ref oid, ref data, freeIt);

        internal static int LdapParseReference(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr referrals) => Interop.ldap_parse_reference(ldapHandle, result, ref referrals, IntPtr.Zero, 0);

        internal static int LdapParseResult(ConnectionHandle ldapHandle, IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt) =>
                                Interop.ldap_parse_result(ldapHandle, result, ref serverError, ref dn, ref message, ref referral, ref control, freeIt);

        internal static int LdapParseResultReferral(ConnectionHandle ldapHandle, IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt)
            => Interop.ldap_parse_result_referral(ldapHandle, result, serverError, dn, message, ref referral, control, freeIt);

        internal static int LdapRename(ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.ldap_rename(ldapHandle, dn, newRdn, newParentDn, deleteOldRdn, servercontrol, clientcontrol, ref messageNumber);

        internal static int LdapResult(ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Message) => Interop.ldap_result(ldapHandle, messageId, all, timeout, ref Message);

        internal static int LdapResult2error(ConnectionHandle ldapHandle, IntPtr result, int freeIt) => Interop.ldap_result2error(ldapHandle, result, freeIt);

        internal static int LdapSearch(ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber) =>
                                Interop.ldap_search(ldapHandle, dn, scope, filter, attributes, attributeOnly, servercontrol, clientcontrol, timelimit, sizelimit, ref messageNumber);

        // This option is not supported in Linux, so it would most likely throw.
        internal static int LdapSetOptionClientCert(ConnectionHandle ldapHandle, LdapOption option, QUERYCLIENTCERT outValue) => Interop.ldap_set_option_clientcert(ldapHandle, option, outValue);

        internal static int LdapSetOptionInt(ConnectionHandle ld, LdapOption option, ref int inValue) => Interop.ldap_set_option_int(ld, option, ref inValue);

        internal static int LdapSetOptionPtr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr inValue) => Interop.ldap_set_option_ptr(ldapHandle, option, ref inValue);

        internal static int LdapSetOptionReferral(ConnectionHandle ldapHandle, LdapOption option, ref LdapReferralCallback outValue) => Interop.ldap_set_option_referral(ldapHandle, option, ref outValue);

        // This option is not supported in Linux, so it would most likely throw.
        internal static int LdapSetOptionServercert(ConnectionHandle ldapHandle, LdapOption option, VERIFYSERVERCERT outValue) => Interop.ldap_set_option_servercert(ldapHandle, option, outValue);

        internal static int LdapSimpleBind(ConnectionHandle ld, string who, string passwd) => Interop.ldap_simple_bind(ld, who, passwd);

        internal static int LdapStartTls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls) => Interop.ldap_start_tls(ldapHandle, ref ServerReturnValue, ref Message, ServerControls, ClientControls);

        // openldap doesn't have a ldap_stop_tls function. Returning true as no-op for Linux.
        internal static byte LdapStopTls(ConnectionHandle ldapHandle) => 1;

        internal static void LdapValueFree(IntPtr referral) => Interop.ldap_value_free(referral);

        internal static void LdapValueFreeLen(IntPtr berelement) => Interop.ldap_value_free_len(berelement);

        internal static string PtrToString(IntPtr requestName) => Marshal.PtrToStringAnsi(requestName);

        internal static IntPtr StringToPtr(string s) => Marshal.StringToHGlobalAnsi(s);
    }
}

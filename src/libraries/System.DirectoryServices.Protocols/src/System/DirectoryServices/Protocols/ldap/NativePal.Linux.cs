// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    internal static class NativePal
    {
        public static void BerBvecfree(IntPtr ptrResult) => OpenLDAP.ber_bvecfree(ptrResult);

        public static void BerBvfree(IntPtr flattenptr) => OpenLDAP.ber_bvfree(flattenptr);

        public static void BerFree(IntPtr berelement, int option) => OpenLDAP.ber_free(berelement, option);

        public static int BerFlatten(BerSafeHandle berElement, ref IntPtr flattenptr) => OpenLDAP.ber_flatten(berElement, ref flattenptr);

        public static int BerPrintfBerarray(BerSafeHandle berElement, string format, IntPtr value) => OpenLDAP.ber_printf_berarray(berElement, format, value);

        public static int BerPrintfBytearray(BerSafeHandle berElement, string format, HGlobalMemHandle value, int length) => OpenLDAP.ber_printf_bytearray(berElement, format, value, length);

        public static int BerPrintfEmptyarg(BerSafeHandle berElement, string format) => OpenLDAP.ber_printf_emptyarg(berElement, format);

        public static int BerPrintfInt(BerSafeHandle berElement, string format, int value) => OpenLDAP.ber_printf_int(berElement, format, value);

        public static int BerScanf(BerSafeHandle berElement, string format) => OpenLDAP.ber_scanf(berElement, format);

        public static int BerScanfBitstring(BerSafeHandle berElement, string format, ref IntPtr ptrResult, ref int length) => OpenLDAP.ber_scanf_bitstring(berElement, format, ref ptrResult, ref length);

        public static int BerScanfInt(BerSafeHandle berElement, string format, ref int result) => OpenLDAP.ber_scanf_int(berElement, format, ref result);

        public static int BerScanfPtr(BerSafeHandle berElement, string format, ref IntPtr value) => OpenLDAP.ber_scanf_ptr(berElement, format, ref value);

        public static void LdapAbandon(ConnectionHandle ldapHandle, int messagId) => OpenLDAP.ldap_abandon(ldapHandle, messagId);

        public static int LdapAdd(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                OpenLDAP.ldap_add(ldapHandle, dn, attrs, servercontrol, clientcontrol, ref messageNumber);

        public static int LdapCompare(ConnectionHandle ldapHandle, string dn, string attributeName, string strValue, berval binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                OpenLDAP.ldap_compare(ldapHandle, dn, attributeName, binaryValue, servercontrol, clientcontrol, ref messageNumber);

        public static void LdapControlFree(IntPtr control) => OpenLDAP.ldap_control_free(control);

        public static void LdapControlsFree(IntPtr value) => OpenLDAP.ldap_controls_free(value);

        public static int LdapCreateSortControl(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control) => OpenLDAP.ldap_create_sort_control(handle, keys, critical, ref control);

        public static int LdapDeleteExt(ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) => OpenLDAP.ldap_delete_ext(ldapHandle, dn, servercontrol, clientcontrol, ref messageNumber);

        public static int LdapExtendedOperation(ConnectionHandle ldapHandle, string oid, berval data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                OpenLDAP.ldap_extended_operation(ldapHandle, oid, data, servercontrol, clientcontrol, ref messageNumber);

        public static IntPtr LdapFirstAttribute(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr address) => OpenLDAP.ldap_first_attribute(ldapHandle, result, ref address);

        public static IntPtr LdapFirstEntry(ConnectionHandle ldapHandle, IntPtr result) => OpenLDAP.ldap_first_entry(ldapHandle, result);

        public static IntPtr LdapFirstReference(ConnectionHandle ldapHandle, IntPtr result) => OpenLDAP.ldap_first_reference(ldapHandle, result);

        public static IntPtr LdapGetDn(ConnectionHandle ldapHandle, IntPtr result) => OpenLDAP.ldap_get_dn(ldapHandle, result);

        public static int LdapGetLastError(ConnectionHandle ldapHandle)
        {
            int result = 0;
            OpenLDAP.ldap_get_option_int(ldapHandle, LdapOption.LDAP_OPT_ERROR_NUMBER, ref result);
            return result;
        }

        public static int LdapGetOptionInt(ConnectionHandle ldapHandle, LdapOption option, ref int outValue) => OpenLDAP.ldap_get_option_int(ldapHandle, option, ref outValue);

        public static int LdapGetOptionPtr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr outValue) => OpenLDAP.ldap_get_option_ptr(ldapHandle, option, ref outValue);

        public static int LdapGetOptionSechandle(ConnectionHandle ldapHandle, LdapOption option, ref SecurityHandle outValue) => OpenLDAP.ldap_get_option_sechandle(ldapHandle, option, ref outValue);

        // This option is not supported on Linux, so it would most likely throw.
        public static int LdapGetOptionSecInfo(ConnectionHandle ldapHandle, LdapOption option, SecurityPackageContextConnectionInformation outValue) => OpenLDAP.ldap_get_option_secInfo(ldapHandle, option, outValue);

        public static IntPtr LdapGetValuesLen(ConnectionHandle ldapHandle, IntPtr result, string name) => OpenLDAP.ldap_get_values_len(ldapHandle, result, name);

        public static void LdapMemfree(IntPtr outValue) => OpenLDAP.ldap_memfree(outValue);

        public static int LdapModify(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                OpenLDAP.ldap_modify(ldapHandle, dn, attrs, servercontrol, clientcontrol, ref messageNumber);

        public static IntPtr LdapNextAttribute(ConnectionHandle ldapHandle, IntPtr result, IntPtr address) => OpenLDAP.ldap_next_attribute(ldapHandle, result, address);

        public static IntPtr LdapNextEntry(ConnectionHandle ldapHandle, IntPtr result) => OpenLDAP.ldap_next_entry(ldapHandle, result);

        public static IntPtr LdapNextReference(ConnectionHandle ldapHandle, IntPtr result) => OpenLDAP.ldap_next_reference(ldapHandle, result);

        public static int LdapParseExtendedResult(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt) => OpenLDAP.ldap_parse_extended_result(ldapHandle, result, ref oid, ref data, freeIt);

        public static int LdapParseReference(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr referrals) => OpenLDAP.ldap_parse_reference(ldapHandle, result, ref referrals, IntPtr.Zero, 0);

        public static int LdapParseResult(ConnectionHandle ldapHandle, IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt) =>
                                OpenLDAP.ldap_parse_result(ldapHandle, result, ref serverError, ref dn, ref message, ref referral, ref control, freeIt);

        public static int LdapParseResultReferral(ConnectionHandle ldapHandle, IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt)
            => OpenLDAP.ldap_parse_result_referral(ldapHandle, result, serverError, dn, message, ref referral, control, freeIt);

        public static int LdapRename(ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                OpenLDAP.ldap_rename(ldapHandle, dn, newRdn, newParentDn, deleteOldRdn, servercontrol, clientcontrol, ref messageNumber);

        public static int LdapResult(ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Message) => OpenLDAP.ldap_result(ldapHandle, messageId, all, timeout, ref Message);

        public static int LdapResult2error(ConnectionHandle ldapHandle, IntPtr result, int freeIt) => OpenLDAP.ldap_result2error(ldapHandle, result, freeIt);

        public static int LdapSearch(ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber) =>
                                OpenLDAP.ldap_search(ldapHandle, dn, scope, filter, attributes, attributeOnly, servercontrol, clientcontrol, timelimit, sizelimit, ref messageNumber);

        // This option is not supported in Linux, so it would most likely throw.
        public static int LdapSetOptionClientcert(ConnectionHandle ldapHandle, LdapOption option, QUERYCLIENTCERT outValue) => OpenLDAP.ldap_set_option_clientcert(ldapHandle, option, outValue);

        public static int LdapSetOptionInt(ConnectionHandle ld, LdapOption option, ref int inValue) => OpenLDAP.ldap_set_option_int(ld, option, ref inValue);

        public static int LdapSetOptionPtr(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr inValue) => OpenLDAP.ldap_set_option_ptr(ldapHandle, option, ref inValue);

        public static int LdapSetOptionReferral(ConnectionHandle ldapHandle, LdapOption option, ref LdapReferralCallback outValue) => OpenLDAP.ldap_set_option_referral(ldapHandle, option, ref outValue);

        // This option is not supported in Linux, so it would most likely throw.
        public static int LdapSetOptionServercert(ConnectionHandle ldapHandle, LdapOption option, VERIFYSERVERCERT outValue) => OpenLDAP.ldap_set_option_servercert(ldapHandle, option, outValue);

        public static int LdapSimpleBind(ConnectionHandle ld, string who, string passwd) => OpenLDAP.ldap_simple_bind(ld, who, passwd);

        public static int LdapStartTls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls) => OpenLDAP.ldap_start_tls(ldapHandle, ref ServerReturnValue, ref Message, ServerControls, ClientControls);

        // openldap doesn't have a ldap_stop_tls function. Returning true as no-op for Linux.
        public static byte LdapStopTls(ConnectionHandle ldapHandle) => 1;

        public static void LdapValueFree(IntPtr referral) => OpenLDAP.ldap_value_free(referral);

        public static void LdapValueFreeLen(IntPtr berelement) => OpenLDAP.ldap_value_free_len(berelement);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    internal static class LdapPal
    {
        internal static void CancelDirectoryAsyncOperation(ConnectionHandle ldapHandle, int messagId) => Interop.Ldap.ldap_abandon(ldapHandle, messagId);

        internal static int AddDirectoryEntry(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.Ldap.ldap_add(ldapHandle, dn, attrs, servercontrol, clientcontrol, ref messageNumber);

        internal static int CompareDirectoryEntries(ConnectionHandle ldapHandle, string dn, string attributeName, string strValue, BerVal binaryValue, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.Ldap.ldap_compare(ldapHandle, dn, attributeName, strValue, binaryValue, servercontrol, clientcontrol, ref messageNumber);

        internal static void FreeDirectoryControl(IntPtr control) => Interop.Ldap.ldap_control_free(control);

        internal static void FreeDirectoryControls(IntPtr value) => Interop.Ldap.ldap_controls_free(value);

        internal static int CreateDirectorySortControl(ConnectionHandle handle, IntPtr keys, byte critical, ref IntPtr control) => Interop.Ldap.ldap_create_sort_control(handle, keys, critical, ref control);

        internal static int DeleteDirectoryEntry(ConnectionHandle ldapHandle, string dn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) => Interop.Ldap.ldap_delete_ext(ldapHandle, dn, servercontrol, clientcontrol, ref messageNumber);

        internal static int ExtendedDirectoryOperation(ConnectionHandle ldapHandle, string oid, BerVal data, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.Ldap.ldap_extended_operation(ldapHandle, oid, data, servercontrol, clientcontrol, ref messageNumber);

        internal static IntPtr GetFirstAttributeFromEntry(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr address) => Interop.Ldap.ldap_first_attribute(ldapHandle, result, ref address);

        internal static IntPtr GetFirstEntryFromResult(ConnectionHandle ldapHandle, IntPtr result) => Interop.Ldap.ldap_first_entry(ldapHandle, result);

        internal static IntPtr GetFirstReferenceFromResult(ConnectionHandle ldapHandle, IntPtr result) => Interop.Ldap.ldap_first_reference(ldapHandle, result);

        internal static IntPtr GetDistinguishedName(ConnectionHandle ldapHandle, IntPtr result) => Interop.Ldap.ldap_get_dn(ldapHandle, result);

        internal static int GetLastErrorFromConnection(ConnectionHandle _ /*ldapHandle*/) => Interop.Ldap.LdapGetLastError();

        internal static int GetIntOption(ConnectionHandle ldapHandle, LdapOption option, ref int outValue) => Interop.Ldap.ldap_get_option_int(ldapHandle, option, ref outValue);

        internal static int GetPtrOption(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr outValue) => Interop.Ldap.ldap_get_option_ptr(ldapHandle, option, ref outValue);

        internal static int GetSecurityHandleOption(ConnectionHandle ldapHandle, LdapOption option, ref SecurityHandle outValue) => Interop.Ldap.ldap_get_option_sechandle(ldapHandle, option, ref outValue);

        internal static unsafe int GetSecInfoOption(ConnectionHandle ldapHandle, LdapOption option, SecurityPackageContextConnectionInformation outValue)
        {
            fixed (void* outValuePtr = outValue)
            {
                return Interop.Ldap.ldap_get_option_secInfo(ldapHandle, option, outValuePtr);
            }
        }

        internal static IntPtr GetValuesFromAttribute(ConnectionHandle ldapHandle, IntPtr result, string name) => Interop.Ldap.ldap_get_values_len(ldapHandle, result, name);

        internal static void FreeMemory(IntPtr outValue) => Interop.Ldap.ldap_memfree(outValue);

        internal static void FreeMessage(IntPtr outValue) => Interop.Ldap.ldap_msgfree(outValue);

        internal static int ModifyDirectoryEntry(ConnectionHandle ldapHandle, string dn, IntPtr attrs, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.Ldap.ldap_modify(ldapHandle, dn, attrs, servercontrol, clientcontrol, ref messageNumber);

        internal static IntPtr GetNextAttributeFromResult(ConnectionHandle ldapHandle, IntPtr result, IntPtr address) => Interop.Ldap.ldap_next_attribute(ldapHandle, result, address);

        internal static IntPtr GetNextEntryFromResult(ConnectionHandle ldapHandle, IntPtr result) => Interop.Ldap.ldap_next_entry(ldapHandle, result);

        internal static IntPtr GetNextReferenceFromResult(ConnectionHandle ldapHandle, IntPtr result) => Interop.Ldap.ldap_next_reference(ldapHandle, result);

        internal static int ParseExtendedResult(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr oid, ref IntPtr data, byte freeIt) => Interop.Ldap.ldap_parse_extended_result(ldapHandle, result, ref oid, ref data, freeIt);

        internal static int ParseReference(ConnectionHandle ldapHandle, IntPtr result, ref IntPtr referrals) => Interop.Ldap.ldap_parse_reference(ldapHandle, result, ref referrals);

        internal static int ParseResult(ConnectionHandle ldapHandle, IntPtr result, ref int serverError, ref IntPtr dn, ref IntPtr message, ref IntPtr referral, ref IntPtr control, byte freeIt) =>
                               Interop.Ldap.ldap_parse_result(ldapHandle, result, ref serverError, ref dn, ref message, ref referral, ref control, freeIt);

        internal static int ParseResultReferral(ConnectionHandle ldapHandle, IntPtr result, IntPtr serverError, IntPtr dn, IntPtr message, ref IntPtr referral, IntPtr control, byte freeIt)
            => Interop.Ldap.ldap_parse_result_referral(ldapHandle, result, serverError, dn, message, ref referral, control, freeIt);

        internal static int RenameDirectoryEntry(ConnectionHandle ldapHandle, string dn, string newRdn, string newParentDn, int deleteOldRdn, IntPtr servercontrol, IntPtr clientcontrol, ref int messageNumber) =>
                                Interop.Ldap.ldap_rename(ldapHandle, dn, newRdn, newParentDn, deleteOldRdn, servercontrol, clientcontrol, ref messageNumber);

        internal static int GetResultFromAsyncOperation(ConnectionHandle ldapHandle, int messageId, int all, LDAP_TIMEVAL timeout, ref IntPtr Message) => Interop.Ldap.ldap_result(ldapHandle, messageId, all, timeout, ref Message);

        internal static int ResultToErrorCode(ConnectionHandle ldapHandle, IntPtr result, int freeIt) => Interop.Ldap.ldap_result2error(ldapHandle, result, freeIt);

        internal static int SearchDirectory(ConnectionHandle ldapHandle, string dn, int scope, string filter, IntPtr attributes, bool attributeOnly, IntPtr servercontrol, IntPtr clientcontrol, int timelimit, int sizelimit, ref int messageNumber) =>
                                Interop.Ldap.ldap_search(ldapHandle, dn, scope, filter, attributes, attributeOnly, servercontrol, clientcontrol, timelimit, sizelimit, ref messageNumber);

        internal static int SetClientCertOption(ConnectionHandle ldapHandle, LdapOption option, QUERYCLIENTCERT outValue) => Interop.Ldap.ldap_set_option_clientcert(ldapHandle, option, outValue);

        internal static int SetIntOption(ConnectionHandle ld, LdapOption option, ref int inValue) => Interop.Ldap.ldap_set_option_int(ld, option, ref inValue);

        internal static int SetPtrOption(ConnectionHandle ldapHandle, LdapOption option, ref IntPtr inValue) => Interop.Ldap.ldap_set_option_ptr(ldapHandle, option, ref inValue);

        internal static int SetReferralOption(ConnectionHandle ldapHandle, LdapOption option, ref LdapReferralCallback outValue) => Interop.Ldap.ldap_set_option_referral(ldapHandle, option, ref outValue);

        internal static int SetServerCertOption(ConnectionHandle ldapHandle, LdapOption option, VERIFYSERVERCERT outValue) => Interop.Ldap.ldap_set_option_servercert(ldapHandle, option, outValue);

        internal static int BindToDirectory(ConnectionHandle ld, string who, string passwd) => Interop.Ldap.ldap_simple_bind_s(ld, who, passwd);

        internal static int StartTls(ConnectionHandle ldapHandle, ref int ServerReturnValue, ref IntPtr Message, IntPtr ServerControls, IntPtr ClientControls) => Interop.Ldap.ldap_start_tls(ldapHandle, ref ServerReturnValue, ref Message, ServerControls, ClientControls);

        internal static byte StopTls(ConnectionHandle ldapHandle) => Interop.Ldap.ldap_stop_tls(ldapHandle);

        internal static void FreeValue(IntPtr referral) => Interop.Ldap.ldap_value_free(referral);

        internal static void FreeAttributes(IntPtr berelement) => Interop.Ldap.ldap_value_free_len(berelement);

        internal static string PtrToString(IntPtr requestName) => Marshal.PtrToStringUni(requestName);

        internal static IntPtr StringToPtr(string s) => Marshal.StringToHGlobalUni(s);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.DirectoryServices.ActiveDirectory
{
    internal enum TRUSTED_INFORMATION_CLASS
    {
        TrustedDomainNameInformation = 1,
        TrustedControllersInformation,
        TrustedPosixOffsetInformation,
        TrustedPasswordInformation,
        TrustedDomainInformationBasic,
        TrustedDomainInformationEx,
        TrustedDomainAuthInformation,
        TrustedDomainFullInformation,
        TrustedDomainAuthInformationInternal,
        TrustedDomainFullInformationInternal,
        TrustedDomainInformationEx2Internal,
        TrustedDomainFullInformation2Internal
    }

    [Flags]
    internal enum TRUST_ATTRIBUTE
    {
        TRUST_ATTRIBUTE_NON_TRANSITIVE = 0x00000001,
        TRUST_ATTRIBUTE_UPLEVEL_ONLY = 0x00000002,
        TRUST_ATTRIBUTE_QUARANTINED_DOMAIN = 0x00000004,
        TRUST_ATTRIBUTE_FOREST_TRANSITIVE = 0x00000008,
        TRUST_ATTRIBUTE_CROSS_ORGANIZATION = 0x00000010,
        TRUST_ATTRIBUTE_WITHIN_FOREST = 0x00000020,
        TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL = 0x00000040
    }

    internal class TrustHelper
    {
        private static int s_STATUS_OBJECT_NAME_NOT_FOUND = 2;
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        internal static int ERROR_NOT_FOUND = 1168;
#pragma warning restore 0414
        internal static int NETLOGON_QUERY_LEVEL = 2;
        internal static int NETLOGON_CONTROL_REDISCOVER = 5;
        private static int s_NETLOGON_CONTROL_TC_VERIFY = 10;
        private static int s_NETLOGON_VERIFY_STATUS_RETURNED = 0x80;
        private static int s_PASSWORD_LENGTH = 15;
        private static int s_TRUST_AUTH_TYPE_CLEAR = 2;
        private static int s_policyDnsDomainInformation = 12;
        private static int s_TRUSTED_SET_POSIX = 0x00000010;
        private static int s_TRUSTED_SET_AUTH = 0x00000020;
        internal static int TRUST_TYPE_DOWNLEVEL = 0x00000001;
        internal static int TRUST_TYPE_UPLEVEL = 0x00000002;
        internal static int TRUST_TYPE_MIT = 0x00000003;
        private static int s_ERROR_ALREADY_EXISTS = 183;
        private static int s_ERROR_INVALID_LEVEL = 124;
        private static char[] s_punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        private TrustHelper() { }

        internal static bool GetTrustedDomainInfoStatus(DirectoryContext context, string sourceName, string targetName, TRUST_ATTRIBUTE attribute, bool isForest)
        {
            PolicySafeHandle handle = null;
            IntPtr buffer = (IntPtr)0;
            LSA_UNICODE_STRING trustedDomainName = null;
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string serverName = null;

            // get policy server name
            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(targetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    int result = UnsafeNativeMethods.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
                    if (result != 0)
                    {
                        int win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == s_STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
                    }

                    Debug.Assert(buffer != (IntPtr)0);

                    TRUSTED_DOMAIN_INFORMATION_EX domainInfo = new TRUSTED_DOMAIN_INFORMATION_EX();
                    Marshal.PtrToStructure(buffer, domainInfo);

                    // validate this is the trust that the user refers to
                    ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                    // get the attribute of the trust

                    // selective authentication info
                    if (attribute == TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION)
                    {
                        if ((domainInfo.TrustAttributes & TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION) == 0)
                            return false;
                        else
                            return true;
                    }
                    // sid filtering behavior for forest trust
                    else if (attribute == TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL)
                    {
                        if ((domainInfo.TrustAttributes & TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL) == 0)
                            return true;
                        else
                            return false;
                    }
                    // sid filtering behavior for domain trust
                    else if (attribute == TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN)
                    {
                        if ((domainInfo.TrustAttributes & TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN) == 0)
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        // should not happen
                        throw new ArgumentException(nameof(attribute));
                    }
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);

                    if (buffer != (IntPtr)0)
                        UnsafeNativeMethods.LsaFreeMemory(buffer);
                }
            }
            catch { throw; }
        }

        internal static void SetTrustedDomainInfoStatus(DirectoryContext context, string sourceName, string targetName, TRUST_ATTRIBUTE attribute, bool status, bool isForest)
        {
            PolicySafeHandle handle = null;
            IntPtr buffer = (IntPtr)0;
            IntPtr newInfo = (IntPtr)0;
            LSA_UNICODE_STRING trustedDomainName = null;
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string serverName = null;

            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(targetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    // get the trusted domain information
                    int result = UnsafeNativeMethods.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
                    if (result != 0)
                    {
                        int win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == s_STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
                    }
                    Debug.Assert(buffer != (IntPtr)0);

                    // get the managed structre representation
                    TRUSTED_DOMAIN_INFORMATION_EX domainInfo = new TRUSTED_DOMAIN_INFORMATION_EX();
                    Marshal.PtrToStructure(buffer, domainInfo);

                    // validate this is the trust that the user refers to
                    ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                    // change the attribute value properly

                    // selective authentication
                    if (attribute == TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION)
                    {
                        if (status)
                        {
                            // turns on selective authentication
                            domainInfo.TrustAttributes |= TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION;
                        }
                        else
                        {
                            // turns off selective authentication
                            domainInfo.TrustAttributes &= ~(TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION);
                        }
                    }
                    // user wants to change sid filtering behavior for forest trust
                    else if (attribute == TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL)
                    {
                        if (status)
                        {
                            // user wants sid filtering behavior
                            domainInfo.TrustAttributes &= ~(TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL);
                        }
                        else
                        {
                            // users wants to turn off sid filtering behavior
                            domainInfo.TrustAttributes |= TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL;
                        }
                    }
                    // user wants to change sid filtering behavior for external trust
                    else if (attribute == TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN)
                    {
                        if (status)
                        {
                            // user wants sid filtering behavior
                            domainInfo.TrustAttributes |= TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN;
                        }
                        else
                        {
                            // user wants to turn off sid filtering behavior
                            domainInfo.TrustAttributes &= ~(TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN);
                        }
                    }
                    else
                    {
                        throw new ArgumentException(nameof(attribute));
                    }

                    // reconstruct the unmanaged structure to set it back
                    newInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TRUSTED_DOMAIN_INFORMATION_EX)));
                    Marshal.StructureToPtr(domainInfo, newInfo, false);

                    result = UnsafeNativeMethods.LsaSetTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, newInfo);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError(result), serverName);
                    }

                    return;
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);

                    if (buffer != (IntPtr)0)
                        UnsafeNativeMethods.LsaFreeMemory(buffer);

                    if (newInfo != (IntPtr)0)
                        Marshal.FreeHGlobal(newInfo);
                }
            }
            catch { throw; }
        }

        internal static void DeleteTrust(DirectoryContext sourceContext, string sourceName, string targetName, bool isForest)
        {
            PolicySafeHandle policyHandle = null;
            LSA_UNICODE_STRING trustedDomainName = null;
            int win32Error = 0;
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string serverName = null;
            IntPtr buffer = (IntPtr)0;

            serverName = Utils.GetPolicyServerName(sourceContext, isForest, false, sourceName);

            impersonated = Utils.Impersonate(sourceContext);

            try
            {
                try
                {
                    // get the policy handle
                    policyHandle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(targetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    // get trust information
                    int result = UnsafeNativeMethods.LsaQueryTrustedDomainInfoByName(policyHandle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
                    if (result != 0)
                    {
                        win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == s_STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
                    }

                    Debug.Assert(buffer != (IntPtr)0);

                    try
                    {
                        TRUSTED_DOMAIN_INFORMATION_EX domainInfo = new TRUSTED_DOMAIN_INFORMATION_EX();
                        Marshal.PtrToStructure(buffer, domainInfo);

                        // validate this is the trust that the user refers to
                        ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                        // delete the trust
                        result = UnsafeNativeMethods.LsaDeleteTrustedDomain(policyHandle, domainInfo.Sid);
                        if (result != 0)
                        {
                            win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
                        }
                    }
                    finally
                    {
                        if (buffer != (IntPtr)0)
                            UnsafeNativeMethods.LsaFreeMemory(buffer);
                    }
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);
                }
            }
            catch { throw; }
        }

        internal static void VerifyTrust(DirectoryContext context, string sourceName, string targetName, bool isForest, TrustDirection direction, bool forceSecureChannelReset, string preferredTargetServer)
        {
            PolicySafeHandle policyHandle = null;
            LSA_UNICODE_STRING trustedDomainName = null;
            int win32Error = 0;
            IntPtr data = (IntPtr)0;
            IntPtr ptr = (IntPtr)0;
            IntPtr buffer1 = (IntPtr)0;
            IntPtr buffer2 = (IntPtr)0;
            bool impersonated = true;
            IntPtr target = (IntPtr)0;
            string policyServerName = null;

            policyServerName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle
                    policyHandle = new PolicySafeHandle(Utils.GetPolicyHandle(policyServerName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(targetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    // validate the trust existence
                    ValidateTrust(policyHandle, trustedDomainName, sourceName, targetName, isForest, (int)direction, policyServerName);  // need to verify direction                

                    if (preferredTargetServer == null)
                        data = Marshal.StringToHGlobalUni(targetName);
                    else
                        // this is the case that we need to specifically go to a particular server. This is the way to tell netlogon to do that.
                        data = Marshal.StringToHGlobalUni(targetName + "\\" + preferredTargetServer);
                    ptr = Marshal.AllocHGlobal(IntPtr.Size);
                    Marshal.WriteIntPtr(ptr, data);

                    if (!forceSecureChannelReset)
                    {
                        win32Error = UnsafeNativeMethods.I_NetLogonControl2(policyServerName, s_NETLOGON_CONTROL_TC_VERIFY, NETLOGON_QUERY_LEVEL, ptr, out buffer1);

                        if (win32Error == 0)
                        {
                            NETLOGON_INFO_2 info = new NETLOGON_INFO_2();
                            Marshal.PtrToStructure(buffer1, info);

                            if ((info.netlog2_flags & s_NETLOGON_VERIFY_STATUS_RETURNED) != 0)
                            {
                                int result = info.netlog2_pdc_connection_status;
                                if (result == 0)
                                {
                                    // verification succeeded
                                    return;
                                }
                                else
                                {
                                    // don't really know which server is down, the source or the target
                                    throw ExceptionHelper.GetExceptionFromErrorCode(result);
                                }
                            }
                            else
                            {
                                int result = info.netlog2_tc_connection_status;
                                throw ExceptionHelper.GetExceptionFromErrorCode(result);
                            }
                        }
                        else
                        {
                            if (win32Error == s_ERROR_INVALID_LEVEL)
                            {
                                // it is pre-win2k SP3 dc that does not support NETLOGON_CONTROL_TC_VERIFY
                                throw new NotSupportedException(SR.TrustVerificationNotSupport);
                            }
                            else
                            {
                                throw ExceptionHelper.GetExceptionFromErrorCode(win32Error);
                            }
                        }
                    }
                    else
                    {
                        // then try secure channel reset
                        win32Error = UnsafeNativeMethods.I_NetLogonControl2(policyServerName, NETLOGON_CONTROL_REDISCOVER, NETLOGON_QUERY_LEVEL, ptr, out buffer2);
                        if (win32Error != 0)
                            // don't really know which server is down, the source or the target
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error);
                    }
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);

                    if (ptr != (IntPtr)0)
                        Marshal.FreeHGlobal(ptr);

                    if (data != (IntPtr)0)
                        Marshal.FreeHGlobal(data);

                    if (buffer1 != (IntPtr)0)
                        UnsafeNativeMethods.NetApiBufferFree(buffer1);

                    if (buffer2 != (IntPtr)0)
                        UnsafeNativeMethods.NetApiBufferFree(buffer2);
                }
            }
            catch { throw; }
        }

        internal static void CreateTrust(DirectoryContext sourceContext, string sourceName, DirectoryContext targetContext, string targetName, bool isForest, TrustDirection direction, string password)
        {
            LSA_AUTH_INFORMATION AuthData = null;
            TRUSTED_DOMAIN_AUTH_INFORMATION AuthInfoEx = null;
            TRUSTED_DOMAIN_INFORMATION_EX tdi = null;
            IntPtr fileTime = (IntPtr)0;
            IntPtr unmanagedPassword = (IntPtr)0;
            IntPtr info = (IntPtr)0;
            IntPtr domainHandle = (IntPtr)0;
            PolicySafeHandle policyHandle = null;
            IntPtr unmanagedAuthData = (IntPtr)0;
            bool impersonated = false;
            string serverName = null;

            // get the domain info first
            info = GetTrustedDomainInfo(targetContext, targetName, isForest);

            try
            {
                try
                {
                    POLICY_DNS_DOMAIN_INFO domainInfo = new POLICY_DNS_DOMAIN_INFO();
                    Marshal.PtrToStructure(info, domainInfo);

                    AuthData = new LSA_AUTH_INFORMATION();
                    fileTime = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileTime)));
                    UnsafeNativeMethods.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime tmp = new FileTime();
                    Marshal.PtrToStructure(fileTime, tmp);
                    AuthData.LastUpdateTime = new LARGE_INTEGER();
                    AuthData.LastUpdateTime.lowPart = tmp.lower;
                    AuthData.LastUpdateTime.highPart = tmp.higher;

                    AuthData.AuthType = s_TRUST_AUTH_TYPE_CLEAR;
                    unmanagedPassword = Marshal.StringToHGlobalUni(password);
                    AuthData.AuthInfo = unmanagedPassword;
                    AuthData.AuthInfoLength = password.Length * 2;          // sizeof(WCHAR)

                    unmanagedAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_AUTH_INFORMATION)));
                    Marshal.StructureToPtr(AuthData, unmanagedAuthData, false);

                    AuthInfoEx = new TRUSTED_DOMAIN_AUTH_INFORMATION();
                    if ((direction & TrustDirection.Inbound) != 0)
                    {
                        AuthInfoEx.IncomingAuthInfos = 1;
                        AuthInfoEx.IncomingAuthenticationInformation = unmanagedAuthData;
                        AuthInfoEx.IncomingPreviousAuthenticationInformation = (IntPtr)0;
                    }

                    if ((direction & TrustDirection.Outbound) != 0)
                    {
                        AuthInfoEx.OutgoingAuthInfos = 1;
                        AuthInfoEx.OutgoingAuthenticationInformation = unmanagedAuthData;
                        AuthInfoEx.OutgoingPreviousAuthenticationInformation = (IntPtr)0;
                    }

                    tdi = new TRUSTED_DOMAIN_INFORMATION_EX();
                    tdi.FlatName = domainInfo.Name;
                    tdi.Name = domainInfo.DnsDomainName;
                    tdi.Sid = domainInfo.Sid;
                    tdi.TrustType = TRUST_TYPE_UPLEVEL;
                    tdi.TrustDirection = (int)direction;
                    if (isForest)
                    {
                        tdi.TrustAttributes = TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE;
                    }
                    else
                    {
                        tdi.TrustAttributes = TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN;
                    }

                    // get server name
                    serverName = Utils.GetPolicyServerName(sourceContext, isForest, false, sourceName);

                    // do impersonation and get policy handle
                    impersonated = Utils.Impersonate(sourceContext);
                    policyHandle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    int result = UnsafeNativeMethods.LsaCreateTrustedDomainEx(policyHandle, tdi, AuthInfoEx, s_TRUSTED_SET_POSIX | s_TRUSTED_SET_AUTH, out domainHandle);
                    if (result != 0)
                    {
                        result = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        if (result == s_ERROR_ALREADY_EXISTS)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectExistsException(SR.Format(SR.AlreadyExistingForestTrust , sourceName, targetName));
                            else
                                throw new ActiveDirectoryObjectExistsException(SR.Format(SR.AlreadyExistingDomainTrust , sourceName, targetName));
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode(result, serverName);
                    }
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (fileTime != (IntPtr)0)
                        Marshal.FreeHGlobal(fileTime);

                    if (domainHandle != (IntPtr)0)
                        UnsafeNativeMethods.LsaClose(domainHandle);

                    if (info != (IntPtr)0)
                        UnsafeNativeMethods.LsaFreeMemory(info);

                    if (unmanagedPassword != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedPassword);

                    if (unmanagedAuthData != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedAuthData);
                }
            }
            catch { throw; }
        }

        internal static string UpdateTrust(DirectoryContext context, string sourceName, string targetName, string password, bool isForest)
        {
            PolicySafeHandle handle = null;
            IntPtr buffer = (IntPtr)0;
            LSA_UNICODE_STRING trustedDomainName = null;
            IntPtr newBuffer = (IntPtr)0;
            bool impersonated = false;
            LSA_AUTH_INFORMATION AuthData = null;
            IntPtr fileTime = (IntPtr)0;
            IntPtr unmanagedPassword = (IntPtr)0;
            IntPtr unmanagedAuthData = (IntPtr)0;
            TRUSTED_DOMAIN_AUTH_INFORMATION AuthInfoEx = null;
            TrustDirection direction;
            IntPtr target = (IntPtr)0;
            string serverName = null;

            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(targetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    // get the trusted domain information                
                    int result = UnsafeNativeMethods.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, ref buffer);
                    if (result != 0)
                    {
                        int win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == s_STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
                    }

                    // get the managed structre representation
                    TRUSTED_DOMAIN_FULL_INFORMATION domainInfo = new TRUSTED_DOMAIN_FULL_INFORMATION();
                    Marshal.PtrToStructure(buffer, domainInfo);

                    // validate the trust attribute first
                    ValidateTrustAttribute(domainInfo.Information, isForest, sourceName, targetName);

                    // get trust direction
                    direction = (TrustDirection)domainInfo.Information.TrustDirection;

                    // change the attribute value properly
                    AuthData = new LSA_AUTH_INFORMATION();
                    fileTime = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileTime)));
                    UnsafeNativeMethods.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime tmp = new FileTime();
                    Marshal.PtrToStructure(fileTime, tmp);
                    AuthData.LastUpdateTime = new LARGE_INTEGER();
                    AuthData.LastUpdateTime.lowPart = tmp.lower;
                    AuthData.LastUpdateTime.highPart = tmp.higher;

                    AuthData.AuthType = s_TRUST_AUTH_TYPE_CLEAR;
                    unmanagedPassword = Marshal.StringToHGlobalUni(password);
                    AuthData.AuthInfo = unmanagedPassword;
                    AuthData.AuthInfoLength = password.Length * 2;

                    unmanagedAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_AUTH_INFORMATION)));
                    Marshal.StructureToPtr(AuthData, unmanagedAuthData, false);

                    AuthInfoEx = new TRUSTED_DOMAIN_AUTH_INFORMATION();
                    if ((direction & TrustDirection.Inbound) != 0)
                    {
                        AuthInfoEx.IncomingAuthInfos = 1;
                        AuthInfoEx.IncomingAuthenticationInformation = unmanagedAuthData;
                        AuthInfoEx.IncomingPreviousAuthenticationInformation = (IntPtr)0;
                    }

                    if ((direction & TrustDirection.Outbound) != 0)
                    {
                        AuthInfoEx.OutgoingAuthInfos = 1;
                        AuthInfoEx.OutgoingAuthenticationInformation = unmanagedAuthData;
                        AuthInfoEx.OutgoingPreviousAuthenticationInformation = (IntPtr)0;
                    }

                    // reconstruct the unmanaged structure to set it back              
                    domainInfo.AuthInformation = AuthInfoEx;
                    newBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TRUSTED_DOMAIN_FULL_INFORMATION)));
                    Marshal.StructureToPtr(domainInfo, newBuffer, false);

                    result = UnsafeNativeMethods.LsaSetTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, newBuffer);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError(result), serverName);
                    }

                    return serverName;
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);

                    if (buffer != (IntPtr)0)
                        UnsafeNativeMethods.LsaFreeMemory(buffer);

                    if (newBuffer != (IntPtr)0)
                        Marshal.FreeHGlobal(newBuffer);

                    if (fileTime != (IntPtr)0)
                        Marshal.FreeHGlobal(fileTime);

                    if (unmanagedPassword != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedPassword);

                    if (unmanagedAuthData != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedAuthData);
                }
            }
            catch { throw; }
        }

        internal static void UpdateTrustDirection(DirectoryContext context, string sourceName, string targetName, string password, bool isForest, TrustDirection newTrustDirection)
        {
            PolicySafeHandle handle = null;
            IntPtr buffer = (IntPtr)0;
            LSA_UNICODE_STRING trustedDomainName = null;
            IntPtr newBuffer = (IntPtr)0;
            bool impersonated = false;
            LSA_AUTH_INFORMATION AuthData = null;
            IntPtr fileTime = (IntPtr)0;
            IntPtr unmanagedPassword = (IntPtr)0;
            IntPtr unmanagedAuthData = (IntPtr)0;
            TRUSTED_DOMAIN_AUTH_INFORMATION AuthInfoEx = null;
            IntPtr target = (IntPtr)0;
            string serverName = null;

            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));

                    // get the target name
                    trustedDomainName = new LSA_UNICODE_STRING();
                    target = Marshal.StringToHGlobalUni(targetName);
                    UnsafeNativeMethods.RtlInitUnicodeString(trustedDomainName, target);

                    // get the trusted domain information                
                    int result = UnsafeNativeMethods.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, ref buffer);
                    if (result != 0)
                    {
                        int win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == s_STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
                    }

                    // get the managed structre representation
                    TRUSTED_DOMAIN_FULL_INFORMATION domainInfo = new TRUSTED_DOMAIN_FULL_INFORMATION();
                    Marshal.PtrToStructure(buffer, domainInfo);

                    // validate the trust attribute first
                    ValidateTrustAttribute(domainInfo.Information, isForest, sourceName, targetName);

                    // change the attribute value properly
                    AuthData = new LSA_AUTH_INFORMATION();
                    fileTime = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileTime)));
                    UnsafeNativeMethods.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime tmp = new FileTime();
                    Marshal.PtrToStructure(fileTime, tmp);
                    AuthData.LastUpdateTime = new LARGE_INTEGER();
                    AuthData.LastUpdateTime.lowPart = tmp.lower;
                    AuthData.LastUpdateTime.highPart = tmp.higher;

                    AuthData.AuthType = s_TRUST_AUTH_TYPE_CLEAR;
                    unmanagedPassword = Marshal.StringToHGlobalUni(password);
                    AuthData.AuthInfo = unmanagedPassword;
                    AuthData.AuthInfoLength = password.Length * 2;

                    unmanagedAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_AUTH_INFORMATION)));
                    Marshal.StructureToPtr(AuthData, unmanagedAuthData, false);

                    AuthInfoEx = new TRUSTED_DOMAIN_AUTH_INFORMATION();
                    if ((newTrustDirection & TrustDirection.Inbound) != 0)
                    {
                        AuthInfoEx.IncomingAuthInfos = 1;
                        AuthInfoEx.IncomingAuthenticationInformation = unmanagedAuthData;
                        AuthInfoEx.IncomingPreviousAuthenticationInformation = (IntPtr)0;
                    }
                    else
                    {
                        AuthInfoEx.IncomingAuthInfos = 0;
                        AuthInfoEx.IncomingAuthenticationInformation = (IntPtr)0;
                        AuthInfoEx.IncomingPreviousAuthenticationInformation = (IntPtr)0;
                    }

                    if ((newTrustDirection & TrustDirection.Outbound) != 0)
                    {
                        AuthInfoEx.OutgoingAuthInfos = 1;
                        AuthInfoEx.OutgoingAuthenticationInformation = unmanagedAuthData;
                        AuthInfoEx.OutgoingPreviousAuthenticationInformation = (IntPtr)0;
                    }
                    else
                    {
                        AuthInfoEx.OutgoingAuthInfos = 0;
                        AuthInfoEx.OutgoingAuthenticationInformation = (IntPtr)0;
                        AuthInfoEx.OutgoingPreviousAuthenticationInformation = (IntPtr)0;
                    }

                    // reconstruct the unmanaged structure to set it back              
                    domainInfo.AuthInformation = AuthInfoEx;
                    // reset the trust direction
                    domainInfo.Information.TrustDirection = (int)newTrustDirection;

                    newBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TRUSTED_DOMAIN_FULL_INFORMATION)));
                    Marshal.StructureToPtr(domainInfo, newBuffer, false);

                    result = UnsafeNativeMethods.LsaSetTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, newBuffer);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError(result), serverName);
                    }

                    return;
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (target != (IntPtr)0)
                        Marshal.FreeHGlobal(target);

                    if (buffer != (IntPtr)0)
                        UnsafeNativeMethods.LsaFreeMemory(buffer);

                    if (newBuffer != (IntPtr)0)
                        Marshal.FreeHGlobal(newBuffer);

                    if (fileTime != (IntPtr)0)
                        Marshal.FreeHGlobal(fileTime);

                    if (unmanagedPassword != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedPassword);

                    if (unmanagedAuthData != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedAuthData);
                }
            }
            catch { throw; }
        }

        private static void ValidateTrust(PolicySafeHandle handle, LSA_UNICODE_STRING trustedDomainName, string sourceName, string targetName, bool isForest, int direction, string serverName)
        {
            IntPtr buffer = (IntPtr)0;

            // get trust information
            int result = UnsafeNativeMethods.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
            if (result != 0)
            {
                int win32Error = UnsafeNativeMethods.LsaNtStatusToWinError(result);
                // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                if (win32Error == s_STATUS_OBJECT_NAME_NOT_FOUND)
                {
                    if (isForest)
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                    else
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                }
                else
                    throw ExceptionHelper.GetExceptionFromErrorCode(win32Error, serverName);
            }

            Debug.Assert(buffer != (IntPtr)0);

            try
            {
                TRUSTED_DOMAIN_INFORMATION_EX domainInfo = new TRUSTED_DOMAIN_INFORMATION_EX();
                Marshal.PtrToStructure(buffer, domainInfo);

                // validate this is the trust that the user refers to
                ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                // validate trust direction if applicable
                if (direction != 0)
                {
                    if ((direction & domainInfo.TrustDirection) == 0)
                    {
                        if (isForest)
                            throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection , sourceName, targetName, (TrustDirection)direction), typeof(ForestTrustRelationshipInformation), null);
                        else
                            throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection , sourceName, targetName, (TrustDirection)direction), typeof(TrustRelationshipInformation), null);
                    }
                }
            }
            finally
            {
                if (buffer != (IntPtr)0)
                    UnsafeNativeMethods.LsaFreeMemory(buffer);
            }
        }

        private static void ValidateTrustAttribute(TRUSTED_DOMAIN_INFORMATION_EX domainInfo, bool isForest, string sourceName, string targetName)
        {
            if (isForest)
            {
                // it should be a forest trust, make sure that TRUST_ATTRIBUTE_FOREST_TRANSITIVE bit is set
                if ((domainInfo.TrustAttributes & TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE) == 0)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist , sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                }
            }
            else
            {
                // it should not be a forest trust, make sure that TRUST_ATTRIBUTE_FOREST_TRANSITIVE bit is not set
                if ((domainInfo.TrustAttributes & TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE) != 0)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongForestTrust , sourceName, targetName), typeof(TrustRelationshipInformation), null);
                }

                // we don't deal with NT4 trust also
                if (domainInfo.TrustType == TRUST_TYPE_DOWNLEVEL)
                    throw new InvalidOperationException(SR.NT4NotSupported);

                // we don't perform any operation on kerberos trust also
                if (domainInfo.TrustType == TRUST_TYPE_MIT)
                    throw new InvalidOperationException(SR.KerberosNotSupported);
            }
        }

        internal static string CreateTrustPassword()
        {
            string password = string.Empty;
            byte[] buf;
            char[] cBuf;

            buf = new byte[s_PASSWORD_LENGTH];
            cBuf = new char[s_PASSWORD_LENGTH];

            using (RandomNumberGenerator RNG = RandomNumberGenerator.Create())
            {
                RNG.GetBytes(buf);
                for (int iter = 0; iter < s_PASSWORD_LENGTH; iter++)
                {
                    int i = (int)(buf[iter] % 87);
                    if (i < 10)
                        cBuf[iter] = (char)('0' + i);
                    else if (i < 36)
                        cBuf[iter] = (char)('A' + i - 10);
                    else if (i < 62)
                        cBuf[iter] = (char)('a' + i - 36);
                    else
                        cBuf[iter] = s_punctuations[i - 62];
                }
            }

            password = new string(cBuf);

            return password;
        }

        private static IntPtr GetTrustedDomainInfo(DirectoryContext targetContext, string targetName, bool isForest)
        {
            PolicySafeHandle policyHandle = null;
            IntPtr buffer = (IntPtr)0;
            bool impersonated = false;
            string serverName = null;

            try
            {
                try
                {
                    serverName = Utils.GetPolicyServerName(targetContext, isForest, false, targetName);
                    impersonated = Utils.Impersonate(targetContext);
                    try
                    {
                        policyHandle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));
                    }
                    catch (ActiveDirectoryOperationException)
                    {
                        if (impersonated)
                        {
                            Utils.Revert();
                            impersonated = false;
                        }
                        // try anonymous          
                        Utils.ImpersonateAnonymous();
                        impersonated = true;
                        policyHandle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (impersonated)
                        {
                            Utils.Revert();
                            impersonated = false;
                        }
                        // try anonymous          
                        Utils.ImpersonateAnonymous();
                        impersonated = true;
                        policyHandle = new PolicySafeHandle(Utils.GetPolicyHandle(serverName));
                    }

                    int result = UnsafeNativeMethods.LsaQueryInformationPolicy(policyHandle, s_policyDnsDomainInformation, out buffer);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError(result), serverName);
                    }

                    return buffer;
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();
                }
            }
            catch { throw; }
        }
    }
}

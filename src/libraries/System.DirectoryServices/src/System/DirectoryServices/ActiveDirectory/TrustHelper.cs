// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.ActiveDirectory
{

    internal static class TrustHelper
    {
        private const int STATUS_OBJECT_NAME_NOT_FOUND = 2;
        internal const int NETLOGON_QUERY_LEVEL = 2;
        internal const int NETLOGON_CONTROL_REDISCOVER = 5;
        private const int NETLOGON_CONTROL_TC_VERIFY = 10;
        private const int NETLOGON_VERIFY_STATUS_RETURNED = 0x80;
        private const int PASSWORD_LENGTH = 15;
        private const int TRUST_AUTH_TYPE_CLEAR = 2;
        private const int policyDnsDomainInformation = 12;
        private const int TRUSTED_SET_POSIX = 0x00000010;
        private const int TRUSTED_SET_AUTH = 0x00000020;
        internal const int TRUST_TYPE_DOWNLEVEL = 0x00000001;
        internal const int TRUST_TYPE_UPLEVEL = 0x00000002;
        internal const int TRUST_TYPE_MIT = 0x00000003;
        private const string PasswordCharacterSet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()_-+=[{]};:>|./?";

        internal static unsafe bool GetTrustedDomainInfoStatus(DirectoryContext context, string? sourceName, string targetName, Interop.Advapi32.TRUST_ATTRIBUTE attribute, bool isForest)
        {
            SafeLsaPolicyHandle? handle = null;
            IntPtr buffer = (IntPtr)0;
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string? serverName = null;

            // get policy server name
            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = Utils.GetPolicyHandle(serverName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(targetName);
                    Interop.NtDll.RtlInitUnicodeString(out trustedDomainName, target);

                    uint result = Interop.Advapi32.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
                    if (result != 0)
                    {
                        uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                    }

                    Debug.Assert(buffer != (IntPtr)0);

                    Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX domainInfo = *(Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX*)buffer;

                    // validate this is the trust that the user refers to
                    ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                    // get the attribute of the trust

                    // selective authentication info
                    if (attribute == Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION)
                    {
                        if ((domainInfo.TrustAttributes & Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION) == 0)
                            return false;
                        else
                            return true;
                    }
                    // sid filtering behavior for forest trust
                    else if (attribute == Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL)
                    {
                        if ((domainInfo.TrustAttributes & Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL) == 0)
                            return true;
                        else
                            return false;
                    }
                    // sid filtering behavior for domain trust
                    else if (attribute == Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN)
                    {
                        if ((domainInfo.TrustAttributes & Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN) == 0)
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
                        global::Interop.Advapi32.LsaFreeMemory(buffer);
                }
            }
            catch { throw; }
        }

        internal static unsafe void SetTrustedDomainInfoStatus(DirectoryContext context, string? sourceName, string targetName, Interop.Advapi32.TRUST_ATTRIBUTE attribute, bool status, bool isForest)
        {
            SafeLsaPolicyHandle? handle = null;
            IntPtr buffer = (IntPtr)0;
            IntPtr newInfo = (IntPtr)0;
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string? serverName = null;

            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = Utils.GetPolicyHandle(serverName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(targetName);
                    Interop.NtDll.RtlInitUnicodeString(out trustedDomainName, target);

                    // get the trusted domain information
                    uint result = Interop.Advapi32.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
                    if (result != 0)
                    {
                        uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                    }
                    Debug.Assert(buffer != (IntPtr)0);

                    // get the managed structure representation
                    Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX domainInfo = *(Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX*)buffer;

                    // validate this is the trust that the user refers to
                    ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                    // change the attribute value properly

                    // selective authentication
                    if (attribute == Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION)
                    {
                        if (status)
                        {
                            // turns on selective authentication
                            domainInfo.TrustAttributes |= Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION;
                        }
                        else
                        {
                            // turns off selective authentication
                            domainInfo.TrustAttributes &= ~(Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION);
                        }
                    }
                    // user wants to change sid filtering behavior for forest trust
                    else if (attribute == Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL)
                    {
                        if (status)
                        {
                            // user wants sid filtering behavior
                            domainInfo.TrustAttributes &= ~(Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL);
                        }
                        else
                        {
                            // users wants to turn off sid filtering behavior
                            domainInfo.TrustAttributes |= Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL;
                        }
                    }
                    // user wants to change sid filtering behavior for external trust
                    else if (attribute == Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN)
                    {
                        if (status)
                        {
                            // user wants sid filtering behavior
                            domainInfo.TrustAttributes |= Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN;
                        }
                        else
                        {
                            // user wants to turn off sid filtering behavior
                            domainInfo.TrustAttributes &= ~(Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN);
                        }
                    }
                    else
                    {
                        throw new ArgumentException(nameof(attribute));
                    }

                    // reconstruct the unmanaged structure to set it back
                    newInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX)));
                    Marshal.StructureToPtr(domainInfo, newInfo, false);

                    result = Interop.Advapi32.LsaSetTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, newInfo);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode((int)global::Interop.Advapi32.LsaNtStatusToWinError(result), serverName);
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
                        global::Interop.Advapi32.LsaFreeMemory(buffer);

                    if (newInfo != (IntPtr)0)
                        Marshal.FreeHGlobal(newInfo);
                }
            }
            catch { throw; }
        }

        internal static unsafe void DeleteTrust(DirectoryContext sourceContext, string? sourceName, string? targetName, bool isForest)
        {
            SafeLsaPolicyHandle? policyHandle = null;
            bool impersonated = false;
            IntPtr target = (IntPtr)0;
            string? serverName = null;
            IntPtr buffer = (IntPtr)0;

            serverName = Utils.GetPolicyServerName(sourceContext, isForest, false, sourceName);

            impersonated = Utils.Impersonate(sourceContext);

            try
            {
                try
                {
                    // get the policy handle
                    policyHandle = Utils.GetPolicyHandle(serverName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(targetName);
                    Interop.NtDll.RtlInitUnicodeString(out trustedDomainName, target);

                    // get trust information
                    uint result = Interop.Advapi32.LsaQueryTrustedDomainInfoByName(policyHandle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
                    if (result != 0)
                    {
                        uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                    }

                    Debug.Assert(buffer != (IntPtr)0);

                    try
                    {
                        Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX domainInfo = *(Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX*)buffer;

                        // validate this is the trust that the user refers to
                        ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                        // delete the trust
                        result = Interop.Advapi32.LsaDeleteTrustedDomain(policyHandle, domainInfo.Sid);
                        if (result != 0)
                        {
                            uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                        }
                    }
                    finally
                    {
                        if (buffer != (IntPtr)0)
                            global::Interop.Advapi32.LsaFreeMemory(buffer);
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

        internal static void VerifyTrust(DirectoryContext context, string? sourceName, string? targetName, bool isForest, TrustDirection direction, bool forceSecureChannelReset, string? preferredTargetServer)
        {
            SafeLsaPolicyHandle? policyHandle = null;
            int win32Error = 0;
            IntPtr data = (IntPtr)0;
            IntPtr ptr = (IntPtr)0;
            IntPtr buffer1 = (IntPtr)0;
            IntPtr buffer2 = (IntPtr)0;
            bool impersonated = true;
            IntPtr target = (IntPtr)0;
            string? policyServerName = null;

            policyServerName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle
                    policyHandle = Utils.GetPolicyHandle(policyServerName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(targetName);
                    Interop.NtDll.RtlInitUnicodeString(out trustedDomainName, target);

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
                        win32Error = Interop.Netapi32.I_NetLogonControl2(policyServerName, NETLOGON_CONTROL_TC_VERIFY, NETLOGON_QUERY_LEVEL, ptr, out buffer1);

                        if (win32Error == 0)
                        {
                            NETLOGON_INFO_2 info = new NETLOGON_INFO_2();
                            Marshal.PtrToStructure(buffer1, info);

                            if ((info.netlog2_flags & NETLOGON_VERIFY_STATUS_RETURNED) != 0)
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
                            if (win32Error == Interop.Errors.ERROR_INVALID_LEVEL)
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
                        win32Error = Interop.Netapi32.I_NetLogonControl2(policyServerName, NETLOGON_CONTROL_REDISCOVER, NETLOGON_QUERY_LEVEL, ptr, out buffer2);
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
                        Interop.Netapi32.NetApiBufferFree(buffer1);

                    if (buffer2 != (IntPtr)0)
                        Interop.Netapi32.NetApiBufferFree(buffer2);
                }
            }
            catch { throw; }
        }

        internal static void CreateTrust(DirectoryContext sourceContext, string? sourceName, DirectoryContext targetContext, string? targetName, bool isForest, TrustDirection direction, string password)
        {
            LSA_AUTH_INFORMATION? AuthData = null;
            IntPtr fileTime = (IntPtr)0;
            IntPtr unmanagedPassword = (IntPtr)0;
            IntPtr info = (IntPtr)0;
            IntPtr domainHandle = (IntPtr)0;
            SafeLsaPolicyHandle? policyHandle = null;
            IntPtr unmanagedAuthData = (IntPtr)0;
            bool impersonated = false;
            string? serverName = null;

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
                    Interop.Kernel32.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime tmp = new FileTime();
                    Marshal.PtrToStructure(fileTime, tmp);
                    AuthData.LastUpdateTime = new LARGE_INTEGER();
                    AuthData.LastUpdateTime.lowPart = tmp.lower;
                    AuthData.LastUpdateTime.highPart = tmp.higher;

                    AuthData.AuthType = TRUST_AUTH_TYPE_CLEAR;
                    unmanagedPassword = Marshal.StringToHGlobalUni(password);
                    AuthData.AuthInfo = unmanagedPassword;
                    AuthData.AuthInfoLength = password.Length * 2;          // sizeof(WCHAR)

                    unmanagedAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_AUTH_INFORMATION)));
                    Marshal.StructureToPtr(AuthData, unmanagedAuthData, false);

                    Interop.Advapi32.TRUSTED_DOMAIN_AUTH_INFORMATION AuthInfoEx = default;
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

                    Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX tdi = new Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX()
                    {
                        FlatName = domainInfo.Name,
                        Name = domainInfo.DnsDomainName,
                        Sid = domainInfo.Sid,
                        TrustType = TRUST_TYPE_UPLEVEL,
                        TrustDirection = (int)direction
                    };
                    if (isForest)
                    {
                        tdi.TrustAttributes = Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE;
                    }
                    else
                    {
                        tdi.TrustAttributes = Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN;
                    }

                    // get server name
                    serverName = Utils.GetPolicyServerName(sourceContext, isForest, false, sourceName);

                    // do impersonation and get policy handle
                    impersonated = Utils.Impersonate(sourceContext);
                    policyHandle = Utils.GetPolicyHandle(serverName);

                    uint result = Interop.Advapi32.LsaCreateTrustedDomainEx(policyHandle, tdi, AuthInfoEx, TRUSTED_SET_POSIX | TRUSTED_SET_AUTH, out domainHandle);
                    if (result != 0)
                    {
                        result = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        if (result == Interop.Errors.ERROR_ALREADY_EXISTS)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectExistsException(SR.Format(SR.AlreadyExistingForestTrust, sourceName, targetName));
                            else
                                throw new ActiveDirectoryObjectExistsException(SR.Format(SR.AlreadyExistingDomainTrust, sourceName, targetName));
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)result, serverName);
                    }
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();

                    if (fileTime != (IntPtr)0)
                        Marshal.FreeHGlobal(fileTime);

                    if (domainHandle != (IntPtr)0)
                        global::Interop.Advapi32.LsaClose(domainHandle);

                    if (info != (IntPtr)0)
                        global::Interop.Advapi32.LsaFreeMemory(info);

                    if (unmanagedPassword != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedPassword);

                    if (unmanagedAuthData != (IntPtr)0)
                        Marshal.FreeHGlobal(unmanagedAuthData);
                }
            }
            catch { throw; }
        }

        internal static unsafe string UpdateTrust(DirectoryContext context, string? sourceName, string? targetName, string password, bool isForest)
        {
            SafeLsaPolicyHandle? handle = null;
            IntPtr buffer = (IntPtr)0;
            IntPtr newBuffer = (IntPtr)0;
            bool impersonated = false;
            LSA_AUTH_INFORMATION? AuthData = null;
            IntPtr fileTime = (IntPtr)0;
            IntPtr unmanagedPassword = (IntPtr)0;
            IntPtr unmanagedAuthData = (IntPtr)0;
            TrustDirection direction;
            IntPtr target = (IntPtr)0;
            string? serverName = null;

            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = Utils.GetPolicyHandle(serverName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(targetName);
                    Interop.NtDll.RtlInitUnicodeString(out trustedDomainName, target);

                    // get the trusted domain information
                    uint result = Interop.Advapi32.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, ref buffer);
                    if (result != 0)
                    {
                        uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                    }

                    // get the managed structure representation
                    Interop.Advapi32.TRUSTED_DOMAIN_FULL_INFORMATION domainInfo = *(Interop.Advapi32.TRUSTED_DOMAIN_FULL_INFORMATION*)buffer;

                    // validate the trust attribute first
                    ValidateTrustAttribute(domainInfo.Information!, isForest, sourceName, targetName);

                    // get trust direction
                    direction = (TrustDirection)domainInfo.Information!.TrustDirection;

                    // change the attribute value properly
                    AuthData = new LSA_AUTH_INFORMATION();
                    fileTime = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileTime)));
                    Interop.Kernel32.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime tmp = new FileTime();
                    Marshal.PtrToStructure(fileTime, tmp);
                    AuthData.LastUpdateTime = new LARGE_INTEGER();
                    AuthData.LastUpdateTime.lowPart = tmp.lower;
                    AuthData.LastUpdateTime.highPart = tmp.higher;

                    AuthData.AuthType = TRUST_AUTH_TYPE_CLEAR;
                    unmanagedPassword = Marshal.StringToHGlobalUni(password);
                    AuthData.AuthInfo = unmanagedPassword;
                    AuthData.AuthInfoLength = password.Length * 2;

                    unmanagedAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_AUTH_INFORMATION)));
                    Marshal.StructureToPtr(AuthData, unmanagedAuthData, false);

                    Interop.Advapi32.TRUSTED_DOMAIN_AUTH_INFORMATION AuthInfoEx = default;
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
                    newBuffer = Marshal.AllocHGlobal(sizeof(Interop.Advapi32.TRUSTED_DOMAIN_FULL_INFORMATION));
                    Marshal.StructureToPtr(domainInfo, newBuffer, false);

                    result = Interop.Advapi32.LsaSetTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, newBuffer);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode((int)global::Interop.Advapi32.LsaNtStatusToWinError(result), serverName);
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
                        global::Interop.Advapi32.LsaFreeMemory(buffer);

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

        internal static unsafe void UpdateTrustDirection(DirectoryContext context, string? sourceName, string? targetName, string password, bool isForest, TrustDirection newTrustDirection)
        {
            SafeLsaPolicyHandle? handle = null;
            IntPtr buffer = (IntPtr)0;
            IntPtr newBuffer = (IntPtr)0;
            bool impersonated = false;
            LSA_AUTH_INFORMATION? AuthData = null;
            IntPtr fileTime = (IntPtr)0;
            IntPtr unmanagedPassword = (IntPtr)0;
            IntPtr unmanagedAuthData = (IntPtr)0;
            IntPtr target = (IntPtr)0;
            string? serverName = null;

            serverName = Utils.GetPolicyServerName(context, isForest, false, sourceName);

            impersonated = Utils.Impersonate(context);

            try
            {
                try
                {
                    // get the policy handle first
                    handle = Utils.GetPolicyHandle(serverName);

                    // get the target name
                    global::Interop.UNICODE_STRING trustedDomainName;
                    target = Marshal.StringToHGlobalUni(targetName);
                    Interop.NtDll.RtlInitUnicodeString(out trustedDomainName, target);

                    // get the trusted domain information
                    uint result = Interop.Advapi32.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, ref buffer);
                    if (result != 0)
                    {
                        uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                        // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                        if (win32Error == STATUS_OBJECT_NAME_NOT_FOUND)
                        {
                            if (isForest)
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                            else
                                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, sourceName, targetName), typeof(TrustRelationshipInformation), null);
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
                    }

                    // get the managed structure representation
                    Interop.Advapi32.TRUSTED_DOMAIN_FULL_INFORMATION domainInfo = *(Interop.Advapi32.TRUSTED_DOMAIN_FULL_INFORMATION*)buffer;

                    // validate the trust attribute first
                    ValidateTrustAttribute(domainInfo.Information!, isForest, sourceName, targetName);

                    // change the attribute value properly
                    AuthData = new LSA_AUTH_INFORMATION();
                    fileTime = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileTime)));
                    Interop.Kernel32.GetSystemTimeAsFileTime(fileTime);

                    // set the time
                    FileTime tmp = new FileTime();
                    Marshal.PtrToStructure(fileTime, tmp);
                    AuthData.LastUpdateTime = new LARGE_INTEGER();
                    AuthData.LastUpdateTime.lowPart = tmp.lower;
                    AuthData.LastUpdateTime.highPart = tmp.higher;

                    AuthData.AuthType = TRUST_AUTH_TYPE_CLEAR;
                    unmanagedPassword = Marshal.StringToHGlobalUni(password);
                    AuthData.AuthInfo = unmanagedPassword;
                    AuthData.AuthInfoLength = password.Length * 2;

                    unmanagedAuthData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LSA_AUTH_INFORMATION)));
                    Marshal.StructureToPtr(AuthData, unmanagedAuthData, false);

                    Interop.Advapi32.TRUSTED_DOMAIN_AUTH_INFORMATION AuthInfoEx;
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
                    domainInfo.Information!.TrustDirection = (int)newTrustDirection;

                    newBuffer = Marshal.AllocHGlobal(sizeof(Interop.Advapi32.TRUSTED_DOMAIN_FULL_INFORMATION));
                    Marshal.StructureToPtr(domainInfo, newBuffer, false);

                    result = Interop.Advapi32.LsaSetTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainFullInformation, newBuffer);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode((int)global::Interop.Advapi32.LsaNtStatusToWinError(result), serverName);
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
                        global::Interop.Advapi32.LsaFreeMemory(buffer);

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

        private static unsafe void ValidateTrust(SafeLsaPolicyHandle handle, global::Interop.UNICODE_STRING trustedDomainName, string? sourceName, string? targetName, bool isForest, int direction, string serverName)
        {
            IntPtr buffer = (IntPtr)0;

            // get trust information
            uint result = Interop.Advapi32.LsaQueryTrustedDomainInfoByName(handle, trustedDomainName, Interop.Advapi32.TRUSTED_INFORMATION_CLASS.TrustedDomainInformationEx, ref buffer);
            if (result != 0)
            {
                uint win32Error = global::Interop.Advapi32.LsaNtStatusToWinError(result);
                // 2 ERROR_FILE_NOT_FOUND <--> 0xc0000034 STATUS_OBJECT_NAME_NOT_FOUND
                if (win32Error == STATUS_OBJECT_NAME_NOT_FOUND)
                {
                    if (isForest)
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                    else
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, sourceName, targetName), typeof(TrustRelationshipInformation), null);
                }
                else
                    throw ExceptionHelper.GetExceptionFromErrorCode((int)win32Error, serverName);
            }

            Debug.Assert(buffer != (IntPtr)0);

            try
            {
                Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX domainInfo = *(Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX*)buffer;

                // validate this is the trust that the user refers to
                ValidateTrustAttribute(domainInfo, isForest, sourceName, targetName);

                // validate trust direction if applicable
                if (direction != 0)
                {
                    if ((direction & domainInfo.TrustDirection) == 0)
                    {
                        if (isForest)
                            throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, sourceName, targetName, (TrustDirection)direction), typeof(ForestTrustRelationshipInformation), null);
                        else
                            throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, sourceName, targetName, (TrustDirection)direction), typeof(TrustRelationshipInformation), null);
                    }
                }
            }
            finally
            {
                if (buffer != (IntPtr)0)
                    global::Interop.Advapi32.LsaFreeMemory(buffer);
            }
        }

        private static void ValidateTrustAttribute(Interop.Advapi32.TRUSTED_DOMAIN_INFORMATION_EX domainInfo, bool isForest, string? sourceName, string? targetName)
        {
            if (isForest)
            {
                // it should be a forest trust, make sure that TRUST_ATTRIBUTE_FOREST_TRANSITIVE bit is set
                if ((domainInfo.TrustAttributes & Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE) == 0)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, sourceName, targetName), typeof(ForestTrustRelationshipInformation), null);
                }
            }
            else
            {
                // it should not be a forest trust, make sure that TRUST_ATTRIBUTE_FOREST_TRANSITIVE bit is not set
                if ((domainInfo.TrustAttributes & Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE) != 0)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongForestTrust, sourceName, targetName), typeof(TrustRelationshipInformation), null);
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
#if NET8_0_OR_GREATER
            return RandomNumberGenerator.GetString(PasswordCharacterSet, PASSWORD_LENGTH);
#else
            char[] cBuf = new char[PASSWORD_LENGTH];
            byte[] randomBuffer = new byte[1];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < PASSWORD_LENGTH; i++)
                {
                    cBuf[i] = PasswordCharacterSet[GetRandomByte(rng, PasswordCharacterSet.Length, randomBuffer)];
                }
            }

            return new string(cBuf);

            // This is a private copy of RandomNumberGenerator.GetInt32 that works
            // on platforms that don't have it. Since this is for a private specific
            // use, it can be simplified to assume some things about the ranges.
            // We know the upper-bound is < 255, so we can avoid converting random
            // bytes to integers and mask a single byte instead. We also know the
            // lower-bound is 0.
            // randomBuffer is a scratch buffer that is populated with random data
            // so we don't allocate an array per-invocation.
            static int GetRandomByte(RandomNumberGenerator rng, int toExclusive, byte[] randomBuffer)
            {
                Debug.Assert(toExclusive > 0 && toExclusive < byte.MaxValue);
                Debug.Assert(randomBuffer.Length == 1);

                // The total possible range is [0, 255).
                int range = toExclusive - 1;

                // Create a mask for only the applicable bits.
                int mask = range;
                mask |= mask >> 1;
                mask |= mask >> 2;
                mask |= mask >> 4;
                // Don't need >> 8 or >> 16 since it is known the range is less than 255.

                int result;

                do
                {
                    rng.GetBytes(randomBuffer);
                    result = mask & randomBuffer[0];
                }
                while (result > range);

                Debug.Assert(result < toExclusive);
                return result;
            }
#endif
        }

        private static IntPtr GetTrustedDomainInfo(DirectoryContext targetContext, string? targetName, bool isForest)
        {
            SafeLsaPolicyHandle? policyHandle = null;
            IntPtr buffer = (IntPtr)0;
            bool impersonated = false;
            string? serverName = null;

            try
            {
                try
                {
                    serverName = Utils.GetPolicyServerName(targetContext, isForest, false, targetName);
                    impersonated = Utils.Impersonate(targetContext);
                    try
                    {
                        policyHandle = Utils.GetPolicyHandle(serverName);
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
                        policyHandle = Utils.GetPolicyHandle(serverName);
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
                        policyHandle = Utils.GetPolicyHandle(serverName);
                    }

                    uint result = global::Interop.Advapi32.LsaQueryInformationPolicy(policyHandle.DangerousGetHandle(), policyDnsDomainInformation, ref buffer);
                    if (result != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode((int)global::Interop.Advapi32.LsaNtStatusToWinError(result), serverName);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.AccountManagement
{
    internal static class Utils
    {
        //
        // byte utilities
        //

        /// <summary>
        /// Performs bytewise comparison of two byte[] arrays
        /// </summary>
        /// <param name="src">Array to compare</param>
        /// <param name="tgt">Array to compare against src</param>
        /// <returns>true if identical, false otherwise</returns>
        internal static bool AreBytesEqual(byte[] src, byte[] tgt)
        {
            if (src.Length != tgt.Length)
                return false;

            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] != tgt[i])
                    return false;
            }

            return true;
        }

        internal static void ClearBit(ref int value, uint bitmask)
        {
            value = (int)(((uint)value) & ((uint)(~bitmask)));
        }

        internal static void SetBit(ref int value, uint bitmask)
        {
            value = (int)(((uint)value) | ((uint)bitmask));
        }

        // {0xa2, 0x3f,...} --> "a23f..."
        internal static string ByteArrayToString(byte[] byteArray)
        {
            StringBuilder stringizedArray = new StringBuilder();
            foreach (byte b in byteArray)
            {
                stringizedArray.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return stringizedArray.ToString();
        }

        // Use this for ldap search filter string...
        internal static string SecurityIdentifierToLdapHexFilterString(SecurityIdentifier sid)
        {
            return (ADUtils.HexStringToLdapHexString(SecurityIdentifierToLdapHexBindingString(sid)));
        }

        // use this for binding string...
        internal static string SecurityIdentifierToLdapHexBindingString(SecurityIdentifier sid)
        {
            byte[] sidB = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidB, 0);
            StringBuilder stringizedBinarySid = new StringBuilder();
            foreach (byte b in sidB)
            {
                stringizedBinarySid.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return stringizedBinarySid.ToString();
        }

        internal static byte[] StringToByteArray(string s)
        {
            if (s.Length % 2 != 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Utils", "StringToByteArray: string has bad length " + s.Length);
                return null;
            }

            byte[] bytes = new byte[s.Length / 2];

            for (int i = 0; i < (s.Length) / 2; i++)
            {
                char firstChar = s[i * 2];
                char secondChar = s[(i * 2) + 1];

                if (((firstChar >= '0' && firstChar <= '9') || (firstChar >= 'A' && firstChar <= 'F') || (firstChar >= 'a' && firstChar <= 'f')) &&
                     ((secondChar >= '0' && secondChar <= '9') || (secondChar >= 'A' && secondChar <= 'F') || (secondChar >= 'a' && secondChar <= 'f')))
                {
                    byte b = byte.Parse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    bytes[i] = b;
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Utils", "StringToByteArray: invalid string: " + s);
                    return null;
                }
            }

            return bytes;
        }

        //
        // SID Utilities
        //

        internal static string ConvertSidToSDDL(byte[] sid)
        {
            // To put the byte[] SID into SDDL, we use ConvertSidToStringSid.
            // Calling that requires we first copy the SID into native memory.
            IntPtr pSid = IntPtr.Zero;

            try
            {
                pSid = ConvertByteArrayToIntPtr(sid);

                if (Interop.Advapi32.ConvertSidToStringSid(pSid, out string sddlSid) != Interop.BOOL.FALSE)
                {
                    return sddlSid;
                }
                else
                {
                    int lastErrorCode = Marshal.GetLastPInvokeError();

                    GlobalDebug.WriteLineIf(
                                      GlobalDebug.Warn,
                                      "Utils",
                                      "ConvertSidToSDDL: ConvertSidToStringSid failed, " + lastErrorCode);
                    return null;
                }
            }
            finally
            {
                if (pSid != IntPtr.Zero)
                    Marshal.FreeHGlobal(pSid);
            }
        }

        // The caller must call Marshal.FreeHGlobal on the returned
        // value to free it.
        internal static IntPtr ConvertByteArrayToIntPtr(byte[] bytes)
        {
            IntPtr pBytes = IntPtr.Zero;

            pBytes = Marshal.AllocHGlobal(bytes.Length);

            try
            {
                Marshal.Copy(bytes, 0, pBytes, bytes.Length);
            }
            catch (Exception e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "ConvertByteArrayToIntPtr: caught exception of type "
                                                   + e.GetType().ToString() +
                                                   " and message " + e.Message);

                Marshal.FreeHGlobal(pBytes);
                throw;
            }

            Debug.Assert(pBytes != IntPtr.Zero);
            return pBytes;
        }


        internal static byte[] ConvertNativeSidToByteArray(IntPtr pSid)
        {
            int sidLength = Interop.Advapi32.GetLengthSid(pSid);
            byte[] sid = new byte[sidLength];
            Marshal.Copy(pSid, sid, 0, sidLength);

            return sid;
        }

        internal static SidType ClassifySID(byte[] sid)
        {
            IntPtr pSid = IntPtr.Zero;

            try
            {
                pSid = ConvertByteArrayToIntPtr(sid);

                return ClassifySID(pSid);
            }
            finally
            {
                if (pSid != IntPtr.Zero)
                    Marshal.FreeHGlobal(pSid);
            }
        }


        internal static SidType ClassifySID(IntPtr pSid)
        {
            Debug.Assert(Interop.Advapi32.IsValidSid(pSid));

            // Get the issuing authority and the first RID
            IntPtr pIdentAuth = Interop.Advapi32.GetSidIdentifierAuthority(pSid);

            Interop.Advapi32.SID_IDENTIFIER_AUTHORITY identAuth =
                (Interop.Advapi32.SID_IDENTIFIER_AUTHORITY)Marshal.PtrToStructure(pIdentAuth, typeof(Interop.Advapi32.SID_IDENTIFIER_AUTHORITY));

            IntPtr pRid = Interop.Advapi32.GetSidSubAuthority(pSid, 0);
            int rid = Marshal.ReadInt32(pRid);

            // These bit signify that the sid was issued by ADAM.  If so then it can't be a fake sid.
            if ((identAuth.b3 & 0xF0) == 0x10)
                return SidType.RealObject;

            // Is it S-1-5-...?
            if (!(identAuth.b1 == 0) &&
                  (identAuth.b2 == 0) &&
                  (identAuth.b3 == 0) &&
                  (identAuth.b4 == 0) &&
                  (identAuth.b5 == 0) &&
                  (identAuth.b6 == 5))
            {
                // No, so it can't be an account or builtin SID.
                // Probably something like \Everyone or \LOCAL.
                return SidType.FakeObject;
            }

            return rid switch
            {
                21 => SidType.RealObject, // Account SID
                32 => SidType.RealObjectFakeDomain, // BUILTIN SID
                _ => SidType.FakeObject,
            };
        }


        internal static int GetLastRidFromSid(IntPtr pSid)
        {
            IntPtr pRidCount = Interop.Advapi32.GetSidSubAuthorityCount(pSid);
            int ridCount = Marshal.ReadByte(pRidCount);
            IntPtr pLastRid = Interop.Advapi32.GetSidSubAuthority(pSid, ridCount - 1);
            int lastRid = Marshal.ReadInt32(pLastRid);

            return lastRid;
        }

        internal static int GetLastRidFromSid(byte[] sid)
        {
            IntPtr pSid = IntPtr.Zero;

            try
            {
                pSid = Utils.ConvertByteArrayToIntPtr(sid);
                int rid = GetLastRidFromSid(pSid);

                return rid;
            }
            finally
            {
                if (pSid != IntPtr.Zero)
                    Marshal.FreeHGlobal(pSid);
            }
        }

        //
        //
        //

        internal static bool IsSamUser()
        {
            //
            // Basic algorithm
            //
            // Get SID of current user (via OpenThreadToken/GetTokenInformation/CloseHandle for TokenUser)
            //
            // Is the user SID of the form S-1-5-21-... (does GetSidIdentityAuthority(u) == 5 and GetSidSubauthority(u, 0) == 21)?
            // If NO ---> is local user
            // If YES --->
            //      Get machine domain SID (via LsaOpenPolicy/LsaQueryInformationPolicy for PolicyAccountDomainInformation/LsaClose)
            //      Does EqualDomainSid indicate the current user SID and the machine domain SID have the same domain?
            //      If YES -->
            //          IS the local machine a DC
            //          If NO --> is local user
            //         If YES --> is _not_ local user
            //      If NO --> is _not_ local user
            //

            IntPtr pCopyOfUserSid = IntPtr.Zero;
            IntPtr pMachineDomainSid = IntPtr.Zero;

            try
            {
                // Get the user's SID
                pCopyOfUserSid = GetCurrentUserSid();

                // Is it of S-1-5-21 form: Is the issuing authority NT_AUTHORITY and the RID NT_NOT_UNIQUE?
                SidType sidType = ClassifySID(pCopyOfUserSid);

                if (sidType == SidType.RealObject)
                {
                    // It's a domain SID.  Now, is the domain portion for the local machine, or something else?

                    // Get the machine domain SID
                    pMachineDomainSid = GetMachineDomainSid();

                    // Does the user SID have the same domain as the machine SID?
                    bool sameDomain = false;
                    bool success = Interop.Advapi32.EqualDomainSid(pCopyOfUserSid, pMachineDomainSid, ref sameDomain);

                    // Since both pCopyOfUserSid and pMachineDomainSid should always be account SIDs
                    Debug.Assert(success);

                    // If user SID is the same domain as the machine domain, and the machine is not a DC then the user is a local (machine) user
                    return sameDomain ? !IsMachineDC(null) : false;
                }
                else
                {
                    // It's not a domain SID, must be local (e.g., NT AUTHORITY\foo, or BUILTIN\foo)
                    return true;
                }
            }
            finally
            {
                if (pCopyOfUserSid != IntPtr.Zero)
                    Marshal.FreeHGlobal(pCopyOfUserSid);

                if (pMachineDomainSid != IntPtr.Zero)
                    Marshal.FreeHGlobal(pMachineDomainSid);
            }
        }


        internal static IntPtr GetCurrentUserSid()
        {
            SafeTokenHandle tokenHandle = null;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                //
                // Get the current user's SID
                //
                int error = 0;

                // Get the current thread's token
                if (!Interop.Advapi32.OpenThreadToken(
                                Interop.Kernel32.GetCurrentThread(),
                                TokenAccessLevels.Query,
                                true,
                                out tokenHandle
                                ))
                {
                    if ((error = Marshal.GetLastPInvokeError()) == 1008) // ERROR_NO_TOKEN
                    {
                        Debug.Assert(tokenHandle.IsInvalid);
                        tokenHandle.Dispose();

                        // Current thread doesn't have a token, try the process
                        if (!Interop.Advapi32.OpenProcessToken(
                                        Interop.Kernel32.GetCurrentProcess(),
                                        (int)TokenAccessLevels.Query,
                                        out tokenHandle
                                        ))
                        {
                            int lastError = Marshal.GetLastPInvokeError();
                            GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "GetCurrentUserSid: OpenProcessToken failed, gle=" + lastError);

                            throw new PrincipalOperationException(SR.Format(SR.UnableToOpenToken, lastError));
                        }
                    }
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "GetCurrentUserSid: OpenThreadToken failed, gle=" + error);

                        throw new PrincipalOperationException(SR.Format(SR.UnableToOpenToken, error));
                    }
                }

                Debug.Assert(!tokenHandle.IsInvalid);

                uint neededBufferSize = 0;

                // Retrieve the user info from the current thread's token
                // First, determine how big a buffer we need.
                bool success = Interop.Advapi32.GetTokenInformation(
                                        tokenHandle.DangerousGetHandle(),
                                        (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenUser,
                                        IntPtr.Zero,
                                        0,
                                        out neededBufferSize);

                int getTokenInfoError = 0;
                if ((getTokenInfoError = Marshal.GetLastPInvokeError()) != 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "GetCurrentUserSid: GetTokenInformation (1st try) failed, gle=" + getTokenInfoError);

                    throw new PrincipalOperationException(
                                    SR.Format(SR.UnableToRetrieveTokenInfo, getTokenInfoError));
                }

                // Allocate the necessary buffer.
                Debug.Assert(neededBufferSize > 0);
                pBuffer = Marshal.AllocHGlobal((int)neededBufferSize);

                // Load the user info into the buffer
                success = Interop.Advapi32.GetTokenInformation(
                                        tokenHandle.DangerousGetHandle(),
                                        (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenUser,
                                        pBuffer,
                                        neededBufferSize,
                                        out neededBufferSize);

                if (!success)
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                      "Utils",
                                      "GetCurrentUserSid: GetTokenInformation (2nd try) failed, neededBufferSize=" + neededBufferSize + ", gle=" + lastError);

                    throw new PrincipalOperationException(
                                    SR.Format(SR.UnableToRetrieveTokenInfo, lastError));
                }

                // Retrieve the user's SID from the user info
                Interop.TOKEN_USER tokenUser = (Interop.TOKEN_USER)Marshal.PtrToStructure(pBuffer, typeof(Interop.TOKEN_USER));
                IntPtr pUserSid = tokenUser.sidAndAttributes.Sid;   // this is a reference into the NATIVE memory (into pBuffer)

                Debug.Assert(Interop.Advapi32.IsValidSid(pUserSid));

                // Now we make a copy of the SID to return
                int userSidLength = Interop.Advapi32.GetLengthSid(pUserSid);
                IntPtr pCopyOfUserSid = Marshal.AllocHGlobal(userSidLength);
                success = Interop.Advapi32.CopySid(userSidLength, pCopyOfUserSid, pUserSid);
                if (!success)
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                      "Utils",
                                      "GetCurrentUserSid: CopySid failed, errorcode=" + lastError);

                    throw new PrincipalOperationException(
                                    SR.Format(SR.UnableToRetrieveTokenInfo, lastError));
                }

                return pCopyOfUserSid;
            }
            finally
            {
                tokenHandle?.Dispose();

                if (pBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(pBuffer);
            }
        }


        internal static IntPtr GetMachineDomainSid()
        {
            SafeLsaPolicyHandle policyHandle = null;
            IntPtr pBuffer = IntPtr.Zero;

            try
            {
                Interop.OBJECT_ATTRIBUTES oa = default;

                uint err = Interop.Advapi32.LsaOpenPolicy(
                                SystemName: null,
                                ref oa,
                                (int)Interop.Advapi32.PolicyRights.POLICY_VIEW_LOCAL_INFORMATION,
                                out policyHandle);
                if (err != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "GetMachineDomainSid: LsaOpenPolicy failed, gle=" + Interop.Advapi32.LsaNtStatusToWinError(err));

                    throw new PrincipalOperationException(SR.Format(
                                                               SR.UnableToRetrievePolicy,
                                                               Interop.Advapi32.LsaNtStatusToWinError(err)));
                }

                Debug.Assert(!policyHandle.IsInvalid);
                err = Interop.Advapi32.LsaQueryInformationPolicy(
                                policyHandle.DangerousGetHandle(),
                                5,              // PolicyAccountDomainInformation
                                ref pBuffer);

                if (err != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "GetMachineDomainSid: LsaQueryInformationPolicy failed, gle=" + Interop.Advapi32.LsaNtStatusToWinError(err));

                    throw new PrincipalOperationException(SR.Format(
                                                               SR.UnableToRetrievePolicy,
                                                               Interop.Advapi32.LsaNtStatusToWinError(err)));
                }

                Debug.Assert(pBuffer != IntPtr.Zero);
                UnsafeNativeMethods.POLICY_ACCOUNT_DOMAIN_INFO info = (UnsafeNativeMethods.POLICY_ACCOUNT_DOMAIN_INFO)
                                    Marshal.PtrToStructure(pBuffer, typeof(UnsafeNativeMethods.POLICY_ACCOUNT_DOMAIN_INFO));

                Debug.Assert(Interop.Advapi32.IsValidSid(info.DomainSid));

                // Now we make a copy of the SID to return
                int sidLength = Interop.Advapi32.GetLengthSid(info.DomainSid);
                IntPtr pCopyOfSid = Marshal.AllocHGlobal(sidLength);
                bool success = Interop.Advapi32.CopySid(sidLength, pCopyOfSid, info.DomainSid);
                if (!success)
                {
                    int lastError = Marshal.GetLastPInvokeError();
                    GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                      "Utils",
                                      "GetMachineDomainSid: CopySid failed, errorcode=" + lastError);

                    throw new PrincipalOperationException(
                                    SR.Format(SR.UnableToRetrievePolicy, lastError));
                }

                return pCopyOfSid;
            }
            finally
            {
                policyHandle?.Dispose();

                if (pBuffer != IntPtr.Zero)
                    Interop.Advapi32.LsaFreeMemory(pBuffer);
            }
        }

        // Returns name in the form "domain\user"
        internal static string GetNT4UserName()
        {
            using (WindowsIdentity currentIdentity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                string s = currentIdentity.Name;
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "GetNT4UserName: name is " + s);
                return s;
            }
        }

        internal static string GetComputerFlatName()
        {
            //string s = System.Windows.Forms.SystemInformation.ComputerName;
            string s = Environment.MachineName;
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "GetComputerFlatName: name is " + s);

            return s;
        }

        //
        // Interop support
        //

        internal static UnsafeNativeMethods.DomainControllerInfo GetDcName(string computerName, string domainName, string siteName, int flags)
        {
            IntPtr domainControllerInfoPtr = IntPtr.Zero;

            try
            {
                int err = Interop.Logoncli.DsGetDcName(computerName, domainName, IntPtr.Zero, siteName, flags, out domainControllerInfoPtr);

                if (err != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "GetDcName: DsGetDcName failed, err=" + err);
                    throw new PrincipalOperationException(
                                    SR.Format(
                                            SR.UnableToRetrieveDomainInfo,
                                            err),
                                    err);
                }

                UnsafeNativeMethods.DomainControllerInfo domainControllerInfo =
                    (UnsafeNativeMethods.DomainControllerInfo)Marshal.PtrToStructure(domainControllerInfoPtr, typeof(UnsafeNativeMethods.DomainControllerInfo));

                return domainControllerInfo;
            }
            finally
            {
                if (domainControllerInfoPtr != IntPtr.Zero)
                    Interop.Netutils.NetApiBufferFree(domainControllerInfoPtr);
            }
        }

        internal static unsafe int LookupSid(string serverName, NetCred credentials, byte[] sid, out string name, out string domainName, out int accountUsage)
        {
            int nameLength = 0;
            int domainNameLength = 0;

            accountUsage = 0;
            name = null;
            domainName = null;

            IntPtr hUser = IntPtr.Zero;

            try
            {
                Utils.BeginImpersonation(credentials, out hUser);

                // hUser could be null if no credentials were specified
                Debug.Assert(hUser != IntPtr.Zero ||
                                (credentials == null || (credentials.UserName == null && credentials.Password == null)));

                int f = Interop.Advapi32.LookupAccountSid(serverName, sid, null, ref nameLength, null, ref domainNameLength, out accountUsage);

                int lastErr = Marshal.GetLastPInvokeError();
                if (lastErr != 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "LookupSid: LookupAccountSid (1st try) failed, gle=" + lastErr);
                    return lastErr;
                }

                Debug.Assert(f == 0);   // should never succeed, with a 0 buffer size

                Debug.Assert(nameLength > 0);
                Debug.Assert(domainNameLength > 0);

                fixed (char* sbName = new char[nameLength])
                fixed (char* sbDomainName = new char[domainNameLength])
                {
                    f = Interop.Advapi32.LookupAccountSid(serverName, sid, sbName, ref nameLength, sbDomainName, ref domainNameLength, out accountUsage);

                    if (f == 0)
                    {
                        lastErr = Marshal.GetLastPInvokeError();
                        Debug.Assert(lastErr != 0);

                        GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "LookupSid: LookupAccountSid (2nd try) failed, gle=" + lastErr);
                        return lastErr;
                    }

                    name = new string(sbName);
                    domainName = new string(sbDomainName);
                }

                return 0;
            }
            finally
            {
                if (hUser != IntPtr.Zero)
                    Utils.EndImpersonation(hUser);
            }
        }


        internal static Principal ConstructFakePrincipalFromSID(
                                                            byte[] sid,
                                                            PrincipalContext ctx,
                                                            string serverName,
                                                            NetCred credentials,
                                                            string authorityName)
        {
            GlobalDebug.WriteLineIf(
                        GlobalDebug.Info,
                        "Utils",
                        "ConstructFakePrincipalFromSID: Build principal for SID={0}, server={1}, authority={2}",
                        Utils.ByteArrayToString(sid),
                        serverName ?? "NULL",
                        authorityName ?? "NULL");

            Debug.Assert(ClassifySID(sid) == SidType.FakeObject);

            // Get the name for it
            string nt4Name = "";

            int accountUsage = 0;
            string name;
            string domainName;

            int err = Utils.LookupSid(serverName, credentials, sid, out name, out domainName, out accountUsage);
            if (err == 0)
            {
                // If it failed, we'll just live without a name
                //Debug.Assert(accountUsage == 5 /*WellKnownGroup*/);
                nt4Name = (!string.IsNullOrEmpty(domainName) ? domainName + "\\" : "") + name;
            }
            else
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Warn,
                            "Utils",
                            "ConstructFakePrincipalFromSID: LookupSid failed (ignoring), serverName=" + serverName + ", err=" + err);
            }

            // Since LookupAccountSid indicates all of the NT AUTHORITY, etc., SIDs are WellKnownGroups,
            // we'll map them all to Group.

            // Create a Principal object to represent it
            GroupPrincipal g = GroupPrincipal.MakeGroup(ctx);

            g.fakePrincipal = true;
            g.unpersisted = false;

            // Set the display name on the object
            g.LoadValueIntoProperty(PropertyNames.PrincipalDisplayName, nt4Name);

            // Set the display name on the object
            g.LoadValueIntoProperty(PropertyNames.PrincipalName, name);

            // Set the display name on the object
            g.LoadValueIntoProperty(PropertyNames.PrincipalSamAccountName, name);

            // SID IdentityClaim
            SecurityIdentifier sidObj = new SecurityIdentifier(Utils.ConvertSidToSDDL(sid));

            // Set the display name on the object
            g.LoadValueIntoProperty(PropertyNames.PrincipalSid, sidObj);

            g.LoadValueIntoProperty(PropertyNames.GroupIsSecurityGroup, true);
            return g;
        }

        //
        // Impersonation
        //
        internal static bool BeginImpersonation(NetCred credential, out IntPtr hUserToken)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "Entering BeginImpersonation");

            hUserToken = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;

            // default credential is specified, no need to do impersonation
            if (credential == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "BeginImpersonation: nothing to impersonate");
                return false;
            }

            // Retrieve the parsed username which has had the domain removed because LogonUser
            // expects creds this way.
            string userName = credential.ParsedUserName;
            string password = credential.Password;
            string domainName = credential.Domain;

            // no need to do impersonation as username and password are both null
            if (userName == null && password == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "BeginImpersonation: nothing to impersonate (2)");
                return false;
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "BeginImpersonation: trying to impersonate " + userName);

            int result = Interop.Advapi32.LogonUser(
                                            userName,
                                            domainName,
                                            password,
                                            9, /* LOGON32_LOGON_NEW_CREDENTIALS */
                                            3, /* LOGON32_PROVIDER_WINNT50 */
                                            ref hToken);
            // check the result
            if (result == 0)
            {
                int lastError = Marshal.GetLastPInvokeError();
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "BeginImpersonation: LogonUser failed, gle=" + lastError);

                throw new PrincipalOperationException(
                    SR.Format(SR.UnableToImpersonateCredentials, lastError));
            }

            result = Interop.Advapi32.ImpersonateLoggedOnUser(hToken);
            if (result == 0)
            {
                int lastError = Marshal.GetLastPInvokeError();
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "BeginImpersonation: ImpersonateLoggedOnUser failed, gle=" + lastError);

                // Close the token the was created above....
                Interop.Kernel32.CloseHandle(hToken);

                throw new PrincipalOperationException(
                    SR.Format(SR.UnableToImpersonateCredentials, lastError));
            }

            hUserToken = hToken;
            return true;
        }

        internal static void EndImpersonation(IntPtr hUserToken)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Utils", "Entering EndImpersonation");

            Interop.Advapi32.RevertToSelf();
            Interop.Kernel32.CloseHandle(hUserToken);
        }

        internal static bool IsMachineDC(string computerName)
        {
            IntPtr dsRoleInfoPtr = IntPtr.Zero;
            int err = -1;

            try
            {
                err = Interop.Dsrole.DsRoleGetPrimaryDomainInformation(computerName, Interop.Dsrole.DSROLE_PRIMARY_DOMAIN_INFO_LEVEL.DsRolePrimaryDomainInfoBasic, out dsRoleInfoPtr);

                if (err != 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Error, "Utils", "IsMachineDC: DsRoleGetPrimaryDomainInformation failed, err=" + err);
                    throw new PrincipalOperationException(
                                    SR.Format(
                                            SR.UnableToRetrieveDomainInfo,
                                            err));
                }

                UnsafeNativeMethods.DSROLE_PRIMARY_DOMAIN_INFO_BASIC dsRolePrimaryDomainInfo =
                    (UnsafeNativeMethods.DSROLE_PRIMARY_DOMAIN_INFO_BASIC)Marshal.PtrToStructure(dsRoleInfoPtr, typeof(UnsafeNativeMethods.DSROLE_PRIMARY_DOMAIN_INFO_BASIC));

                return (dsRolePrimaryDomainInfo.MachineRole == UnsafeNativeMethods.DSROLE_MACHINE_ROLE.DsRole_RoleBackupDomainController ||
                             dsRolePrimaryDomainInfo.MachineRole == UnsafeNativeMethods.DSROLE_MACHINE_ROLE.DsRole_RolePrimaryDomainController);
            }
            finally
            {
                if (dsRoleInfoPtr != IntPtr.Zero)
                    Interop.Dsrole.DsRoleFreeMemory(dsRoleInfoPtr);
            }
        }
    }
}

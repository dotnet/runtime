// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Thread
    {
        internal static void UninterruptibleSleep0() => Interop.Kernel32.Sleep(0);

#if !CORECLR
        private static void SleepInternal(int millisecondsTimeout)
        {
            Debug.Assert(millisecondsTimeout >= -1);
            Interop.Kernel32.Sleep((uint)millisecondsTimeout);
        }
#endif

        internal static int GetCurrentProcessorNumber()
        {
            Interop.Kernel32.PROCESSOR_NUMBER procNumber;
            Interop.Kernel32.GetCurrentProcessorNumberEx(out procNumber);
            return (procNumber.Group << 6) | procNumber.Number;
        }

        internal readonly ref struct CurrentUserSecurityDescriptorInfo
        {
            private readonly SafeTokenHandle _token;
            private readonly SafeLocalAllocHandle _tokenUser;
            private readonly SafeLocalAllocHandle _dacl;
            private readonly SafeLocalAllocHandle _sacl;
            private readonly SafeLocalAllocHandle _securityDescriptor;

            public SafeLocalAllocHandle TokenUser
            {
                get
                {
                    Debug.Assert(_securityDescriptor is not null);
                    return _tokenUser;
                }
            }

            public nint SecurityDescriptor
            {
                get
                {
                    Debug.Assert(_securityDescriptor is not null);
                    return _securityDescriptor.DangerousGetHandle();
                }
            }

            public CurrentUserSecurityDescriptorInfo(int accessMask)
            {
                this = default; // zero-initialize first in case of exception
                _token = OpenCurrentToken();
                try
                {
                    _tokenUser = GetTokenUser(_token);
                    nint tokenUserSid = GetTokenUserSid(_tokenUser);
                    _dacl = CreateDacl(tokenUserSid, accessMask);
                    _sacl = CreateMandatoryLabelAceSacl(_token);
                    _securityDescriptor = CreateSecurityDescriptor(tokenUserSid, _dacl, _sacl);
                }
                catch
                {
                    _securityDescriptor?.Dispose();
                    _sacl?.Dispose();
                    _dacl?.Dispose();
                    _tokenUser?.Dispose();
                    _token.Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                if (_securityDescriptor is null)
                {
                    return;
                }

                _securityDescriptor.Dispose();
                _sacl.Dispose();
                _dacl.Dispose();
                _tokenUser.Dispose();
                _token.Dispose();
            }

            private static SafeTokenHandle OpenCurrentToken()
            {
                nint threadHandle = Interop.Kernel32.GetCurrentThread();
                if (Interop.Advapi32.OpenThreadToken(
                        threadHandle,
                        (int)Interop.Advapi32.TOKEN_ACCESS_LEVELS.Query,
                        OpenAsSelf: true,
                        out SafeTokenHandle token))
                {
                    return token;
                }

                token.Dispose();
                if (Interop.Advapi32.OpenThreadToken(
                        threadHandle,
                        (int)Interop.Advapi32.TOKEN_ACCESS_LEVELS.Query,
                        OpenAsSelf: false,
                        out token))
                {
                    return token;
                }

                token.Dispose();
                if (!Interop.Advapi32.OpenProcessToken(
                        Interop.Kernel32.GetCurrentProcess(),
                        (int)Interop.Advapi32.TOKEN_ACCESS_LEVELS.Query,
                        out token))
                {
                    int error = Marshal.GetLastPInvokeError();
                    token.Dispose();
                    ThrowExceptionForError(error);
                }

                return token;
            }

            private static SafeLocalAllocHandle GetTokenUser(SafeTokenHandle token)
            {
                // Get the buffer size needed for user info
                Interop.Advapi32.GetTokenInformation(
                    token.DangerousGetHandle(),
                    (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenUser,
                    TokenInformation: 0,
                    TokenInformationLength: 0,
                    out uint tokenUserSize);
                int error = Marshal.GetLastPInvokeError();
                if (error != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                {
                    ThrowExceptionForError(error);
                }

                Debug.Assert((int)tokenUserSize > 0);
                SafeLocalAllocHandle tokenUser = SafeLocalAllocHandle.LocalAlloc((int)tokenUserSize);
                try
                {
                    if (!Interop.Advapi32.GetTokenInformation(
                            token.DangerousGetHandle(),
                            (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenUser,
                            tokenUser.DangerousGetHandle(),
                            tokenUserSize,
                            out tokenUserSize))
                    {
                        ThrowExceptionForLastError();
                    }

                    return tokenUser;
                }
                catch
                {
                    tokenUser.Dispose();
                    throw;
                }
            }

            private static nint GetTokenUserSid(SafeLocalAllocHandle tokenUser) =>
                tokenUser.Read<nint>(0); // TOKEN_USER.User.Sid

            private static unsafe SafeLocalAllocHandle CreateDacl(nint tokenUserSid, int userAccessMask)
            {
                // Create an SID for the BUILTIN\Administrators group
                uint adminGroupSidSize = Interop.Advapi32.SECURITY_MAX_SID_SIZE;
                byte* adminGroupSid = stackalloc byte[(int)adminGroupSidSize];
                if (!Interop.Advapi32.CreateWellKnownSid(
                        (int)Interop.Advapi32.WELL_KNOWN_SID_TYPE.WinBuiltinAdministratorsSid,
                        domainSid: 0,
                        (nint)adminGroupSid,
                        ref adminGroupSidSize))
                {
                    ThrowExceptionForLastError();
                }

                Interop.Advapi32.EXPLICIT_ACCESS* ea = stackalloc Interop.Advapi32.EXPLICIT_ACCESS[2] { default, default };

                // Allow the requested access rights for the token user
                ea[0].grfAccessPermissions = userAccessMask;
                ea[0].grfAccessMode = Interop.Advapi32.ACCESS_MODE.SET_ACCESS;
                ea[0].grfInheritance = Interop.Advapi32.EXPLICIT_ACCESS_NO_INHERITANCE;
                ea[0].Trustee.TrusteeForm = Interop.Advapi32.TRUSTEE_FORM.TRUSTEE_IS_SID;
                ea[0].Trustee.TrusteeType = Interop.Advapi32.TRUSTEE_TYPE.TRUSTEE_IS_USER;
                ea[0].Trustee.ptstrName = tokenUserSid;

                // Allow READ_CONTROL for the BUILTIN\Administrators group to enable administrators to read object permissions
                // using a tool like SysInternals accesschk
                ea[1].grfAccessPermissions = Interop.Kernel32.READ_CONTROL;
                ea[1].grfAccessMode = Interop.Advapi32.ACCESS_MODE.SET_ACCESS;
                ea[1].grfInheritance = Interop.Advapi32.EXPLICIT_ACCESS_NO_INHERITANCE;
                ea[1].Trustee.TrusteeForm = Interop.Advapi32.TRUSTEE_FORM.TRUSTEE_IS_SID;
                ea[1].Trustee.TrusteeType = Interop.Advapi32.TRUSTEE_TYPE.TRUSTEE_IS_GROUP;
                ea[1].Trustee.ptstrName = (nint)adminGroupSid;

                int error =
                    Interop.Advapi32.SetEntriesInAcl(cCountOfExplicitEntries: 2, ea, OldAcl: 0, out SafeLocalAllocHandle dacl);
                if (error != Interop.Errors.ERROR_SUCCESS)
                {
                    dacl.Dispose();
                    if (error == Interop.Errors.ERROR_PROC_NOT_FOUND)
                    {
                        // This error may result when the current token has reduced privileges due to other access restrictions
                        // during delay loading. Other ACL APIs result in access-denied in this case, which is more clear, so
                        // treat it as such.
                        error = Interop.Errors.ERROR_ACCESS_DENIED;
                    }

                    ThrowExceptionForError(error);
                }

                return dacl;
            }

            private static unsafe SafeLocalAllocHandle CreateMandatoryLabelAceSacl(SafeTokenHandle token)
            {
                // By default, the system assigns a mandatory label of an appropriate integrity level with
                // SYSTEM_MANDATORY_LABEL_NO_WRITE_UP to new objects, which means relevant lower-integrity processes cannot get
                // write access to the object. This is not sufficient for named synchronization objects though, for named
                // mutexes for instance, the SYNCHRONIZE access right is enough to acquire and release a named mutex, but it's
                // not classified as write access. We need to explicitly assign a mandatory label that also has
                // SYSTEM_MANDATORY_LABEL_NO_READ_UP to prevent relevant lower-integrity processes from operating on the object.

                // Get the token's integrity level
                using SafeLocalAllocHandle tokenMandatoryLabel = GetTokenMandatoryLabel(token);
                nint tokenIntegrityLevelSid = tokenMandatoryLabel.Read<nint>(0); // TOKEN_MANDATORY_LABEL.Label.Sid
                uint tokenIntegrityLevel = GetIntegrityLevel(tokenIntegrityLevelSid);

                uint integrityLevelSidSize;
                byte* integrityLevelSidBuffer = stackalloc byte[Interop.Advapi32.SECURITY_MAX_SID_SIZE];
                nint integrityLevelSid;
                if (tokenIntegrityLevel >= Interop.Advapi32.SECURITY_MANDATORY_MEDIUM_RID)
                {
                    // For Medium or higher token integrity levels, the default integrity level for new objects is Medium for
                    // interoperability, for instance when UAC is enabled, between elevated and unelevated processes running as
                    // an admin user, or in the presence of changes to UAC enablement. The goal is mainly to restrict processes
                    // running at integrity levels lower than Medium from interacting with objects at Medium or higher integrity
                    // levels. The default integrity level is retained here.

                    // Create a SID for the Medium integrity level
                    integrityLevelSidSize = Interop.Advapi32.SECURITY_MAX_SID_SIZE;
                    if (!Interop.Advapi32.CreateWellKnownSid(
                            (int)Interop.Advapi32.WELL_KNOWN_SID_TYPE.WinMediumLabelSid,
                            domainSid: 0,
                            (nint)integrityLevelSidBuffer,
                            ref integrityLevelSidSize))
                    {
                        ThrowExceptionForLastError();
                    }

                    integrityLevelSid = (nint)integrityLevelSidBuffer;
                }
                else
                {
                    // For integrity levels less than Medium, just transfer the token's integrity level to the object. For
                    // instance, if the token's integrity level is greater than Low and less than Medium, it may be undesirable
                    // to allow a process with an integrity level of Low to interoperate with our objects.
                    integrityLevelSid = tokenIntegrityLevelSid;
                    integrityLevelSidSize = (uint)Interop.Advapi32.GetLengthSid(integrityLevelSid);
                }

                Debug.Assert((int)integrityLevelSidSize > 0);
                int saclSize =
                    sizeof(Interop.Advapi32.ACL) +
                    sizeof(Interop.Advapi32.ACE) - Interop.Advapi32.ACE.SizeOfSidPortionInAce +
                    (int)integrityLevelSidSize;
                saclSize = (saclSize + 3) & ~3; // must be DWORD-aligned
                SafeLocalAllocHandle sacl = SafeLocalAllocHandle.LocalAlloc(saclSize);
                try
                {
                    if (!Interop.Advapi32.InitializeAcl(sacl.DangerousGetHandle(), saclSize, Interop.Advapi32.ACL_REVISION))
                    {
                        ThrowExceptionForLastError();
                    }

                    if (!Interop.Advapi32.AddMandatoryAce(
                            sacl.DangerousGetHandle(),
                            Interop.Advapi32.ACL_REVISION,
                            AceFlags: 0,
                            (
                                Interop.Advapi32.SYSTEM_MANDATORY_LABEL_NO_WRITE_UP |
                                Interop.Advapi32.SYSTEM_MANDATORY_LABEL_NO_READ_UP
                            ),
                            integrityLevelSid))
                    {
                        ThrowExceptionForLastError();
                    }

                    return sacl;
                }
                catch
                {
                    sacl.Dispose();
                    throw;
                }
            }

            private static SafeLocalAllocHandle GetTokenMandatoryLabel(SafeTokenHandle token)
            {
                // Get the buffer size needed for integrity level info
                Interop.Advapi32.GetTokenInformation(
                    token.DangerousGetHandle(),
                    (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel,
                    TokenInformation: 0,
                    TokenInformationLength: 0,
                    out uint tokenMandatoryLabelSize);
                int error = Marshal.GetLastPInvokeError();
                if (error != Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
                {
                    ThrowExceptionForError(error);
                }

                Debug.Assert((int)tokenMandatoryLabelSize > 0);
                SafeLocalAllocHandle tokenMandatoryLabel = SafeLocalAllocHandle.LocalAlloc((int)tokenMandatoryLabelSize);
                try
                {
                    if (!Interop.Advapi32.GetTokenInformation(
                            token.DangerousGetHandle(),
                            (uint)Interop.Advapi32.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel,
                            tokenMandatoryLabel.DangerousGetHandle(),
                            tokenMandatoryLabelSize,
                            out tokenMandatoryLabelSize))
                    {
                        ThrowExceptionForLastError();
                    }

                    return tokenMandatoryLabel;
                }
                catch
                {
                    tokenMandatoryLabel.Dispose();
                    throw;
                }
            }

            private static unsafe uint GetIntegrityLevel(nint integrityLevelSid)
            {
                // The last sub-authority has the RID that represents the integrity level
                byte subAuthorityCount =
                    Unsafe.Read<byte>((void*)Interop.Advapi32.GetSidSubAuthorityCount(integrityLevelSid));
                Debug.Assert(subAuthorityCount != 0);
                return Unsafe.Read<uint>((void*)Interop.Advapi32.GetSidSubAuthority(integrityLevelSid, subAuthorityCount - 1));
            }

            private static SafeLocalAllocHandle CreateSecurityDescriptor(
                nint tokenUserSid,
                SafeLocalAllocHandle dacl,
                SafeLocalAllocHandle? sacl)
            {
                SafeLocalAllocHandle securityDescriptor =
                    SafeLocalAllocHandle.LocalAlloc(Interop.Advapi32.SECURITY_DESCRIPTOR_MIN_LENGTH);
                try
                {
                    if (!Interop.Advapi32.InitializeSecurityDescriptor(
                            securityDescriptor.DangerousGetHandle(),
                            Interop.Advapi32.SECURITY_DESCRIPTOR_REVISION))
                    {
                        ThrowExceptionForLastError();
                    }

                    if (!Interop.Advapi32.SetSecurityDescriptorOwner(
                            securityDescriptor.DangerousGetHandle(),
                            tokenUserSid,
                            bOwnerDefaulted: false))
                    {
                        ThrowExceptionForLastError();
                    }

                    if (!Interop.Advapi32.SetSecurityDescriptorGroup(
                        securityDescriptor.DangerousGetHandle(),
                        tokenUserSid,
                        bGroupDefaulted: false))
                    {
                        ThrowExceptionForLastError();
                    }

                    if (!Interop.Advapi32.SetSecurityDescriptorDacl(
                            securityDescriptor.DangerousGetHandle(),
                            bDaclPresent: true,
                            dacl.DangerousGetHandle(),
                            bDaclDefaulted: false))
                    {
                        ThrowExceptionForLastError();
                    }

                    if (sacl is not null &&
                        !Interop.Advapi32.SetSecurityDescriptorSacl(
                            securityDescriptor.DangerousGetHandle(),
                            bSaclPresent: true,
                            sacl.DangerousGetHandle(),
                            bSaclDefaulted: false))
                    {
                        ThrowExceptionForLastError();
                    }

                    return securityDescriptor;
                }
                catch
                {
                    securityDescriptor.Dispose();
                    throw;
                }
            }

            public static bool IsValidSecurityDescriptor(SafeWaitHandle objectHandle, int modifyStateAccessMask)
            {
                using SafeTokenHandle token = OpenCurrentToken();
                using SafeLocalAllocHandle tokenUser = GetTokenUser(token);
                return IsSecurityDescriptorCompatible(tokenUser, objectHandle, modifyStateAccessMask);
            }

            public static unsafe bool IsSecurityDescriptorCompatible(
                SafeLocalAllocHandle tokenUser,
                SafeWaitHandle objectHandle,
                int modifyStateAccessMask)
            {
                Debug.Assert(
                    modifyStateAccessMask == Interop.Kernel32.MUTEX_MODIFY_STATE ||
                    modifyStateAccessMask == Interop.Kernel32.SEMAPHORE_MODIFY_STATE ||
                    modifyStateAccessMask == Interop.Kernel32.EVENT_MODIFY_STATE);

                nint tokenUserSid = GetTokenUserSid(tokenUser);

                // Get the object's relevant security information
                using var securityDescriptor = new SafeLocalAllocHandle();
                nint ownerSid = 0;
                Interop.Advapi32.ACL* dacl = null;
                nint securityDescriptorNInt = 0;
                int error =
                    (int)
                    Interop.Advapi32.GetSecurityInfoByHandle(
                        objectHandle,
                        (int)Interop.Advapi32.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        Interop.Advapi32.OWNER_SECURITY_INFORMATION | Interop.Advapi32.DACL_SECURITY_INFORMATION,
                        &ownerSid,
                        null, // sidGroup
                        (nint*)(&dacl),
                        null, // sacl
                        &securityDescriptorNInt);
                if (error != Interop.Errors.ERROR_SUCCESS)
                {
                    ThrowExceptionForError(error);
                }

                securityDescriptor.SetHandle(securityDescriptorNInt);

                // The owner must be the same as the token user
                if (!Interop.Advapi32.EqualSid(ownerSid, tokenUserSid))
                {
                    return false;
                }

                // Any access-allowed ACE that allows manipulating the object must be for the token user
                int anyWriteAccessMask =
                    Interop.Kernel32.WRITE_DAC |
                    Interop.Kernel32.WRITE_OWNER |
                    Interop.Kernel32.SYNCHRONIZE |
                    modifyStateAccessMask;
                for (int i = 0, n = dacl->AceCount; i < n; i++)
                {
                    if (!Interop.Advapi32.GetAce(dacl, i, out Interop.Advapi32.ACE* ace))
                    {
                        ThrowExceptionForError(error);
                    }

                    if (ace->Header.AceType == Interop.Advapi32.ACCESS_ALLOWED_ACE_TYPE &&
                        (ace->Mask & anyWriteAccessMask) != 0 &&
                        !Interop.Advapi32.EqualSid((nint)(&ace->SidStart), tokenUserSid))
                    {
                        return false;
                    }
                }

                return true;
            }

            [DoesNotReturn]
            private static void ThrowExceptionForLastError() => ThrowExceptionForError(Marshal.GetLastPInvokeError());

            [DoesNotReturn]
            private static void ThrowExceptionForError(int error) => throw Win32Marshal.GetExceptionForWin32Error(error);
        }
    }
}

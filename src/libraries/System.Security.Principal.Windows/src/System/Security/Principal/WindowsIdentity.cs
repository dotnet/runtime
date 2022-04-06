// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading;

using KERB_LOGON_SUBMIT_TYPE = Interop.SspiCli.KERB_LOGON_SUBMIT_TYPE;
using KERB_S4U_LOGON = Interop.SspiCli.KERB_S4U_LOGON;
using KerbS4uLogonFlags = Interop.SspiCli.KerbS4uLogonFlags;
using LUID = Interop.LUID;
using LSA_STRING = Interop.Advapi32.LSA_STRING;
using QUOTA_LIMITS = Interop.SspiCli.QUOTA_LIMITS;
using SECURITY_LOGON_TYPE = Interop.SspiCli.SECURITY_LOGON_TYPE;
using TOKEN_SOURCE = Interop.SspiCli.TOKEN_SOURCE;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace System.Security.Principal
{
    public enum WindowsAccountType
    {
        Normal = 0,
        Guest = 1,
        System = 2,
        Anonymous = 3
    }

    public class WindowsIdentity : ClaimsIdentity, IDisposable, ISerializable, IDeserializationCallback
    {
        private static SecurityIdentifier? s_authenticatedUserRid;
        private static SecurityIdentifier? s_domainRid;
        private static SecurityIdentifier? s_localSystemRid;
        private static SecurityIdentifier? s_anonymousRid;

        private string? _name;
        private SecurityIdentifier? _owner;
        private SecurityIdentifier? _user;
        private IdentityReferenceCollection? _groups;

        private SafeAccessTokenHandle _safeTokenHandle = SafeAccessTokenHandle.InvalidHandle;
        private readonly string? _authType;
        private int _isAuthenticated = -1;
        private volatile TokenImpersonationLevel _impersonationLevel;
        private volatile bool _impersonationLevelInitialized;

        public new const string DefaultIssuer = @"AD AUTHORITY";
        private readonly string _issuerName = DefaultIssuer;
        private object? _claimsIntiailizedLock;
        private bool _claimsInitialized;
        private List<Claim>? _deviceClaims;
        private List<Claim>? _userClaims;

        private static bool s_ignoreWindows8Properties;

        public WindowsIdentity(IntPtr userToken) : this(userToken, null, -1) { }

        public WindowsIdentity(IntPtr userToken, string type) : this(userToken, type, -1) { }

        // The actual accType is ignored and always will be retrieved from the system.
        public WindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType) : this(userToken, type, -1) { }

        public WindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType, bool isAuthenticated)
            : this(userToken, type, isAuthenticated ? 1 : 0) { }

        protected WindowsIdentity(WindowsIdentity identity)
            : base(identity, null, GetAuthType(identity), null, null)
        {
            bool mustDecrement = false;

            try
            {
                if (!identity._safeTokenHandle.IsInvalid && identity._safeTokenHandle != SafeAccessTokenHandle.InvalidHandle && identity._safeTokenHandle.DangerousGetHandle() != IntPtr.Zero)
                {
                    identity._safeTokenHandle.DangerousAddRef(ref mustDecrement);

                    if (!identity._safeTokenHandle.IsInvalid && identity._safeTokenHandle.DangerousGetHandle() != IntPtr.Zero)
                        CreateFromToken(identity._safeTokenHandle.DangerousGetHandle());

                    _authType = identity._authType;
                    _isAuthenticated = identity._isAuthenticated;
                }
            }
            finally
            {
                if (mustDecrement)
                    identity._safeTokenHandle.DangerousRelease();
            }
        }

        private WindowsIdentity(IntPtr userToken, string? authType, int isAuthenticated)
            : base(null, null, null, ClaimTypes.Name, ClaimTypes.GroupSid)
        {
            CreateFromToken(userToken);
            _authType = authType;
            _isAuthenticated = isAuthenticated;
        }

        private WindowsIdentity()
            : base(null, null, null, ClaimTypes.Name, ClaimTypes.GroupSid)
        { }

        /// <summary>
        /// Initializes a new instance of the WindowsIdentity class for the user represented by the specified User Principal Name (UPN).
        /// </summary>
        /// <remarks>
        /// Unlike the .NET Framework version, we connect to Lsa only as an untrusted caller. We do not attempt to exploit Tcb privilege or adjust the current
        /// thread privilege to include Tcb.
        /// </remarks>
        public WindowsIdentity(string sUserPrincipalName)
            : base(null, null, null, ClaimTypes.Name, ClaimTypes.GroupSid)
        {
            // .NET Framework compat: See comments below for why we don't validate sUserPrincipalName.

            using (SafeLsaHandle lsaHandle = ConnectToLsa())
            {
                int packageId = LookupAuthenticationPackage(lsaHandle, Interop.SspiCli.AuthenticationPackageNames.MICROSOFT_KERBEROS_NAME_A);

                // 8 byte or less name that indicates the source of the access token. This choice of name is visible to callers through the native GetTokenInformation() api
                // so we'll use the same name the CLR used even though we're not actually the "CLR."
                byte[] sourceName = { (byte)'C', (byte)'L', (byte)'R', (byte)0 };

                TOKEN_SOURCE sourceContext;
                unsafe
                {
                    if (!Interop.Advapi32.AllocateLocallyUniqueId(&sourceContext.SourceIdentifier))
                        throw new SecurityException(new Win32Exception().Message);

                    sourceName.AsSpan().CopyTo(new Span<byte>(sourceContext.SourceName, TOKEN_SOURCE.TOKEN_SOURCE_LENGTH));
                }

                ArgumentNullException.ThrowIfNull(sUserPrincipalName);

                byte[] upnBytes = Encoding.Unicode.GetBytes(sUserPrincipalName);
                if (upnBytes.Length > ushort.MaxValue)
                {
                    // .NET Framework compat: LSA only allocates 16 bits to hold the UPN size. We should throw an exception here but unfortunately, the desktop did an unchecked cast to ushort,
                    // effectively truncating upnBytes to the first (N % 64K) bytes. We'll simulate the same behavior here (albeit in a way that makes it look less accidental.)
                    Array.Resize(ref upnBytes, upnBytes.Length & ushort.MaxValue);
                }
                unsafe
                {
                    //
                    // Build the KERB_S4U_LOGON structure.  Note that the LSA expects this entire
                    // structure to be contained within the same block of memory, so we need to allocate
                    // enough room for both the structure itself and the UPN string in a single buffer
                    // and do the marshalling into this buffer by hand.
                    //
                    int authenticationInfoLength = checked(sizeof(KERB_S4U_LOGON) + upnBytes.Length);
                    using (SafeLocalAllocHandle authenticationInfo = SafeLocalAllocHandle.LocalAlloc(authenticationInfoLength))
                    {
                        KERB_S4U_LOGON* pKerbS4uLogin = (KERB_S4U_LOGON*)(authenticationInfo.DangerousGetHandle());
                        pKerbS4uLogin->MessageType = KERB_LOGON_SUBMIT_TYPE.KerbS4ULogon;
                        pKerbS4uLogin->Flags = KerbS4uLogonFlags.None;

                        pKerbS4uLogin->ClientUpn.Length = pKerbS4uLogin->ClientUpn.MaximumLength = checked((ushort)upnBytes.Length);

                        IntPtr pUpnOffset = (IntPtr)(pKerbS4uLogin + 1);
                        pKerbS4uLogin->ClientUpn.Buffer = pUpnOffset;
                        Marshal.Copy(upnBytes, 0, pKerbS4uLogin->ClientUpn.Buffer, upnBytes.Length);

                        pKerbS4uLogin->ClientRealm.Length = pKerbS4uLogin->ClientRealm.MaximumLength = 0;
                        pKerbS4uLogin->ClientRealm.Buffer = IntPtr.Zero;

                        ushort sourceNameLength = checked((ushort)(sourceName.Length));
                        using (SafeLocalAllocHandle sourceNameBuffer = SafeLocalAllocHandle.LocalAlloc(sourceNameLength))
                        {
                            Marshal.Copy(sourceName, 0, sourceNameBuffer.DangerousGetHandle(), sourceName.Length);
                            LSA_STRING lsaOriginName = new LSA_STRING(sourceNameBuffer.DangerousGetHandle(), sourceNameLength);

                            int ntStatus = Interop.SspiCli.LsaLogonUser(
                                lsaHandle,
                                lsaOriginName,
                                SECURITY_LOGON_TYPE.Network,
                                packageId,
                                authenticationInfo.DangerousGetHandle(),
                                authenticationInfoLength,
                                IntPtr.Zero,
                                sourceContext,
                                out SafeLsaReturnBufferHandle profileBuffer,
                                out int profileBufferLength,
                                out LUID logonId,
                                out SafeAccessTokenHandle accessTokenHandle,
                                out QUOTA_LIMITS quota,
                                out int subStatus);

                            if (ntStatus == unchecked((int)Interop.StatusOptions.STATUS_ACCOUNT_RESTRICTION) && subStatus < 0)
                                ntStatus = subStatus;
                            if (ntStatus < 0) // non-negative numbers indicate success
                                throw GetExceptionFromNtStatus(ntStatus);
                            if (subStatus < 0) // non-negative numbers indicate success
                                throw GetExceptionFromNtStatus(subStatus);

                            if (profileBuffer != null)
                                profileBuffer.Dispose();

                            _safeTokenHandle = accessTokenHandle;
                        }
                    }
                }
            }
        }

        private static SafeLsaHandle ConnectToLsa()
        {
            int ntStatus = Interop.SspiCli.LsaConnectUntrusted(out SafeLsaHandle lsaHandle);
            if (ntStatus < 0) // non-negative numbers indicate success
                throw GetExceptionFromNtStatus(ntStatus);
            return lsaHandle;
        }

        private static int LookupAuthenticationPackage(SafeLsaHandle lsaHandle, string packageName)
        {
            Debug.Assert(!string.IsNullOrEmpty(packageName));
            unsafe
            {
                int packageId;
                byte[] asciiPackageName = Encoding.ASCII.GetBytes(packageName);
                fixed (byte* pAsciiPackageName = &asciiPackageName[0])
                {
                    LSA_STRING lsaPackageName = new LSA_STRING((IntPtr)pAsciiPackageName, checked((ushort)(asciiPackageName.Length)));
                    int ntStatus = Interop.SspiCli.LsaLookupAuthenticationPackage(lsaHandle, ref lsaPackageName, out packageId);
                    if (ntStatus < 0) // non-negative numbers indicate success
                        throw GetExceptionFromNtStatus(ntStatus);
                }
                return packageId;
            }
        }

        private static SafeAccessTokenHandle DuplicateAccessToken(IntPtr accessToken)
        {
            if (accessToken == IntPtr.Zero)
            {
                throw new ArgumentException(SR.Argument_TokenZero);
            }

            // Find out if the specified token is a valid.
            if (!Interop.Advapi32.GetTokenInformation(
                    accessToken,
                    (uint)TokenInformationClass.TokenType,
                    IntPtr.Zero,
                    0,
                    out _) &&
                Marshal.GetLastWin32Error() == Interop.Errors.ERROR_INVALID_HANDLE)
            {
                throw new ArgumentException(SR.Argument_InvalidImpersonationToken);
            }

            SafeAccessTokenHandle duplicateAccessToken = SafeAccessTokenHandle.InvalidHandle;
            IntPtr currentProcessHandle = Interop.Kernel32.GetCurrentProcess();
            if (!Interop.Kernel32.DuplicateHandle(
                    currentProcessHandle,
                    accessToken,
                    currentProcessHandle,
                    ref duplicateAccessToken,
                    0,
                    true,
                    Interop.DuplicateHandleOptions.DUPLICATE_SAME_ACCESS))
            {
                throw new SecurityException(new Win32Exception().Message);
            }

            return duplicateAccessToken;
        }

        private static SafeAccessTokenHandle DuplicateAccessToken(SafeAccessTokenHandle accessToken)
        {
            if (accessToken.IsInvalid)
            {
                return accessToken;
            }

            bool refAdded = false;
            try
            {
                accessToken.DangerousAddRef(ref refAdded);
                return DuplicateAccessToken(accessToken.DangerousGetHandle());
            }
            finally
            {
                if (refAdded)
                {
                    accessToken.DangerousRelease();
                }
            }
        }

        private void CreateFromToken(IntPtr userToken)
        {
            _safeTokenHandle = DuplicateAccessToken(userToken);
        }

        public WindowsIdentity(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            throw new PlatformNotSupportedException();
        }

        //
        // Factory methods.
        //

        public static WindowsIdentity GetCurrent()
        {
            // not null when threadOnly argument is false
            return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, threadOnly: false)!;
        }


        public static WindowsIdentity? GetCurrent(bool ifImpersonating)
        {

            return GetCurrentInternal(TokenAccessLevels.MaximumAllowed, ifImpersonating);
        }


        public static WindowsIdentity GetCurrent(TokenAccessLevels desiredAccess)
        {
            // not null when threadOnly argument is false
            return GetCurrentInternal(desiredAccess, threadOnly: false)!;
        }

        // GetAnonymous() is used heavily in ASP.NET requests as a dummy identity to indicate
        // the request is anonymous. It does not represent a real process or thread token so
        // it cannot impersonate or do anything useful. Note this identity does not represent the
        // usual concept of an anonymous token, and the name is simply misleading but we cannot change it now.

        public static WindowsIdentity GetAnonymous()
        {
            return new WindowsIdentity();
        }

        //
        // Properties.
        //
        // this is defined 'override sealed' for back compat. Il generated is 'virtual final' and this needs to be the same.
        public sealed override unsafe string? AuthenticationType
        {
            get
            {
                // If this is an anonymous identity, return an empty string
                if (_safeTokenHandle.IsInvalid)
                    return string.Empty;

                if (_authType == null)
                {
                    Interop.LUID authId = GetLogonAuthId(_safeTokenHandle);
                    if (authId.LowPart == Interop.LuidOptions.ANONYMOUS_LOGON_LUID)
                        return string.Empty; // no authentication, just return an empty string

                    SafeLsaReturnBufferHandle? pLogonSessionData = null;
                    try
                    {
                        int status = Interop.SspiCli.LsaGetLogonSessionData(ref authId, out pLogonSessionData);
                        if (status < 0) // non-negative numbers indicate success
                            throw GetExceptionFromNtStatus(status);

                        pLogonSessionData.Initialize((uint)sizeof(Interop.SECURITY_LOGON_SESSION_DATA));

                        Interop.SECURITY_LOGON_SESSION_DATA logonSessionData = pLogonSessionData.Read<Interop.SECURITY_LOGON_SESSION_DATA>(0);
                        return Marshal.PtrToStringUni(logonSessionData.AuthenticationPackage.Buffer);
                    }
                    finally
                    {
                        pLogonSessionData?.Dispose();
                    }
                }

                return _authType;
            }
        }

        public TokenImpersonationLevel ImpersonationLevel
        {
            get
            {
                // In case of a race condition here here, both threads will set m_impersonationLevel to the same value,
                // which is ok.
                if (!_impersonationLevelInitialized)
                {
                    TokenImpersonationLevel impersonationLevel;
                    // If this is an anonymous identity
                    if (_safeTokenHandle.IsInvalid)
                    {
                        impersonationLevel = TokenImpersonationLevel.Anonymous;
                    }
                    else
                    {
                        TokenType tokenType = (TokenType)GetTokenInformation<int>(TokenInformationClass.TokenType);
                        if (tokenType == TokenType.TokenPrimary)
                        {
                            impersonationLevel = TokenImpersonationLevel.None; // primary token;
                        }
                        else
                        {
                            // This is an impersonation token, get the impersonation level
                            int level = GetTokenInformation<int>(TokenInformationClass.TokenImpersonationLevel);
                            impersonationLevel = (TokenImpersonationLevel)level + 1;
                        }
                    }

                    _impersonationLevel = impersonationLevel;
                    _impersonationLevelInitialized = true;
                }

                return _impersonationLevel;
            }
        }

        public override bool IsAuthenticated
        {
            get
            {
                if (_isAuthenticated == -1)
                {
                    if (s_authenticatedUserRid is null)
                    {
                        s_authenticatedUserRid = new SecurityIdentifier(
                            IdentifierAuthority.NTAuthority,
                            new int[] { Interop.SecurityIdentifier.SECURITY_AUTHENTICATED_USER_RID }
                        );
                    }
                    // This approach will not work correctly for domain guests (will return false
                    // instead of true). This is a corner-case that is not very interesting.
                    _isAuthenticated = CheckNtTokenForSid(s_authenticatedUserRid) ? 1 : 0;
                }
                return _isAuthenticated == 1;
            }
        }

        private bool CheckNtTokenForSid(SecurityIdentifier sid)
        {
            // special case the anonymous identity.
            if (_safeTokenHandle.IsInvalid)
                return false;

            // CheckTokenMembership expects an impersonation token
            SafeAccessTokenHandle token = SafeAccessTokenHandle.InvalidHandle;
            TokenImpersonationLevel til = ImpersonationLevel;
            bool isMember = false;

            try
            {
                if (til == TokenImpersonationLevel.None)
                {
                    if (!Interop.Advapi32.DuplicateTokenEx(_safeTokenHandle,
                                                      (uint)TokenAccessLevels.Query,
                                                      IntPtr.Zero,
                                                      (uint)TokenImpersonationLevel.Identification,
                                                      (uint)TokenType.TokenImpersonation,
                                                      ref token))
                        throw new SecurityException(new Win32Exception().Message);
                }


                // CheckTokenMembership will check if the SID is both present and enabled in the access token.
                if (!Interop.Advapi32.CheckTokenMembership((til != TokenImpersonationLevel.None ? _safeTokenHandle : token),
                                                      sid.BinaryForm,
                                                      ref isMember))
                    throw new SecurityException(new Win32Exception().Message);


            }
            finally
            {
                if (token != SafeAccessTokenHandle.InvalidHandle)
                {
                    token.Dispose();
                }
            }

            return isMember;
        }

        //
        // IsGuest, IsSystem and IsAnonymous are maintained for compatibility reasons. It is always
        // possible to extract this same information from the User SID property and the new
        // (and more general) methods defined in the SID class (IsWellKnown, etc...).
        //

        public virtual bool IsGuest
        {
            get
            {
                // special case the anonymous identity.
                if (_safeTokenHandle.IsInvalid)
                    return false;

                if (s_domainRid is null)
                {
                    s_domainRid = new SecurityIdentifier(
                        IdentifierAuthority.NTAuthority,
                        new int[] { Interop.SecurityIdentifier.SECURITY_BUILTIN_DOMAIN_RID, (int)WindowsBuiltInRole.Guest }
                    );
                }

                return CheckNtTokenForSid(s_domainRid);
            }
        }

        public virtual bool IsSystem
        {
            get
            {
                // special case the anonymous identity.
                if (_safeTokenHandle.IsInvalid)
                    return false;

                if (s_localSystemRid is null)
                {
                    s_localSystemRid = new SecurityIdentifier(
                        IdentifierAuthority.NTAuthority,
                        new int[] { Interop.SecurityIdentifier.SECURITY_LOCAL_SYSTEM_RID }
                    );
                }

                return User == s_localSystemRid;
            }
        }

        public virtual bool IsAnonymous
        {
            get
            {
                // special case the anonymous identity.
                if (_safeTokenHandle.IsInvalid)
                    return true;

                if (s_anonymousRid is null)
                {
                    s_anonymousRid = new SecurityIdentifier(
                        IdentifierAuthority.NTAuthority,
                        new int[] { Interop.SecurityIdentifier.SECURITY_ANONYMOUS_LOGON_RID }
                    );
                }

                return User == s_anonymousRid;
            }
        }

        public override string Name
        {
            get
            {
                return GetName();
            }
        }

        internal string GetName()
        {
            // special case the anonymous identity.
            if (_safeTokenHandle.IsInvalid)
                return string.Empty;

            if (_name == null)
            {
                // revert thread impersonation for the duration of the call to get the name.
                RunImpersonated(SafeAccessTokenHandle.InvalidHandle, delegate
                {
                    NTAccount? ntAccount = User!.Translate(typeof(NTAccount)) as NTAccount;
                    _name = ntAccount!.ToString();
                });
            }

            return _name!;
        }

        public SecurityIdentifier? Owner
        {
            get
            {
                // special case the anonymous identity.
                if (_safeTokenHandle.IsInvalid)
                    return null;

                if (_owner == null)
                {
                    using (SafeLocalAllocHandle tokenOwner = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenOwner, nullOnInvalidParam: false)!)
                    {
                        _owner = new SecurityIdentifier(tokenOwner.Read<IntPtr>(0));
                    }
                }

                return _owner;
            }
        }

        public SecurityIdentifier? User
        {
            get
            {
                // special case the anonymous identity.
                if (_safeTokenHandle.IsInvalid)
                    return null;

                if (_user == null)
                {
                    using (SafeLocalAllocHandle tokenUser = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenUser, nullOnInvalidParam: false)!)
                    {
                        _user = new SecurityIdentifier(tokenUser!.Read<IntPtr>(0));
                    }
                }

                return _user;
            }
        }

        public unsafe IdentityReferenceCollection? Groups
        {
            get
            {
                // special case the anonymous identity.
                if (_safeTokenHandle.IsInvalid)
                    return null;

                if (_groups == null)
                {
                    IdentityReferenceCollection groups = new IdentityReferenceCollection();
                    using (SafeLocalAllocHandle pGroups = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenGroups, nullOnInvalidParam: false)!)
                    {
                        Interop.TOKEN_GROUPS tokenGroups = pGroups!.Read<Interop.TOKEN_GROUPS>(0);
                        Interop.SID_AND_ATTRIBUTES[] groupDetails = new Interop.SID_AND_ATTRIBUTES[tokenGroups.GroupCount];
                        pGroups.ReadArray((uint)sizeof(IntPtr) /* offsetof(Interop.TOKEN_GROUPS, Groups) */,
                                          groupDetails,
                                          0,
                                          groupDetails.Length);

                        foreach (Interop.SID_AND_ATTRIBUTES group in groupDetails)
                        {
                            // Ignore disabled, logon ID, and deny-only groups.
                            uint mask = Interop.SecurityGroups.SE_GROUP_ENABLED | Interop.SecurityGroups.SE_GROUP_LOGON_ID | Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY;
                            if ((group.Attributes & mask) == Interop.SecurityGroups.SE_GROUP_ENABLED)
                            {
                                groups.Add(new SecurityIdentifier(group.Sid));
                            }
                        }
                    }
                    Interlocked.CompareExchange(ref _groups, groups, null);
                }

                return _groups;
            }
        }

        public SafeAccessTokenHandle AccessToken
        {
            get
            {
                return _safeTokenHandle;
            }
        }

        public virtual IntPtr Token
        {
            get
            {
                return _safeTokenHandle.DangerousGetHandle();
            }
        }

        //
        // Public methods.
        //

        public static void RunImpersonated(SafeAccessTokenHandle safeAccessTokenHandle, Action action!!)
        {
            RunImpersonatedInternal(safeAccessTokenHandle, action);
        }


        public static T RunImpersonated<T>(SafeAccessTokenHandle safeAccessTokenHandle, Func<T> func!!)
        {
            T result = default!;
            RunImpersonatedInternal(safeAccessTokenHandle, () => result = func());
            return result;
        }

        /// <summary>
        /// Runs the specified asynchronous action as the impersonated Windows identity
        /// </summary>
        /// <param name="safeAccessTokenHandle">The SafeAccessTokenHandle of the impersonated Windows identity.</param>
        /// <param name="func">The <see cref="System.Func{Task}"/> to run.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation of the provided <see cref="System.Func{Task}"/>.</returns>
        public static Task RunImpersonatedAsync(SafeAccessTokenHandle safeAccessTokenHandle, Func<Task> func)
            => RunImpersonated(safeAccessTokenHandle, func);

        /// <summary>
        /// Runs the specified asynchronous action as the impersonated Windows identity
        /// </summary>
        /// <typeparam name="T">The type of the object to return.</typeparam>
        /// <param name="safeAccessTokenHandle">The SafeAccessTokenHandle of the impersonated Windows identity.</param>
        /// <param name="func">The <see cref="System.Func{Task}"/> of <see cref="System.Threading.Tasks.Task{T}"/> to run.</param>
        /// <returns>A <see cref="Task{T}"/> that represents the asynchronous operation of the <see cref="System.Func{Task}"/> of <see cref="System.Threading.Tasks.Task{T}"/> provided.</returns>
        public static Task<T> RunImpersonatedAsync<T>(SafeAccessTokenHandle safeAccessTokenHandle, Func<Task<T>> func)
            => RunImpersonated(safeAccessTokenHandle, func);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_safeTokenHandle != null && !_safeTokenHandle.IsClosed)
                    _safeTokenHandle.Dispose();
            }
            _name = null;
            _owner = null;
            _user = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        //
        // internal.
        //

        private static readonly AsyncLocal<SafeAccessTokenHandle?> s_currentImpersonatedToken = new AsyncLocal<SafeAccessTokenHandle?>(CurrentImpersonatedTokenChanged);

        private static void RunImpersonatedInternal(SafeAccessTokenHandle token, Action action)
        {
            token = DuplicateAccessToken(token);

            SafeAccessTokenHandle previousToken = GetCurrentToken(TokenAccessLevels.MaximumAllowed, false, out bool isImpersonating, out int hr);
            if (previousToken == null || previousToken.IsInvalid)
                throw new SecurityException(new Win32Exception(hr).Message);

            s_currentImpersonatedToken.Value = isImpersonating ? previousToken : null;

            ExecutionContext? currentContext = ExecutionContext.Capture();

            // Run everything else inside of ExecutionContext.Run, so that any EC changes will be undone
            // on the way out.
            ExecutionContext.Run(
                currentContext!,
                delegate
                {
                    if (!Interop.Advapi32.RevertToSelf())
                        Environment.FailFast(new Win32Exception().Message);

                    s_currentImpersonatedToken.Value = null;

                    if (!token.IsInvalid && !Interop.Advapi32.ImpersonateLoggedOnUser(token))
                        throw new SecurityException(SR.Argument_ImpersonateUser);

                    s_currentImpersonatedToken.Value = token;

                    action();
                },
                null);
        }

        private static void CurrentImpersonatedTokenChanged(AsyncLocalValueChangedArgs<SafeAccessTokenHandle?> args)
        {
            if (!args.ThreadContextChanged)
                return; // we handle explicit Value property changes elsewhere.

            if (!Interop.Advapi32.RevertToSelf())
                Environment.FailFast(new Win32Exception().Message);

            if (args.CurrentValue != null && !args.CurrentValue.IsInvalid)
            {
                if (!Interop.Advapi32.ImpersonateLoggedOnUser(args.CurrentValue))
                    Environment.FailFast(new Win32Exception().Message);
            }
        }

        internal static WindowsIdentity? GetCurrentInternal(TokenAccessLevels desiredAccess, bool threadOnly)
        {
            SafeAccessTokenHandle safeTokenHandle = GetCurrentToken(desiredAccess, threadOnly, out bool isImpersonating, out int hr);
            if (safeTokenHandle == null || safeTokenHandle.IsInvalid)
            {
                // either we wanted only ThreadToken - return null
                if (threadOnly && !isImpersonating)
                    return null;
                // or there was an error
                throw new SecurityException(new Win32Exception(hr).Message);
            }
            WindowsIdentity wi = new WindowsIdentity();
            wi._safeTokenHandle.Dispose();
            wi._safeTokenHandle = safeTokenHandle;
            return wi;
        }

        //
        // private.
        //
        private static int GetHRForWin32Error(int dwLastError)
        {
            if ((dwLastError & 0x80000000) == 0x80000000)
                return dwLastError;
            else
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        }

        private static Exception GetExceptionFromNtStatus(int status)
        {
            if ((uint)status == Interop.StatusOptions.STATUS_ACCESS_DENIED)
                return new UnauthorizedAccessException();

            if ((uint)status == Interop.StatusOptions.STATUS_INSUFFICIENT_RESOURCES || (uint)status == Interop.StatusOptions.STATUS_NO_MEMORY)
                return new OutOfMemoryException();

            uint win32ErrorCode = Interop.Advapi32.LsaNtStatusToWinError((uint)status);
            return new SecurityException(new Win32Exception(unchecked((int)win32ErrorCode)).Message);
        }

        private static SafeAccessTokenHandle GetCurrentToken(TokenAccessLevels desiredAccess, bool threadOnly, out bool isImpersonating, out int hr)
        {
            isImpersonating = true;
            hr = 0;
            bool success = Interop.Advapi32.OpenThreadToken(desiredAccess, WinSecurityContext.Both, out SafeAccessTokenHandle safeTokenHandle);
            if (!success)
                hr = Marshal.GetHRForLastWin32Error();
            if (!success && hr == GetHRForWin32Error(Interop.Errors.ERROR_NO_TOKEN))
            {
                // No impersonation
                isImpersonating = false;
                if (!threadOnly)
                    safeTokenHandle = GetCurrentProcessToken(desiredAccess, out hr);
            }
            return safeTokenHandle;
        }

        private static SafeAccessTokenHandle GetCurrentProcessToken(TokenAccessLevels desiredAccess, out int hr)
        {
            hr = 0;
            if (!Interop.Advapi32.OpenProcessToken(Interop.Kernel32.GetCurrentProcess(), desiredAccess, out SafeAccessTokenHandle safeTokenHandle))
                hr = GetHRForWin32Error(Marshal.GetLastWin32Error());
            return safeTokenHandle;
        }

        /// <summary>
        ///   Get a property from the current token
        /// </summary>

        private unsafe T GetTokenInformation<T>(TokenInformationClass tokenInformationClass) where T : unmanaged
        {
            Debug.Assert(!_safeTokenHandle.IsInvalid && !_safeTokenHandle.IsClosed, "!m_safeTokenHandle.IsInvalid && !m_safeTokenHandle.IsClosed");

            using (SafeLocalAllocHandle information = GetTokenInformation(_safeTokenHandle, tokenInformationClass, nullOnInvalidParam: false)!)
            {
                Debug.Assert(information!.ByteLength >= (ulong)sizeof(T),
                                "information.ByteLength >= (ulong)sizeof(T)");

                return information.Read<T>(0);
            }
        }

        private static Interop.LUID GetLogonAuthId(SafeAccessTokenHandle safeTokenHandle)
        {
            using (SafeLocalAllocHandle pStatistics = GetTokenInformation(safeTokenHandle, TokenInformationClass.TokenStatistics, nullOnInvalidParam: false)!)
            {
                Interop.TOKEN_STATISTICS statistics = pStatistics!.Read<Interop.TOKEN_STATISTICS>(0);
                return statistics.AuthenticationId;
            }
        }

        private static SafeLocalAllocHandle? GetTokenInformation(SafeAccessTokenHandle tokenHandle, TokenInformationClass tokenInformationClass, bool nullOnInvalidParam = false)
        {
            SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
            Interop.Advapi32.GetTokenInformation(tokenHandle,
                                                          (uint)tokenInformationClass,
                                                          safeLocalAllocHandle,
                                                          0,
                                                          out uint dwLength);
            int dwErrorCode = Marshal.GetLastWin32Error();
            switch (dwErrorCode)
            {
                case Interop.Errors.ERROR_BAD_LENGTH:
                // special case for TokenSessionId. Falling through
                case Interop.Errors.ERROR_INSUFFICIENT_BUFFER:
                    safeLocalAllocHandle.Dispose();
                    safeLocalAllocHandle = SafeLocalAllocHandle.LocalAlloc(checked((int)dwLength));

                    bool result = Interop.Advapi32.GetTokenInformation(tokenHandle,
                                                             (uint)tokenInformationClass,
                                                             safeLocalAllocHandle,
                                                             dwLength,
                                                             out _);
                    if (!result)
                        throw new SecurityException(new Win32Exception().Message);
                    break;
                case Interop.Errors.ERROR_INVALID_HANDLE:
                    throw new ArgumentException(SR.Argument_InvalidImpersonationToken);
                case Interop.Errors.ERROR_INVALID_PARAMETER:
                    if (nullOnInvalidParam)
                    {
                        safeLocalAllocHandle.Dispose();
                        return null;
                    }

                    // Throw the exception.
                    goto default;
                default:
                    throw new SecurityException(new Win32Exception(dwErrorCode).Message);
            }
            return safeLocalAllocHandle;
        }

        private static string? GetAuthType(WindowsIdentity identity!!)
        {
            return identity._authType;
        }

        /// <summary>
        /// Returns a new instance of <see cref="WindowsIdentity"/> with values copied from this object.
        /// </summary>
        public override ClaimsIdentity Clone()
        {
            return new WindowsIdentity(this);
        }

        /// <summary>
        /// Gets the 'User Claims' from the NTToken that represents this identity
        /// </summary>
        public virtual IEnumerable<Claim> UserClaims
        {
            get
            {
                InitializeClaims();

                return _userClaims!.ToArray();
            }
        }

        /// <summary>
        /// Gets the 'Device Claims' from the NTToken that represents the device the identity is using
        /// </summary>
        public virtual IEnumerable<Claim> DeviceClaims
        {
            get
            {
                InitializeClaims();

                return _deviceClaims!.ToArray();
            }
        }

        /// <summary>
        /// Gets the claims as <see cref="IEnumerable{Claim}"/>, associated with this <see cref="WindowsIdentity"/>.
        /// Includes UserClaims and DeviceClaims.
        /// </summary>
        public override IEnumerable<Claim> Claims
        {
            get
            {
                if (!_claimsInitialized)
                {
                    InitializeClaims();
                }

                foreach (Claim claim in base.Claims)
                    yield return claim;

                foreach (Claim claim in _userClaims!)
                    yield return claim;

                foreach (Claim claim in _deviceClaims!)
                    yield return claim;
            }
        }

        /// <summary>
        /// Internal method to initialize the claim collection.
        /// Lazy init is used so claims are not initialized until needed
        /// </summary>
        private void InitializeClaims()
        {
            bool discard = false;

            LazyInitializer.EnsureInitialized(
                ref discard,
                ref _claimsInitialized,
                ref _claimsIntiailizedLock,
                () =>
                {
                    _userClaims = new List<Claim>();
                    _deviceClaims = new List<Claim>();

                    if (!string.IsNullOrEmpty(Name))
                    {
                        //
                        // Add the name claim only if the WindowsIdentity.Name is populated
                        // WindowsIdentity.Name will be null when it is the fake anonymous user
                        // with a token value of IntPtr.Zero
                        //
                        _userClaims.Add(new Claim(NameClaimType, Name, ClaimValueTypes.String, _issuerName, _issuerName, this));
                    }

                    // primary sid
                    AddPrimarySidClaim(_userClaims);

                    // group sids
                    AddGroupSidClaims(_userClaims);

                    if (!s_ignoreWindows8Properties)
                    {
                        // Device group sids (may cause s_ignoreWindows8Properties to be set to true, so must be first in this block)
                        AddDeviceGroupSidClaims(_deviceClaims, TokenInformationClass.TokenDeviceGroups);

                        if (!s_ignoreWindows8Properties)
                        {
                            // User token claims
                            AddTokenClaims(_userClaims, TokenInformationClass.TokenUserClaimAttributes, ClaimTypes.WindowsUserClaim);

                            // Device token claims
                            AddTokenClaims(_deviceClaims, TokenInformationClass.TokenDeviceClaimAttributes, ClaimTypes.WindowsDeviceClaim);
                        }
                    }
                    return true;
                }
            );
        }

        /// <summary>
        /// Creates a collection of SID claims that represent the users groups.
        /// </summary>
        private unsafe void AddGroupSidClaims(List<Claim> instanceClaims)
        {
            // special case the anonymous identity.
            if (_safeTokenHandle.IsInvalid)
                return;

            SafeLocalAllocHandle? safeAllocHandle = null;
            SafeLocalAllocHandle? safeAllocHandlePrimaryGroup = null;
            try
            {
                // Retrieve the primary group sid
                safeAllocHandlePrimaryGroup = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenPrimaryGroup);
                Interop.TOKEN_PRIMARY_GROUP primaryGroup = *(Interop.TOKEN_PRIMARY_GROUP*)(safeAllocHandlePrimaryGroup!.DangerousGetHandle());
                SecurityIdentifier primaryGroupSid = new SecurityIdentifier(primaryGroup.PrimaryGroup);

                // only add one primary group sid
                bool foundPrimaryGroupSid = false;

                // Retrieve all group sids, primary group sid is one of them
                safeAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenGroups);
                int count = *(int*)safeAllocHandle!.DangerousGetHandle();
                Interop.SID_AND_ATTRIBUTES* pSidAndAttributes = (Interop.SID_AND_ATTRIBUTES*)
                    ((byte*)safeAllocHandle.DangerousGetHandle() + sizeof(IntPtr) /* offsetof(Interop.TOKEN_GROUPS, Groups) */);
                Claim claim;
                for (int i = 0; i < count; i++)
                {
                    Interop.SID_AND_ATTRIBUTES group = pSidAndAttributes[i];
                    uint mask = Interop.SecurityGroups.SE_GROUP_ENABLED | Interop.SecurityGroups.SE_GROUP_LOGON_ID | Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY;
                    SecurityIdentifier groupSid = new SecurityIdentifier(group.Sid);

                    if ((group.Attributes & mask) == Interop.SecurityGroups.SE_GROUP_ENABLED)
                    {
                        if (!foundPrimaryGroupSid && StringComparer.Ordinal.Equals(groupSid.Value, primaryGroupSid.Value))
                        {
                            claim = new Claim(ClaimTypes.PrimaryGroupSid, groupSid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                            claim.Properties.Add(ClaimTypes.WindowsSubAuthority, groupSid.IdentifierAuthority.ToString());
                            instanceClaims.Add(claim);
                            foundPrimaryGroupSid = true;
                        }
                        //Primary group sid generates both regular groupsid claim and primary groupsid claim
                        claim = new Claim(ClaimTypes.GroupSid, groupSid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                        claim.Properties.Add(ClaimTypes.WindowsSubAuthority, groupSid.IdentifierAuthority.ToString());
                        instanceClaims.Add(claim);
                    }
                    else if ((group.Attributes & mask) == Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY)
                    {
                        if (!foundPrimaryGroupSid && StringComparer.Ordinal.Equals(groupSid.Value, primaryGroupSid.Value))
                        {
                            claim = new Claim(ClaimTypes.DenyOnlyPrimaryGroupSid, groupSid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                            claim.Properties.Add(ClaimTypes.WindowsSubAuthority, groupSid.IdentifierAuthority.ToString());
                            instanceClaims.Add(claim);
                            foundPrimaryGroupSid = true;
                        }
                        //Primary group sid generates both regular groupsid claim and primary groupsid claim
                        claim = new Claim(ClaimTypes.DenyOnlySid, groupSid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                        claim.Properties.Add(ClaimTypes.WindowsSubAuthority, groupSid.IdentifierAuthority.ToString());
                        instanceClaims.Add(claim);
                    }
                }
            }
            finally
            {
                safeAllocHandle?.Dispose();
                safeAllocHandlePrimaryGroup?.Dispose();
            }
        }

        /// <summary>
        /// Creates a Windows SID Claim and adds to collection of claims.
        /// </summary>
        private unsafe void AddPrimarySidClaim(List<Claim> instanceClaims)
        {
            // special case the anonymous identity.
            if (_safeTokenHandle.IsInvalid)
                return;

            SafeLocalAllocHandle? safeAllocHandle = null;
            try
            {
                safeAllocHandle = GetTokenInformation(_safeTokenHandle, TokenInformationClass.TokenUser);
                Interop.SID_AND_ATTRIBUTES user = *(Interop.SID_AND_ATTRIBUTES*)(safeAllocHandle!.DangerousGetHandle());
                uint mask = Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY;

                SecurityIdentifier sid = new SecurityIdentifier(user.Sid);
                Claim claim;
                if (user.Attributes == 0)
                {
                    claim = new Claim(ClaimTypes.PrimarySid, sid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                    claim.Properties.Add(ClaimTypes.WindowsSubAuthority, sid.IdentifierAuthority.ToString());
                    instanceClaims.Add(claim);
                }
                else if ((user.Attributes & mask) == Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY)
                {
                    claim = new Claim(ClaimTypes.DenyOnlyPrimarySid, sid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                    claim.Properties.Add(ClaimTypes.WindowsSubAuthority, sid.IdentifierAuthority.ToString());
                    instanceClaims.Add(claim);
                }
            }
            finally
            {
                safeAllocHandle?.Dispose();
            }
        }

        private unsafe void AddDeviceGroupSidClaims(List<Claim> instanceClaims, TokenInformationClass tokenInformationClass)
        {
            // special case the anonymous identity.
            if (_safeTokenHandle.IsInvalid)
                return;

            SafeLocalAllocHandle? safeAllocHandle = null;
            try
            {
                // Retrieve all group sids

                safeAllocHandle = GetTokenInformation(_safeTokenHandle, tokenInformationClass, nullOnInvalidParam: true);

                if (safeAllocHandle == null)
                {
                    s_ignoreWindows8Properties = true;
                    return;
                }

                int count = *(int*)safeAllocHandle.DangerousGetHandle();
                Interop.SID_AND_ATTRIBUTES* pSidAndAttributes = (Interop.SID_AND_ATTRIBUTES*)
                    ((byte*)safeAllocHandle.DangerousGetHandle() + sizeof(IntPtr) /* offsetof(Interop.TOKEN_GROUPS, Groups) */);
                for (int i = 0; i < count; i++)
                {
                    Interop.SID_AND_ATTRIBUTES group = pSidAndAttributes[i];
                    uint mask = Interop.SecurityGroups.SE_GROUP_ENABLED | Interop.SecurityGroups.SE_GROUP_LOGON_ID | Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY;
                    SecurityIdentifier groupSid = new SecurityIdentifier(group.Sid);
                    string claimType;
                    if ((group.Attributes & mask) == Interop.SecurityGroups.SE_GROUP_ENABLED)
                    {
                        claimType = ClaimTypes.WindowsDeviceGroup;
                        Claim claim = new Claim(claimType, groupSid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                        claim.Properties.Add(ClaimTypes.WindowsSubAuthority, groupSid.IdentifierAuthority.ToString());
                        claim.Properties.Add(claimType, "");
                        instanceClaims.Add(claim);
                    }
                    else if ((group.Attributes & mask) == Interop.SecurityGroups.SE_GROUP_USE_FOR_DENY_ONLY)
                    {
                        claimType = ClaimTypes.DenyOnlyWindowsDeviceGroup;
                        Claim claim = new Claim(claimType, groupSid.Value, ClaimValueTypes.String, _issuerName, _issuerName, this);
                        claim.Properties.Add(ClaimTypes.WindowsSubAuthority, groupSid.IdentifierAuthority.ToString());
                        claim.Properties.Add(claimType, "");
                        instanceClaims.Add(claim);
                    }
                }
            }
            finally
            {
                safeAllocHandle?.Dispose();
            }
        }

        private unsafe void AddTokenClaims(List<Claim> instanceClaims, TokenInformationClass tokenInformationClass, string propertyValue)
        {
            // special case the anonymous identity.
            if (_safeTokenHandle.IsInvalid)
                return;

            SafeLocalAllocHandle? safeAllocHandle = null;

            try
            {
                safeAllocHandle = GetTokenInformation(_safeTokenHandle, tokenInformationClass);

                Interop.CLAIM_SECURITY_ATTRIBUTES_INFORMATION claimAttributes = *(Interop.CLAIM_SECURITY_ATTRIBUTES_INFORMATION*)(safeAllocHandle!.DangerousGetHandle());
                // An attribute represents a collection of claims.  Inside each attribute a claim can be multivalued, we create a claim for each value.
                // It is a ragged multi-dimentional array, where each cell can be of different lenghts.

                for (int attribute = 0; attribute < claimAttributes.AttributeCount; attribute++)
                {
                    Interop.CLAIM_SECURITY_ATTRIBUTE_V1 windowsClaim = ((Interop.CLAIM_SECURITY_ATTRIBUTE_V1*)claimAttributes.Attribute.pAttributeV1)[attribute];

                    string name = Marshal.PtrToStringUni(windowsClaim.Name)!;

                    // the switch was written this way, which appears to have multiple for loops, because each item in the ValueCount is of the same ValueType.  This saves the type check each item.
                    switch (windowsClaim.ValueType)
                    {
                        case Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING:
                            IntPtr[] stringPointers = new IntPtr[windowsClaim.ValueCount];
                            Marshal.Copy(windowsClaim.Values.ppString, stringPointers, 0, (int)windowsClaim.ValueCount);

                            for (int item = 0; item < windowsClaim.ValueCount; item++)
                            {
                                Claim c = new Claim(name, Marshal.PtrToStringUni(stringPointers[item])!, ClaimValueTypes.String, _issuerName, _issuerName, this);
                                c.Properties.Add(propertyValue, string.Empty);
                                instanceClaims.Add(c);
                            }
                            break;

                        case Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64:
                            long[] intValues = new long[windowsClaim.ValueCount];
                            Marshal.Copy(windowsClaim.Values.pInt64, intValues, 0, (int)windowsClaim.ValueCount);

                            for (int item = 0; item < windowsClaim.ValueCount; item++)
                            {
                                Claim c = new Claim(name, intValues[item].ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64, _issuerName, _issuerName, this);
                                c.Properties.Add(propertyValue, string.Empty);
                                instanceClaims.Add(c);
                            }
                            break;


                        case Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64:
                            long[] uintValues = new long[windowsClaim.ValueCount];
                            Marshal.Copy(windowsClaim.Values.pUint64, uintValues, 0, (int)windowsClaim.ValueCount);

                            for (int item = 0; item < windowsClaim.ValueCount; item++)
                            {
                                Claim c = new Claim(name, ((ulong)uintValues[item]).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.UInteger64, _issuerName, _issuerName, this);
                                c.Properties.Add(propertyValue, string.Empty);
                                instanceClaims.Add(c);
                            }
                            break;

                        case Interop.ClaimSecurityAttributeType.CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN:
                            long[] boolValues = new long[windowsClaim.ValueCount];
                            Marshal.Copy(windowsClaim.Values.pUint64, boolValues, 0, (int)windowsClaim.ValueCount);

                            for (int item = 0; item < windowsClaim.ValueCount; item++)
                            {
                                Claim c = new Claim(
                                    name,
                                    ((ulong)boolValues[item] != 0).ToString(),
                                    ClaimValueTypes.Boolean,
                                    _issuerName,
                                    _issuerName,
                                    this);

                                c.Properties.Add(propertyValue, string.Empty);
                                instanceClaims.Add(c);
                            }
                            break;
                    }
                }
            }
            finally
            {
                safeAllocHandle?.Dispose();
            }
        }
    }

    internal enum WinSecurityContext
    {
        Thread = 1, // OpenAsSelf = false
        Process = 2, // OpenAsSelf = true
        Both = 3 // OpenAsSelf = true, then OpenAsSelf = false
    }

    internal enum TokenType : int
    {
        TokenPrimary = 1,
        TokenImpersonation
    }

    internal enum TokenInformationClass : int
    {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin,
        TokenElevationType,
        TokenLinkedToken,
        TokenElevation,
        TokenHasRestrictions,
        TokenAccessInformation,
        TokenVirtualizationAllowed,
        TokenVirtualizationEnabled,
        TokenIntegrityLevel,
        TokenUIAccess,
        TokenMandatoryPolicy,
        TokenLogonSid,
        TokenIsAppContainer,
        TokenCapabilities,
        TokenAppContainerSid,
        TokenAppContainerNumber,
        TokenUserClaimAttributes,
        TokenDeviceClaimAttributes,
        TokenRestrictedUserClaimAttributes,
        TokenRestrictedDeviceClaimAttributes,
        TokenDeviceGroups,
        TokenRestrictedDeviceGroups,
        MaxTokenInfoClass  // MaxTokenInfoClass should always be the last enum
    }
}

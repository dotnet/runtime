// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeAccessTokenHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeAccessTokenHandle(System.IntPtr handle) : base(default(System.IntPtr), default(bool)) { }
        public static Microsoft.Win32.SafeHandles.SafeAccessTokenHandle InvalidHandle { get { throw null; } }
        public override bool IsInvalid { get { throw null; } }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.Security.Principal
{
    public sealed partial class IdentityNotMappedException : System.SystemException
    {
        public IdentityNotMappedException() { }
        public IdentityNotMappedException(string message) { }
        public IdentityNotMappedException(string message, System.Exception inner) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public System.Security.Principal.IdentityReferenceCollection UnmappedIdentities { get { throw null; } }
    }
    public abstract partial class IdentityReference
    {
        internal IdentityReference() { }
        public abstract string Value { get; }
        public abstract override bool Equals(object o);
        public abstract override int GetHashCode();
        public abstract bool IsValidTargetType(System.Type targetType);
        public static bool operator ==(System.Security.Principal.IdentityReference left, System.Security.Principal.IdentityReference right) { throw null; }
        public static bool operator !=(System.Security.Principal.IdentityReference left, System.Security.Principal.IdentityReference right) { throw null; }
        public abstract override string ToString();
        public abstract System.Security.Principal.IdentityReference Translate(System.Type targetType);
    }
    public partial class IdentityReferenceCollection : System.Collections.Generic.ICollection<System.Security.Principal.IdentityReference>, System.Collections.Generic.IEnumerable<System.Security.Principal.IdentityReference>, System.Collections.IEnumerable
    {
        public IdentityReferenceCollection() { }
        public IdentityReferenceCollection(int capacity) { }
        public int Count { get { throw null; } }
        public System.Security.Principal.IdentityReference this[int index] { get { throw null; } set { } }
        bool System.Collections.Generic.ICollection<System.Security.Principal.IdentityReference>.IsReadOnly { get { throw null; } }
        public void Add(System.Security.Principal.IdentityReference identity) { }
        public void Clear() { }
        public bool Contains(System.Security.Principal.IdentityReference identity) { throw null; }
        public void CopyTo(System.Security.Principal.IdentityReference[] array, int offset) { }
        public System.Collections.Generic.IEnumerator<System.Security.Principal.IdentityReference> GetEnumerator() { throw null; }
        public bool Remove(System.Security.Principal.IdentityReference identity) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public System.Security.Principal.IdentityReferenceCollection Translate(System.Type targetType) { throw null; }
        public System.Security.Principal.IdentityReferenceCollection Translate(System.Type targetType, bool forceSuccess) { throw null; }
    }
    public sealed partial class NTAccount : System.Security.Principal.IdentityReference
    {
        public NTAccount(string name) { }
        public NTAccount(string domainName, string accountName) { }
        public override string Value { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsValidTargetType(System.Type targetType) { throw null; }
        public static bool operator ==(System.Security.Principal.NTAccount left, System.Security.Principal.NTAccount right) { throw null; }
        public static bool operator !=(System.Security.Principal.NTAccount left, System.Security.Principal.NTAccount right) { throw null; }
        public override string ToString() { throw null; }
        public override System.Security.Principal.IdentityReference Translate(System.Type targetType) { throw null; }
    }
    public sealed partial class SecurityIdentifier : System.Security.Principal.IdentityReference, System.IComparable<System.Security.Principal.SecurityIdentifier>
    {
        public static readonly int MaxBinaryLength;
        public static readonly int MinBinaryLength;
        public SecurityIdentifier(byte[] binaryForm, int offset) { }
        public SecurityIdentifier(System.IntPtr binaryForm) { }
        public SecurityIdentifier(System.Security.Principal.WellKnownSidType sidType, System.Security.Principal.SecurityIdentifier domainSid) { }
        public SecurityIdentifier(string sddlForm) { }
        public System.Security.Principal.SecurityIdentifier AccountDomainSid { get { throw null; } }
        public int BinaryLength { get { throw null; } }
        public override string Value { get { throw null; } }
        public int CompareTo(System.Security.Principal.SecurityIdentifier sid) { throw null; }
        public override bool Equals(object o) { throw null; }
        public bool Equals(System.Security.Principal.SecurityIdentifier sid) { throw null; }
        public void GetBinaryForm(byte[] binaryForm, int offset) { }
        public override int GetHashCode() { throw null; }
        public bool IsAccountSid() { throw null; }
        public bool IsEqualDomainSid(System.Security.Principal.SecurityIdentifier sid) { throw null; }
        public override bool IsValidTargetType(System.Type targetType) { throw null; }
        public bool IsWellKnown(System.Security.Principal.WellKnownSidType type) { throw null; }
        public static bool operator ==(System.Security.Principal.SecurityIdentifier left, System.Security.Principal.SecurityIdentifier right) { throw null; }
        public static bool operator !=(System.Security.Principal.SecurityIdentifier left, System.Security.Principal.SecurityIdentifier right) { throw null; }
        public override string ToString() { throw null; }
        public override System.Security.Principal.IdentityReference Translate(System.Type targetType) { throw null; }
    }
    [System.FlagsAttribute]
    public enum TokenAccessLevels
    {
        AdjustDefault = 128,
        AdjustGroups = 64,
        AdjustPrivileges = 32,
        AdjustSessionId = 256,
        AllAccess = 983551,
        AssignPrimary = 1,
        Duplicate = 2,
        Impersonate = 4,
        MaximumAllowed = 33554432,
        Query = 8,
        QuerySource = 16,
        Read = 131080,
        Write = 131296,
    }
    public enum WellKnownSidType
    {
        /// <summary>Indicates a null SID.</summary>
        NullSid = 0,
        /// <summary>Indicates a SID that matches everyone.</summary>
        WorldSid = 1,
        /// <summary>Indicates a local SID.</summary>
        LocalSid = 2,
        /// <summary>Indicates a SID that matches the owner or creator of an object.</summary>
        CreatorOwnerSid = 3,
        /// <summary>Indicates a SID that matches the creator group of an object.</summary>
        CreatorGroupSid = 4,
        /// <summary>Indicates a creator owner server SID.</summary>
        CreatorOwnerServerSid = 5,
        /// <summary>Indicates a creator group server SID.</summary>
        CreatorGroupServerSid = 6,
        /// <summary>Indicates a SID for the Windows NT authority account.</summary>
        NTAuthoritySid = 7,
        /// <summary>Indicates a SID for a dial-up account.</summary>
        DialupSid = 8,
        /// <summary>Indicates a SID for a network account. This SID is added to the process of a token when it logs on across a network.</summary>
        NetworkSid = 9,
        /// <summary>Indicates a SID for a batch process. This SID is added to the process of a token when it logs on as a batch job.</summary>
        BatchSid = 10,
        /// <summary>Indicates a SID for an interactive account. This SID is added to the process of a token when it logs on interactively.</summary>
        InteractiveSid = 11,
        /// <summary>Indicates a SID for a service. This SID is added to the process of a token when it logs on as a service.</summary>
        ServiceSid = 12,
        /// <summary>Indicates a SID for the anonymous account.</summary>
        AnonymousSid = 13,
        /// <summary>Indicates a proxy SID.</summary>
        ProxySid = 14,
        /// <summary>Indicates a SID for an enterprise controller.</summary>
        EnterpriseControllersSid = 15,
        /// <summary>Indicates a SID for self.</summary>
        SelfSid = 16,
        /// <summary>Indicates a SID that matches any authenticated user.</summary>
        AuthenticatedUserSid = 17,
        /// <summary>Indicates a SID for restricted code.</summary>
        RestrictedCodeSid = 18,
        /// <summary>Indicates a SID that matches a terminal server account.</summary>
        TerminalServerSid = 19,
        /// <summary>Indicates a SID that matches remote logons.</summary>
        RemoteLogonIdSid = 20,
        /// <summary>Indicates a SID that matches logon IDs.</summary>
        LogonIdsSid = 21,
        /// <summary>Indicates a SID that matches the local system.</summary>
        LocalSystemSid = 22,
        /// <summary>Indicates a SID that matches a local service.</summary>
        LocalServiceSid = 23,
        /// <summary>Indicates a SID that matches a network service.</summary>
        NetworkServiceSid = 24,
        /// <summary>Indicates a SID that matches the domain account.</summary>
        BuiltinDomainSid = 25,
        /// <summary>Indicates a SID that matches the administrator group.</summary>
        BuiltinAdministratorsSid = 26,
        /// <summary>Indicates a SID that matches built-in user accounts.</summary>
        BuiltinUsersSid = 27,
        /// <summary>Indicates a SID that matches the guest account.</summary>
        BuiltinGuestsSid = 28,
        /// <summary>Indicates a SID that matches the power users group.</summary>
        BuiltinPowerUsersSid = 29,
        /// <summary>Indicates a SID that matches the account operators account.</summary>
        BuiltinAccountOperatorsSid = 30,
        /// <summary>Indicates a SID that matches the system operators group.</summary>
        BuiltinSystemOperatorsSid = 31,
        /// <summary>Indicates a SID that matches the print operators group.</summary>
        BuiltinPrintOperatorsSid = 32,
        /// <summary>Indicates a SID that matches the backup operators group.</summary>
        BuiltinBackupOperatorsSid = 33,
        /// <summary>Indicates a SID that matches the replicator account.</summary>
        BuiltinReplicatorSid = 34,
        /// <summary>Indicates a SID that matches pre-Windows 2000 compatible accounts.</summary>
        BuiltinPreWindows2000CompatibleAccessSid = 35,
        /// <summary>Indicates a SID that matches remote desktop users.</summary>
        BuiltinRemoteDesktopUsersSid = 36,
        /// <summary>Indicates a SID that matches the network operators group.</summary>
        BuiltinNetworkConfigurationOperatorsSid = 37,
        /// <summary>Indicates a SID that matches the account administrator's account.</summary>
        AccountAdministratorSid = 38,
        /// <summary>Indicates a SID that matches the account guest group.</summary>
        AccountGuestSid = 39,
        /// <summary>Indicates a SID that matches account Kerberos target group.</summary>
        AccountKrbtgtSid = 40,
        /// <summary>Indicates a SID that matches the account domain administrator group.</summary>
        AccountDomainAdminsSid = 41,
        /// <summary>Indicates a SID that matches the account domain users group.</summary>
        AccountDomainUsersSid = 42,
        /// <summary>Indicates a SID that matches the account domain guests group.</summary>
        AccountDomainGuestsSid = 43,
        /// <summary>Indicates a SID that matches the account computer group.</summary>
        AccountComputersSid = 44,
        /// <summary>Indicates a SID that matches the account controller group.</summary>
        AccountControllersSid = 45,
        /// <summary>Indicates a SID that matches the certificate administrators group.</summary>
        AccountCertAdminsSid = 46,
        /// <summary>Indicates a SID that matches the schema administrators group.</summary>
        AccountSchemaAdminsSid = 47,
        /// <summary>Indicates a SID that matches the enterprise administrators group.</summary>
        AccountEnterpriseAdminsSid = 48,
        /// <summary>Indicates a SID that matches the policy administrators group.</summary>
        AccountPolicyAdminsSid = 49,
        /// <summary>Indicates a SID that matches the RAS and IAS server account.</summary>
        AccountRasAndIasServersSid = 50,
        /// <summary>Indicates a SID present when the Microsoft NTLM authentication package authenticated the client.</summary>
        NtlmAuthenticationSid = 51,
        /// <summary>Indicates a SID present when the Microsoft Digest authentication package authenticated the client.</summary>
        DigestAuthenticationSid = 52,
        /// <summary>Indicates a SID present when the Secure Channel (SSL/TLS) authentication package authenticated the client.</summary>
        SChannelAuthenticationSid = 53,
        /// <summary>Indicates a SID present when the user authenticated from within the forest or across a trust that does not have the selective authentication option enabled. If this SID is present, then <see cref="WinOtherOrganizationSid"/> cannot be present.</summary>
        ThisOrganizationSid = 54,
        /// <summary>Indicates a SID present when the user authenticated across a forest with the selective authentication option enabled. If this SID is present, then <see cref="WinThisOrganizationSid"/> cannot be present.</summary>
        OtherOrganizationSid = 55,
        /// <summary>Indicates a SID that allows a user to create incoming forest trusts. It is added to the token of users who are a member of the Incoming Forest Trust Builders built-in group in the root domain of the forest.</summary>
        BuiltinIncomingForestTrustBuildersSid = 56,
        /// <summary>Indicates a SID that matches the performance monitor user group.</summary>
        BuiltinPerformanceMonitoringUsersSid = 57,
        /// <summary>Indicates a SID that matches the performance log user group.</summary>
        BuiltinPerformanceLoggingUsersSid = 58,
        /// <summary>Indicates a SID that matches the Windows Authorization Access group.</summary>
        BuiltinAuthorizationAccessSid = 59,
        /// <summary>Indicates a SID is present in a server that can issue terminal server licenses.</summary>
        WinBuiltinTerminalServerLicenseServersSid = 60,
        [System.ObsoleteAttribute("This member has been depcreated and is only maintained for backwards compatability. WellKnownSidType values greater than MaxDefined may be defined in future releases.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        MaxDefined = WinBuiltinTerminalServerLicenseServersSid,
        /// <summary>Indicates a SID that matches the distributed COM user group.</summary>
        WinBuiltinDCOMUsersSid = 61,
        /// <summary>Indicates a SID that matches the Internet built-in user group.</summary>
        WinBuiltinIUsersSid = 62,
        /// <summary>Indicates a SID that matches the Internet user group.</summary>
        WinIUserSid = 63,
        /// <summary>Indicates a SID that allows a user to use cryptographic operations. It is added to the token of users who are a member of the CryptoOperators built-in group. </summary>
        WinBuiltinCryptoOperatorsSid = 64,
        /// <summary>Indicates a SID that matches an untrusted label.</summary>
        WinUntrustedLabelSid = 65,
        /// <summary>Indicates a SID that matches an low level of trust label.</summary>
        WinLowLabelSid = 66,
        /// <summary>Indicates a SID that matches an medium level of trust label.</summary>
        WinMediumLabelSid = 67,
        /// <summary>Indicates a SID that matches a high level of trust label.</summary>
        WinHighLabelSid = 68,
        /// <summary>Indicates a SID that matches a system label.</summary>
        WinSystemLabelSid = 69,
        /// <summary>Indicates a SID that matches a write restricted code group.</summary>
        WinWriteRestrictedCodeSid = 70,
        /// <summary>Indicates a SID that matches a creator and owner rights group.</summary>
        WinCreatorOwnerRightsSid = 71,
        /// <summary>Indicates a SID that matches a cacheable principals group.</summary>
        WinCacheablePrincipalsGroupSid = 72,
        /// <summary>Indicates a SID that matches a non-cacheable principals group.</summary>
        WinNonCacheablePrincipalsGroupSid = 73,
        /// <summary>Indicates a SID that matches an enterprise wide read-only controllers group.</summary>
        WinEnterpriseReadonlyControllersSid = 74,
        /// <summary>Indicates a SID that matches an account read-only controllers group.</summary>
        WinAccountReadonlyControllersSid = 75,
        /// <summary>Indicates a SID that matches an event log readers group.</summary>
        WinBuiltinEventLogReadersGroup = 76,
        /// <summary>Indicates a SID that matches a read-only enterprise domain controller.</summary>
        WinNewEnterpriseReadonlyControllersSid = 77,
        /// <summary>Indicates a SID that matches the built-in DCOM certification services access group.</summary>
        WinBuiltinCertSvcDComAccessGroup = 78,
        /// <summary>Indicates a SID that matches the medium plus integrity label.</summary>
        WinMediumPlusLabelSid = 79,
        /// <summary>Indicates a SID that matches a local logon group.</summary>
        WinLocalLogonSid = 80,
        /// <summary>Indicates a SID that matches a console logon group.</summary>
        WinConsoleLogonSid = 81,
        /// <summary>Indicates a SID that matches a certificate for the given organization.</summary>
        WinThisOrganizationCertificateSid = 82,
        /// <summary>Indicates a SID that matches the application package authority.</summary>
        WinApplicationPackageAuthoritySid = 83,
        /// <summary>Indicates a SID that applies to all app containers.</summary>
        WinBuiltinAnyPackageSid = 84,
        /// <summary>Indicates a SID of Internet client capability for app containers.</summary>
        WinCapabilityInternetClientSid = 85,
        /// <summary>Indicates a SID of Internet client and server capability for app containers.</summary>
        WinCapabilityInternetClientServerSid = 86,
        /// <summary>Indicates a SID of private network client and server capability for app containers.</summary>
        WinCapabilityPrivateNetworkClientServerSid = 87,
        /// <summary>Indicates a SID for pictures library capability for app containers.</summary>
        WinCapabilityPicturesLibrarySid = 88,
        /// <summary>Indicates a SID for videos library capability for app containers.</summary>
        WinCapabilityVideosLibrarySid = 89,
        /// <summary>Indicates a SID for music library capability for app containers.</summary>
        WinCapabilityMusicLibrarySid = 90,
        /// <summary>Indicates a SID for documents library capability for app containers.</summary>
        WinCapabilityDocumentsLibrarySid = 91,
        /// <summary>Indicates a SID for shared user certificates capability for app containers.</summary>
        WinCapabilitySharedUserCertificatesSid = 92,
        /// <summary>Indicates a SID for Windows credentials capability for app containers.</summary>
        WinCapabilityEnterpriseAuthenticationSid = 93,
        /// <summary>Indicates a SID for removable storage capability for app containers.</summary>
        WinCapabilityRemovableStorageSid = 94
    }
    public enum WindowsBuiltInRole
    {
        AccountOperator = 548,
        Administrator = 544,
        BackupOperator = 551,
        Guest = 546,
        PowerUser = 547,
        PrintOperator = 550,
        Replicator = 552,
        SystemOperator = 549,
        User = 545,
    }
    public partial class WindowsIdentity : System.Security.Claims.ClaimsIdentity, System.IDisposable, System.Runtime.Serialization.ISerializable, System.Runtime.Serialization.IDeserializationCallback
    {
        public new const string DefaultIssuer = "AD AUTHORITY";
        public WindowsIdentity(System.IntPtr userToken) { }
        public WindowsIdentity(System.IntPtr userToken, string type) { }
        public WindowsIdentity(string sUserPrincipalName) { }
        public WindowsIdentity(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public Microsoft.Win32.SafeHandles.SafeAccessTokenHandle AccessToken { get { throw null; } }
        public sealed override string AuthenticationType { get { throw null; } }
        public override System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> Claims { get { throw null; } }
        public System.Security.Principal.IdentityReferenceCollection Groups { get { throw null; } }
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { throw null; } }
        public virtual bool IsAnonymous { get { throw null; } }
        public override bool IsAuthenticated { get { throw null; } }
        public virtual bool IsGuest { get { throw null; } }
        public virtual bool IsSystem { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.Security.Principal.SecurityIdentifier Owner { get { throw null; } }
        public virtual IntPtr Token { get { throw null; } }
        public System.Security.Principal.SecurityIdentifier User { get { throw null; } }
        public override System.Security.Claims.ClaimsIdentity Clone() { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.Security.Principal.WindowsIdentity GetAnonymous() { throw null; }
        public static System.Security.Principal.WindowsIdentity GetCurrent() { throw null; }
        public static System.Security.Principal.WindowsIdentity GetCurrent(bool ifImpersonating) { throw null; }
        public static System.Security.Principal.WindowsIdentity GetCurrent(System.Security.Principal.TokenAccessLevels desiredAccess) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public static void RunImpersonated(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle safeAccessTokenHandle, System.Action action) { }
        public static T RunImpersonated<T>(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle safeAccessTokenHandle, System.Func<T> func) { throw null; }
    }
    public partial class WindowsPrincipal : System.Security.Claims.ClaimsPrincipal
    {
        public WindowsPrincipal(System.Security.Principal.WindowsIdentity ntIdentity) { }
        public override System.Security.Principal.IIdentity Identity { get { throw null; } }
        public virtual bool IsInRole(int rid) { throw null; }
        public virtual bool IsInRole(System.Security.Principal.SecurityIdentifier sid) { throw null; }
        public virtual bool IsInRole(System.Security.Principal.WindowsBuiltInRole role) { throw null; }
        public override bool IsInRole(string role) { throw null; }
    }
}

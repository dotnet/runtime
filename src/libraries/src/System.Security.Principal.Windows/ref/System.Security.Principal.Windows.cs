// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class SafeAccessTokenHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeAccessTokenHandle(System.IntPtr handle) : base(default(System.IntPtr), default(bool)) { }
        public static Microsoft.Win32.SafeHandles.SafeAccessTokenHandle InvalidHandle {[System.Security.SecurityCriticalAttribute]get { return default(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle); } }
        public override bool IsInvalid {[System.Security.SecurityCriticalAttribute]get { return default(bool); } }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.Security.Principal
{
    public sealed partial class IdentityNotMappedException : System.Exception
    {
        public IdentityNotMappedException() { }
        public IdentityNotMappedException(string message) { }
        public IdentityNotMappedException(string message, System.Exception inner) { }
        public System.Security.Principal.IdentityReferenceCollection UnmappedIdentities { get { return default(System.Security.Principal.IdentityReferenceCollection); } }
    }
    public abstract partial class IdentityReference
    {
        internal IdentityReference() { }
        public abstract string Value { get; }
        public abstract override bool Equals(object o);
        public abstract override int GetHashCode();
        public abstract bool IsValidTargetType(System.Type targetType);
        public static bool operator ==(System.Security.Principal.IdentityReference left, System.Security.Principal.IdentityReference right) { return default(bool); }
        public static bool operator !=(System.Security.Principal.IdentityReference left, System.Security.Principal.IdentityReference right) { return default(bool); }
        public abstract override string ToString();
        public abstract System.Security.Principal.IdentityReference Translate(System.Type targetType);
    }
    public partial class IdentityReferenceCollection : System.Collections.Generic.ICollection<System.Security.Principal.IdentityReference>, System.Collections.Generic.IEnumerable<System.Security.Principal.IdentityReference>, System.Collections.IEnumerable
    {
        public IdentityReferenceCollection() { }
        public IdentityReferenceCollection(int capacity) { }
        public int Count { get { return default(int); } }
        public System.Security.Principal.IdentityReference this[int index] { get { return default(System.Security.Principal.IdentityReference); } set { } }
        bool System.Collections.Generic.ICollection<System.Security.Principal.IdentityReference>.IsReadOnly { get { return default(bool); } }
        public void Add(System.Security.Principal.IdentityReference identity) { }
        public void Clear() { }
        public bool Contains(System.Security.Principal.IdentityReference identity) { return default(bool); }
        public void CopyTo(System.Security.Principal.IdentityReference[] array, int offset) { }
        public System.Collections.Generic.IEnumerator<System.Security.Principal.IdentityReference> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Security.Principal.IdentityReference>); }
        public bool Remove(System.Security.Principal.IdentityReference identity) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public System.Security.Principal.IdentityReferenceCollection Translate(System.Type targetType) { return default(System.Security.Principal.IdentityReferenceCollection); }
        public System.Security.Principal.IdentityReferenceCollection Translate(System.Type targetType, bool forceSuccess) { return default(System.Security.Principal.IdentityReferenceCollection); }
    }
    public sealed partial class NTAccount : System.Security.Principal.IdentityReference
    {
        public NTAccount(string name) { }
        public NTAccount(string domainName, string accountName) { }
        public override string Value { get { return default(string); } }
        public override bool Equals(object o) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override bool IsValidTargetType(System.Type targetType) { return default(bool); }
        public static bool operator ==(System.Security.Principal.NTAccount left, System.Security.Principal.NTAccount right) { return default(bool); }
        public static bool operator !=(System.Security.Principal.NTAccount left, System.Security.Principal.NTAccount right) { return default(bool); }
        public override string ToString() { return default(string); }
        public override System.Security.Principal.IdentityReference Translate(System.Type targetType) { return default(System.Security.Principal.IdentityReference); }
    }
    public sealed partial class SecurityIdentifier : System.Security.Principal.IdentityReference, System.IComparable<System.Security.Principal.SecurityIdentifier>
    {
        public static readonly int MaxBinaryLength;
        public static readonly int MinBinaryLength;
        public SecurityIdentifier(byte[] binaryForm, int offset) { }
        public SecurityIdentifier(System.IntPtr binaryForm) { }
        public SecurityIdentifier(System.Security.Principal.WellKnownSidType sidType, System.Security.Principal.SecurityIdentifier domainSid) { }
        public SecurityIdentifier(string sddlForm) { }
        public System.Security.Principal.SecurityIdentifier AccountDomainSid { get { return default(System.Security.Principal.SecurityIdentifier); } }
        public int BinaryLength { get { return default(int); } }
        public override string Value { get { return default(string); } }
        public int CompareTo(System.Security.Principal.SecurityIdentifier sid) { return default(int); }
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(System.Security.Principal.SecurityIdentifier sid) { return default(bool); }
        public void GetBinaryForm(byte[] binaryForm, int offset) { }
        public override int GetHashCode() { return default(int); }
        public bool IsAccountSid() { return default(bool); }
        public bool IsEqualDomainSid(System.Security.Principal.SecurityIdentifier sid) { return default(bool); }
        public override bool IsValidTargetType(System.Type targetType) { return default(bool); }
        public bool IsWellKnown(System.Security.Principal.WellKnownSidType type) { return default(bool); }
        public static bool operator ==(System.Security.Principal.SecurityIdentifier left, System.Security.Principal.SecurityIdentifier right) { return default(bool); }
        public static bool operator !=(System.Security.Principal.SecurityIdentifier left, System.Security.Principal.SecurityIdentifier right) { return default(bool); }
        public override string ToString() { return default(string); }
        public override System.Security.Principal.IdentityReference Translate(System.Type targetType) { return default(System.Security.Principal.IdentityReference); }
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
        AccountAdministratorSid = 38,
        AccountCertAdminsSid = 46,
        AccountComputersSid = 44,
        AccountControllersSid = 45,
        AccountDomainAdminsSid = 41,
        AccountDomainGuestsSid = 43,
        AccountDomainUsersSid = 42,
        AccountEnterpriseAdminsSid = 48,
        AccountGuestSid = 39,
        AccountKrbtgtSid = 40,
        AccountPolicyAdminsSid = 49,
        AccountRasAndIasServersSid = 50,
        AccountSchemaAdminsSid = 47,
        AnonymousSid = 13,
        AuthenticatedUserSid = 17,
        BatchSid = 10,
        BuiltinAccountOperatorsSid = 30,
        BuiltinAdministratorsSid = 26,
        BuiltinAuthorizationAccessSid = 59,
        BuiltinBackupOperatorsSid = 33,
        BuiltinDomainSid = 25,
        BuiltinGuestsSid = 28,
        BuiltinIncomingForestTrustBuildersSid = 56,
        BuiltinNetworkConfigurationOperatorsSid = 37,
        BuiltinPerformanceLoggingUsersSid = 58,
        BuiltinPerformanceMonitoringUsersSid = 57,
        BuiltinPowerUsersSid = 29,
        BuiltinPreWindows2000CompatibleAccessSid = 35,
        BuiltinPrintOperatorsSid = 32,
        BuiltinRemoteDesktopUsersSid = 36,
        BuiltinReplicatorSid = 34,
        BuiltinSystemOperatorsSid = 31,
        BuiltinUsersSid = 27,
        CreatorGroupServerSid = 6,
        CreatorGroupSid = 4,
        CreatorOwnerServerSid = 5,
        CreatorOwnerSid = 3,
        DialupSid = 8,
        DigestAuthenticationSid = 52,
        EnterpriseControllersSid = 15,
        InteractiveSid = 11,
        LocalServiceSid = 23,
        LocalSid = 2,
        LocalSystemSid = 22,
        LogonIdsSid = 21,
        MaxDefined = 60,
        NetworkServiceSid = 24,
        NetworkSid = 9,
        NTAuthoritySid = 7,
        NtlmAuthenticationSid = 51,
        NullSid = 0,
        OtherOrganizationSid = 55,
        ProxySid = 14,
        RemoteLogonIdSid = 20,
        RestrictedCodeSid = 18,
        SChannelAuthenticationSid = 53,
        SelfSid = 16,
        ServiceSid = 12,
        TerminalServerSid = 19,
        ThisOrganizationSid = 54,
        WinBuiltinTerminalServerLicenseServersSid = 60,
        WorldSid = 1,
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
    public partial class WindowsIdentity : System.Security.Claims.ClaimsIdentity, System.IDisposable
    {
        public WindowsIdentity(System.IntPtr userToken) { }
        public WindowsIdentity(System.IntPtr userToken, string type) { }
        public WindowsIdentity(string sUserPrincipalName) { }
        public Microsoft.Win32.SafeHandles.SafeAccessTokenHandle AccessToken {[System.Security.SecurityCriticalAttribute]get { return default(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle); } }
        public System.Security.Principal.IdentityReferenceCollection Groups { get { return default(System.Security.Principal.IdentityReferenceCollection); } }
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { return default(System.Security.Principal.TokenImpersonationLevel); } }
        public virtual bool IsAnonymous { get { return default(bool); } }
        public virtual bool IsGuest { get { return default(bool); } }
        public virtual bool IsSystem { get { return default(bool); } }
        public System.Security.Principal.SecurityIdentifier Owner { get { return default(System.Security.Principal.SecurityIdentifier); } }
        public System.Security.Principal.SecurityIdentifier User { get { return default(System.Security.Principal.SecurityIdentifier); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.Security.Principal.WindowsIdentity GetAnonymous() { return default(System.Security.Principal.WindowsIdentity); }
        public static System.Security.Principal.WindowsIdentity GetCurrent() { return default(System.Security.Principal.WindowsIdentity); }
        public static System.Security.Principal.WindowsIdentity GetCurrent(bool ifImpersonating) { return default(System.Security.Principal.WindowsIdentity); }
        public static System.Security.Principal.WindowsIdentity GetCurrent(System.Security.Principal.TokenAccessLevels desiredAccess) { return default(System.Security.Principal.WindowsIdentity); }
        public static void RunImpersonated(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle safeAccessTokenHandle, System.Action action) { }
        public static T RunImpersonated<T>(Microsoft.Win32.SafeHandles.SafeAccessTokenHandle safeAccessTokenHandle, System.Func<T> func) { return default(T); }
    }
    public partial class WindowsPrincipal : System.Security.Claims.ClaimsPrincipal
    {
        public WindowsPrincipal(System.Security.Principal.WindowsIdentity ntIdentity) { }
        public virtual bool IsInRole(int rid) { return default(bool); }
        public virtual bool IsInRole(System.Security.Principal.SecurityIdentifier sid) { return default(bool); }
        public virtual bool IsInRole(System.Security.Principal.WindowsBuiltInRole role) { return default(bool); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices.AccountManagement
{
    public partial class AdvancedFilters
    {
        protected internal AdvancedFilters(System.DirectoryServices.AccountManagement.Principal p) { }
        public void AccountExpirationDate(System.DateTime expirationTime, System.DirectoryServices.AccountManagement.MatchType match) { }
        public void AccountLockoutTime(System.DateTime lockoutTime, System.DirectoryServices.AccountManagement.MatchType match) { }
        protected void AdvancedFilterSet(string attribute, object value, System.Type objectType, System.DirectoryServices.AccountManagement.MatchType mt) { }
        public void BadLogonCount(int badLogonCount, System.DirectoryServices.AccountManagement.MatchType match) { }
        public void LastBadPasswordAttempt(System.DateTime lastAttempt, System.DirectoryServices.AccountManagement.MatchType match) { }
        public void LastLogonTime(System.DateTime logonTime, System.DirectoryServices.AccountManagement.MatchType match) { }
        public void LastPasswordSetTime(System.DateTime passwordSetTime, System.DirectoryServices.AccountManagement.MatchType match) { }
    }
    [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
    public partial class AuthenticablePrincipal : System.DirectoryServices.AccountManagement.Principal
    {
        protected internal AuthenticablePrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) { }
        protected internal AuthenticablePrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName, string password, bool enabled) { }
        public System.DateTime? AccountExpirationDate { get { throw null; } set { } }
        public System.DateTime? AccountLockoutTime { get { throw null; } }
        public virtual System.DirectoryServices.AccountManagement.AdvancedFilters AdvancedSearchFilter { get { throw null; } }
        public bool AllowReversiblePasswordEncryption { get { throw null; } set { } }
        public int BadLogonCount { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { throw null; } }
        public bool DelegationPermitted { get { throw null; } set { } }
        public bool? Enabled { get { throw null; } set { } }
        public string HomeDirectory { get { throw null; } set { } }
        public string HomeDrive { get { throw null; } set { } }
        public System.DateTime? LastBadPasswordAttempt { get { throw null; } }
        public System.DateTime? LastLogon { get { throw null; } }
        public System.DateTime? LastPasswordSet { get { throw null; } }
        public bool PasswordNeverExpires { get { throw null; } set { } }
        public bool PasswordNotRequired { get { throw null; } set { } }
        public byte[] PermittedLogonTimes { get { throw null; } set { } }
        public System.DirectoryServices.AccountManagement.PrincipalValueCollection<string> PermittedWorkstations { get { throw null; } }
        public string ScriptPath { get { throw null; } set { } }
        public bool SmartcardLogonRequired { get { throw null; } set { } }
        public bool UserCannotChangePassword { get { throw null; } set { } }
        public void ChangePassword(string oldPassword, string newPassword) { }
        public void ExpirePasswordNow() { }
        public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByBadPasswordAttempt(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByBadPasswordAttempt<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByExpirationTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByExpirationTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByLockoutTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByLockoutTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByLogonTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByLogonTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByPasswordSetTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByPasswordSetTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public bool IsAccountLockedOut() { throw null; }
        public void RefreshExpiredPassword() { }
        public void SetPassword(string newPassword) { }
        public void UnlockAccount() { }
    }
    [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
    public partial class ComputerPrincipal : System.DirectoryServices.AccountManagement.AuthenticablePrincipal
    {
        public ComputerPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
        public ComputerPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName, string password, bool enabled) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
        public System.DirectoryServices.AccountManagement.PrincipalValueCollection<string> ServicePrincipalNames { get { throw null; } }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByBadPasswordAttempt(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByExpirationTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.ComputerPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public static new System.DirectoryServices.AccountManagement.ComputerPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByLockoutTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByLogonTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByPasswordSetTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
    }
    [System.FlagsAttribute]
    public enum ContextOptions
    {
        Negotiate = 1,
        SimpleBind = 2,
        SecureSocketLayer = 4,
        Signing = 8,
        Sealing = 16,
        ServerBind = 32,
    }
    public enum ContextType
    {
        Machine = 0,
        Domain = 1,
        ApplicationDirectory = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class DirectoryObjectClassAttribute : System.Attribute
    {
        public DirectoryObjectClassAttribute(string objectClass) { }
        public System.DirectoryServices.AccountManagement.ContextType? Context { get { throw null; } }
        public string ObjectClass { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=true)]
    public sealed partial class DirectoryPropertyAttribute : System.Attribute
    {
        public DirectoryPropertyAttribute(string schemaAttributeName) { }
        public System.DirectoryServices.AccountManagement.ContextType? Context { get { throw null; } set { } }
        public string SchemaAttributeName { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class DirectoryRdnPrefixAttribute : System.Attribute
    {
        public DirectoryRdnPrefixAttribute(string rdnPrefix) { }
        public System.DirectoryServices.AccountManagement.ContextType? Context { get { throw null; } }
        public string RdnPrefix { get { throw null; } }
    }
    [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
    public partial class GroupPrincipal : System.DirectoryServices.AccountManagement.Principal
    {
        public GroupPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) { }
        public GroupPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName) { }
        public System.DirectoryServices.AccountManagement.GroupScope? GroupScope { get { throw null; } set { } }
        public bool? IsSecurityGroup { get { throw null; } set { } }
        public System.DirectoryServices.AccountManagement.PrincipalCollection Members { get { throw null; } }
        public override void Dispose() { }
        public static new System.DirectoryServices.AccountManagement.GroupPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public static new System.DirectoryServices.AccountManagement.GroupPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { throw null; }
        public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetMembers() { throw null; }
        public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetMembers(bool recursive) { throw null; }
    }
    public enum GroupScope
    {
        Local = 0,
        Global = 1,
        Universal = 2,
    }
    public enum IdentityType
    {
        SamAccountName = 0,
        Name = 1,
        UserPrincipalName = 2,
        DistinguishedName = 3,
        Sid = 4,
        Guid = 5,
    }
    public enum MatchType
    {
        Equals = 0,
        NotEquals = 1,
        GreaterThan = 2,
        GreaterThanOrEquals = 3,
        LessThan = 4,
        LessThanOrEquals = 5,
    }
    public partial class MultipleMatchesException : System.DirectoryServices.AccountManagement.PrincipalException
    {
        public MultipleMatchesException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected MultipleMatchesException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MultipleMatchesException(string message) { }
        public MultipleMatchesException(string message, System.Exception innerException) { }
    }
    public partial class NoMatchingPrincipalException : System.DirectoryServices.AccountManagement.PrincipalException
    {
        public NoMatchingPrincipalException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected NoMatchingPrincipalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NoMatchingPrincipalException(string message) { }
        public NoMatchingPrincipalException(string message, System.Exception innerException) { }
    }
    public partial class PasswordException : System.DirectoryServices.AccountManagement.PrincipalException
    {
        public PasswordException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected PasswordException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PasswordException(string message) { }
        public PasswordException(string message, System.Exception innerException) { }
    }
    public abstract partial class Principal : System.IDisposable
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected Principal() { }
        public System.DirectoryServices.AccountManagement.PrincipalContext Context { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal System.DirectoryServices.AccountManagement.PrincipalContext ContextRaw { get { throw null; } set { } }
        public System.DirectoryServices.AccountManagement.ContextType ContextType { get { throw null; } }
        public string Description { get { throw null; } set { } }
        public string DisplayName { get { throw null; } set { } }
        public string DistinguishedName { get { throw null; } }
        public System.Guid? Guid { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string SamAccountName { get { throw null; } set { } }
        public System.Security.Principal.SecurityIdentifier Sid { get { throw null; } }
        public string StructuralObjectClass { get { throw null; } }
        public string UserPrincipalName { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected void CheckDisposedOrDeleted() { }
        public void Delete() { }
        public virtual void Dispose() { }
        public override bool Equals(object o) { throw null; }
        protected object[] ExtensionGet(string attribute) { throw null; }
        protected void ExtensionSet(string attribute, object value) { }
        public static System.DirectoryServices.AccountManagement.Principal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public static System.DirectoryServices.AccountManagement.Principal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected static System.DirectoryServices.AccountManagement.Principal FindByIdentityWithType(System.DirectoryServices.AccountManagement.PrincipalContext context, System.Type principalType, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected static System.DirectoryServices.AccountManagement.Principal FindByIdentityWithType(System.DirectoryServices.AccountManagement.PrincipalContext context, System.Type principalType, string identityValue) { throw null; }
        public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetGroups() { throw null; }
        public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetGroups(System.DirectoryServices.AccountManagement.PrincipalContext contextToQuery) { throw null; }
        public override int GetHashCode() { throw null; }
        public object GetUnderlyingObject() { throw null; }
        public System.Type GetUnderlyingObjectType() { throw null; }
        public bool IsMemberOf(System.DirectoryServices.AccountManagement.GroupPrincipal group) { throw null; }
        public bool IsMemberOf(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public void Save() { }
        public void Save(System.DirectoryServices.AccountManagement.PrincipalContext context) { }
        public override string ToString() { throw null; }
    }
    public partial class PrincipalCollection : System.Collections.Generic.ICollection<System.DirectoryServices.AccountManagement.Principal>, System.Collections.Generic.IEnumerable<System.DirectoryServices.AccountManagement.Principal>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal PrincipalCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void Add(System.DirectoryServices.AccountManagement.ComputerPrincipal computer) { }
        public void Add(System.DirectoryServices.AccountManagement.GroupPrincipal group) { }
        public void Add(System.DirectoryServices.AccountManagement.Principal principal) { }
        public void Add(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { }
        public void Add(System.DirectoryServices.AccountManagement.UserPrincipal user) { }
        public void Clear() { }
        public bool Contains(System.DirectoryServices.AccountManagement.ComputerPrincipal computer) { throw null; }
        public bool Contains(System.DirectoryServices.AccountManagement.GroupPrincipal group) { throw null; }
        public bool Contains(System.DirectoryServices.AccountManagement.Principal principal) { throw null; }
        public bool Contains(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public bool Contains(System.DirectoryServices.AccountManagement.UserPrincipal user) { throw null; }
        public void CopyTo(System.DirectoryServices.AccountManagement.Principal[] array, int index) { }
        public System.Collections.Generic.IEnumerator<System.DirectoryServices.AccountManagement.Principal> GetEnumerator() { throw null; }
        public bool Remove(System.DirectoryServices.AccountManagement.ComputerPrincipal computer) { throw null; }
        public bool Remove(System.DirectoryServices.AccountManagement.GroupPrincipal group) { throw null; }
        public bool Remove(System.DirectoryServices.AccountManagement.Principal principal) { throw null; }
        public bool Remove(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public bool Remove(System.DirectoryServices.AccountManagement.UserPrincipal user) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class PrincipalContext : System.IDisposable
    {
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType) { }
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name) { }
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container) { }
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container, System.DirectoryServices.AccountManagement.ContextOptions options) { }
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container, System.DirectoryServices.AccountManagement.ContextOptions options, string userName, string password) { }
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string userName, string password) { }
        public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container, string userName, string password) { }
        public string ConnectedServer { get { throw null; } }
        public string Container { get { throw null; } }
        public System.DirectoryServices.AccountManagement.ContextType ContextType { get { throw null; } }
        public string Name { get { throw null; } }
        public System.DirectoryServices.AccountManagement.ContextOptions Options { get { throw null; } }
        public string UserName { get { throw null; } }
        public void Dispose() { }
        public bool ValidateCredentials(string userName, string password) { throw null; }
        public bool ValidateCredentials(string userName, string password, System.DirectoryServices.AccountManagement.ContextOptions options) { throw null; }
    }
    public abstract partial class PrincipalException : System.SystemException
    {
        private protected PrincipalException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected PrincipalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrincipalExistsException : System.DirectoryServices.AccountManagement.PrincipalException
    {
        public PrincipalExistsException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected PrincipalExistsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context){ }
        public PrincipalExistsException(string message){ }
        public PrincipalExistsException(string message, System.Exception innerException) { }
    }
    public partial class PrincipalOperationException : System.DirectoryServices.AccountManagement.PrincipalException
    {
        public PrincipalOperationException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected PrincipalOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PrincipalOperationException(string message) { }
        public PrincipalOperationException(string message, System.Exception innerException) { }
        public PrincipalOperationException(string message, System.Exception innerException, int errorCode) { }
        public PrincipalOperationException(string message, int errorCode) { }
        public int ErrorCode { get { throw null; } }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrincipalSearcher : System.IDisposable
    {
        public PrincipalSearcher() { }
        public PrincipalSearcher(System.DirectoryServices.AccountManagement.Principal queryFilter) { }
        public System.DirectoryServices.AccountManagement.PrincipalContext Context { get { throw null; } }
        public System.DirectoryServices.AccountManagement.Principal QueryFilter { get { throw null; } set { } }
        public virtual void Dispose() { }
        public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> FindAll() { throw null; }
        public System.DirectoryServices.AccountManagement.Principal FindOne() { throw null; }
        public object GetUnderlyingSearcher() { throw null; }
        public System.Type GetUnderlyingSearcherType() { throw null; }
    }
    public partial class PrincipalSearchResult<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.IDisposable
    {
        internal PrincipalSearchResult() { }
        public void Dispose() { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class PrincipalServerDownException : System.DirectoryServices.AccountManagement.PrincipalException
    {
        public PrincipalServerDownException() { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        protected PrincipalServerDownException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
        public PrincipalServerDownException(string message) { }
        public PrincipalServerDownException(string message, System.Exception innerException) { }
        public PrincipalServerDownException(string message, System.Exception innerException, int errorCode) { }
        public PrincipalServerDownException(string message, System.Exception innerException, int errorCode, string serverName) { }
        public PrincipalServerDownException(string message, int errorCode) { }
#if NET8_0_OR_GREATER
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class PrincipalValueCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal PrincipalValueCollection() { }
        public int Count { get { throw null; } }
        public bool IsFixedSize { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public T this[int index] { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(T value) { }
        public void Clear() { }
        public bool Contains(T value) { throw null; }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public int IndexOf(T value) { throw null; }
        public void Insert(int index, T value) { }
        public bool Remove(T value) { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
    public partial class UserPrincipal : System.DirectoryServices.AccountManagement.AuthenticablePrincipal
    {
        public UserPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
        public UserPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName, string password, bool enabled) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
        public override System.DirectoryServices.AccountManagement.AdvancedFilters AdvancedSearchFilter { get { throw null; } }
        public static System.DirectoryServices.AccountManagement.UserPrincipal Current { get { throw null; } }
        public string EmailAddress { get { throw null; } set { } }
        public string EmployeeId { get { throw null; } set { } }
        public string GivenName { get { throw null; } set { } }
        public string MiddleName { get { throw null; } set { } }
        public string Surname { get { throw null; } set { } }
        public string VoiceTelephoneNumber { get { throw null; } set { } }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByBadPasswordAttempt(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByExpirationTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.UserPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { throw null; }
        public static new System.DirectoryServices.AccountManagement.UserPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByLockoutTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByLogonTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByPasswordSetTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { throw null; }
        public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetAuthorizationGroups() { throw null; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices.AccountManagement {
  public partial class AdvancedFilters {
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
  public partial class AuthenticablePrincipal : System.DirectoryServices.AccountManagement.Principal {
    protected internal AuthenticablePrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) { }
    protected internal AuthenticablePrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName, string password, bool enabled) { }
    public System.Nullable<System.DateTime> AccountExpirationDate { get { return default(System.Nullable<System.DateTime>); } set { } }
    public System.Nullable<System.DateTime> AccountLockoutTime { get { return default(System.Nullable<System.DateTime>); } }
    public virtual System.DirectoryServices.AccountManagement.AdvancedFilters AdvancedSearchFilter { get { return default(System.DirectoryServices.AccountManagement.AdvancedFilters); } }
    public bool AllowReversiblePasswordEncryption { get { return default(bool); } set { } }
    public int BadLogonCount { get { return default(int); } }
    public System.Security.Cryptography.X509Certificates.X509Certificate2Collection Certificates { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate2Collection); } }
    public bool DelegationPermitted { get { return default(bool); } set { } }
    public System.Nullable<bool> Enabled { get { return default(System.Nullable<bool>); } set { } }
    public string HomeDirectory { get { return default(string); } set { } }
    public string HomeDrive { get { return default(string); } set { } }
    public System.Nullable<System.DateTime> LastBadPasswordAttempt { get { return default(System.Nullable<System.DateTime>); } }
    public System.Nullable<System.DateTime> LastLogon { get { return default(System.Nullable<System.DateTime>); } }
    public System.Nullable<System.DateTime> LastPasswordSet { get { return default(System.Nullable<System.DateTime>); } }
    public bool PasswordNeverExpires { get { return default(bool); } set { } }
    public bool PasswordNotRequired { get { return default(bool); } set { } }
    public byte[] PermittedLogonTimes { get { return default(byte[]); } set { } }
    public System.DirectoryServices.AccountManagement.PrincipalValueCollection<string> PermittedWorkstations { get { return default(System.DirectoryServices.AccountManagement.PrincipalValueCollection<string>); } }
    public string ScriptPath { get { return default(string); } set { } }
    public bool SmartcardLogonRequired { get { return default(bool); } set { } }
    public bool UserCannotChangePassword { get { return default(bool); } set { } }
    public void ChangePassword(string oldPassword, string newPassword) { }
    public void ExpirePasswordNow() { }
    public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByBadPasswordAttempt(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal>); }
    protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByBadPasswordAttempt<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<T>); }
    public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByExpirationTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal>); }
    protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByExpirationTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<T>); }
    public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByLockoutTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal>); }
    protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByLockoutTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<T>); }
    public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByLogonTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal>); }
    protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByLogonTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<T>); }
    public static System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal> FindByPasswordSetTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.AuthenticablePrincipal>); }
    protected static System.DirectoryServices.AccountManagement.PrincipalSearchResult<T> FindByPasswordSetTime<T>(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<T>); }
    public bool IsAccountLockedOut() { return default(bool); }
    public void RefreshExpiredPassword() { }
    public void SetPassword(string newPassword) { }
    public void UnlockAccount() { }
  }
  [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
  public partial class ComputerPrincipal : System.DirectoryServices.AccountManagement.AuthenticablePrincipal {
    public ComputerPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
    public ComputerPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName, string password, bool enabled) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
    public System.DirectoryServices.AccountManagement.PrincipalValueCollection<string> ServicePrincipalNames { get { return default(System.DirectoryServices.AccountManagement.PrincipalValueCollection<string>); } }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByBadPasswordAttempt(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByExpirationTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.ComputerPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(System.DirectoryServices.AccountManagement.ComputerPrincipal); }
    public static new System.DirectoryServices.AccountManagement.ComputerPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { return default(System.DirectoryServices.AccountManagement.ComputerPrincipal); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByLockoutTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByLogonTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal> FindByPasswordSetTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.ComputerPrincipal>); }
  }
  [System.FlagsAttribute]
  public enum ContextOptions {
    Negotiate = 1,
    Sealing = 16,
    SecureSocketLayer = 4,
    ServerBind = 32,
    Signing = 8,
    SimpleBind = 2,
  }
  public enum ContextType {
    ApplicationDirectory = 2,
    Domain = 1,
    Machine = 0,
  }
  [System.AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple=true)]
  public sealed partial class DirectoryObjectClassAttribute : System.Attribute {
    public DirectoryObjectClassAttribute(string objectClass) { }
    public System.Nullable<System.DirectoryServices.AccountManagement.ContextType> Context { get { return default(System.Nullable<System.DirectoryServices.AccountManagement.ContextType>); } }
    public string ObjectClass { get { return default(string); } }
  }
  [System.AttributeUsageAttribute(AttributeTargets.Property, AllowMultiple=true)]
  public sealed partial class DirectoryPropertyAttribute : System.Attribute {
    public DirectoryPropertyAttribute(string schemaAttributeName) { }
    public System.Nullable<System.DirectoryServices.AccountManagement.ContextType> Context { get { return default(System.Nullable<System.DirectoryServices.AccountManagement.ContextType>); } set { } }
    public string SchemaAttributeName { get { return default(string); } }
  }
  [System.AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple=true)]
  public sealed partial class DirectoryRdnPrefixAttribute : System.Attribute {
    public DirectoryRdnPrefixAttribute(string rdnPrefix) { }
    public System.Nullable<System.DirectoryServices.AccountManagement.ContextType> Context { get { return default(System.Nullable<System.DirectoryServices.AccountManagement.ContextType>); } }
    public string RdnPrefix { get { return default(string); } }
  }
  [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
  public partial class GroupPrincipal : System.DirectoryServices.AccountManagement.Principal {
    public GroupPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) { }
    public GroupPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName) { }
    public System.Nullable<System.DirectoryServices.AccountManagement.GroupScope> GroupScope { get { return default(System.Nullable<System.DirectoryServices.AccountManagement.GroupScope>); } set { } }
    public System.Nullable<bool> IsSecurityGroup { get { return default(System.Nullable<bool>); } set { } }
    public System.DirectoryServices.AccountManagement.PrincipalCollection Members { get { return default(System.DirectoryServices.AccountManagement.PrincipalCollection); } }
    public override void Dispose() { }
    public static new System.DirectoryServices.AccountManagement.GroupPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(System.DirectoryServices.AccountManagement.GroupPrincipal); }
    public static new System.DirectoryServices.AccountManagement.GroupPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { return default(System.DirectoryServices.AccountManagement.GroupPrincipal); }
    public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetMembers() { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal>); }
    public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetMembers(bool recursive) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal>); }
  }
  public enum GroupScope {
    Global = 1,
    Local = 0,
    Universal = 2,
  }
  public enum IdentityType {
    DistinguishedName = 3,
    Guid = 5,
    Name = 1,
    SamAccountName = 0,
    Sid = 4,
    UserPrincipalName = 2,
  }
  public enum MatchType {
    Equals = 0,
    GreaterThan = 2,
    GreaterThanOrEquals = 3,
    LessThan = 4,
    LessThanOrEquals = 5,
    NotEquals = 1,
  }
  public partial class MultipleMatchesException : System.DirectoryServices.AccountManagement.PrincipalException {
    public MultipleMatchesException() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    protected MultipleMatchesException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public MultipleMatchesException(string message) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public MultipleMatchesException(string message, System.Exception innerException) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
  }
  public partial class NoMatchingPrincipalException : System.DirectoryServices.AccountManagement.PrincipalException {
    public NoMatchingPrincipalException() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    protected NoMatchingPrincipalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public NoMatchingPrincipalException(string message) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public NoMatchingPrincipalException(string message, System.Exception innerException) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
  }
  public partial class PasswordException : System.DirectoryServices.AccountManagement.PrincipalException {
    public PasswordException() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    protected PasswordException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PasswordException(string message) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PasswordException(string message, System.Exception innerException) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
  }
  public abstract partial class Principal : System.IDisposable {
    protected Principal() { }
    public System.DirectoryServices.AccountManagement.PrincipalContext Context { get { return default(System.DirectoryServices.AccountManagement.PrincipalContext); } }
    protected internal System.DirectoryServices.AccountManagement.PrincipalContext ContextRaw { get { return default(System.DirectoryServices.AccountManagement.PrincipalContext); } set { } }
    public System.DirectoryServices.AccountManagement.ContextType ContextType { get { return default(System.DirectoryServices.AccountManagement.ContextType); } }
    public string Description { get { return default(string); } set { } }
    public string DisplayName { get { return default(string); } set { } }
    public string DistinguishedName { get { return default(string); } }
    public System.Nullable<System.Guid> Guid { get { return default(System.Nullable<System.Guid>); } }
    public string Name { get { return default(string); } set { } }
    public string SamAccountName { get { return default(string); } set { } }
    public System.Security.Principal.SecurityIdentifier Sid { get { return default(System.Security.Principal.SecurityIdentifier); } }
    public string StructuralObjectClass { get { return default(string); } }
    public string UserPrincipalName { get { return default(string); } set { } }
    protected void CheckDisposedOrDeleted() { }
    public void Delete() { }
    public virtual void Dispose() { }
    public override bool Equals(object o) { return default(bool); }
    protected object[] ExtensionGet(string attribute) { return default(object[]); }
    protected void ExtensionSet(string attribute, object value) { }
    public static System.DirectoryServices.AccountManagement.Principal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(System.DirectoryServices.AccountManagement.Principal); }
    public static System.DirectoryServices.AccountManagement.Principal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { return default(System.DirectoryServices.AccountManagement.Principal); }
    protected static System.DirectoryServices.AccountManagement.Principal FindByIdentityWithType(System.DirectoryServices.AccountManagement.PrincipalContext context, System.Type principalType, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(System.DirectoryServices.AccountManagement.Principal); }
    protected static System.DirectoryServices.AccountManagement.Principal FindByIdentityWithType(System.DirectoryServices.AccountManagement.PrincipalContext context, System.Type principalType, string identityValue) { return default(System.DirectoryServices.AccountManagement.Principal); }
    public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetGroups() { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal>); }
    public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetGroups(System.DirectoryServices.AccountManagement.PrincipalContext contextToQuery) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal>); }
    public override int GetHashCode() { return default(int); }
    public object GetUnderlyingObject() { return default(object); }
    public System.Type GetUnderlyingObjectType() { return default(System.Type); }
    public bool IsMemberOf(System.DirectoryServices.AccountManagement.GroupPrincipal group) { return default(bool); }
    public bool IsMemberOf(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(bool); }
    public void Save() { }
    public void Save(System.DirectoryServices.AccountManagement.PrincipalContext context) { }
    public override string ToString() { return default(string); }
  }
  public partial class PrincipalCollection : System.Collections.Generic.ICollection<System.DirectoryServices.AccountManagement.Principal>, System.Collections.Generic.IEnumerable<System.DirectoryServices.AccountManagement.Principal>, System.Collections.ICollection, System.Collections.IEnumerable {
    internal PrincipalCollection() { }
    public int Count { get { return default(int); } }
    public bool IsReadOnly { get { return default(bool); } }
    public bool IsSynchronized { get { return default(bool); } }
    public object SyncRoot { get { return default(object); } }
    int System.Collections.ICollection.Count { get { return default(int); } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    public void Add(System.DirectoryServices.AccountManagement.ComputerPrincipal computer) { }
    public void Add(System.DirectoryServices.AccountManagement.GroupPrincipal group) { }
    public void Add(System.DirectoryServices.AccountManagement.Principal principal) { }
    public void Add(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { }
    public void Add(System.DirectoryServices.AccountManagement.UserPrincipal user) { }
    public void Clear() { }
    public bool Contains(System.DirectoryServices.AccountManagement.ComputerPrincipal computer) { return default(bool); }
    public bool Contains(System.DirectoryServices.AccountManagement.GroupPrincipal group) { return default(bool); }
    public bool Contains(System.DirectoryServices.AccountManagement.Principal principal) { return default(bool); }
    public bool Contains(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(bool); }
    public bool Contains(System.DirectoryServices.AccountManagement.UserPrincipal user) { return default(bool); }
    public void CopyTo(System.DirectoryServices.AccountManagement.Principal[] array, int index) { }
    public System.Collections.Generic.IEnumerator<System.DirectoryServices.AccountManagement.Principal> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.DirectoryServices.AccountManagement.Principal>); }
    public bool Remove(System.DirectoryServices.AccountManagement.ComputerPrincipal computer) { return default(bool); }
    public bool Remove(System.DirectoryServices.AccountManagement.GroupPrincipal group) { return default(bool); }
    public bool Remove(System.DirectoryServices.AccountManagement.Principal principal) { return default(bool); }
    public bool Remove(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(bool); }
    public bool Remove(System.DirectoryServices.AccountManagement.UserPrincipal user) { return default(bool); }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public partial class PrincipalContext : System.IDisposable {
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType) { }
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name) { }
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container) { }
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container, System.DirectoryServices.AccountManagement.ContextOptions options) { }
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container, System.DirectoryServices.AccountManagement.ContextOptions options, string userName, string password) { }
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string userName, string password) { }
    public PrincipalContext(System.DirectoryServices.AccountManagement.ContextType contextType, string name, string container, string userName, string password) { }
    public string ConnectedServer { get { return default(string); } }
    public string Container { get { return default(string); } }
    public System.DirectoryServices.AccountManagement.ContextType ContextType { get { return default(System.DirectoryServices.AccountManagement.ContextType); } }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.AccountManagement.ContextOptions Options { get { return default(System.DirectoryServices.AccountManagement.ContextOptions); } }
    public string UserName { get { return default(string); } }
    public void Dispose() { }
    public bool ValidateCredentials(string userName, string password) { return default(bool); }
    public bool ValidateCredentials(string userName, string password, System.DirectoryServices.AccountManagement.ContextOptions options) { return default(bool); }
  }
  public abstract partial class PrincipalException : System.SystemException {
    protected PrincipalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
  }
  public partial class PrincipalExistsException : System.DirectoryServices.AccountManagement.PrincipalException {
    public PrincipalExistsException() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    protected PrincipalExistsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalExistsException(string message) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalExistsException(string message, System.Exception innerException) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
  }
  public partial class PrincipalOperationException : System.DirectoryServices.AccountManagement.PrincipalException {
    public PrincipalOperationException() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    protected PrincipalOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalOperationException(string message) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalOperationException(string message, System.Exception innerException) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalOperationException(string message, System.Exception innerException, int errorCode) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalOperationException(string message, int errorCode) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public int ErrorCode { get { return default(int); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
  }
  public partial class PrincipalSearcher : System.IDisposable {

    public PrincipalSearcher() { }

    public PrincipalSearcher(System.DirectoryServices.AccountManagement.Principal queryFilter) { }
    public System.DirectoryServices.AccountManagement.PrincipalContext Context { get { return default(System.DirectoryServices.AccountManagement.PrincipalContext); } }
    public System.DirectoryServices.AccountManagement.Principal QueryFilter { get { return default(System.DirectoryServices.AccountManagement.Principal); } set { } }

    public virtual void Dispose() { }

    public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> FindAll() { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal>); }

    public System.DirectoryServices.AccountManagement.Principal FindOne() { return default(System.DirectoryServices.AccountManagement.Principal); }

    public object GetUnderlyingSearcher() { return default(object); }

    public System.Type GetUnderlyingSearcherType() { return default(System.Type); }
  }
  public partial class PrincipalSearchResult<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.IDisposable {
    internal PrincipalSearchResult() { }
    public void Dispose() { }

    public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public partial class PrincipalServerDownException : System.DirectoryServices.AccountManagement.PrincipalException {
    public PrincipalServerDownException() : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    protected PrincipalServerDownException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalServerDownException(string message) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalServerDownException(string message, System.Exception innerException) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalServerDownException(string message, System.Exception innerException, int errorCode) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalServerDownException(string message, System.Exception innerException, int errorCode, string serverName) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public PrincipalServerDownException(string message, int errorCode) : base (default(System.Runtime.Serialization.SerializationInfo), default(System.Runtime.Serialization.StreamingContext)) { }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
  }
  public partial class PrincipalValueCollection<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList {
    internal PrincipalValueCollection() { }
    public int Count { get { return default(int); } }
    public bool IsFixedSize { get { return default(bool); } }
    public bool IsReadOnly { get { return default(bool); } }
    public bool IsSynchronized { get { return default(bool); } }
    public T this[int index] { get { return default(T); } set { } }
    public object SyncRoot { get { return default(object); } }
    int System.Collections.ICollection.Count { get { return default(int); } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
    bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
    object System.Collections.IList.this[int index] { get { return default(object); } set { } }
    public void Add(T value) { }
    public void Clear() { }
    public bool Contains(T value) { return default(bool); }
    public void CopyTo(T[] array, int index) { }

    public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
    public int IndexOf(T value) { return default(int); }
    public void Insert(int index, T value) { }
    public bool Remove(T value) { return default(bool); }
    public void RemoveAt(int index) { }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }

    int System.Collections.IList.Add(object value) { return default(int); }

    void System.Collections.IList.Clear() { }
    bool System.Collections.IList.Contains(object value) { return default(bool); }

    int System.Collections.IList.IndexOf(object value) { return default(int); }

    void System.Collections.IList.Insert(int index, object value) { }
    void System.Collections.IList.Remove(object value) { }

    void System.Collections.IList.RemoveAt(int index) { }
  }
  [System.DirectoryServices.AccountManagement.DirectoryRdnPrefixAttribute("CN")]
  public partial class UserPrincipal : System.DirectoryServices.AccountManagement.AuthenticablePrincipal {
    public UserPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
    public UserPrincipal(System.DirectoryServices.AccountManagement.PrincipalContext context, string samAccountName, string password, bool enabled) : base (default(System.DirectoryServices.AccountManagement.PrincipalContext)) { }
    public override System.DirectoryServices.AccountManagement.AdvancedFilters AdvancedSearchFilter { get { return default(System.DirectoryServices.AccountManagement.AdvancedFilters); } }
    public static System.DirectoryServices.AccountManagement.UserPrincipal Current { get { return default(System.DirectoryServices.AccountManagement.UserPrincipal); } }
    public string EmailAddress { get { return default(string); } set { } }
    public string EmployeeId { get { return default(string); } set { } }
    public string GivenName { get { return default(string); } set { } }
    public string MiddleName { get { return default(string); } set { } }
    public string Surname { get { return default(string); } set { } }
    public string VoiceTelephoneNumber { get { return default(string); } set { } }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByBadPasswordAttempt(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByExpirationTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.UserPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DirectoryServices.AccountManagement.IdentityType identityType, string identityValue) { return default(System.DirectoryServices.AccountManagement.UserPrincipal); }
    public static new System.DirectoryServices.AccountManagement.UserPrincipal FindByIdentity(System.DirectoryServices.AccountManagement.PrincipalContext context, string identityValue) { return default(System.DirectoryServices.AccountManagement.UserPrincipal); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByLockoutTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByLogonTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal>); }
    public static new System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal> FindByPasswordSetTime(System.DirectoryServices.AccountManagement.PrincipalContext context, System.DateTime time, System.DirectoryServices.AccountManagement.MatchType type) { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.UserPrincipal>); }
    public System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal> GetAuthorizationGroups() { return default(System.DirectoryServices.AccountManagement.PrincipalSearchResult<System.DirectoryServices.AccountManagement.Principal>); }
  }
}


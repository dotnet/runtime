// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices {
  public partial class ActiveDirectoryAccessRule : System.Security.AccessControl.ObjectAccessRule {
    public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) {}
    public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) {}
    public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) {}
    public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.Guid objectType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) {}
    public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) {}
    public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) {}
    public System.DirectoryServices.ActiveDirectoryRights ActiveDirectoryRights { get { return default(System.DirectoryServices.ActiveDirectoryRights); } }
    public System.DirectoryServices.ActiveDirectorySecurityInheritance InheritanceType { get { return default(System.DirectoryServices.ActiveDirectorySecurityInheritance); } }
  }
  public partial class ActiveDirectoryAuditRule : System.Security.AccessControl.ObjectAuditRule {
    public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) {}
    public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) {}
    public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) {}
    public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.Guid objectType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) {}
    public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) {}
    public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType)  : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) {}
    public System.DirectoryServices.ActiveDirectoryRights ActiveDirectoryRights { get { return default(System.DirectoryServices.ActiveDirectoryRights); } }
    public System.DirectoryServices.ActiveDirectorySecurityInheritance InheritanceType { get { return default(System.DirectoryServices.ActiveDirectorySecurityInheritance); } }
  }
  [System.FlagsAttribute]
  public enum ActiveDirectoryRights {
    AccessSystemSecurity = 16777216,
    CreateChild = 1,
    Delete = 65536,
    DeleteChild = 2,
    DeleteTree = 64,
    ExtendedRight = 256,
    GenericAll = 983551,
    GenericExecute = 131076,
    GenericRead = 131220,
    GenericWrite = 131112,
    ListChildren = 4,
    ListObject = 128,
    ReadControl = 131072,
    ReadProperty = 16,
    Self = 8,
    Synchronize = 1048576,
    WriteDacl = 262144,
    WriteOwner = 524288,
    WriteProperty = 32,
  }
  public partial class ActiveDirectorySecurity : System.Security.AccessControl.DirectoryObjectSecurity {
    public ActiveDirectorySecurity() { }
    public override System.Type AccessRightType { get { return default(System.Type); } }
    public override System.Type AccessRuleType { get { return default(System.Type); } }
    public override System.Type AuditRuleType { get { return default(System.Type); } }
    public sealed override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { return default(System.Security.AccessControl.AccessRule); }
    public sealed override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type, System.Guid objectGuid, System.Guid inheritedObjectGuid) { return default(System.Security.AccessControl.AccessRule); }
    public void AddAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
    public void AddAuditRule(System.DirectoryServices.ActiveDirectoryAuditRule rule) { }
    public sealed override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { return default(System.Security.AccessControl.AuditRule); }
    public sealed override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags, System.Guid objectGuid, System.Guid inheritedObjectGuid) { return default(System.Security.AccessControl.AuditRule); }
    public override bool ModifyAccessRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified) { modified = default(bool); return default(bool); }
    public override bool ModifyAuditRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified) { modified = default(bool); return default(bool); }
    public override void PurgeAccessRules(System.Security.Principal.IdentityReference identity) { }
    public override void PurgeAuditRules(System.Security.Principal.IdentityReference identity) { }
    public void RemoveAccess(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) { }
    public bool RemoveAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { return default(bool); }
    public void RemoveAccessRuleSpecific(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
    public void RemoveAudit(System.Security.Principal.IdentityReference identity) { }
    public bool RemoveAuditRule(System.DirectoryServices.ActiveDirectoryAuditRule rule) { return default(bool); }
    public void RemoveAuditRuleSpecific(System.DirectoryServices.ActiveDirectoryAuditRule rule) { }
    public void ResetAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
    public void SetAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
    public void SetAuditRule(System.DirectoryServices.ActiveDirectoryAuditRule rule) { }
  }
  public enum ActiveDirectorySecurityInheritance {
    All = 1,
    Children = 4,
    Descendents = 2,
    None = 0,
    SelfAndChildren = 3,
  }
  [System.FlagsAttribute]
  public enum AuthenticationTypes {
    Anonymous = 16,
    Delegation = 256,
    Encryption = 2,
    FastBind = 32,
    None = 0,
    ReadonlyServer = 4,
    Sealing = 128,
    Secure = 1,
    SecureSocketsLayer = 2,
    ServerBind = 512,
    Signing = 64,
  }
  public sealed partial class CreateChildAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public sealed partial class DeleteChildAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public sealed partial class DeleteTreeAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public DeleteTreeAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteTreeAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public DeleteTreeAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public enum DereferenceAlias {
    Always = 3,
    FindingBaseObject = 2,
    InSearching = 1,
    Never = 0,
  }
  public partial class DirectoryEntries : System.Collections.IEnumerable {
    internal DirectoryEntries() { }
    public System.DirectoryServices.SchemaNameCollection SchemaFilter { get { return default(System.DirectoryServices.SchemaNameCollection); } }
    public System.DirectoryServices.DirectoryEntry Add(string name, string schemaClassName) { return default(System.DirectoryServices.DirectoryEntry); }
    public System.DirectoryServices.DirectoryEntry Find(string name) { return default(System.DirectoryServices.DirectoryEntry); }
    public System.DirectoryServices.DirectoryEntry Find(string name, string schemaClassName) { return default(System.DirectoryServices.DirectoryEntry); }
    public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    public void Remove(System.DirectoryServices.DirectoryEntry entry) { }
  }
  public partial class DirectoryEntry : System.ComponentModel.Component {
    public DirectoryEntry() { }
    public DirectoryEntry(object adsObject) { }
    public DirectoryEntry(string path) { }
    public DirectoryEntry(string path, string username, string password) { }
    public DirectoryEntry(string path, string username, string password, System.DirectoryServices.AuthenticationTypes authenticationType) { }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.AuthenticationTypes)(1))]
    public System.DirectoryServices.AuthenticationTypes AuthenticationType { get { return default(System.DirectoryServices.AuthenticationTypes); } set { } }
    public System.DirectoryServices.DirectoryEntries Children { get { return default(System.DirectoryServices.DirectoryEntries); } }
    public System.Guid Guid { get { return default(System.Guid); } }
    public string Name { get { return default(string); } }
    public string NativeGuid { get { return default(string); } }
    public object NativeObject { get { return default(object); } }
    public System.DirectoryServices.ActiveDirectorySecurity ObjectSecurity { get { return default(System.DirectoryServices.ActiveDirectorySecurity); } set { } }
    public System.DirectoryServices.DirectoryEntryConfiguration Options { get { return default(System.DirectoryServices.DirectoryEntryConfiguration); } }
    public System.DirectoryServices.DirectoryEntry Parent { get { return default(System.DirectoryServices.DirectoryEntry); } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public string Password { set { } }
    [System.ComponentModel.DefaultValueAttribute("")]
    public string Path { get { return default(string); } set { } }
    public System.DirectoryServices.PropertyCollection Properties { get { return default(System.DirectoryServices.PropertyCollection); } }
    public string SchemaClassName { get { return default(string); } }
    public System.DirectoryServices.DirectoryEntry SchemaEntry { get { return default(System.DirectoryServices.DirectoryEntry); } }
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool UsePropertyCache { get { return default(bool); } set { } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public string Username { get { return default(string); } set { } }
    public void Close() { }
    public void CommitChanges() { }
    public System.DirectoryServices.DirectoryEntry CopyTo(System.DirectoryServices.DirectoryEntry newParent) { return default(System.DirectoryServices.DirectoryEntry); }
    public System.DirectoryServices.DirectoryEntry CopyTo(System.DirectoryServices.DirectoryEntry newParent, string newName) { return default(System.DirectoryServices.DirectoryEntry); }
    public void DeleteTree() { }
    protected override void Dispose(bool disposing) { }
    public static bool Exists(string path) { return default(bool); }
    public object Invoke(string methodName, params object[] args) { return default(object); }
    public object InvokeGet(string propertyName) { return default(object); }
    public void InvokeSet(string propertyName, params object[] args) { }
    public void MoveTo(System.DirectoryServices.DirectoryEntry newParent) { }
    public void MoveTo(System.DirectoryServices.DirectoryEntry newParent, string newName) { }
    public void RefreshCache() { }
    public void RefreshCache(string[] propertyNames) { }
    public void Rename(string newName) { }
  }
  public partial class DirectoryEntryConfiguration {
    internal DirectoryEntryConfiguration() { }
    public int PageSize { get { return default(int); } set { } }
    public System.DirectoryServices.PasswordEncodingMethod PasswordEncoding { get { return default(System.DirectoryServices.PasswordEncodingMethod); } set { } }
    public int PasswordPort { get { return default(int); } set { } }
    public System.DirectoryServices.ReferralChasingOption Referral { get { return default(System.DirectoryServices.ReferralChasingOption); } set { } }
    public System.DirectoryServices.SecurityMasks SecurityMasks { get { return default(System.DirectoryServices.SecurityMasks); } set { } }
    public string GetCurrentServerName() { return default(string); }
    public bool IsMutuallyAuthenticated() { return default(bool); }
    public void SetUserNameQueryQuota(string accountName) { }
  }
  public partial class DirectorySearcher : System.ComponentModel.Component {
    public DirectorySearcher() { }
    public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot) { }
    public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot, string filter) { }
    public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot, string filter, string[] propertiesToLoad) { }
    public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot, string filter, string[] propertiesToLoad, System.DirectoryServices.SearchScope scope) { }
    public DirectorySearcher(string filter) { }
    public DirectorySearcher(string filter, string[] propertiesToLoad) { }
    public DirectorySearcher(string filter, string[] propertiesToLoad, System.DirectoryServices.SearchScope scope) { }
    [System.ComponentModel.DefaultValueAttribute(false)]
    public bool Asynchronous { get { return default(bool); } set { } }
    [System.ComponentModel.DefaultValueAttribute("")]
    public string AttributeScopeQuery { get { return default(string); } set { } }
    [System.ComponentModel.DefaultValueAttribute(true)]
    public bool CacheResults { get { return default(bool); } set { } }
    public System.TimeSpan ClientTimeout { get { return default(System.TimeSpan); } set { } }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.DereferenceAlias)(0))]
    public System.DirectoryServices.DereferenceAlias DerefAlias { get { return default(System.DirectoryServices.DereferenceAlias); } set { } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public System.DirectoryServices.DirectorySynchronization DirectorySynchronization { get { return default(System.DirectoryServices.DirectorySynchronization); } set { } }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.ExtendedDN)(-1))]
    public System.DirectoryServices.ExtendedDN ExtendedDN { get { return default(System.DirectoryServices.ExtendedDN); } set { } }
    [System.ComponentModel.DefaultValueAttribute("(objectClass=*)")]
    public string Filter { get { return default(string); } set { } }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int PageSize { get { return default(int); } set { } }
    public System.Collections.Specialized.StringCollection PropertiesToLoad { get { return default(System.Collections.Specialized.StringCollection); } }
    [System.ComponentModel.DefaultValueAttribute(false)]
    public bool PropertyNamesOnly { get { return default(bool); } set { } }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.ReferralChasingOption)(64))]
    public System.DirectoryServices.ReferralChasingOption ReferralChasing { get { return default(System.DirectoryServices.ReferralChasingOption); } set { } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public System.DirectoryServices.DirectoryEntry SearchRoot { get { return default(System.DirectoryServices.DirectoryEntry); } set { } }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.SearchScope)(2))]
    public System.DirectoryServices.SearchScope SearchScope { get { return default(System.DirectoryServices.SearchScope); } set { } }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.SecurityMasks)(0))]
    public System.DirectoryServices.SecurityMasks SecurityMasks { get { return default(System.DirectoryServices.SecurityMasks); } set { } }
    public System.TimeSpan ServerPageTimeLimit { get { return default(System.TimeSpan); } set { } }
    public System.TimeSpan ServerTimeLimit { get { return default(System.TimeSpan); } set { } }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int SizeLimit { get { return default(int); } set { } }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public System.DirectoryServices.SortOption Sort { get { return default(System.DirectoryServices.SortOption); } set { } }
    [System.ComponentModel.DefaultValueAttribute(false)]
    public bool Tombstone { get { return default(bool); } set { } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public System.DirectoryServices.DirectoryVirtualListView VirtualListView { get { return default(System.DirectoryServices.DirectoryVirtualListView); } set { } }
    protected override void Dispose(bool disposing) { }
    public System.DirectoryServices.SearchResultCollection FindAll() { return default(System.DirectoryServices.SearchResultCollection); }
    public System.DirectoryServices.SearchResult FindOne() { return default(System.DirectoryServices.SearchResult); }
  }
  public partial class DirectoryServicesCOMException : System.Runtime.InteropServices.COMException, System.Runtime.Serialization.ISerializable {
    public DirectoryServicesCOMException() { }
    protected DirectoryServicesCOMException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public DirectoryServicesCOMException(string message) { }
    public DirectoryServicesCOMException(string message, System.Exception inner) { }
    public int ExtendedError { get { return default(int); } }
    public string ExtendedErrorMessage { get { return default(string); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
  }
  public partial class DirectorySynchronization {
    public DirectorySynchronization() { }
    public DirectorySynchronization(byte[] cookie) { }
    public DirectorySynchronization(System.DirectoryServices.DirectorySynchronization sync) { }
    public DirectorySynchronization(System.DirectoryServices.DirectorySynchronizationOptions option) { }
    public DirectorySynchronization(System.DirectoryServices.DirectorySynchronizationOptions option, byte[] cookie) { }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.DirectorySynchronizationOptions)(0))]
    public System.DirectoryServices.DirectorySynchronizationOptions Option { get { return default(System.DirectoryServices.DirectorySynchronizationOptions); } set { } }
    public System.DirectoryServices.DirectorySynchronization Copy() { return default(System.DirectoryServices.DirectorySynchronization); }
    public byte[] GetDirectorySynchronizationCookie() { return default(byte[]); }
    public void ResetDirectorySynchronizationCookie() { }
    public void ResetDirectorySynchronizationCookie(byte[] cookie) { }
  }
  [System.FlagsAttribute]
  public enum DirectorySynchronizationOptions : long {
    IncrementalValues = (long)2147483648,
    None = (long)0,
    ObjectSecurity = (long)1,
    ParentsFirst = (long)2048,
    PublicDataOnly = (long)8192,
  }
  public partial class DirectoryVirtualListView {
    public DirectoryVirtualListView() { }
    public DirectoryVirtualListView(int afterCount) { }
    public DirectoryVirtualListView(int beforeCount, int afterCount, int offset) { }
    public DirectoryVirtualListView(int beforeCount, int afterCount, int offset, System.DirectoryServices.DirectoryVirtualListViewContext context) { }
    public DirectoryVirtualListView(int beforeCount, int afterCount, string target) { }
    public DirectoryVirtualListView(int beforeCount, int afterCount, string target, System.DirectoryServices.DirectoryVirtualListViewContext context) { }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int AfterCount { get { return default(int); } set { } }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int ApproximateTotal { get { return default(int); } set { } }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int BeforeCount { get { return default(int); } set { } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public System.DirectoryServices.DirectoryVirtualListViewContext DirectoryVirtualListViewContext { get { return default(System.DirectoryServices.DirectoryVirtualListViewContext); } set { } }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int Offset { get { return default(int); } set { } }
    [System.ComponentModel.DefaultValueAttribute("")]
    public string Target { get { return default(string); } set { } }
    [System.ComponentModel.DefaultValueAttribute(0)]
    public int TargetPercentage { get { return default(int); } set { } }
  }
  public partial class DirectoryVirtualListViewContext {
    public DirectoryVirtualListViewContext() { }
    public System.DirectoryServices.DirectoryVirtualListViewContext Copy() { return default(System.DirectoryServices.DirectoryVirtualListViewContext); }
  }
  public enum ExtendedDN {
    HexString = 0,
    None = -1,
    Standard = 1,
  }
  public sealed partial class ExtendedRightAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid extendedRightType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid extendedRightType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid extendedRightType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public sealed partial class ListChildrenAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public ListChildrenAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ListChildrenAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public ListChildrenAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public enum PasswordEncodingMethod {
    PasswordEncodingClear = 1,
    PasswordEncodingSsl = 0,
  }
  public enum PropertyAccess {
    Read = 0,
    Write = 1,
  }
  public sealed partial class PropertyAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertyType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertyType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertyType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public partial class PropertyCollection : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable {
    internal PropertyCollection() { }
    public int Count { get { return default(int); } }
    public System.DirectoryServices.PropertyValueCollection this[string propertyName] { get { return default(System.DirectoryServices.PropertyValueCollection); } }
    public System.Collections.ICollection PropertyNames { get { return default(System.Collections.ICollection); } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
    bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
    object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
    System.Collections.ICollection System.Collections.IDictionary.Keys { get { return default(System.Collections.ICollection); } }
    public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
    public bool Contains(string propertyName) { return default(bool); }
    public void CopyTo(System.DirectoryServices.PropertyValueCollection[] array, int index) { }
    public System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    void System.Collections.IDictionary.Add(object key, object value) { }
    void System.Collections.IDictionary.Clear() { }
    bool System.Collections.IDictionary.Contains(object value) { return default(bool); }
    void System.Collections.IDictionary.Remove(object key) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public sealed partial class PropertySetAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule {
    public PropertySetAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertySetType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertySetAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertySetType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    public PropertySetAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertySetType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
  }
  public partial class PropertyValueCollection : System.Collections.CollectionBase {
    internal PropertyValueCollection() { }
    public object this[int index] { get { return default(object); } set { } }
    public string PropertyName { get { return default(string); } }
    public object Value { get { return default(object); } set { } }
    public int Add(object value) { return default(int); }
    public void AddRange(System.DirectoryServices.PropertyValueCollection value) { }
    public void AddRange(object[] value) { }
    public bool Contains(object value) { return default(bool); }
    public void CopyTo(object[] array, int index) { }
    public int IndexOf(object value) { return default(int); }
    public void Insert(int index, object value) { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    public void Remove(object value) { }
  }
  public enum ReferralChasingOption {
    All = 96,
    External = 64,
    None = 0,
    Subordinate = 32,
  }
  public partial class ResultPropertyCollection : System.Collections.DictionaryBase {
    internal ResultPropertyCollection() { }
    public System.DirectoryServices.ResultPropertyValueCollection this[string name] { get { return default(System.DirectoryServices.ResultPropertyValueCollection); } }
    public System.Collections.ICollection PropertyNames { get { return default(System.Collections.ICollection); } }
    public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
    public bool Contains(string propertyName) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ResultPropertyValueCollection[] array, int index) { }
  }
  public partial class ResultPropertyValueCollection : System.Collections.ReadOnlyCollectionBase {
    internal ResultPropertyValueCollection() { }
    public object this[int index] { get { return default(object); } }
    public bool Contains(object value) { return default(bool); }
    public void CopyTo(object[] values, int index) { }
    public int IndexOf(object value) { return default(int); }
  }
  public partial class SchemaNameCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList {
    internal SchemaNameCollection() { }
    public int Count { get { return default(int); } }
    public string this[int index] { get { return default(string); } set { } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
    bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
    object System.Collections.IList.this[int index] { get { return default(object); } set { } }
    public int Add(string value) { return default(int); }
    public void AddRange(System.DirectoryServices.SchemaNameCollection value) { }
    public void AddRange(string[] value) { }
    public void Clear() { }
    public bool Contains(string value) { return default(bool); }
    public void CopyTo(string[] stringArray, int index) { }
    public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    public int IndexOf(string value) { return default(int); }
    public void Insert(int index, string value) { }
    public void Remove(string value) { }
    public void RemoveAt(int index) { }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    int System.Collections.IList.Add(object value) { return default(int); }
    bool System.Collections.IList.Contains(object value) { return default(bool); }
    int System.Collections.IList.IndexOf(object value) { return default(int); }
    void System.Collections.IList.Insert(int index, object value) { }
    void System.Collections.IList.Remove(object value) { }
  }
  public partial class SearchResult {
    internal SearchResult() { }
    public string Path { get { return default(string); } }
    public System.DirectoryServices.ResultPropertyCollection Properties { get { return default(System.DirectoryServices.ResultPropertyCollection); } }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
  }
  public partial class SearchResultCollection : System.MarshalByRefObject, System.Collections.ICollection, System.Collections.IEnumerable, System.IDisposable {
    internal SearchResultCollection() { }
    public int Count { get { return default(int); } }
    public System.IntPtr Handle { get { return default(System.IntPtr); } }
    public System.DirectoryServices.SearchResult this[int index] { get { return default(System.DirectoryServices.SearchResult); } }
    public string[] PropertiesLoaded { get { return default(string[]); } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    public bool Contains(System.DirectoryServices.SearchResult result) { return default(bool); }
    public void CopyTo(System.DirectoryServices.SearchResult[] results, int index) { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    ~SearchResultCollection() { }
    public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    public int IndexOf(System.DirectoryServices.SearchResult result) { return default(int); }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
  }
  public enum SearchScope {
    Base = 0,
    OneLevel = 1,
    Subtree = 2,
  }
  [System.FlagsAttribute]
  public enum SecurityMasks {
    Dacl = 4,
    Group = 2,
    None = 0,
    Owner = 1,
    Sacl = 8,
  }
  public enum SortDirection {
    Ascending = 0,
    Descending = 1,
  }
  [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
  public partial class SortOption {
    public SortOption() { }
    public SortOption(string propertyName, System.DirectoryServices.SortDirection direction) { }
    [System.ComponentModel.DefaultValueAttribute((System.DirectoryServices.SortDirection)(0))]
    public System.DirectoryServices.SortDirection Direction { get { return default(System.DirectoryServices.SortDirection); } set { } }
    [System.ComponentModel.DefaultValueAttribute(null)]
    public string PropertyName { get { return default(string); } set { } }
  }
}
namespace System.DirectoryServices.ActiveDirectory {
  public partial class ActiveDirectoryInterSiteTransport : System.IDisposable {
    internal ActiveDirectoryInterSiteTransport() { }
    public bool BridgeAllSiteLinks { get { return default(bool); } set { } }
    public bool IgnoreReplicationSchedule { get { return default(bool); } set { } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkBridgeCollection SiteLinkBridges { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkBridgeCollection); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkCollection SiteLinks { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkCollection); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType); } }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectoryInterSiteTransport FindByTransportType(System.DirectoryServices.ActiveDirectory.DirectoryContext context, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryInterSiteTransport); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectoryObjectExistsException : System.Exception {
    public ActiveDirectoryObjectExistsException() { }
    protected ActiveDirectoryObjectExistsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ActiveDirectoryObjectExistsException(string message) { }
    public ActiveDirectoryObjectExistsException(string message, System.Exception inner) { }
  }
  public partial class ActiveDirectoryObjectNotFoundException : System.Exception, System.Runtime.Serialization.ISerializable {
    public ActiveDirectoryObjectNotFoundException() { }
    protected ActiveDirectoryObjectNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ActiveDirectoryObjectNotFoundException(string message) { }
    public ActiveDirectoryObjectNotFoundException(string message, System.Exception inner) { }
    public ActiveDirectoryObjectNotFoundException(string message, System.Type type, string name) { }
    public string Name { get { return default(string); } }
    public System.Type Type { get { return default(System.Type); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
  }
  public partial class ActiveDirectoryOperationException : System.Exception, System.Runtime.Serialization.ISerializable {
    public ActiveDirectoryOperationException() { }
    protected ActiveDirectoryOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ActiveDirectoryOperationException(string message) { }
    public ActiveDirectoryOperationException(string message, System.Exception inner) { }
    public ActiveDirectoryOperationException(string message, System.Exception inner, int errorCode) { }
    public ActiveDirectoryOperationException(string message, int errorCode) { }
    public int ErrorCode { get { return default(int); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
  }
  public abstract partial class ActiveDirectoryPartition : System.IDisposable {
    protected ActiveDirectoryPartition() { }
    public string Name { get { return default(string); } }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public abstract System.DirectoryServices.DirectoryEntry GetDirectoryEntry();
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectoryReplicationMetadata : System.Collections.DictionaryBase {
    internal ActiveDirectoryReplicationMetadata() { }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyStringCollection AttributeNames { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyStringCollection); } }
    public System.DirectoryServices.ActiveDirectory.AttributeMetadata this[string name] { get { return default(System.DirectoryServices.ActiveDirectory.AttributeMetadata); } }
    public System.DirectoryServices.ActiveDirectory.AttributeMetadataCollection Values { get { return default(System.DirectoryServices.ActiveDirectory.AttributeMetadataCollection); } }
    public bool Contains(string attributeName) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.AttributeMetadata[] array, int index) { }
  }
  public enum ActiveDirectoryRole {
    InfrastructureRole = 4,
    NamingRole = 1,
    PdcRole = 2,
    RidRole = 3,
    SchemaRole = 0,
  }
  public partial class ActiveDirectoryRoleCollection : System.Collections.ReadOnlyCollectionBase {
    internal ActiveDirectoryRoleCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole[] roles, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { return default(int); }
  }
  public partial class ActiveDirectorySchedule {
    public ActiveDirectorySchedule() { }
    public ActiveDirectorySchedule(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule) { }
    public bool[,,] RawSchedule { get { return default(bool[,,]); } set { } }
    public void ResetSchedule() { }
    public void SetDailySchedule(System.DirectoryServices.ActiveDirectory.HourOfDay fromHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour fromMinute, System.DirectoryServices.ActiveDirectory.HourOfDay toHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour toMinute) { }
    public void SetSchedule(System.DayOfWeek day, System.DirectoryServices.ActiveDirectory.HourOfDay fromHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour fromMinute, System.DirectoryServices.ActiveDirectory.HourOfDay toHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour toMinute) { }
    public void SetSchedule(System.DayOfWeek[] days, System.DirectoryServices.ActiveDirectory.HourOfDay fromHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour fromMinute, System.DirectoryServices.ActiveDirectory.HourOfDay toHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour toMinute) { }
  }
  public partial class ActiveDirectorySchema : System.DirectoryServices.ActiveDirectory.ActiveDirectoryPartition {
    internal ActiveDirectorySchema() { }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer SchemaRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); } }
    protected override void Dispose(bool disposing) { }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection FindAllClasses() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection FindAllClasses(System.DirectoryServices.ActiveDirectory.SchemaClassType type) { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection FindAllDefunctClasses() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllDefunctProperties() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties(System.DirectoryServices.ActiveDirectory.PropertyTypes type) { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection); }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass FindClass(string ldapDisplayName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass); }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass FindDefunctClass(string commonName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass); }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty FindDefunctProperty(string commonName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty); }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty FindProperty(string ldapDisplayName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty); }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema GetCurrentSchema() { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema); }
    public override System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema GetSchema(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema); }
    public void RefreshSchema() { }
  }
  public partial class ActiveDirectorySchemaClass : System.IDisposable {
    public ActiveDirectorySchemaClass(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection AuxiliaryClasses { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection); } }
    public string CommonName { get { return default(string); } set { } }
    public System.DirectoryServices.ActiveDirectorySecurity DefaultObjectSecurityDescriptor { get { return default(System.DirectoryServices.ActiveDirectorySecurity); } set { } }
    public string Description { get { return default(string); } set { } }
    public bool IsDefunct { get { return default(bool); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection MandatoryProperties { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection); } }
    public string Name { get { return default(string); } }
    public string Oid { get { return default(string); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection OptionalProperties { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection PossibleInferiors { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection PossibleSuperiors { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection); } }
    public System.Guid SchemaGuid { get { return default(System.Guid); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass SubClassOf { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass); } set { } }
    public System.DirectoryServices.ActiveDirectory.SchemaClassType Type { get { return default(System.DirectoryServices.ActiveDirectory.SchemaClassType); } set { } }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection GetAllProperties() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectorySchemaClassCollection : System.Collections.CollectionBase {
    internal ActiveDirectorySchemaClassCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass); } set { } }
    public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { return default(int); }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass[] schemaClasses) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection schemaClasses) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection schemaClasses) { }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass[] schemaClasses, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { return default(int); }
    public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    protected override void OnValidate(object value) { }
    public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { }
  }
  public partial class ActiveDirectorySchemaProperty : System.IDisposable {
    public ActiveDirectorySchemaProperty(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { }
    public string CommonName { get { return default(string); } set { } }
    public string Description { get { return default(string); } set { } }
    public bool IsDefunct { get { return default(bool); } set { } }
    public bool IsInAnr { get { return default(bool); } set { } }
    public bool IsIndexed { get { return default(bool); } set { } }
    public bool IsIndexedOverContainer { get { return default(bool); } set { } }
    public bool IsInGlobalCatalog { get { return default(bool); } set { } }
    public bool IsOnTombstonedObject { get { return default(bool); } set { } }
    public bool IsSingleValued { get { return default(bool); } set { } }
    public bool IsTupleIndexed { get { return default(bool); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty Link { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty); } }
    public System.Nullable<int> LinkId { get { return default(System.Nullable<int>); } set { } }
    public string Name { get { return default(string); } }
    public string Oid { get { return default(string); } set { } }
    public System.Nullable<int> RangeLower { get { return default(System.Nullable<int>); } set { } }
    public System.Nullable<int> RangeUpper { get { return default(System.Nullable<int>); } set { } }
    public System.Guid SchemaGuid { get { return default(System.Guid); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySyntax Syntax { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySyntax); } set { } }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectorySchemaPropertyCollection : System.Collections.CollectionBase {
    internal ActiveDirectorySchemaPropertyCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty); } set { } }
    public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { return default(int); }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty[] properties) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection properties) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection properties) { }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty[] properties, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { return default(int); }
    public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    protected override void OnValidate(object value) { }
    public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { }
  }
  public partial class ActiveDirectoryServerDownException : System.Exception, System.Runtime.Serialization.ISerializable {
    public ActiveDirectoryServerDownException() { }
    protected ActiveDirectoryServerDownException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ActiveDirectoryServerDownException(string message) { }
    public ActiveDirectoryServerDownException(string message, System.Exception inner) { }
    public ActiveDirectoryServerDownException(string message, System.Exception inner, int errorCode, string name) { }
    public ActiveDirectoryServerDownException(string message, int errorCode, string name) { }
    public int ErrorCode { get { return default(int); } }
    public override string Message { get { return default(string); } }
    public string Name { get { return default(string); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
  }
  public partial class ActiveDirectorySite : System.IDisposable {
    public ActiveDirectorySite(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { }
    public System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection AdjacentSites { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection BridgeheadServers { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection); } }
    public System.DirectoryServices.ActiveDirectory.DomainCollection Domains { get { return default(System.DirectoryServices.ActiveDirectory.DomainCollection); } }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer InterSiteTopologyGenerator { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule IntraSiteReplicationSchedule { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule); } set { } }
    public string Location { get { return default(string); } set { } }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteOptions Options { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteOptions); } set { } }
    public System.DirectoryServices.ActiveDirectory.DirectoryServerCollection PreferredRpcBridgeheadServers { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServerCollection); } }
    public System.DirectoryServices.ActiveDirectory.DirectoryServerCollection PreferredSmtpBridgeheadServers { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServerCollection); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection Servers { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkCollection SiteLinks { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkCollection); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnetCollection Subnets { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnetCollection); } }
    public void Delete() { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySite FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite); }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySite GetComputerSite() { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectorySiteCollection : System.Collections.CollectionBase {
    internal ActiveDirectorySiteCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySite this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite); } set { } }
    public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { return default(int); }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite[] sites) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteCollection sites) { }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { return default(int); }
    public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    protected override void OnValidate(object value) { }
    public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { }
  }
  public partial class ActiveDirectorySiteLink : System.IDisposable {
    public ActiveDirectorySiteLink(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName) { }
    public ActiveDirectorySiteLink(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
    public ActiveDirectorySiteLink(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule) { }
    public int Cost { get { return default(int); } set { } }
    public bool DataCompressionEnabled { get { return default(bool); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule InterSiteReplicationSchedule { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule); } set { } }
    public string Name { get { return default(string); } }
    public bool NotificationEnabled { get { return default(bool); } set { } }
    public bool ReciprocalReplicationEnabled { get { return default(bool); } set { } }
    public System.TimeSpan ReplicationInterval { get { return default(System.TimeSpan); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteCollection Sites { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteCollection); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType); } }
    public void Delete() { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink); }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectorySiteLinkBridge : System.IDisposable {
    public ActiveDirectorySiteLinkBridge(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName) { }
    public ActiveDirectorySiteLinkBridge(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkCollection SiteLinks { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkCollection); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType); } }
    public void Delete() { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge); }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectorySiteLinkCollection : System.Collections.CollectionBase {
    internal ActiveDirectorySiteLinkCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink); } set { } }
    public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { return default(int); }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink[] links) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkCollection links) { }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { return default(int); }
    public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    protected override void OnValidate(object value) { }
    public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { }
  }
  [System.FlagsAttribute]
  public enum ActiveDirectorySiteOptions {
    AutoInterSiteTopologyDisabled = 16,
    AutoMinimumHopDisabled = 4,
    AutoTopologyDisabled = 1,
    ForceKccWindows2003Behavior = 64,
    GroupMembershipCachingEnabled = 32,
    None = 0,
    RandomBridgeHeaderServerSelectionDisabled = 256,
    RedundantServerTopologyEnabled = 1024,
    StaleServerDetectDisabled = 8,
    TopologyCleanupDisabled = 2,
    UseHashingForReplicationSchedule = 512,
    UseWindows2000IstgElection = 128,
  }
  public partial class ActiveDirectorySubnet : System.IDisposable {
    public ActiveDirectorySubnet(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string subnetName) { }
    public ActiveDirectorySubnet(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string subnetName, string siteName) { }
    public string Location { get { return default(string); } set { } }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySite Site { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite); } set { } }
    public void Delete() { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string subnetName) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ActiveDirectorySubnetCollection : System.Collections.CollectionBase {
    internal ActiveDirectorySubnetCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet); } set { } }
    public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { return default(int); }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet[] subnets) { }
    public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnetCollection subnets) { }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { return default(int); }
    public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { }
    protected override void OnClear() { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    protected override void OnValidate(object value) { }
    public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { }
  }
  public enum ActiveDirectorySyntax {
    AccessPointDN = 19,
    Bool = 8,
    CaseExactString = 0,
    CaseIgnoreString = 1,
    DirectoryString = 3,
    DN = 12,
    DNWithBinary = 13,
    DNWithString = 14,
    Enumeration = 15,
    GeneralizedTime = 10,
    IA5String = 16,
    Int = 6,
    Int64 = 7,
    NumericString = 2,
    OctetString = 4,
    Oid = 9,
    ORName = 20,
    PresentationAddress = 21,
    PrintableString = 17,
    ReplicaLink = 22,
    SecurityDescriptor = 5,
    Sid = 18,
    UtcTime = 11,
  }
  public enum ActiveDirectoryTransportType {
    Rpc = 0,
    Smtp = 1,
  }
  public partial class AdamInstance : System.DirectoryServices.ActiveDirectory.DirectoryServer {
    internal AdamInstance() { }
    public System.DirectoryServices.ActiveDirectory.ConfigurationSet ConfigurationSet { get { return default(System.DirectoryServices.ActiveDirectory.ConfigurationSet); } }
    public string DefaultPartition { get { return default(string); } set { } }
    public string HostName { get { return default(string); } }
    public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection InboundConnections { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection); } }
    public override string IPAddress { get { return default(string); } }
    public int LdapPort { get { return default(int); } }
    public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection OutboundConnections { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection); } }
    public System.DirectoryServices.ActiveDirectory.AdamRoleCollection Roles { get { return default(System.DirectoryServices.ActiveDirectory.AdamRoleCollection); } }
    public override string SiteName { get { return default(string); } }
    public int SslPort { get { return default(int); } }
    public override System.DirectoryServices.ActiveDirectory.SyncUpdateCallback SyncFromAllServersCallback { get { return default(System.DirectoryServices.ActiveDirectory.SyncUpdateCallback); } set { } }
    public override void CheckReplicationConsistency() { }
    protected override void Dispose(bool disposing) { }
    ~AdamInstance() { }
    public static System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string partitionName) { return default(System.DirectoryServices.ActiveDirectory.AdamInstanceCollection); }
    public static System.DirectoryServices.ActiveDirectory.AdamInstance FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string partitionName) { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); }
    public static System.DirectoryServices.ActiveDirectory.AdamInstance GetAdamInstance(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetAllReplicationNeighbors() { return default(System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection GetReplicationConnectionFailures() { return default(System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection GetReplicationCursors(string partition) { return default(System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection); }
    public override System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetReplicationNeighbors(string partition) { return default(System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation GetReplicationOperationInformation() { return default(System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation); }
    public void Save() { }
    public void SeizeRoleOwnership(System.DirectoryServices.ActiveDirectory.AdamRole role) { }
    public override void SyncReplicaFromAllServers(string partition, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOptions options) { }
    public override void SyncReplicaFromServer(string partition, string sourceServer) { }
    public void TransferRoleOwnership(System.DirectoryServices.ActiveDirectory.AdamRole role) { }
    public override void TriggerSyncReplicaFromNeighbors(string partition) { }
  }
  public partial class AdamInstanceCollection : System.Collections.ReadOnlyCollectionBase {
    internal AdamInstanceCollection() { }
    public System.DirectoryServices.ActiveDirectory.AdamInstance this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.AdamInstance adamInstance) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.AdamInstance[] adamInstances, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.AdamInstance adamInstance) { return default(int); }
  }
  public enum AdamRole {
    NamingRole = 1,
    SchemaRole = 0,
  }
  public partial class AdamRoleCollection : System.Collections.ReadOnlyCollectionBase {
    internal AdamRoleCollection() { }
    public System.DirectoryServices.ActiveDirectory.AdamRole this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.AdamRole); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.AdamRole role) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.AdamRole[] roles, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.AdamRole role) { return default(int); }
  }
  public partial class ApplicationPartition : System.DirectoryServices.ActiveDirectory.ActiveDirectoryPartition {
    public ApplicationPartition(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string distinguishedName) { }
    public ApplicationPartition(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string distinguishedName, string objectClass) { }
    public System.DirectoryServices.ActiveDirectory.DirectoryServerCollection DirectoryServers { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServerCollection); } }
    public string SecurityReferenceDomain { get { return default(string); } set { } }
    public void Delete() { }
    protected override void Dispose(bool disposing) { }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDirectoryServers() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDirectoryServers(string siteName) { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServers() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServers(string siteName) { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection); }
    public static System.DirectoryServices.ActiveDirectory.ApplicationPartition FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string distinguishedName) { return default(System.DirectoryServices.ActiveDirectory.ApplicationPartition); }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer() { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer(bool forceRediscovery) { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer(string siteName) { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer(string siteName, bool forceRediscovery) { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); }
    public static System.DirectoryServices.ActiveDirectory.ApplicationPartition GetApplicationPartition(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.ApplicationPartition); }
    public override System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
  }
  public partial class ApplicationPartitionCollection : System.Collections.ReadOnlyCollectionBase {
    internal ApplicationPartitionCollection() { }
    public System.DirectoryServices.ActiveDirectory.ApplicationPartition this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ApplicationPartition); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ApplicationPartition applicationPartition) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ApplicationPartition[] applicationPartitions, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ApplicationPartition applicationPartition) { return default(int); }
  }
  public partial class AttributeMetadata {
    internal AttributeMetadata() { }
    public System.DateTime LastOriginatingChangeTime { get { return default(System.DateTime); } }
    public System.Guid LastOriginatingInvocationId { get { return default(System.Guid); } }
    public long LocalChangeUsn { get { return default(long); } }
    public string Name { get { return default(string); } }
    public long OriginatingChangeUsn { get { return default(long); } }
    public string OriginatingServer { get { return default(string); } }
    public int Version { get { return default(int); } }
  }
  public partial class AttributeMetadataCollection : System.Collections.ReadOnlyCollectionBase {
    internal AttributeMetadataCollection() { }
    public System.DirectoryServices.ActiveDirectory.AttributeMetadata this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.AttributeMetadata); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.AttributeMetadata metadata) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.AttributeMetadata[] metadata, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.AttributeMetadata metadata) { return default(int); }
  }
  public partial class ConfigurationSet {
    internal ConfigurationSet() { }
    public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection AdamInstances { get { return default(System.DirectoryServices.ActiveDirectory.AdamInstanceCollection); } }
    public System.DirectoryServices.ActiveDirectory.ApplicationPartitionCollection ApplicationPartitions { get { return default(System.DirectoryServices.ActiveDirectory.ApplicationPartitionCollection); } }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.AdamInstance NamingRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema Schema { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema); } }
    public System.DirectoryServices.ActiveDirectory.AdamInstance SchemaRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection Sites { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection); } }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    public System.DirectoryServices.ActiveDirectory.AdamInstance FindAdamInstance() { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); }
    public System.DirectoryServices.ActiveDirectory.AdamInstance FindAdamInstance(string partitionName) { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); }
    public System.DirectoryServices.ActiveDirectory.AdamInstance FindAdamInstance(string partitionName, string siteName) { return default(System.DirectoryServices.ActiveDirectory.AdamInstance); }
    public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAllAdamInstances() { return default(System.DirectoryServices.ActiveDirectory.AdamInstanceCollection); }
    public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAllAdamInstances(string partitionName) { return default(System.DirectoryServices.ActiveDirectory.AdamInstanceCollection); }
    public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAllAdamInstances(string partitionName, string siteName) { return default(System.DirectoryServices.ActiveDirectory.AdamInstanceCollection); }
    public static System.DirectoryServices.ActiveDirectory.ConfigurationSet GetConfigurationSet(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.ConfigurationSet); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public System.DirectoryServices.ActiveDirectory.ReplicationSecurityLevel GetSecurityLevel() { return default(System.DirectoryServices.ActiveDirectory.ReplicationSecurityLevel); }
    public void SetSecurityLevel(System.DirectoryServices.ActiveDirectory.ReplicationSecurityLevel securityLevel) { }
    public override string ToString() { return default(string); }
  }
  public partial class DirectoryContext {
    public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType) { }
    public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType, string name) { }
    public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType, string username, string password) { }
    public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType, string name, string username, string password) { }
    public System.DirectoryServices.ActiveDirectory.DirectoryContextType ContextType { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryContextType); } }
    public string Name { get { return default(string); } }
    public string UserName { get { return default(string); } }
  }
  public enum DirectoryContextType {
    ApplicationPartition = 4,
    ConfigurationSet = 3,
    DirectoryServer = 2,
    Domain = 0,
    Forest = 1,
  }
  public abstract partial class DirectoryServer : System.IDisposable {
    protected DirectoryServer() { }
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection InboundConnections { get; }
    public abstract string IPAddress { get; }
    public string Name { get { return default(string); } }
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection OutboundConnections { get; }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyStringCollection Partitions { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyStringCollection); } }
    public abstract string SiteName { get; }
    public abstract System.DirectoryServices.ActiveDirectory.SyncUpdateCallback SyncFromAllServersCallback { get; set; }
    public abstract void CheckReplicationConsistency();
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    ~DirectoryServer() { }
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetAllReplicationNeighbors();
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection GetReplicationConnectionFailures();
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection GetReplicationCursors(string partition);
    public abstract System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath);
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetReplicationNeighbors(string partition);
    public abstract System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation GetReplicationOperationInformation();
    public void MoveToAnotherSite(string siteName) { }
    public abstract void SyncReplicaFromAllServers(string partition, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOptions options);
    public abstract void SyncReplicaFromServer(string partition, string sourceServer);
    public override string ToString() { return default(string); }
    public abstract void TriggerSyncReplicaFromNeighbors(string partition);
  }
  public partial class DirectoryServerCollection : System.Collections.CollectionBase {
    internal DirectoryServerCollection() { }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); } set { } }
    public int Add(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { return default(int); }
    public void AddRange(System.DirectoryServices.ActiveDirectory.DirectoryServer[] servers) { }
    public bool Contains(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.DirectoryServer[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { return default(int); }
    public void Insert(int index, System.DirectoryServices.ActiveDirectory.DirectoryServer server) { }
    protected override void OnClear() { }
    protected override void OnClearComplete() { }
    protected override void OnInsertComplete(int index, object value) { }
    protected override void OnRemoveComplete(int index, object value) { }
    protected override void OnSetComplete(int index, object oldValue, object newValue) { }
    protected override void OnValidate(object value) { }
    public void Remove(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { }
  }
  public partial class Domain : System.DirectoryServices.ActiveDirectory.ActiveDirectoryPartition {
    internal Domain() { }
    public System.DirectoryServices.ActiveDirectory.DomainCollection Children { get { return default(System.DirectoryServices.ActiveDirectory.DomainCollection); } }
    public System.DirectoryServices.ActiveDirectory.DomainControllerCollection DomainControllers { get { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); } }
    public System.DirectoryServices.ActiveDirectory.DomainMode DomainMode { get { return default(System.DirectoryServices.ActiveDirectory.DomainMode); } }
    public int DomainModeLevel { get { return default(int); } }
    public System.DirectoryServices.ActiveDirectory.Forest Forest { get { return default(System.DirectoryServices.ActiveDirectory.Forest); } }
    public System.DirectoryServices.ActiveDirectory.DomainController InfrastructureRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.DomainController); } }
    public System.DirectoryServices.ActiveDirectory.Domain Parent { get { return default(System.DirectoryServices.ActiveDirectory.Domain); } }
    public System.DirectoryServices.ActiveDirectory.DomainController PdcRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.DomainController); } }
    public System.DirectoryServices.ActiveDirectory.DomainController RidRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.DomainController); } }
    public void CreateLocalSideOfTrustRelationship(string targetDomainName, System.DirectoryServices.ActiveDirectory.TrustDirection direction, string trustPassword) { }
    public void CreateTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
    public void DeleteLocalSideOfTrustRelationship(string targetDomainName) { }
    public void DeleteTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain) { }
    public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDiscoverableDomainControllers() { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); }
    public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDiscoverableDomainControllers(string siteName) { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); }
    public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDomainControllers() { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); }
    public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDomainControllers(string siteName) { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); }
    public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController() { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController(System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController(string siteName) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController(string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformationCollection GetAllTrustRelationships() { return default(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformationCollection); }
    public static System.DirectoryServices.ActiveDirectory.Domain GetComputerDomain() { return default(System.DirectoryServices.ActiveDirectory.Domain); }
    public static System.DirectoryServices.ActiveDirectory.Domain GetCurrentDomain() { return default(System.DirectoryServices.ActiveDirectory.Domain); }
    public override System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public static System.DirectoryServices.ActiveDirectory.Domain GetDomain(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.Domain); }
    public bool GetSelectiveAuthenticationStatus(string targetDomainName) { return default(bool); }
    public bool GetSidFilteringStatus(string targetDomainName) { return default(bool); }
    public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation GetTrustRelationship(string targetDomainName) { return default(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation); }
    public void RaiseDomainFunctionality(System.DirectoryServices.ActiveDirectory.DomainMode domainMode) { }
    public void RaiseDomainFunctionalityLevel(int domainMode) { }
    public void RepairTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain) { }
    public void SetSelectiveAuthenticationStatus(string targetDomainName, bool enable) { }
    public void SetSidFilteringStatus(string targetDomainName, bool enable) { }
    public void UpdateLocalSideOfTrustRelationship(string targetDomainName, System.DirectoryServices.ActiveDirectory.TrustDirection newTrustDirection, string newTrustPassword) { }
    public void UpdateLocalSideOfTrustRelationship(string targetDomainName, string newTrustPassword) { }
    public void UpdateTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain, System.DirectoryServices.ActiveDirectory.TrustDirection newTrustDirection) { }
    public void VerifyOutboundTrustRelationship(string targetDomainName) { }
    public void VerifyTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
  }
  public partial class DomainCollection : System.Collections.ReadOnlyCollectionBase {
    internal DomainCollection() { }
    public System.DirectoryServices.ActiveDirectory.Domain this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.Domain); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.Domain domain) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.Domain[] domains, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.Domain domain) { return default(int); }
  }
  [System.FlagsAttribute]
  public enum DomainCollisionOptions {
    NetBiosNameDisabledByAdmin = 4,
    NetBiosNameDisabledByConflict = 8,
    None = 0,
    SidDisabledByAdmin = 1,
    SidDisabledByConflict = 2,
  }
  public partial class DomainController : System.DirectoryServices.ActiveDirectory.DirectoryServer {
    protected DomainController() { }
    public System.DateTime CurrentTime { get { return default(System.DateTime); } }
    public System.DirectoryServices.ActiveDirectory.Domain Domain { get { return default(System.DirectoryServices.ActiveDirectory.Domain); } }
    public System.DirectoryServices.ActiveDirectory.Forest Forest { get { return default(System.DirectoryServices.ActiveDirectory.Forest); } }
    public long HighestCommittedUsn { get { return default(long); } }
    public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection InboundConnections { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection); } }
    public override string IPAddress { get { return default(string); } }
    public string OSVersion { get { return default(string); } }
    public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection OutboundConnections { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryRoleCollection Roles { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRoleCollection); } }
    public override string SiteName { get { return default(string); } }
    public override System.DirectoryServices.ActiveDirectory.SyncUpdateCallback SyncFromAllServersCallback { get { return default(System.DirectoryServices.ActiveDirectory.SyncUpdateCallback); } set { } }
    public override void CheckReplicationConsistency() { }
    protected override void Dispose(bool disposing) { }
    public virtual System.DirectoryServices.ActiveDirectory.GlobalCatalog EnableGlobalCatalog() { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    ~DomainController() { }
    public static System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); }
    public static System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { return default(System.DirectoryServices.ActiveDirectory.DomainControllerCollection); }
    public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetAllReplicationNeighbors() { return default(System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection); }
    public virtual System.DirectoryServices.DirectorySearcher GetDirectorySearcher() { return default(System.DirectoryServices.DirectorySearcher); }
    public static System.DirectoryServices.ActiveDirectory.DomainController GetDomainController(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection GetReplicationConnectionFailures() { return default(System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection GetReplicationCursors(string partition) { return default(System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection); }
    public override System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath) { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetReplicationNeighbors(string partition) { return default(System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection); }
    public override System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation GetReplicationOperationInformation() { return default(System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation); }
    public virtual bool IsGlobalCatalog() { return default(bool); }
    public void SeizeRoleOwnership(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { }
    public override void SyncReplicaFromAllServers(string partition, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOptions options) { }
    public override void SyncReplicaFromServer(string partition, string sourceServer) { }
    public void TransferRoleOwnership(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { }
    public override void TriggerSyncReplicaFromNeighbors(string partition) { }
  }
  public partial class DomainControllerCollection : System.Collections.ReadOnlyCollectionBase {
    internal DomainControllerCollection() { }
    public System.DirectoryServices.ActiveDirectory.DomainController this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.DomainController); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.DomainController domainController) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.DomainController[] domainControllers, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.DomainController domainController) { return default(int); }
  }
  public enum DomainMode {
    Unknown = -1,
    Windows2000MixedDomain = 0,
    Windows2000NativeDomain = 1,
    Windows2003Domain = 3,
    Windows2003InterimDomain = 2,
    Windows2008Domain = 4,
    Windows2008R2Domain = 5,
    Windows2012R2Domain = 7,
    Windows8Domain = 6,
  }
  public partial class Forest : System.IDisposable {
    internal Forest() { }
    public System.DirectoryServices.ActiveDirectory.ApplicationPartitionCollection ApplicationPartitions { get { return default(System.DirectoryServices.ActiveDirectory.ApplicationPartitionCollection); } }
    public System.DirectoryServices.ActiveDirectory.DomainCollection Domains { get { return default(System.DirectoryServices.ActiveDirectory.DomainCollection); } }
    public System.DirectoryServices.ActiveDirectory.ForestMode ForestMode { get { return default(System.DirectoryServices.ActiveDirectory.ForestMode); } }
    public int ForestModeLevel { get { return default(int); } }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection GlobalCatalogs { get { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); } }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.DomainController NamingRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.DomainController); } }
    public System.DirectoryServices.ActiveDirectory.Domain RootDomain { get { return default(System.DirectoryServices.ActiveDirectory.Domain); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema Schema { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema); } }
    public System.DirectoryServices.ActiveDirectory.DomainController SchemaRoleOwner { get { return default(System.DirectoryServices.ActiveDirectory.DomainController); } }
    public System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection Sites { get { return default(System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection); } }
    public void CreateLocalSideOfTrustRelationship(string targetForestName, System.DirectoryServices.ActiveDirectory.TrustDirection direction, string trustPassword) { }
    public void CreateTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
    public void DeleteLocalSideOfTrustRelationship(string targetForestName) { }
    public void DeleteTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest) { }
    public void Dispose() { }
    protected void Dispose(bool disposing) { }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllDiscoverableGlobalCatalogs() { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllDiscoverableGlobalCatalogs(string siteName) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllGlobalCatalogs() { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllGlobalCatalogs(string siteName) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog() { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog(System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog(string siteName) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog(string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformationCollection GetAllTrustRelationships() { return default(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformationCollection); }
    public static System.DirectoryServices.ActiveDirectory.Forest GetCurrentForest() { return default(System.DirectoryServices.ActiveDirectory.Forest); }
    public static System.DirectoryServices.ActiveDirectory.Forest GetForest(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.Forest); }
    public bool GetSelectiveAuthenticationStatus(string targetForestName) { return default(bool); }
    public bool GetSidFilteringStatus(string targetForestName) { return default(bool); }
    public System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipInformation GetTrustRelationship(string targetForestName) { return default(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipInformation); }
    public void RaiseForestFunctionality(System.DirectoryServices.ActiveDirectory.ForestMode forestMode) { }
    public void RaiseForestFunctionalityLevel(int forestMode) { }
    public void RepairTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest) { }
    public void SetSelectiveAuthenticationStatus(string targetForestName, bool enable) { }
    public void SetSidFilteringStatus(string targetForestName, bool enable) { }
    public override string ToString() { return default(string); }
    public void UpdateLocalSideOfTrustRelationship(string targetForestName, System.DirectoryServices.ActiveDirectory.TrustDirection newTrustDirection, string newTrustPassword) { }
    public void UpdateLocalSideOfTrustRelationship(string targetForestName, string newTrustPassword) { }
    public void UpdateTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest, System.DirectoryServices.ActiveDirectory.TrustDirection newTrustDirection) { }
    public void VerifyOutboundTrustRelationship(string targetForestName) { }
    public void VerifyTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
  }
  public enum ForestMode {
    Unknown = -1,
    Windows2000Forest = 0,
    Windows2003Forest = 2,
    Windows2003InterimForest = 1,
    Windows2008Forest = 3,
    Windows2008R2Forest = 4,
    Windows2012R2Forest = 6,
    Windows8Forest = 5,
  }
  public partial class ForestTrustCollisionException : System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException, System.Runtime.Serialization.ISerializable {
    public ForestTrustCollisionException() { }
    protected ForestTrustCollisionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public ForestTrustCollisionException(string message) { }
    public ForestTrustCollisionException(string message, System.Exception inner) { }
    public ForestTrustCollisionException(string message, System.Exception inner, System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollisionCollection collisions) { }
    public System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollisionCollection Collisions { get { return default(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollisionCollection); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
  }
  public enum ForestTrustCollisionType {
    Domain = 1,
    Other = 2,
    TopLevelName = 0,
  }
  public partial class ForestTrustDomainInfoCollection : System.Collections.ReadOnlyCollectionBase {
    internal ForestTrustDomainInfoCollection() { }
    public System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation information) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation information) { return default(int); }
  }
  public partial class ForestTrustDomainInformation {
    internal ForestTrustDomainInformation() { }
    public string DnsName { get { return default(string); } }
    public string DomainSid { get { return default(string); } }
    public string NetBiosName { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ForestTrustDomainStatus Status { get { return default(System.DirectoryServices.ActiveDirectory.ForestTrustDomainStatus); } set { } }
  }
  public enum ForestTrustDomainStatus {
    Enabled = 0,
    NetBiosNameAdminDisabled = 4,
    NetBiosNameConflictDisabled = 8,
    SidAdminDisabled = 1,
    SidConflictDisabled = 2,
  }
  public partial class ForestTrustRelationshipCollision {
    internal ForestTrustRelationshipCollision() { }
    public string CollisionRecord { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ForestTrustCollisionType CollisionType { get { return default(System.DirectoryServices.ActiveDirectory.ForestTrustCollisionType); } }
    public System.DirectoryServices.ActiveDirectory.DomainCollisionOptions DomainCollisionOption { get { return default(System.DirectoryServices.ActiveDirectory.DomainCollisionOptions); } }
    public System.DirectoryServices.ActiveDirectory.TopLevelNameCollisionOptions TopLevelNameCollisionOption { get { return default(System.DirectoryServices.ActiveDirectory.TopLevelNameCollisionOptions); } }
  }
  public partial class ForestTrustRelationshipCollisionCollection : System.Collections.ReadOnlyCollectionBase {
    internal ForestTrustRelationshipCollisionCollection() { }
    public System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision collision) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision collision) { return default(int); }
  }
  public partial class ForestTrustRelationshipInformation : System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation {
    internal ForestTrustRelationshipInformation() { }
    public System.Collections.Specialized.StringCollection ExcludedTopLevelNames { get { return default(System.Collections.Specialized.StringCollection); } }
    public System.DirectoryServices.ActiveDirectory.TopLevelNameCollection TopLevelNames { get { return default(System.DirectoryServices.ActiveDirectory.TopLevelNameCollection); } }
    public System.DirectoryServices.ActiveDirectory.ForestTrustDomainInfoCollection TrustedDomainInformation { get { return default(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInfoCollection); } }
    public void Save() { }
  }
  public partial class GlobalCatalog : System.DirectoryServices.ActiveDirectory.DomainController {
    internal GlobalCatalog() { }
    public System.DirectoryServices.ActiveDirectory.DomainController DisableGlobalCatalog() { return default(System.DirectoryServices.ActiveDirectory.DomainController); }
    public override System.DirectoryServices.ActiveDirectory.GlobalCatalog EnableGlobalCatalog() { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public static new System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); }
    public static new System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection); }
    public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties() { return default(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection); }
    public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public override System.DirectoryServices.DirectorySearcher GetDirectorySearcher() { return default(System.DirectoryServices.DirectorySearcher); }
    public static System.DirectoryServices.ActiveDirectory.GlobalCatalog GetGlobalCatalog(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); }
    public override bool IsGlobalCatalog() { return default(bool); }
  }
  public partial class GlobalCatalogCollection : System.Collections.ReadOnlyCollectionBase {
    internal GlobalCatalogCollection() { }
    public System.DirectoryServices.ActiveDirectory.GlobalCatalog this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.GlobalCatalog); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.GlobalCatalog globalCatalog) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.GlobalCatalog[] globalCatalogs, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.GlobalCatalog globalCatalog) { return default(int); }
  }
  public enum HourOfDay {
    Eight = 8,
    Eighteen = 18,
    Eleven = 11,
    Fifteen = 15,
    Five = 5,
    Four = 4,
    Fourteen = 14,
    Nine = 9,
    Nineteen = 19,
    One = 1,
    Seven = 7,
    Seventeen = 17,
    Six = 6,
    Sixteen = 16,
    Ten = 10,
    Thirteen = 13,
    Three = 3,
    Twelve = 12,
    Twenty = 20,
    TwentyOne = 21,
    TwentyThree = 23,
    TwentyTwo = 22,
    Two = 2,
    Zero = 0,
  }
  [System.FlagsAttribute]
  public enum LocatorOptions : long {
    AvoidSelf = (long)16384,
    ForceRediscovery = (long)1,
    KdcRequired = (long)1024,
    TimeServerRequired = (long)2048,
    WriteableRequired = (long)4096,
  }
  public enum MinuteOfHour {
    Fifteen = 15,
    FortyFive = 45,
    Thirty = 30,
    Zero = 0,
  }
  public enum NotificationStatus {
    IntraSiteOnly = 1,
    NoNotification = 0,
    NotificationAlways = 2,
  }
  [System.FlagsAttribute]
  public enum PropertyTypes {
    Indexed = 2,
    InGlobalCatalog = 4,
  }
  public partial class ReadOnlyActiveDirectorySchemaClassCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlyActiveDirectorySchemaClassCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass[] classes, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { return default(int); }
  }
  public partial class ReadOnlyActiveDirectorySchemaPropertyCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlyActiveDirectorySchemaPropertyCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty[] properties, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { return default(int); }
  }
  public partial class ReadOnlyDirectoryServerCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlyDirectoryServerCollection() { }
    public System.DirectoryServices.ActiveDirectory.DirectoryServer this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.DirectoryServer); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.DirectoryServer directoryServer) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.DirectoryServer[] directoryServers, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.DirectoryServer directoryServer) { return default(int); }
  }
  public partial class ReadOnlySiteCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlySiteCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySite this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite[] sites, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { return default(int); }
  }
  public partial class ReadOnlySiteLinkBridgeCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlySiteLinkBridgeCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge bridge) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge[] bridges, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge bridge) { return default(int); }
  }
  public partial class ReadOnlySiteLinkCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlySiteLinkCollection() { }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink[] links, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { return default(int); }
  }
  public partial class ReadOnlyStringCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReadOnlyStringCollection() { }
    public string this[int index] { get { return default(string); } }
    public bool Contains(string value) { return default(bool); }
    public void CopyTo(string[] values, int index) { }
    public int IndexOf(string value) { return default(int); }
  }
  public partial class ReplicationConnection : System.IDisposable {
    public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer) { }
    public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule) { }
    public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
    public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
    public System.DirectoryServices.ActiveDirectory.NotificationStatus ChangeNotificationStatus { get { return default(System.DirectoryServices.ActiveDirectory.NotificationStatus); } set { } }
    public bool DataCompressionEnabled { get { return default(bool); } set { } }
    public string DestinationServer { get { return default(string); } }
    public bool Enabled { get { return default(bool); } set { } }
    public bool GeneratedByKcc { get { return default(bool); } set { } }
    public string Name { get { return default(string); } }
    public bool ReciprocalReplicationEnabled { get { return default(bool); } set { } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule ReplicationSchedule { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule); } set { } }
    public bool ReplicationScheduleOwnedByUser { get { return default(bool); } set { } }
    public System.DirectoryServices.ActiveDirectory.ReplicationSpan ReplicationSpan { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationSpan); } }
    public string SourceServer { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType); } }
    public void Delete() { }
    public void Dispose() { }
    protected virtual void Dispose(bool disposing) { }
    ~ReplicationConnection() { }
    public static System.DirectoryServices.ActiveDirectory.ReplicationConnection FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name) { return default(System.DirectoryServices.ActiveDirectory.ReplicationConnection); }
    public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { return default(System.DirectoryServices.DirectoryEntry); }
    public void Save() { }
    public override string ToString() { return default(string); }
  }
  public partial class ReplicationConnectionCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReplicationConnectionCollection() { }
    public System.DirectoryServices.ActiveDirectory.ReplicationConnection this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationConnection); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationConnection connection) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationConnection[] connections, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationConnection connection) { return default(int); }
  }
  public partial class ReplicationCursor {
    internal ReplicationCursor() { }
    public System.DateTime LastSuccessfulSyncTime { get { return default(System.DateTime); } }
    public string PartitionName { get { return default(string); } }
    public System.Guid SourceInvocationId { get { return default(System.Guid); } }
    public string SourceServer { get { return default(string); } }
    public long UpToDatenessUsn { get { return default(long); } }
  }
  public partial class ReplicationCursorCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReplicationCursorCollection() { }
    public System.DirectoryServices.ActiveDirectory.ReplicationCursor this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationCursor); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationCursor cursor) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationCursor[] values, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationCursor cursor) { return default(int); }
  }
  public partial class ReplicationFailure {
    internal ReplicationFailure() { }
    public int ConsecutiveFailureCount { get { return default(int); } }
    public System.DateTime FirstFailureTime { get { return default(System.DateTime); } }
    public int LastErrorCode { get { return default(int); } }
    public string LastErrorMessage { get { return default(string); } }
    public string SourceServer { get { return default(string); } }
  }
  public partial class ReplicationFailureCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReplicationFailureCollection() { }
    public System.DirectoryServices.ActiveDirectory.ReplicationFailure this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationFailure); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationFailure failure) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationFailure[] failures, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationFailure failure) { return default(int); }
  }
  public partial class ReplicationNeighbor {
    internal ReplicationNeighbor() { }
    public int ConsecutiveFailureCount { get { return default(int); } }
    public System.DateTime LastAttemptedSync { get { return default(System.DateTime); } }
    public System.DateTime LastSuccessfulSync { get { return default(System.DateTime); } }
    public string LastSyncMessage { get { return default(string); } }
    public int LastSyncResult { get { return default(int); } }
    public string PartitionName { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ReplicationNeighbor.ReplicationNeighborOptions ReplicationNeighborOption { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor.ReplicationNeighborOptions); } }
    public System.Guid SourceInvocationId { get { return default(System.Guid); } }
    public string SourceServer { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { return default(System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType); } }
    public long UsnAttributeFilter { get { return default(long); } }
    public long UsnLastObjectChangeSynced { get { return default(long); } }
    [System.FlagsAttribute]
    public enum ReplicationNeighborOptions : long {
      CompressChanges = (long)268435456,
      DisableScheduledSync = (long)134217728,
      FullSyncInProgress = (long)65536,
      FullSyncNextPacket = (long)131072,
      IgnoreChangeNotifications = (long)67108864,
      NeverSynced = (long)2097152,
      NoChangeNotifications = (long)536870912,
      PartialAttributeSet = (long)1073741824,
      Preempted = (long)16777216,
      ReturnObjectParent = (long)2048,
      ScheduledSync = (long)64,
      SyncOnStartup = (long)32,
      TwoWaySync = (long)512,
      UseInterSiteTransport = (long)128,
      Writeable = (long)16,
    }
  }
  public partial class ReplicationNeighborCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReplicationNeighborCollection() { }
    public System.DirectoryServices.ActiveDirectory.ReplicationNeighbor this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor neighbor) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor[] neighbors, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor neighbor) { return default(int); }
  }
  public partial class ReplicationOperation {
    internal ReplicationOperation() { }
    public int OperationNumber { get { return default(int); } }
    public System.DirectoryServices.ActiveDirectory.ReplicationOperationType OperationType { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationOperationType); } }
    public string PartitionName { get { return default(string); } }
    public int Priority { get { return default(int); } }
    public string SourceServer { get { return default(string); } }
    public System.DateTime TimeEnqueued { get { return default(System.DateTime); } }
  }
  public partial class ReplicationOperationCollection : System.Collections.ReadOnlyCollectionBase {
    internal ReplicationOperationCollection() { }
    public System.DirectoryServices.ActiveDirectory.ReplicationOperation this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationOperation); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationOperation operation) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationOperation[] operations, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationOperation operation) { return default(int); }
  }
  public partial class ReplicationOperationInformation {
    public ReplicationOperationInformation() { }
    public System.DirectoryServices.ActiveDirectory.ReplicationOperation CurrentOperation { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationOperation); } }
    public System.DateTime OperationStartTime { get { return default(System.DateTime); } }
    public System.DirectoryServices.ActiveDirectory.ReplicationOperationCollection PendingOperations { get { return default(System.DirectoryServices.ActiveDirectory.ReplicationOperationCollection); } }
  }
  public enum ReplicationOperationType {
    Add = 1,
    Delete = 2,
    Modify = 3,
    Sync = 0,
    UpdateReference = 4,
  }
  public enum ReplicationSecurityLevel {
    MutualAuthentication = 2,
    Negotiate = 1,
    NegotiatePassThrough = 0,
  }
  public enum ReplicationSpan {
    InterSite = 1,
    IntraSite = 0,
  }
  public enum SchemaClassType {
    Abstract = 2,
    Auxiliary = 3,
    Structural = 1,
    Type88 = 0,
  }
  public enum SyncFromAllServersErrorCategory {
    ErrorContactingServer = 0,
    ErrorReplicating = 1,
    ServerUnreachable = 2,
  }
  public partial class SyncFromAllServersErrorInformation {
    internal SyncFromAllServersErrorInformation() { }
    public System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorCategory ErrorCategory { get { return default(System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorCategory); } }
    public int ErrorCode { get { return default(int); } }
    public string ErrorMessage { get { return default(string); } }
    public string SourceServer { get { return default(string); } }
    public string TargetServer { get { return default(string); } }
  }
  public enum SyncFromAllServersEvent {
    Error = 0,
    Finished = 3,
    SyncCompleted = 2,
    SyncStarted = 1,
  }
  public partial class SyncFromAllServersOperationException : System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException, System.Runtime.Serialization.ISerializable {
    public SyncFromAllServersOperationException() { }
    protected SyncFromAllServersOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public SyncFromAllServersOperationException(string message) { }
    public SyncFromAllServersOperationException(string message, System.Exception inner) { }
    public SyncFromAllServersOperationException(string message, System.Exception inner, System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorInformation[] errors) { }
    public System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorInformation[] ErrorInformation { get { return default(System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorInformation[]); } }
    public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
  }
  [System.FlagsAttribute]
  public enum SyncFromAllServersOptions {
    AbortIfServerUnavailable = 1,
    CheckServerAlivenessOnly = 8,
    CrossSite = 64,
    None = 0,
    PushChangeOutward = 32,
    SkipInitialCheck = 16,
    SyncAdjacentServerOnly = 2,
  }
  public delegate bool SyncUpdateCallback(System.DirectoryServices.ActiveDirectory.SyncFromAllServersEvent eventType, string targetServer, string sourceServer, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOperationException exception);
  public partial class TopLevelName {
    internal TopLevelName() { }
    public string Name { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.TopLevelNameStatus Status { get { return default(System.DirectoryServices.ActiveDirectory.TopLevelNameStatus); } set { } }
  }
  public partial class TopLevelNameCollection : System.Collections.ReadOnlyCollectionBase {
    internal TopLevelNameCollection() { }
    public System.DirectoryServices.ActiveDirectory.TopLevelName this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.TopLevelName); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.TopLevelName name) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.TopLevelName[] names, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.TopLevelName name) { return default(int); }
  }
  [System.FlagsAttribute]
  public enum TopLevelNameCollisionOptions {
    DisabledByAdmin = 2,
    DisabledByConflict = 4,
    NewlyCreated = 1,
    None = 0,
  }
  public enum TopLevelNameStatus {
    AdminDisabled = 2,
    ConflictDisabled = 4,
    Enabled = 0,
    NewlyCreated = 1,
  }
  public enum TrustDirection {
    Bidirectional = 3,
    Inbound = 1,
    Outbound = 2,
  }
  public partial class TrustRelationshipInformation {
    internal TrustRelationshipInformation() { }
    public string SourceName { get { return default(string); } }
    public string TargetName { get { return default(string); } }
    public System.DirectoryServices.ActiveDirectory.TrustDirection TrustDirection { get { return default(System.DirectoryServices.ActiveDirectory.TrustDirection); } }
    public System.DirectoryServices.ActiveDirectory.TrustType TrustType { get { return default(System.DirectoryServices.ActiveDirectory.TrustType); } }
  }
  public partial class TrustRelationshipInformationCollection : System.Collections.ReadOnlyCollectionBase {
    internal TrustRelationshipInformationCollection() { }
    public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation this[int index] { get { return default(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation); } }
    public bool Contains(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation information) { return default(bool); }
    public void CopyTo(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation[] array, int index) { }
    public int IndexOf(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation information) { return default(int); }
  }
  public enum TrustType {
    CrossLink = 2,
    External = 3,
    Forest = 4,
    Kerberos = 5,
    ParentChild = 1,
    TreeRoot = 0,
    Unknown = 6,
  }
}

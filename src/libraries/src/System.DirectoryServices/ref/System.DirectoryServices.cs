// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices
{
    public partial class ActiveDirectoryAccessRule : System.Security.AccessControl.ObjectAccessRule
    {
        public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) { }
        public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) { }
        public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) { }
        public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.Guid objectType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) { }
        public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) { }
        public ActiveDirectoryAccessRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AccessControlType type, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AccessControlType)) { }
        public System.DirectoryServices.ActiveDirectoryRights ActiveDirectoryRights { get { throw null; } }
        public System.DirectoryServices.ActiveDirectorySecurityInheritance InheritanceType { get { throw null; } }
    }
    public partial class ActiveDirectoryAuditRule : System.Security.AccessControl.ObjectAuditRule
    {
        public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) { }
        public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) { }
        public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) { }
        public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.Guid objectType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) { }
        public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) { }
        public ActiveDirectoryAuditRule(System.Security.Principal.IdentityReference identity, System.DirectoryServices.ActiveDirectoryRights adRights, System.Security.AccessControl.AuditFlags auditFlags, System.Guid objectType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Guid), default(System.Guid), default(System.Security.AccessControl.AuditFlags)) { }
        public System.DirectoryServices.ActiveDirectoryRights ActiveDirectoryRights { get { throw null; } }
        public System.DirectoryServices.ActiveDirectorySecurityInheritance InheritanceType { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum ActiveDirectoryRights
    {
        CreateChild = 1,
        DeleteChild = 2,
        ListChildren = 4,
        Self = 8,
        ReadProperty = 16,
        WriteProperty = 32,
        DeleteTree = 64,
        ListObject = 128,
        ExtendedRight = 256,
        Delete = 65536,
        ReadControl = 131072,
        GenericExecute = 131076,
        GenericWrite = 131112,
        GenericRead = 131220,
        WriteDacl = 262144,
        WriteOwner = 524288,
        GenericAll = 983551,
        Synchronize = 1048576,
        AccessSystemSecurity = 16777216,
    }
    public partial class ActiveDirectorySecurity : System.Security.AccessControl.DirectoryObjectSecurity
    {
        public ActiveDirectorySecurity() { }
        public override System.Type AccessRightType { get { throw null; } }
        public override System.Type AccessRuleType { get { throw null; } }
        public override System.Type AuditRuleType { get { throw null; } }
        public sealed override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { throw null; }
        public sealed override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type, System.Guid objectGuid, System.Guid inheritedObjectGuid) { throw null; }
        public void AddAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
        public void AddAuditRule(System.DirectoryServices.ActiveDirectoryAuditRule rule) { }
        public sealed override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { throw null; }
        public sealed override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags, System.Guid objectGuid, System.Guid inheritedObjectGuid) { throw null; }
        public override bool ModifyAccessRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified) { throw null; }
        public override bool ModifyAuditRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified) { throw null; }
        public override void PurgeAccessRules(System.Security.Principal.IdentityReference identity) { }
        public override void PurgeAuditRules(System.Security.Principal.IdentityReference identity) { }
        public void RemoveAccess(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) { }
        public bool RemoveAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { throw null; }
        public void RemoveAccessRuleSpecific(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
        public void RemoveAudit(System.Security.Principal.IdentityReference identity) { }
        public bool RemoveAuditRule(System.DirectoryServices.ActiveDirectoryAuditRule rule) { throw null; }
        public void RemoveAuditRuleSpecific(System.DirectoryServices.ActiveDirectoryAuditRule rule) { }
        public void ResetAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
        public void SetAccessRule(System.DirectoryServices.ActiveDirectoryAccessRule rule) { }
        public void SetAuditRule(System.DirectoryServices.ActiveDirectoryAuditRule rule) { }
    }
    public enum ActiveDirectorySecurityInheritance
    {
        None = 0,
        All = 1,
        Descendents = 2,
        SelfAndChildren = 3,
        Children = 4,
    }
    [System.FlagsAttribute]
    public enum AuthenticationTypes
    {
        None = 0,
        Secure = 1,
        Encryption = 2,
        SecureSocketsLayer = 2,
        ReadonlyServer = 4,
        Anonymous = 16,
        FastBind = 32,
        Signing = 64,
        Sealing = 128,
        Delegation = 256,
        ServerBind = 512,
    }
    public sealed partial class CreateChildAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public CreateChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public sealed partial class DeleteChildAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteChildAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid childType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public sealed partial class DeleteTreeAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public DeleteTreeAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteTreeAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public DeleteTreeAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public enum DereferenceAlias
    {
        Never = 0,
        InSearching = 1,
        FindingBaseObject = 2,
        Always = 3,
    }
    public partial class DirectoryEntries : System.Collections.IEnumerable
    {
        internal DirectoryEntries() { }
        public System.DirectoryServices.SchemaNameCollection SchemaFilter { get { throw null; } }
        public System.DirectoryServices.DirectoryEntry Add(string name, string schemaClassName) { throw null; }
        public System.DirectoryServices.DirectoryEntry Find(string name) { throw null; }
        public System.DirectoryServices.DirectoryEntry Find(string name, string schemaClassName) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public void Remove(System.DirectoryServices.DirectoryEntry entry) { }
    }
    public partial class DirectoryEntry : System.ComponentModel.Component
    {
        public DirectoryEntry() { }
        public DirectoryEntry(object adsObject) { }
        public DirectoryEntry(string path) { }
        public DirectoryEntry(string path, string username, string password) { }
        public DirectoryEntry(string path, string username, string password, System.DirectoryServices.AuthenticationTypes authenticationType) { }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.AuthenticationTypes.Secure)]
        public System.DirectoryServices.AuthenticationTypes AuthenticationType { get { throw null; } set { } }
        public System.DirectoryServices.DirectoryEntries Children { get { throw null; } }
        public System.Guid Guid { get { throw null; } }
        public string Name { get { throw null; } }
        public string NativeGuid { get { throw null; } }
        public object NativeObject { get { throw null; } }
        public System.DirectoryServices.ActiveDirectorySecurity ObjectSecurity { get { throw null; } set { } }
        public System.DirectoryServices.DirectoryEntryConfiguration Options { get { throw null; } }
        public System.DirectoryServices.DirectoryEntry Parent { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string Password { set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string Path { get { throw null; } set { } }
        public System.DirectoryServices.PropertyCollection Properties { get { throw null; } }
        public string SchemaClassName { get { throw null; } }
        public System.DirectoryServices.DirectoryEntry SchemaEntry { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool UsePropertyCache { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string Username { get { throw null; } set { } }
        public void Close() { }
        public void CommitChanges() { }
        public System.DirectoryServices.DirectoryEntry CopyTo(System.DirectoryServices.DirectoryEntry newParent) { throw null; }
        public System.DirectoryServices.DirectoryEntry CopyTo(System.DirectoryServices.DirectoryEntry newParent, string newName) { throw null; }
        public void DeleteTree() { }
        protected override void Dispose(bool disposing) { }
        public static bool Exists(string path) { throw null; }
        public object Invoke(string methodName, params object[] args) { throw null; }
        public object InvokeGet(string propertyName) { throw null; }
        public void InvokeSet(string propertyName, params object[] args) { }
        public void MoveTo(System.DirectoryServices.DirectoryEntry newParent) { }
        public void MoveTo(System.DirectoryServices.DirectoryEntry newParent, string newName) { }
        public void RefreshCache() { }
        public void RefreshCache(string[] propertyNames) { }
        public void Rename(string newName) { }
    }
    public partial class DirectoryEntryConfiguration
    {
        internal DirectoryEntryConfiguration() { }
        public int PageSize { get { throw null; } set { } }
        public System.DirectoryServices.PasswordEncodingMethod PasswordEncoding { get { throw null; } set { } }
        public int PasswordPort { get { throw null; } set { } }
        public System.DirectoryServices.ReferralChasingOption Referral { get { throw null; } set { } }
        public System.DirectoryServices.SecurityMasks SecurityMasks { get { throw null; } set { } }
        public string GetCurrentServerName() { throw null; }
        public bool IsMutuallyAuthenticated() { throw null; }
        public void SetUserNameQueryQuota(string accountName) { }
    }
    public partial class DirectorySearcher : System.ComponentModel.Component
    {
        public DirectorySearcher() { }
        public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot) { }
        public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot, string filter) { }
        public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot, string filter, string[] propertiesToLoad) { }
        public DirectorySearcher(System.DirectoryServices.DirectoryEntry searchRoot, string filter, string[] propertiesToLoad, System.DirectoryServices.SearchScope scope) { }
        public DirectorySearcher(string filter) { }
        public DirectorySearcher(string filter, string[] propertiesToLoad) { }
        public DirectorySearcher(string filter, string[] propertiesToLoad, System.DirectoryServices.SearchScope scope) { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Asynchronous { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string AttributeScopeQuery { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool CacheResults { get { throw null; } set { } }
        public System.TimeSpan ClientTimeout { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.DereferenceAlias.Never)]
        public System.DirectoryServices.DereferenceAlias DerefAlias { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.DirectoryServices.DirectorySynchronization DirectorySynchronization { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.ExtendedDN.None)]
        public System.DirectoryServices.ExtendedDN ExtendedDN { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("(objectClass=*)")]
        public string Filter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int PageSize { get { throw null; } set { } }
        public System.Collections.Specialized.StringCollection PropertiesToLoad { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool PropertyNamesOnly { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.ReferralChasingOption.External)]
        public System.DirectoryServices.ReferralChasingOption ReferralChasing { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.DirectoryServices.DirectoryEntry SearchRoot { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.SearchScope.Subtree)]
        public System.DirectoryServices.SearchScope SearchScope { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.SecurityMasks.None)]
        public System.DirectoryServices.SecurityMasks SecurityMasks { get { throw null; } set { } }
        public System.TimeSpan ServerPageTimeLimit { get { throw null; } set { } }
        public System.TimeSpan ServerTimeLimit { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int SizeLimit { get { throw null; } set { } }
        [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
        public System.DirectoryServices.SortOption Sort { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Tombstone { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.DirectoryServices.DirectoryVirtualListView VirtualListView { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        public System.DirectoryServices.SearchResultCollection FindAll() { throw null; }
        public System.DirectoryServices.SearchResult FindOne() { throw null; }
    }
    public partial class DirectoryServicesCOMException : System.Runtime.InteropServices.COMException, System.Runtime.Serialization.ISerializable
    {
        public DirectoryServicesCOMException() { }
        protected DirectoryServicesCOMException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DirectoryServicesCOMException(string message) { }
        public DirectoryServicesCOMException(string message, System.Exception inner) { }
        public int ExtendedError { get { throw null; } }
        public string ExtendedErrorMessage { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public sealed partial class DirectoryServicesPermission : System.Security.Permissions.ResourcePermissionBase
    {
        public DirectoryServicesPermission() { }
        public DirectoryServicesPermission(System.DirectoryServices.DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public DirectoryServicesPermission(System.DirectoryServices.DirectoryServicesPermissionEntry[] permissionAccessEntries) { }
        public DirectoryServicesPermission(System.Security.Permissions.PermissionState state) { }
        public System.DirectoryServices.DirectoryServicesPermissionEntryCollection PermissionEntries { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum DirectoryServicesPermissionAccess
    {
        None = 0,
        Browse = 2,
        Write = 6,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Event | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true, Inherited=false)]
    public partial class DirectoryServicesPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public DirectoryServicesPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
        public string Path { get { throw null; } set { } }
        public System.DirectoryServices.DirectoryServicesPermissionAccess PermissionAccess { get { throw null; } set { } }
        public override System.Security.IPermission CreatePermission() { throw null; }
    }
    public partial class DirectoryServicesPermissionEntry
    {
        public DirectoryServicesPermissionEntry(System.DirectoryServices.DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public string Path { get { throw null; } }
        public System.DirectoryServices.DirectoryServicesPermissionAccess PermissionAccess { get { throw null; } }
    }
    public partial class DirectoryServicesPermissionEntryCollection : System.Collections.CollectionBase
    {
        internal DirectoryServicesPermissionEntryCollection() { }
        public System.DirectoryServices.DirectoryServicesPermissionEntry this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.DirectoryServicesPermissionEntry value) { throw null; }
        public void AddRange(System.DirectoryServices.DirectoryServicesPermissionEntryCollection value) { }
        public void AddRange(System.DirectoryServices.DirectoryServicesPermissionEntry[] value) { }
        public bool Contains(System.DirectoryServices.DirectoryServicesPermissionEntry value) { throw null; }
        public void CopyTo(System.DirectoryServices.DirectoryServicesPermissionEntry[] array, int index) { }
        public int IndexOf(System.DirectoryServices.DirectoryServicesPermissionEntry value) { throw null; }
        public void Insert(int index, System.DirectoryServices.DirectoryServicesPermissionEntry value) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(System.DirectoryServices.DirectoryServicesPermissionEntry value) { }
    }
    public partial class DirectorySynchronization
    {
        public DirectorySynchronization() { }
        public DirectorySynchronization(byte[] cookie) { }
        public DirectorySynchronization(System.DirectoryServices.DirectorySynchronization sync) { }
        public DirectorySynchronization(System.DirectoryServices.DirectorySynchronizationOptions option) { }
        public DirectorySynchronization(System.DirectoryServices.DirectorySynchronizationOptions option, byte[] cookie) { }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.DirectorySynchronizationOptions.None)]
        public System.DirectoryServices.DirectorySynchronizationOptions Option { get { throw null; } set { } }
        public System.DirectoryServices.DirectorySynchronization Copy() { throw null; }
        public byte[] GetDirectorySynchronizationCookie() { throw null; }
        public void ResetDirectorySynchronizationCookie() { }
        public void ResetDirectorySynchronizationCookie(byte[] cookie) { }
    }
    [System.FlagsAttribute]
    public enum DirectorySynchronizationOptions : long
    {
        None = (long)0,
        ObjectSecurity = (long)1,
        ParentsFirst = (long)2048,
        PublicDataOnly = (long)8192,
        IncrementalValues = (long)2147483648,
    }
    public partial class DirectoryVirtualListView
    {
        public DirectoryVirtualListView() { }
        public DirectoryVirtualListView(int afterCount) { }
        public DirectoryVirtualListView(int beforeCount, int afterCount, int offset) { }
        public DirectoryVirtualListView(int beforeCount, int afterCount, int offset, System.DirectoryServices.DirectoryVirtualListViewContext context) { }
        public DirectoryVirtualListView(int beforeCount, int afterCount, string target) { }
        public DirectoryVirtualListView(int beforeCount, int afterCount, string target, System.DirectoryServices.DirectoryVirtualListViewContext context) { }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int AfterCount { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int ApproximateTotal { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int BeforeCount { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.DirectoryServices.DirectoryVirtualListViewContext DirectoryVirtualListViewContext { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int Offset { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string Target { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int TargetPercentage { get { throw null; } set { } }
    }
    public partial class DirectoryVirtualListViewContext
    {
        public DirectoryVirtualListViewContext() { }
        public System.DirectoryServices.DirectoryVirtualListViewContext Copy() { throw null; }
    }
    public enum ExtendedDN
    {
        None = -1,
        HexString = 0,
        Standard = 1,
    }
    public sealed partial class ExtendedRightAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid extendedRightType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid extendedRightType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ExtendedRightAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.Guid extendedRightType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public sealed partial class ListChildrenAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public ListChildrenAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ListChildrenAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public ListChildrenAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public enum PasswordEncodingMethod
    {
        PasswordEncodingSsl = 0,
        PasswordEncodingClear = 1,
    }
    public enum PropertyAccess
    {
        Read = 0,
        Write = 1,
    }
    public sealed partial class PropertyAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertyType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertyType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertyAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertyType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public partial class PropertyCollection : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        internal PropertyCollection() { }
        public int Count { get { throw null; } }
        public System.DirectoryServices.PropertyValueCollection this[string propertyName] { get { throw null; } }
        public System.Collections.ICollection PropertyNames { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public bool Contains(string propertyName) { throw null; }
        public void CopyTo(System.DirectoryServices.PropertyValueCollection[] array, int index) { }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object value) { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class PropertySetAccessRule : System.DirectoryServices.ActiveDirectoryAccessRule
    {
        public PropertySetAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertySetType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertySetAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertySetType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
        public PropertySetAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.AccessControlType type, System.DirectoryServices.PropertyAccess access, System.Guid propertySetType, System.DirectoryServices.ActiveDirectorySecurityInheritance inheritanceType, System.Guid inheritedObjectType) : base (default(System.Security.Principal.IdentityReference), default(System.DirectoryServices.ActiveDirectoryRights), default(System.Security.AccessControl.AccessControlType)) { }
    }
    public partial class PropertyValueCollection : System.Collections.CollectionBase
    {
        internal PropertyValueCollection() { }
        public object this[int index] { get { throw null; } set { } }
        public string PropertyName { get { throw null; } }
        public object Value { get { throw null; } set { } }
        public int Add(object value) { throw null; }
        public void AddRange(System.DirectoryServices.PropertyValueCollection value) { }
        public void AddRange(object[] value) { }
        public bool Contains(object value) { throw null; }
        public void CopyTo(object[] array, int index) { }
        public int IndexOf(object value) { throw null; }
        public void Insert(int index, object value) { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        public void Remove(object value) { }
    }
    public enum ReferralChasingOption
    {
        None = 0,
        Subordinate = 32,
        External = 64,
        All = 96,
    }
    public partial class ResultPropertyCollection : System.Collections.DictionaryBase
    {
        internal ResultPropertyCollection() { }
        public System.DirectoryServices.ResultPropertyValueCollection this[string name] { get { throw null; } }
        public System.Collections.ICollection PropertyNames { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public bool Contains(string propertyName) { throw null; }
        public void CopyTo(System.DirectoryServices.ResultPropertyValueCollection[] array, int index) { }
    }
    public partial class ResultPropertyValueCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ResultPropertyValueCollection() { }
        public object this[int index] { get { throw null; } }
        public bool Contains(object value) { throw null; }
        public void CopyTo(object[] values, int index) { }
        public int IndexOf(object value) { throw null; }
    }
    public partial class SchemaNameCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal SchemaNameCollection() { }
        public int Count { get { throw null; } }
        public string this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(string value) { throw null; }
        public void AddRange(System.DirectoryServices.SchemaNameCollection value) { }
        public void AddRange(string[] value) { }
        public void Clear() { }
        public bool Contains(string value) { throw null; }
        public void CopyTo(string[] stringArray, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(string value) { throw null; }
        public void Insert(int index, string value) { }
        public void Remove(string value) { }
        public void RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        int System.Collections.IList.Add(object value) { throw null; }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
    }
    public partial class SearchResult
    {
        internal SearchResult() { }
        public string Path { get { throw null; } }
        public System.DirectoryServices.ResultPropertyCollection Properties { get { throw null; } }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
    }
    public partial class SearchResultCollection : System.MarshalByRefObject, System.Collections.ICollection, System.Collections.IEnumerable, System.IDisposable
    {
        internal SearchResultCollection() { }
        public int Count { get { throw null; } }
        public System.IntPtr Handle { get { throw null; } }
        public System.DirectoryServices.SearchResult this[int index] { get { throw null; } }
        public string[] PropertiesLoaded { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public bool Contains(System.DirectoryServices.SearchResult result) { throw null; }
        public void CopyTo(System.DirectoryServices.SearchResult[] results, int index) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~SearchResultCollection() { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.DirectoryServices.SearchResult result) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public enum SearchScope
    {
        Base = 0,
        OneLevel = 1,
        Subtree = 2,
    }
    [System.FlagsAttribute]
    public enum SecurityMasks
    {
        None = 0,
        Owner = 1,
        Group = 2,
        Dacl = 4,
        Sacl = 8,
    }
    public enum SortDirection
    {
        Ascending = 0,
        Descending = 1,
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class SortOption
    {
        public SortOption() { }
        public SortOption(string propertyName, System.DirectoryServices.SortDirection direction) { }
        [System.ComponentModel.DefaultValueAttribute(System.DirectoryServices.SortDirection.Ascending)]
        public System.DirectoryServices.SortDirection Direction { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public string PropertyName { get { throw null; } set { } }
    }
}
namespace System.DirectoryServices.ActiveDirectory
{
    public partial class ActiveDirectoryInterSiteTransport : System.IDisposable
    {
        internal ActiveDirectoryInterSiteTransport() { }
        public bool BridgeAllSiteLinks { get { throw null; } set { } }
        public bool IgnoreReplicationSchedule { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkBridgeCollection SiteLinkBridges { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkCollection SiteLinks { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectoryInterSiteTransport FindByTransportType(System.DirectoryServices.ActiveDirectory.DirectoryContext context, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectoryObjectExistsException : System.Exception
    {
        public ActiveDirectoryObjectExistsException() { }
        protected ActiveDirectoryObjectExistsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ActiveDirectoryObjectExistsException(string message) { }
        public ActiveDirectoryObjectExistsException(string message, System.Exception inner) { }
    }
    public partial class ActiveDirectoryObjectNotFoundException : System.Exception, System.Runtime.Serialization.ISerializable
    {
        public ActiveDirectoryObjectNotFoundException() { }
        protected ActiveDirectoryObjectNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ActiveDirectoryObjectNotFoundException(string message) { }
        public ActiveDirectoryObjectNotFoundException(string message, System.Exception inner) { }
        public ActiveDirectoryObjectNotFoundException(string message, System.Type type, string name) { }
        public string Name { get { throw null; } }
        public System.Type Type { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class ActiveDirectoryOperationException : System.Exception, System.Runtime.Serialization.ISerializable
    {
        public ActiveDirectoryOperationException() { }
        protected ActiveDirectoryOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ActiveDirectoryOperationException(string message) { }
        public ActiveDirectoryOperationException(string message, System.Exception inner) { }
        public ActiveDirectoryOperationException(string message, System.Exception inner, int errorCode) { }
        public ActiveDirectoryOperationException(string message, int errorCode) { }
        public int ErrorCode { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public abstract partial class ActiveDirectoryPartition : System.IDisposable
    {
        protected ActiveDirectoryPartition() { }
        public string Name { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract System.DirectoryServices.DirectoryEntry GetDirectoryEntry();
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectoryReplicationMetadata : System.Collections.DictionaryBase
    {
        internal ActiveDirectoryReplicationMetadata() { }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyStringCollection AttributeNames { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.AttributeMetadata this[string name] { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.AttributeMetadataCollection Values { get { throw null; } }
        public bool Contains(string attributeName) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.AttributeMetadata[] array, int index) { }
    }
    public enum ActiveDirectoryRole
    {
        SchemaRole = 0,
        NamingRole = 1,
        PdcRole = 2,
        RidRole = 3,
        InfrastructureRole = 4,
    }
    public partial class ActiveDirectoryRoleCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ActiveDirectoryRoleCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole[] roles, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { throw null; }
    }
    public partial class ActiveDirectorySchedule
    {
        public ActiveDirectorySchedule() { }
        public ActiveDirectorySchedule(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule) { }
        public bool[,,] RawSchedule { get { throw null; } set { } }
        public void ResetSchedule() { }
        public void SetDailySchedule(System.DirectoryServices.ActiveDirectory.HourOfDay fromHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour fromMinute, System.DirectoryServices.ActiveDirectory.HourOfDay toHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour toMinute) { }
        public void SetSchedule(System.DayOfWeek day, System.DirectoryServices.ActiveDirectory.HourOfDay fromHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour fromMinute, System.DirectoryServices.ActiveDirectory.HourOfDay toHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour toMinute) { }
        public void SetSchedule(System.DayOfWeek[] days, System.DirectoryServices.ActiveDirectory.HourOfDay fromHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour fromMinute, System.DirectoryServices.ActiveDirectory.HourOfDay toHour, System.DirectoryServices.ActiveDirectory.MinuteOfHour toMinute) { }
    }
    public partial class ActiveDirectorySchema : System.DirectoryServices.ActiveDirectory.ActiveDirectoryPartition
    {
        internal ActiveDirectorySchema() { }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer SchemaRoleOwner { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection FindAllClasses() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection FindAllClasses(System.DirectoryServices.ActiveDirectory.SchemaClassType type) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection FindAllDefunctClasses() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllDefunctProperties() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties(System.DirectoryServices.ActiveDirectory.PropertyTypes type) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass FindClass(string ldapDisplayName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass FindDefunctClass(string commonName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty FindDefunctProperty(string commonName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty FindProperty(string ldapDisplayName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema GetCurrentSchema() { throw null; }
        public override System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema GetSchema(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public void RefreshSchema() { }
    }
    public partial class ActiveDirectorySchemaClass : System.IDisposable
    {
        public ActiveDirectorySchemaClass(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection AuxiliaryClasses { get { throw null; } }
        public string CommonName { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectorySecurity DefaultObjectSecurityDescriptor { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
        public bool IsDefunct { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection MandatoryProperties { get { throw null; } }
        public string Name { get { throw null; } }
        public string Oid { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection OptionalProperties { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection PossibleInferiors { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection PossibleSuperiors { get { throw null; } }
        public System.Guid SchemaGuid { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass SubClassOf { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.SchemaClassType Type { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection GetAllProperties() { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectorySchemaClassCollection : System.Collections.CollectionBase
    {
        internal ActiveDirectorySchemaClassCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { throw null; }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClassCollection schemaClasses) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass[] schemaClasses) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaClassCollection schemaClasses) { }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass[] schemaClasses, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { throw null; }
        public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { }
    }
    public partial class ActiveDirectorySchemaProperty : System.IDisposable
    {
        public ActiveDirectorySchemaProperty(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { }
        public string CommonName { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
        public bool IsDefunct { get { throw null; } set { } }
        public bool IsInAnr { get { throw null; } set { } }
        public bool IsIndexed { get { throw null; } set { } }
        public bool IsIndexedOverContainer { get { throw null; } set { } }
        public bool IsInGlobalCatalog { get { throw null; } set { } }
        public bool IsOnTombstonedObject { get { throw null; } set { } }
        public bool IsSingleValued { get { throw null; } set { } }
        public bool IsTupleIndexed { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty Link { get { throw null; } }
        public int? LinkId { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public string Oid { get { throw null; } set { } }
        public int? RangeLower { get { throw null; } set { } }
        public int? RangeUpper { get { throw null; } set { } }
        public System.Guid SchemaGuid { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySyntax Syntax { get { throw null; } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string ldapDisplayName) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectorySchemaPropertyCollection : System.Collections.CollectionBase
    {
        internal ActiveDirectorySchemaPropertyCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { throw null; }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaPropertyCollection properties) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty[] properties) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection properties) { }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty[] properties, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { throw null; }
        public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { }
    }
    public partial class ActiveDirectoryServerDownException : System.Exception, System.Runtime.Serialization.ISerializable
    {
        public ActiveDirectoryServerDownException() { }
        protected ActiveDirectoryServerDownException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ActiveDirectoryServerDownException(string message) { }
        public ActiveDirectoryServerDownException(string message, System.Exception inner) { }
        public ActiveDirectoryServerDownException(string message, System.Exception inner, int errorCode, string name) { }
        public ActiveDirectoryServerDownException(string message, int errorCode, string name) { }
        public int ErrorCode { get { throw null; } }
        public override string Message { get { throw null; } }
        public string Name { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class ActiveDirectorySite : System.IDisposable
    {
        public ActiveDirectorySite(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { }
        public System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection AdjacentSites { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection BridgeheadServers { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainCollection Domains { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer InterSiteTopologyGenerator { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule IntraSiteReplicationSchedule { get { throw null; } set { } }
        public string Location { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteOptions Options { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.DirectoryServerCollection PreferredRpcBridgeheadServers { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DirectoryServerCollection PreferredSmtpBridgeheadServers { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection Servers { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlySiteLinkCollection SiteLinks { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnetCollection Subnets { get { throw null; } }
        public void Delete() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySite FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySite GetComputerSite() { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectorySiteCollection : System.Collections.CollectionBase
    {
        internal ActiveDirectorySiteCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySite this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { throw null; }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteCollection sites) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite[] sites) { }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { throw null; }
        public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { }
    }
    public partial class ActiveDirectorySiteLink : System.IDisposable
    {
        public ActiveDirectorySiteLink(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName) { }
        public ActiveDirectorySiteLink(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
        public ActiveDirectorySiteLink(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule) { }
        public int Cost { get { throw null; } set { } }
        public bool DataCompressionEnabled { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule InterSiteReplicationSchedule { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public bool NotificationEnabled { get { throw null; } set { } }
        public bool ReciprocalReplicationEnabled { get { throw null; } set { } }
        public System.TimeSpan ReplicationInterval { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteCollection Sites { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { throw null; } }
        public void Delete() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteLinkName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectorySiteLinkBridge : System.IDisposable
    {
        public ActiveDirectorySiteLinkBridge(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName) { }
        public ActiveDirectorySiteLinkBridge(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
        public string Name { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkCollection SiteLinks { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { throw null; } }
        public void Delete() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string bridgeName, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectorySiteLinkCollection : System.Collections.CollectionBase
    {
        internal ActiveDirectorySiteLinkCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { throw null; }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkCollection links) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink[] links) { }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { throw null; }
        public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { }
    }
    [System.FlagsAttribute]
    public enum ActiveDirectorySiteOptions
    {
        None = 0,
        AutoTopologyDisabled = 1,
        TopologyCleanupDisabled = 2,
        AutoMinimumHopDisabled = 4,
        StaleServerDetectDisabled = 8,
        AutoInterSiteTopologyDisabled = 16,
        GroupMembershipCachingEnabled = 32,
        ForceKccWindows2003Behavior = 64,
        UseWindows2000IstgElection = 128,
        RandomBridgeHeaderServerSelectionDisabled = 256,
        UseHashingForReplicationSchedule = 512,
        RedundantServerTopologyEnabled = 1024,
    }
    public partial class ActiveDirectorySubnet : System.IDisposable
    {
        public ActiveDirectorySubnet(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string subnetName) { }
        public ActiveDirectorySubnet(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string subnetName, string siteName) { }
        public string Location { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySite Site { get { throw null; } set { } }
        public void Delete() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string subnetName) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ActiveDirectorySubnetCollection : System.Collections.CollectionBase
    {
        internal ActiveDirectorySubnetCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { throw null; }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnetCollection subnets) { }
        public void AddRange(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet[] subnets) { }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { throw null; }
        public void Insert(int index, System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { }
        protected override void OnClear() { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.ActiveDirectory.ActiveDirectorySubnet subnet) { }
    }
    public enum ActiveDirectorySyntax
    {
        CaseExactString = 0,
        CaseIgnoreString = 1,
        NumericString = 2,
        DirectoryString = 3,
        OctetString = 4,
        SecurityDescriptor = 5,
        Int = 6,
        Int64 = 7,
        Bool = 8,
        Oid = 9,
        GeneralizedTime = 10,
        UtcTime = 11,
        DN = 12,
        DNWithBinary = 13,
        DNWithString = 14,
        Enumeration = 15,
        IA5String = 16,
        PrintableString = 17,
        Sid = 18,
        AccessPointDN = 19,
        ORName = 20,
        PresentationAddress = 21,
        ReplicaLink = 22,
    }
    public enum ActiveDirectoryTransportType
    {
        Rpc = 0,
        Smtp = 1,
    }
    public partial class AdamInstance : System.DirectoryServices.ActiveDirectory.DirectoryServer
    {
        internal AdamInstance() { }
        public System.DirectoryServices.ActiveDirectory.ConfigurationSet ConfigurationSet { get { throw null; } }
        public string DefaultPartition { get { throw null; } set { } }
        public string HostName { get { throw null; } }
        public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection InboundConnections { get { throw null; } }
        public override string IPAddress { get { throw null; } }
        public int LdapPort { get { throw null; } }
        public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection OutboundConnections { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.AdamRoleCollection Roles { get { throw null; } }
        public override string SiteName { get { throw null; } }
        public int SslPort { get { throw null; } }
        public override System.DirectoryServices.ActiveDirectory.SyncUpdateCallback SyncFromAllServersCallback { get { throw null; } set { } }
        public override void CheckReplicationConsistency() { }
        protected override void Dispose(bool disposing) { }
        ~AdamInstance() { }
        public static System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string partitionName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.AdamInstance FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string partitionName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.AdamInstance GetAdamInstance(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetAllReplicationNeighbors() { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection GetReplicationConnectionFailures() { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection GetReplicationCursors(string partition) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetReplicationNeighbors(string partition) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation GetReplicationOperationInformation() { throw null; }
        public void Save() { }
        public void SeizeRoleOwnership(System.DirectoryServices.ActiveDirectory.AdamRole role) { }
        public override void SyncReplicaFromAllServers(string partition, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOptions options) { }
        public override void SyncReplicaFromServer(string partition, string sourceServer) { }
        public void TransferRoleOwnership(System.DirectoryServices.ActiveDirectory.AdamRole role) { }
        public override void TriggerSyncReplicaFromNeighbors(string partition) { }
    }
    public partial class AdamInstanceCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal AdamInstanceCollection() { }
        public System.DirectoryServices.ActiveDirectory.AdamInstance this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.AdamInstance adamInstance) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.AdamInstance[] adamInstances, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.AdamInstance adamInstance) { throw null; }
    }
    public enum AdamRole
    {
        SchemaRole = 0,
        NamingRole = 1,
    }
    public partial class AdamRoleCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal AdamRoleCollection() { }
        public System.DirectoryServices.ActiveDirectory.AdamRole this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.AdamRole role) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.AdamRole[] roles, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.AdamRole role) { throw null; }
    }
    public partial class ApplicationPartition : System.DirectoryServices.ActiveDirectory.ActiveDirectoryPartition
    {
        public ApplicationPartition(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string distinguishedName) { }
        public ApplicationPartition(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string distinguishedName, string objectClass) { }
        public System.DirectoryServices.ActiveDirectory.DirectoryServerCollection DirectoryServers { get { throw null; } }
        public string SecurityReferenceDomain { get { throw null; } set { } }
        public void Delete() { }
        protected override void Dispose(bool disposing) { }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDirectoryServers() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDirectoryServers(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServers() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServers(string siteName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ApplicationPartition FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string distinguishedName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer() { throw null; }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer(bool forceRediscovery) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer FindDirectoryServer(string siteName, bool forceRediscovery) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ApplicationPartition GetApplicationPartition(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public override System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
    }
    public partial class ApplicationPartitionCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ApplicationPartitionCollection() { }
        public System.DirectoryServices.ActiveDirectory.ApplicationPartition this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ApplicationPartition applicationPartition) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ApplicationPartition[] applicationPartitions, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ApplicationPartition applicationPartition) { throw null; }
    }
    public partial class AttributeMetadata
    {
        internal AttributeMetadata() { }
        public System.DateTime LastOriginatingChangeTime { get { throw null; } }
        public System.Guid LastOriginatingInvocationId { get { throw null; } }
        public long LocalChangeUsn { get { throw null; } }
        public string Name { get { throw null; } }
        public long OriginatingChangeUsn { get { throw null; } }
        public string OriginatingServer { get { throw null; } }
        public int Version { get { throw null; } }
    }
    public partial class AttributeMetadataCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal AttributeMetadataCollection() { }
        public System.DirectoryServices.ActiveDirectory.AttributeMetadata this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.AttributeMetadata metadata) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.AttributeMetadata[] metadata, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.AttributeMetadata metadata) { throw null; }
    }
    public partial class ConfigurationSet
    {
        internal ConfigurationSet() { }
        public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection AdamInstances { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ApplicationPartitionCollection ApplicationPartitions { get { throw null; } }
        public string Name { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.AdamInstance NamingRoleOwner { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema Schema { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.AdamInstance SchemaRoleOwner { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection Sites { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.DirectoryServices.ActiveDirectory.AdamInstance FindAdamInstance() { throw null; }
        public System.DirectoryServices.ActiveDirectory.AdamInstance FindAdamInstance(string partitionName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.AdamInstance FindAdamInstance(string partitionName, string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAllAdamInstances() { throw null; }
        public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAllAdamInstances(string partitionName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.AdamInstanceCollection FindAllAdamInstances(string partitionName, string siteName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.ConfigurationSet GetConfigurationSet(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReplicationSecurityLevel GetSecurityLevel() { throw null; }
        public void SetSecurityLevel(System.DirectoryServices.ActiveDirectory.ReplicationSecurityLevel securityLevel) { }
        public override string ToString() { throw null; }
    }
    public partial class DirectoryContext
    {
        public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType) { }
        public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType, string name) { }
        public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType, string username, string password) { }
        public DirectoryContext(System.DirectoryServices.ActiveDirectory.DirectoryContextType contextType, string name, string username, string password) { }
        public System.DirectoryServices.ActiveDirectory.DirectoryContextType ContextType { get { throw null; } }
        public string Name { get { throw null; } }
        public string UserName { get { throw null; } }
    }
    public enum DirectoryContextType
    {
        Domain = 0,
        Forest = 1,
        DirectoryServer = 2,
        ConfigurationSet = 3,
        ApplicationPartition = 4,
    }
    public abstract partial class DirectoryServer : System.IDisposable
    {
        protected DirectoryServer() { }
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection InboundConnections { get; }
        public abstract string IPAddress { get; }
        public string Name { get { throw null; } }
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection OutboundConnections { get; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyStringCollection Partitions { get { throw null; } }
        public abstract string SiteName { get; }
        public abstract System.DirectoryServices.ActiveDirectory.SyncUpdateCallback SyncFromAllServersCallback { get; set; }
        public abstract void CheckReplicationConsistency();
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~DirectoryServer() { }
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetAllReplicationNeighbors();
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection GetReplicationConnectionFailures();
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection GetReplicationCursors(string partition);
        public abstract System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath);
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetReplicationNeighbors(string partition);
        public abstract System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation GetReplicationOperationInformation();
        public void MoveToAnotherSite(string siteName) { }
        public abstract void SyncReplicaFromAllServers(string partition, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOptions options);
        public abstract void SyncReplicaFromServer(string partition, string sourceServer);
        public override string ToString() { throw null; }
        public abstract void TriggerSyncReplicaFromNeighbors(string partition);
    }
    public partial class DirectoryServerCollection : System.Collections.CollectionBase
    {
        internal DirectoryServerCollection() { }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { throw null; }
        public void AddRange(System.DirectoryServices.ActiveDirectory.DirectoryServer[] servers) { }
        public bool Contains(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.DirectoryServer[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { throw null; }
        public void Insert(int index, System.DirectoryServices.ActiveDirectory.DirectoryServer server) { }
        protected override void OnClear() { }
        protected override void OnClearComplete() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemoveComplete(int index, object value) { }
        protected override void OnSetComplete(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.DirectoryServices.ActiveDirectory.DirectoryServer server) { }
    }
    public partial class Domain : System.DirectoryServices.ActiveDirectory.ActiveDirectoryPartition
    {
        internal Domain() { }
        public System.DirectoryServices.ActiveDirectory.DomainCollection Children { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainControllerCollection DomainControllers { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainMode DomainMode { get { throw null; } }
        public int DomainModeLevel { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.Forest Forest { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainController InfrastructureRoleOwner { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.Domain Parent { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainController PdcRoleOwner { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainController RidRoleOwner { get { throw null; } }
        public void CreateLocalSideOfTrustRelationship(string targetDomainName, System.DirectoryServices.ActiveDirectory.TrustDirection direction, string trustPassword) { }
        public void CreateTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
        public void DeleteLocalSideOfTrustRelationship(string targetDomainName) { }
        public void DeleteTrustRelationship(System.DirectoryServices.ActiveDirectory.Domain targetDomain) { }
        public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDiscoverableDomainControllers() { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDiscoverableDomainControllers(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDomainControllers() { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAllDomainControllers(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController() { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController(System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.DomainController FindDomainController(string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformationCollection GetAllTrustRelationships() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.Domain GetComputerDomain() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.Domain GetCurrentDomain() { throw null; }
        public override System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.Domain GetDomain(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public bool GetSelectiveAuthenticationStatus(string targetDomainName) { throw null; }
        public bool GetSidFilteringStatus(string targetDomainName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation GetTrustRelationship(string targetDomainName) { throw null; }
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
    public partial class DomainCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal DomainCollection() { }
        public System.DirectoryServices.ActiveDirectory.Domain this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.Domain domain) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.Domain[] domains, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.Domain domain) { throw null; }
    }
    [System.FlagsAttribute]
    public enum DomainCollisionOptions
    {
        None = 0,
        SidDisabledByAdmin = 1,
        SidDisabledByConflict = 2,
        NetBiosNameDisabledByAdmin = 4,
        NetBiosNameDisabledByConflict = 8,
    }
    public partial class DomainController : System.DirectoryServices.ActiveDirectory.DirectoryServer
    {
        protected DomainController() { }
        public System.DateTime CurrentTime { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.Domain Domain { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.Forest Forest { get { throw null; } }
        public long HighestCommittedUsn { get { throw null; } }
        public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection InboundConnections { get { throw null; } }
        public override string IPAddress { get { throw null; } }
        public string OSVersion { get { throw null; } }
        public override System.DirectoryServices.ActiveDirectory.ReplicationConnectionCollection OutboundConnections { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryRoleCollection Roles { get { throw null; } }
        public override string SiteName { get { throw null; } }
        public override System.DirectoryServices.ActiveDirectory.SyncUpdateCallback SyncFromAllServersCallback { get { throw null; } set { } }
        public override void CheckReplicationConsistency() { }
        protected override void Dispose(bool disposing) { }
        public virtual System.DirectoryServices.ActiveDirectory.GlobalCatalog EnableGlobalCatalog() { throw null; }
        ~DomainController() { }
        public static System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.DomainControllerCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { throw null; }
        public static System.DirectoryServices.ActiveDirectory.DomainController FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetAllReplicationNeighbors() { throw null; }
        public virtual System.DirectoryServices.DirectorySearcher GetDirectorySearcher() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.DomainController GetDomainController(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationFailureCollection GetReplicationConnectionFailures() { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationCursorCollection GetReplicationCursors(string partition) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationNeighborCollection GetReplicationNeighbors(string partition) { throw null; }
        public override System.DirectoryServices.ActiveDirectory.ReplicationOperationInformation GetReplicationOperationInformation() { throw null; }
        public virtual bool IsGlobalCatalog() { throw null; }
        public void SeizeRoleOwnership(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { }
        public override void SyncReplicaFromAllServers(string partition, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOptions options) { }
        public override void SyncReplicaFromServer(string partition, string sourceServer) { }
        public void TransferRoleOwnership(System.DirectoryServices.ActiveDirectory.ActiveDirectoryRole role) { }
        public override void TriggerSyncReplicaFromNeighbors(string partition) { }
    }
    public partial class DomainControllerCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal DomainControllerCollection() { }
        public System.DirectoryServices.ActiveDirectory.DomainController this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.DomainController domainController) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.DomainController[] domainControllers, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.DomainController domainController) { throw null; }
    }
    public enum DomainMode
    {
        Unknown = -1,
        Windows2000MixedDomain = 0,
        Windows2000NativeDomain = 1,
        Windows2003InterimDomain = 2,
        Windows2003Domain = 3,
        Windows2008Domain = 4,
        Windows2008R2Domain = 5,
        Windows8Domain = 6,
        Windows2012R2Domain = 7,
    }
    public partial class Forest : System.IDisposable
    {
        internal Forest() { }
        public System.DirectoryServices.ActiveDirectory.ApplicationPartitionCollection ApplicationPartitions { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainCollection Domains { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ForestMode ForestMode { get { throw null; } }
        public int ForestModeLevel { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection GlobalCatalogs { get { throw null; } }
        public string Name { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainController NamingRoleOwner { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.Domain RootDomain { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema Schema { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainController SchemaRoleOwner { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReadOnlySiteCollection Sites { get { throw null; } }
        public void CreateLocalSideOfTrustRelationship(string targetForestName, System.DirectoryServices.ActiveDirectory.TrustDirection direction, string trustPassword) { }
        public void CreateTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
        public void DeleteLocalSideOfTrustRelationship(string targetForestName) { }
        public void DeleteTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest) { }
        public void Dispose() { }
        protected void Dispose(bool disposing) { }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllDiscoverableGlobalCatalogs() { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllDiscoverableGlobalCatalogs(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllGlobalCatalogs() { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAllGlobalCatalogs(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog() { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog(System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog(string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalog FindGlobalCatalog(string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformationCollection GetAllTrustRelationships() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.Forest GetCurrentForest() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.Forest GetForest(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public bool GetSelectiveAuthenticationStatus(string targetForestName) { throw null; }
        public bool GetSidFilteringStatus(string targetForestName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipInformation GetTrustRelationship(string targetForestName) { throw null; }
        public void RaiseForestFunctionality(System.DirectoryServices.ActiveDirectory.ForestMode forestMode) { }
        public void RaiseForestFunctionalityLevel(int forestMode) { }
        public void RepairTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest) { }
        public void SetSelectiveAuthenticationStatus(string targetForestName, bool enable) { }
        public void SetSidFilteringStatus(string targetForestName, bool enable) { }
        public override string ToString() { throw null; }
        public void UpdateLocalSideOfTrustRelationship(string targetForestName, System.DirectoryServices.ActiveDirectory.TrustDirection newTrustDirection, string newTrustPassword) { }
        public void UpdateLocalSideOfTrustRelationship(string targetForestName, string newTrustPassword) { }
        public void UpdateTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest, System.DirectoryServices.ActiveDirectory.TrustDirection newTrustDirection) { }
        public void VerifyOutboundTrustRelationship(string targetForestName) { }
        public void VerifyTrustRelationship(System.DirectoryServices.ActiveDirectory.Forest targetForest, System.DirectoryServices.ActiveDirectory.TrustDirection direction) { }
    }
    public enum ForestMode
    {
        Unknown = -1,
        Windows2000Forest = 0,
        Windows2003InterimForest = 1,
        Windows2003Forest = 2,
        Windows2008Forest = 3,
        Windows2008R2Forest = 4,
        Windows8Forest = 5,
        Windows2012R2Forest = 6,
    }
    public partial class ForestTrustCollisionException : System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException, System.Runtime.Serialization.ISerializable
    {
        public ForestTrustCollisionException() { }
        protected ForestTrustCollisionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ForestTrustCollisionException(string message) { }
        public ForestTrustCollisionException(string message, System.Exception inner) { }
        public ForestTrustCollisionException(string message, System.Exception inner, System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollisionCollection collisions) { }
        public System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollisionCollection Collisions { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public enum ForestTrustCollisionType
    {
        TopLevelName = 0,
        Domain = 1,
        Other = 2,
    }
    public partial class ForestTrustDomainInfoCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ForestTrustDomainInfoCollection() { }
        public System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation information) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ForestTrustDomainInformation information) { throw null; }
    }
    public partial class ForestTrustDomainInformation
    {
        internal ForestTrustDomainInformation() { }
        public string DnsName { get { throw null; } }
        public string DomainSid { get { throw null; } }
        public string NetBiosName { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ForestTrustDomainStatus Status { get { throw null; } set { } }
    }
    public enum ForestTrustDomainStatus
    {
        Enabled = 0,
        SidAdminDisabled = 1,
        SidConflictDisabled = 2,
        NetBiosNameAdminDisabled = 4,
        NetBiosNameConflictDisabled = 8,
    }
    public partial class ForestTrustRelationshipCollision
    {
        internal ForestTrustRelationshipCollision() { }
        public string CollisionRecord { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ForestTrustCollisionType CollisionType { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.DomainCollisionOptions DomainCollisionOption { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.TopLevelNameCollisionOptions TopLevelNameCollisionOption { get { throw null; } }
    }
    public partial class ForestTrustRelationshipCollisionCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ForestTrustRelationshipCollisionCollection() { }
        public System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision collision) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ForestTrustRelationshipCollision collision) { throw null; }
    }
    public partial class ForestTrustRelationshipInformation : System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation
    {
        internal ForestTrustRelationshipInformation() { }
        public System.Collections.Specialized.StringCollection ExcludedTopLevelNames { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.TopLevelNameCollection TopLevelNames { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ForestTrustDomainInfoCollection TrustedDomainInformation { get { throw null; } }
        public void Save() { }
    }
    public partial class GlobalCatalog : System.DirectoryServices.ActiveDirectory.DomainController
    {
        internal GlobalCatalog() { }
        public System.DirectoryServices.ActiveDirectory.DomainController DisableGlobalCatalog() { throw null; }
        public override System.DirectoryServices.ActiveDirectory.GlobalCatalog EnableGlobalCatalog() { throw null; }
        public static new System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public static new System.DirectoryServices.ActiveDirectory.GlobalCatalogCollection FindAll(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { throw null; }
        public System.DirectoryServices.ActiveDirectory.ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties() { throw null; }
        public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName) { throw null; }
        public static new System.DirectoryServices.ActiveDirectory.GlobalCatalog FindOne(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string siteName, System.DirectoryServices.ActiveDirectory.LocatorOptions flag) { throw null; }
        public override System.DirectoryServices.DirectorySearcher GetDirectorySearcher() { throw null; }
        public static System.DirectoryServices.ActiveDirectory.GlobalCatalog GetGlobalCatalog(System.DirectoryServices.ActiveDirectory.DirectoryContext context) { throw null; }
        public override bool IsGlobalCatalog() { throw null; }
    }
    public partial class GlobalCatalogCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal GlobalCatalogCollection() { }
        public System.DirectoryServices.ActiveDirectory.GlobalCatalog this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.GlobalCatalog globalCatalog) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.GlobalCatalog[] globalCatalogs, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.GlobalCatalog globalCatalog) { throw null; }
    }
    public enum HourOfDay
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Eleven = 11,
        Twelve = 12,
        Thirteen = 13,
        Fourteen = 14,
        Fifteen = 15,
        Sixteen = 16,
        Seventeen = 17,
        Eighteen = 18,
        Nineteen = 19,
        Twenty = 20,
        TwentyOne = 21,
        TwentyTwo = 22,
        TwentyThree = 23,
    }
    [System.FlagsAttribute]
    public enum LocatorOptions : long
    {
        ForceRediscovery = (long)1,
        KdcRequired = (long)1024,
        TimeServerRequired = (long)2048,
        WriteableRequired = (long)4096,
        AvoidSelf = (long)16384,
    }
    public enum MinuteOfHour
    {
        Zero = 0,
        Fifteen = 15,
        Thirty = 30,
        FortyFive = 45,
    }
    public enum NotificationStatus
    {
        NoNotification = 0,
        IntraSiteOnly = 1,
        NotificationAlways = 2,
    }
    [System.FlagsAttribute]
    public enum PropertyTypes
    {
        Indexed = 2,
        InGlobalCatalog = 4,
    }
    public partial class ReadOnlyActiveDirectorySchemaClassCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlyActiveDirectorySchemaClassCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass[] classes, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaClass schemaClass) { throw null; }
    }
    public partial class ReadOnlyActiveDirectorySchemaPropertyCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlyActiveDirectorySchemaPropertyCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty[] properties, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySchemaProperty schemaProperty) { throw null; }
    }
    public partial class ReadOnlyDirectoryServerCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlyDirectoryServerCollection() { }
        public System.DirectoryServices.ActiveDirectory.DirectoryServer this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.DirectoryServer directoryServer) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.DirectoryServer[] directoryServers, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.DirectoryServer directoryServer) { throw null; }
    }
    public partial class ReadOnlySiteCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlySiteCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySite this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite[] sites, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySite site) { throw null; }
    }
    public partial class ReadOnlySiteLinkBridgeCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlySiteLinkBridgeCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge bridge) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge[] bridges, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLinkBridge bridge) { throw null; }
    }
    public partial class ReadOnlySiteLinkCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlySiteLinkCollection() { }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink[] links, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ActiveDirectorySiteLink link) { throw null; }
    }
    public partial class ReadOnlyStringCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReadOnlyStringCollection() { }
        public string this[int index] { get { throw null; } }
        public bool Contains(string value) { throw null; }
        public void CopyTo(string[] values, int index) { }
        public int IndexOf(string value) { throw null; }
    }
    public partial class ReplicationConnection : System.IDisposable
    {
        public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer) { }
        public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule) { }
        public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer, System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule schedule, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
        public ReplicationConnection(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name, System.DirectoryServices.ActiveDirectory.DirectoryServer sourceServer, System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType transport) { }
        public System.DirectoryServices.ActiveDirectory.NotificationStatus ChangeNotificationStatus { get { throw null; } set { } }
        public bool DataCompressionEnabled { get { throw null; } set { } }
        public string DestinationServer { get { throw null; } }
        public bool Enabled { get { throw null; } set { } }
        public bool GeneratedByKcc { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public bool ReciprocalReplicationEnabled { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectorySchedule ReplicationSchedule { get { throw null; } set { } }
        public bool ReplicationScheduleOwnedByUser { get { throw null; } set { } }
        public System.DirectoryServices.ActiveDirectory.ReplicationSpan ReplicationSpan { get { throw null; } }
        public string SourceServer { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { throw null; } }
        public void Delete() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~ReplicationConnection() { }
        public static System.DirectoryServices.ActiveDirectory.ReplicationConnection FindByName(System.DirectoryServices.ActiveDirectory.DirectoryContext context, string name) { throw null; }
        public System.DirectoryServices.DirectoryEntry GetDirectoryEntry() { throw null; }
        public void Save() { }
        public override string ToString() { throw null; }
    }
    public partial class ReplicationConnectionCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReplicationConnectionCollection() { }
        public System.DirectoryServices.ActiveDirectory.ReplicationConnection this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationConnection connection) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationConnection[] connections, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationConnection connection) { throw null; }
    }
    public partial class ReplicationCursor
    {
        internal ReplicationCursor() { }
        public System.DateTime LastSuccessfulSyncTime { get { throw null; } }
        public string PartitionName { get { throw null; } }
        public System.Guid SourceInvocationId { get { throw null; } }
        public string SourceServer { get { throw null; } }
        public long UpToDatenessUsn { get { throw null; } }
    }
    public partial class ReplicationCursorCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReplicationCursorCollection() { }
        public System.DirectoryServices.ActiveDirectory.ReplicationCursor this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationCursor cursor) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationCursor[] values, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationCursor cursor) { throw null; }
    }
    public partial class ReplicationFailure
    {
        internal ReplicationFailure() { }
        public int ConsecutiveFailureCount { get { throw null; } }
        public System.DateTime FirstFailureTime { get { throw null; } }
        public int LastErrorCode { get { throw null; } }
        public string LastErrorMessage { get { throw null; } }
        public string SourceServer { get { throw null; } }
    }
    public partial class ReplicationFailureCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReplicationFailureCollection() { }
        public System.DirectoryServices.ActiveDirectory.ReplicationFailure this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationFailure failure) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationFailure[] failures, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationFailure failure) { throw null; }
    }
    public partial class ReplicationNeighbor
    {
        internal ReplicationNeighbor() { }
        public int ConsecutiveFailureCount { get { throw null; } }
        public System.DateTime LastAttemptedSync { get { throw null; } }
        public System.DateTime LastSuccessfulSync { get { throw null; } }
        public string LastSyncMessage { get { throw null; } }
        public int LastSyncResult { get { throw null; } }
        public string PartitionName { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReplicationNeighbor.ReplicationNeighborOptions ReplicationNeighborOption { get { throw null; } }
        public System.Guid SourceInvocationId { get { throw null; } }
        public string SourceServer { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ActiveDirectoryTransportType TransportType { get { throw null; } }
        public long UsnAttributeFilter { get { throw null; } }
        public long UsnLastObjectChangeSynced { get { throw null; } }
        [System.FlagsAttribute]
        public enum ReplicationNeighborOptions : long
        {
            Writeable = (long)16,
            SyncOnStartup = (long)32,
            ScheduledSync = (long)64,
            UseInterSiteTransport = (long)128,
            TwoWaySync = (long)512,
            ReturnObjectParent = (long)2048,
            FullSyncInProgress = (long)65536,
            FullSyncNextPacket = (long)131072,
            NeverSynced = (long)2097152,
            Preempted = (long)16777216,
            IgnoreChangeNotifications = (long)67108864,
            DisableScheduledSync = (long)134217728,
            CompressChanges = (long)268435456,
            NoChangeNotifications = (long)536870912,
            PartialAttributeSet = (long)1073741824,
        }
    }
    public partial class ReplicationNeighborCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReplicationNeighborCollection() { }
        public System.DirectoryServices.ActiveDirectory.ReplicationNeighbor this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor neighbor) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor[] neighbors, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationNeighbor neighbor) { throw null; }
    }
    public partial class ReplicationOperation
    {
        internal ReplicationOperation() { }
        public int OperationNumber { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReplicationOperationType OperationType { get { throw null; } }
        public string PartitionName { get { throw null; } }
        public int Priority { get { throw null; } }
        public string SourceServer { get { throw null; } }
        public System.DateTime TimeEnqueued { get { throw null; } }
    }
    public partial class ReplicationOperationCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ReplicationOperationCollection() { }
        public System.DirectoryServices.ActiveDirectory.ReplicationOperation this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.ReplicationOperation operation) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.ReplicationOperation[] operations, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.ReplicationOperation operation) { throw null; }
    }
    public partial class ReplicationOperationInformation
    {
        public ReplicationOperationInformation() { }
        public System.DirectoryServices.ActiveDirectory.ReplicationOperation CurrentOperation { get { throw null; } }
        public System.DateTime OperationStartTime { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.ReplicationOperationCollection PendingOperations { get { throw null; } }
    }
    public enum ReplicationOperationType
    {
        Sync = 0,
        Add = 1,
        Delete = 2,
        Modify = 3,
        UpdateReference = 4,
    }
    public enum ReplicationSecurityLevel
    {
        NegotiatePassThrough = 0,
        Negotiate = 1,
        MutualAuthentication = 2,
    }
    public enum ReplicationSpan
    {
        IntraSite = 0,
        InterSite = 1,
    }
    public enum SchemaClassType
    {
        Type88 = 0,
        Structural = 1,
        Abstract = 2,
        Auxiliary = 3,
    }
    public enum SyncFromAllServersErrorCategory
    {
        ErrorContactingServer = 0,
        ErrorReplicating = 1,
        ServerUnreachable = 2,
    }
    public partial class SyncFromAllServersErrorInformation
    {
        internal SyncFromAllServersErrorInformation() { }
        public System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorCategory ErrorCategory { get { throw null; } }
        public int ErrorCode { get { throw null; } }
        public string ErrorMessage { get { throw null; } }
        public string SourceServer { get { throw null; } }
        public string TargetServer { get { throw null; } }
    }
    public enum SyncFromAllServersEvent
    {
        Error = 0,
        SyncStarted = 1,
        SyncCompleted = 2,
        Finished = 3,
    }
    public partial class SyncFromAllServersOperationException : System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException, System.Runtime.Serialization.ISerializable
    {
        public SyncFromAllServersOperationException() { }
        protected SyncFromAllServersOperationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SyncFromAllServersOperationException(string message) { }
        public SyncFromAllServersOperationException(string message, System.Exception inner) { }
        public SyncFromAllServersOperationException(string message, System.Exception inner, System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorInformation[] errors) { }
        public System.DirectoryServices.ActiveDirectory.SyncFromAllServersErrorInformation[] ErrorInformation { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    [System.FlagsAttribute]
    public enum SyncFromAllServersOptions
    {
        None = 0,
        AbortIfServerUnavailable = 1,
        SyncAdjacentServerOnly = 2,
        CheckServerAlivenessOnly = 8,
        SkipInitialCheck = 16,
        PushChangeOutward = 32,
        CrossSite = 64,
    }
    public delegate bool SyncUpdateCallback(System.DirectoryServices.ActiveDirectory.SyncFromAllServersEvent eventType, string targetServer, string sourceServer, System.DirectoryServices.ActiveDirectory.SyncFromAllServersOperationException exception);
    public partial class TopLevelName
    {
        internal TopLevelName() { }
        public string Name { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.TopLevelNameStatus Status { get { throw null; } set { } }
    }
    public partial class TopLevelNameCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal TopLevelNameCollection() { }
        public System.DirectoryServices.ActiveDirectory.TopLevelName this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.TopLevelName name) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.TopLevelName[] names, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.TopLevelName name) { throw null; }
    }
    [System.FlagsAttribute]
    public enum TopLevelNameCollisionOptions
    {
        None = 0,
        NewlyCreated = 1,
        DisabledByAdmin = 2,
        DisabledByConflict = 4,
    }
    public enum TopLevelNameStatus
    {
        Enabled = 0,
        NewlyCreated = 1,
        AdminDisabled = 2,
        ConflictDisabled = 4,
    }
    public enum TrustDirection
    {
        Inbound = 1,
        Outbound = 2,
        Bidirectional = 3,
    }
    public partial class TrustRelationshipInformation
    {
        internal TrustRelationshipInformation() { }
        public string SourceName { get { throw null; } }
        public string TargetName { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.TrustDirection TrustDirection { get { throw null; } }
        public System.DirectoryServices.ActiveDirectory.TrustType TrustType { get { throw null; } }
    }
    public partial class TrustRelationshipInformationCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal TrustRelationshipInformationCollection() { }
        public System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation this[int index] { get { throw null; } }
        public bool Contains(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation information) { throw null; }
        public void CopyTo(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation[] array, int index) { }
        public int IndexOf(System.DirectoryServices.ActiveDirectory.TrustRelationshipInformation information) { throw null; }
    }
    public enum TrustType
    {
        TreeRoot = 0,
        ParentChild = 1,
        CrossLink = 2,
        External = 3,
        Forest = 4,
        Kerberos = 5,
        Unknown = 6,
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.AccessControl
{
    [System.FlagsAttribute]
    public enum AccessControlActions
    {
        None = 0,
        View = 1,
        Change = 2,
    }
    public enum AccessControlModification
    {
        Add = 0,
        Set = 1,
        Reset = 2,
        Remove = 3,
        RemoveAll = 4,
        RemoveSpecific = 5,
    }
    [System.FlagsAttribute]
    public enum AccessControlSections
    {
        None = 0,
        Audit = 1,
        Access = 2,
        Owner = 4,
        Group = 8,
        All = 15,
    }
    public enum AccessControlType
    {
        Allow = 0,
        Deny = 1,
    }
    public abstract partial class AccessRule : System.Security.AccessControl.AuthorizationRule
    {
        protected AccessRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags)) { }
        public System.Security.AccessControl.AccessControlType AccessControlType { get { throw null; } }
    }
    public partial class AccessRule<T> : System.Security.AccessControl.AccessRule where T : struct
    {
        public AccessRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public AccessRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public AccessRule(string identity, T rights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public AccessRule(string identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public T Rights { get { throw null; } }
    }
    public sealed partial class AceEnumerator : System.Collections.IEnumerator
    {
        internal AceEnumerator() { }
        public System.Security.AccessControl.GenericAce Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    [System.FlagsAttribute]
    public enum AceFlags : byte
    {
        None = (byte)0,
        ObjectInherit = (byte)1,
        ContainerInherit = (byte)2,
        NoPropagateInherit = (byte)4,
        InheritOnly = (byte)8,
        InheritanceFlags = (byte)15,
        Inherited = (byte)16,
        SuccessfulAccess = (byte)64,
        FailedAccess = (byte)128,
        AuditFlags = (byte)192,
    }
    public enum AceQualifier
    {
        AccessAllowed = 0,
        AccessDenied = 1,
        SystemAudit = 2,
        SystemAlarm = 3,
    }
    public enum AceType : byte
    {
        AccessAllowed = (byte)0,
        AccessDenied = (byte)1,
        SystemAudit = (byte)2,
        SystemAlarm = (byte)3,
        AccessAllowedCompound = (byte)4,
        AccessAllowedObject = (byte)5,
        AccessDeniedObject = (byte)6,
        SystemAuditObject = (byte)7,
        SystemAlarmObject = (byte)8,
        AccessAllowedCallback = (byte)9,
        AccessDeniedCallback = (byte)10,
        AccessAllowedCallbackObject = (byte)11,
        AccessDeniedCallbackObject = (byte)12,
        SystemAuditCallback = (byte)13,
        SystemAlarmCallback = (byte)14,
        SystemAuditCallbackObject = (byte)15,
        MaxDefinedAceType = (byte)16,
        SystemAlarmCallbackObject = (byte)16,
    }
    [System.FlagsAttribute]
    public enum AuditFlags
    {
        None = 0,
        Success = 1,
        Failure = 2,
    }
    public abstract partial class AuditRule : System.Security.AccessControl.AuthorizationRule
    {
        protected AuditRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags auditFlags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags)) { }
        public System.Security.AccessControl.AuditFlags AuditFlags { get { throw null; } }
    }
    public partial class AuditRule<T> : System.Security.AccessControl.AuditRule where T : struct
    {
        public AuditRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public AuditRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public AuditRule(string identity, T rights, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public AuditRule(string identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public T Rights { get { throw null; } }
    }
    public abstract partial class AuthorizationRule
    {
        protected internal AuthorizationRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        protected internal int AccessMask { get { throw null; } }
        public System.Security.Principal.IdentityReference IdentityReference { get { throw null; } }
        public System.Security.AccessControl.InheritanceFlags InheritanceFlags { get { throw null; } }
        public bool IsInherited { get { throw null; } }
        public System.Security.AccessControl.PropagationFlags PropagationFlags { get { throw null; } }
    }
    public sealed partial class AuthorizationRuleCollection : System.Collections.ReadOnlyCollectionBase
    {
        public AuthorizationRuleCollection() { }
        public System.Security.AccessControl.AuthorizationRule this[int index] { get { throw null; } }
        public void AddRule(System.Security.AccessControl.AuthorizationRule rule) { }
        public void CopyTo(System.Security.AccessControl.AuthorizationRule[] rules, int index) { }
    }
    public sealed partial class CommonAce : System.Security.AccessControl.QualifiedAce
    {
        public CommonAce(System.Security.AccessControl.AceFlags flags, System.Security.AccessControl.AceQualifier qualifier, int accessMask, System.Security.Principal.SecurityIdentifier sid, bool isCallback, byte[] opaque) { }
        public override int BinaryLength { get { throw null; } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public static int MaxOpaqueLength(bool isCallback) { throw null; }
    }
    public abstract partial class CommonAcl : System.Security.AccessControl.GenericAcl
    {
        internal CommonAcl() { }
        public sealed override int BinaryLength { get { throw null; } }
        public sealed override int Count { get { throw null; } }
        public bool IsCanonical { get { throw null; } }
        public bool IsContainer { get { throw null; } }
        public bool IsDS { get { throw null; } }
        public sealed override System.Security.AccessControl.GenericAce this[int index] { get { throw null; } set { } }
        public sealed override byte Revision { get { throw null; } }
        public sealed override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public void Purge(System.Security.Principal.SecurityIdentifier sid) { }
        public void RemoveInheritedAces() { }
    }
    public abstract partial class CommonObjectSecurity : System.Security.AccessControl.ObjectSecurity
    {
        protected CommonObjectSecurity(bool isContainer) { }
        protected void AddAccessRule(System.Security.AccessControl.AccessRule rule) { }
        protected void AddAuditRule(System.Security.AccessControl.AuditRule rule) { }
        public System.Security.AccessControl.AuthorizationRuleCollection GetAccessRules(bool includeExplicit, bool includeInherited, System.Type targetType) { throw null; }
        public System.Security.AccessControl.AuthorizationRuleCollection GetAuditRules(bool includeExplicit, bool includeInherited, System.Type targetType) { throw null; }
        protected override bool ModifyAccess(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified) { throw null; }
        protected override bool ModifyAudit(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified) { throw null; }
        protected bool RemoveAccessRule(System.Security.AccessControl.AccessRule rule) { throw null; }
        protected void RemoveAccessRuleAll(System.Security.AccessControl.AccessRule rule) { }
        protected void RemoveAccessRuleSpecific(System.Security.AccessControl.AccessRule rule) { }
        protected bool RemoveAuditRule(System.Security.AccessControl.AuditRule rule) { throw null; }
        protected void RemoveAuditRuleAll(System.Security.AccessControl.AuditRule rule) { }
        protected void RemoveAuditRuleSpecific(System.Security.AccessControl.AuditRule rule) { }
        protected void ResetAccessRule(System.Security.AccessControl.AccessRule rule) { }
        protected void SetAccessRule(System.Security.AccessControl.AccessRule rule) { }
        protected void SetAuditRule(System.Security.AccessControl.AuditRule rule) { }
    }
    public sealed partial class CommonSecurityDescriptor : System.Security.AccessControl.GenericSecurityDescriptor
    {
        public CommonSecurityDescriptor(bool isContainer, bool isDS, byte[] binaryForm, int offset) { }
        public CommonSecurityDescriptor(bool isContainer, bool isDS, System.Security.AccessControl.ControlFlags flags, System.Security.Principal.SecurityIdentifier owner, System.Security.Principal.SecurityIdentifier group, System.Security.AccessControl.SystemAcl systemAcl, System.Security.AccessControl.DiscretionaryAcl discretionaryAcl) { }
        public CommonSecurityDescriptor(bool isContainer, bool isDS, System.Security.AccessControl.RawSecurityDescriptor rawSecurityDescriptor) { }
        public CommonSecurityDescriptor(bool isContainer, bool isDS, string sddlForm) { }
        public override System.Security.AccessControl.ControlFlags ControlFlags { get { throw null; } }
        public System.Security.AccessControl.DiscretionaryAcl DiscretionaryAcl { get { throw null; } set { } }
        public override System.Security.Principal.SecurityIdentifier Group { get { throw null; } set { } }
        public bool IsContainer { get { throw null; } }
        public bool IsDiscretionaryAclCanonical { get { throw null; } }
        public bool IsDS { get { throw null; } }
        public bool IsSystemAclCanonical { get { throw null; } }
        public override System.Security.Principal.SecurityIdentifier Owner { get { throw null; } set { } }
        public System.Security.AccessControl.SystemAcl SystemAcl { get { throw null; } set { } }
        public void AddDiscretionaryAcl(byte revision, int trusted) { }
        public void AddSystemAcl(byte revision, int trusted) { }
        public void PurgeAccessControl(System.Security.Principal.SecurityIdentifier sid) { }
        public void PurgeAudit(System.Security.Principal.SecurityIdentifier sid) { }
        public void SetDiscretionaryAclProtection(bool isProtected, bool preserveInheritance) { }
        public void SetSystemAclProtection(bool isProtected, bool preserveInheritance) { }
    }
    public sealed partial class CompoundAce : System.Security.AccessControl.KnownAce
    {
        public CompoundAce(System.Security.AccessControl.AceFlags flags, int accessMask, System.Security.AccessControl.CompoundAceType compoundAceType, System.Security.Principal.SecurityIdentifier sid) { }
        public override int BinaryLength { get { throw null; } }
        public System.Security.AccessControl.CompoundAceType CompoundAceType { get { throw null; } set { } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
    }
    public enum CompoundAceType
    {
        Impersonation = 1,
    }
    [System.FlagsAttribute]
    public enum ControlFlags
    {
        None = 0,
        OwnerDefaulted = 1,
        GroupDefaulted = 2,
        DiscretionaryAclPresent = 4,
        DiscretionaryAclDefaulted = 8,
        SystemAclPresent = 16,
        SystemAclDefaulted = 32,
        DiscretionaryAclUntrusted = 64,
        ServerSecurity = 128,
        DiscretionaryAclAutoInheritRequired = 256,
        SystemAclAutoInheritRequired = 512,
        DiscretionaryAclAutoInherited = 1024,
        SystemAclAutoInherited = 2048,
        DiscretionaryAclProtected = 4096,
        SystemAclProtected = 8192,
        RMControlValid = 16384,
        SelfRelative = 32768,
    }
    public sealed partial class CustomAce : System.Security.AccessControl.GenericAce
    {
        public static readonly int MaxOpaqueLength;
        public CustomAce(System.Security.AccessControl.AceType type, System.Security.AccessControl.AceFlags flags, byte[] opaque) { }
        public override int BinaryLength { get { throw null; } }
        public int OpaqueLength { get { throw null; } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public byte[] GetOpaque() { throw null; }
        public void SetOpaque(byte[] opaque) { }
    }
    public sealed partial class DiscretionaryAcl : System.Security.AccessControl.CommonAcl
    {
        public DiscretionaryAcl(bool isContainer, bool isDS, byte revision, int capacity) { }
        public DiscretionaryAcl(bool isContainer, bool isDS, int capacity) { }
        public DiscretionaryAcl(bool isContainer, bool isDS, System.Security.AccessControl.RawAcl rawAcl) { }
        public void AddAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void AddAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void AddAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAccessRule rule) { }
        public bool RemoveAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { throw null; }
        public bool RemoveAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { throw null; }
        public bool RemoveAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAccessRule rule) { throw null; }
        public void RemoveAccessSpecific(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void RemoveAccessSpecific(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void RemoveAccessSpecific(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAccessRule rule) { }
        public void SetAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void SetAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void SetAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAccessRule rule) { }
    }
    public abstract partial class GenericAce
    {
        internal GenericAce() { }
        public System.Security.AccessControl.AceFlags AceFlags { get { throw null; } set { } }
        public System.Security.AccessControl.AceType AceType { get { throw null; } }
        public System.Security.AccessControl.AuditFlags AuditFlags { get { throw null; } }
        public abstract int BinaryLength { get; }
        public System.Security.AccessControl.InheritanceFlags InheritanceFlags { get { throw null; } }
        public bool IsInherited { get { throw null; } }
        public System.Security.AccessControl.PropagationFlags PropagationFlags { get { throw null; } }
        public System.Security.AccessControl.GenericAce Copy() { throw null; }
        public static System.Security.AccessControl.GenericAce CreateFromBinaryForm(byte[] binaryForm, int offset) { throw null; }
        public sealed override bool Equals(object o) { throw null; }
        public abstract void GetBinaryForm(byte[] binaryForm, int offset);
        public sealed override int GetHashCode() { throw null; }
        public static bool operator ==(System.Security.AccessControl.GenericAce left, System.Security.AccessControl.GenericAce right) { throw null; }
        public static bool operator !=(System.Security.AccessControl.GenericAce left, System.Security.AccessControl.GenericAce right) { throw null; }
    }
    public abstract partial class GenericAcl : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public static readonly byte AclRevision;
        public static readonly byte AclRevisionDS;
        public static readonly int MaxBinaryLength;
        protected GenericAcl() { }
        public abstract int BinaryLength { get; }
        public abstract int Count { get; }
        public bool IsSynchronized { get { throw null; } }
        public abstract System.Security.AccessControl.GenericAce this[int index] { get; set; }
        public abstract byte Revision { get; }
        public virtual object SyncRoot { get { throw null; } }
        public void CopyTo(System.Security.AccessControl.GenericAce[] array, int index) { }
        public abstract void GetBinaryForm(byte[] binaryForm, int offset);
        public System.Security.AccessControl.AceEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public abstract partial class GenericSecurityDescriptor
    {
        internal GenericSecurityDescriptor() { }
        public int BinaryLength { get { throw null; } }
        public abstract System.Security.AccessControl.ControlFlags ControlFlags { get; }
        public abstract System.Security.Principal.SecurityIdentifier Group { get; set; }
        public abstract System.Security.Principal.SecurityIdentifier Owner { get; set; }
        public static byte Revision { get { throw null; } }
        public void GetBinaryForm(byte[] binaryForm, int offset) { }
        public string GetSddlForm(System.Security.AccessControl.AccessControlSections includeSections) { throw null; }
        public static bool IsSddlConversionSupported() { throw null; }
    }
    [System.FlagsAttribute]
    public enum InheritanceFlags
    {
        None = 0,
        ContainerInherit = 1,
        ObjectInherit = 2,
    }
    public abstract partial class KnownAce : System.Security.AccessControl.GenericAce
    {
        internal KnownAce() { }
        public int AccessMask { get { throw null; } set { } }
        public System.Security.Principal.SecurityIdentifier SecurityIdentifier { get { throw null; } set { } }
    }
    public abstract partial class NativeObjectSecurity : System.Security.AccessControl.CommonObjectSecurity
    {
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType) : base (default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections) : base (default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base (default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base (default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections) : base (default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base (default(bool)) { }
        protected sealed override void Persist(System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected void Persist(System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections, object exceptionContext) { }
        protected sealed override void Persist(string name, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected void Persist(string name, System.Security.AccessControl.AccessControlSections includeSections, object exceptionContext) { }
        protected internal delegate System.Exception ExceptionFromErrorCode(int errorCode, string name, System.Runtime.InteropServices.SafeHandle handle, object context);
    }
    public abstract partial class ObjectAccessRule : System.Security.AccessControl.AccessRule
    {
        protected ObjectAccessRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Guid objectType, System.Guid inheritedObjectType, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public System.Guid InheritedObjectType { get { throw null; } }
        public System.Security.AccessControl.ObjectAceFlags ObjectFlags { get { throw null; } }
        public System.Guid ObjectType { get { throw null; } }
    }
    public sealed partial class ObjectAce : System.Security.AccessControl.QualifiedAce
    {
        public ObjectAce(System.Security.AccessControl.AceFlags aceFlags, System.Security.AccessControl.AceQualifier qualifier, int accessMask, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAceFlags flags, System.Guid type, System.Guid inheritedType, bool isCallback, byte[] opaque) { }
        public override int BinaryLength { get { throw null; } }
        public System.Guid InheritedObjectAceType { get { throw null; } set { } }
        public System.Security.AccessControl.ObjectAceFlags ObjectAceFlags { get { throw null; } set { } }
        public System.Guid ObjectAceType { get { throw null; } set { } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public static int MaxOpaqueLength(bool isCallback) { throw null; }
    }
    [System.FlagsAttribute]
    public enum ObjectAceFlags
    {
        None = 0,
        ObjectAceTypePresent = 1,
        InheritedObjectAceTypePresent = 2,
    }
    public abstract partial class ObjectAuditRule : System.Security.AccessControl.AuditRule
    {
        protected ObjectAuditRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Guid objectType, System.Guid inheritedObjectType, System.Security.AccessControl.AuditFlags auditFlags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public System.Guid InheritedObjectType { get { throw null; } }
        public System.Security.AccessControl.ObjectAceFlags ObjectFlags { get { throw null; } }
        public System.Guid ObjectType { get { throw null; } }
    }
    public abstract partial class ObjectSecurity
    {
        protected ObjectSecurity() { }
        protected ObjectSecurity(bool isContainer, bool isDS) { }
        protected ObjectSecurity(System.Security.AccessControl.CommonSecurityDescriptor securityDescriptor) { }
        public abstract System.Type AccessRightType { get; }
        protected bool AccessRulesModified { get { throw null; } set { } }
        public abstract System.Type AccessRuleType { get; }
        public bool AreAccessRulesCanonical { get { throw null; } }
        public bool AreAccessRulesProtected { get { throw null; } }
        public bool AreAuditRulesCanonical { get { throw null; } }
        public bool AreAuditRulesProtected { get { throw null; } }
        protected bool AuditRulesModified { get { throw null; } set { } }
        public abstract System.Type AuditRuleType { get; }
        protected bool GroupModified { get { throw null; } set { } }
        protected bool IsContainer { get { throw null; } }
        protected bool IsDS { get { throw null; } }
        protected bool OwnerModified { get { throw null; } set { } }
        public abstract System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type);
        public abstract System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags);
        public System.Security.Principal.IdentityReference GetGroup(System.Type targetType) { throw null; }
        public System.Security.Principal.IdentityReference GetOwner(System.Type targetType) { throw null; }
        public byte[] GetSecurityDescriptorBinaryForm() { throw null; }
        public string GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections includeSections) { throw null; }
        public static bool IsSddlConversionSupported() { throw null; }
        protected abstract bool ModifyAccess(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified);
        public virtual bool ModifyAccessRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified) { throw null; }
        protected abstract bool ModifyAudit(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified);
        public virtual bool ModifyAuditRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified) { throw null; }
        protected virtual void Persist(bool enableOwnershipPrivilege, string name, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected virtual void Persist(System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected virtual void Persist(string name, System.Security.AccessControl.AccessControlSections includeSections) { }
        public virtual void PurgeAccessRules(System.Security.Principal.IdentityReference identity) { }
        public virtual void PurgeAuditRules(System.Security.Principal.IdentityReference identity) { }
        protected void ReadLock() { }
        protected void ReadUnlock() { }
        protected System.Security.AccessControl.CommonSecurityDescriptor SecurityDescriptor { get { throw null; } }
        public void SetAccessRuleProtection(bool isProtected, bool preserveInheritance) { }
        public void SetAuditRuleProtection(bool isProtected, bool preserveInheritance) { }
        public void SetGroup(System.Security.Principal.IdentityReference identity) { }
        public void SetOwner(System.Security.Principal.IdentityReference identity) { }
        public void SetSecurityDescriptorBinaryForm(byte[] binaryForm) { }
        public void SetSecurityDescriptorBinaryForm(byte[] binaryForm, System.Security.AccessControl.AccessControlSections includeSections) { }
        public void SetSecurityDescriptorSddlForm(string sddlForm) { }
        public void SetSecurityDescriptorSddlForm(string sddlForm, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected void WriteLock() { }
        protected void WriteUnlock() { }
    }
    public abstract partial class ObjectSecurity<T> : System.Security.AccessControl.NativeObjectSecurity where T : struct
    {
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType) : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle safeHandle, System.Security.AccessControl.AccessControlSections includeSections) : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle safeHandle, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections) : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        public override System.Type AccessRightType { get { throw null; } }
        public override System.Type AccessRuleType { get { throw null; } }
        public override System.Type AuditRuleType { get { throw null; } }
        public override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { throw null; }
        public virtual void AddAccessRule(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void AddAuditRule(System.Security.AccessControl.AuditRule<T> rule) { }
        public override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { throw null; }
        protected internal void Persist(System.Runtime.InteropServices.SafeHandle handle) { }
        protected internal void Persist(string name) { }
        public virtual bool RemoveAccessRule(System.Security.AccessControl.AccessRule<T> rule) { throw null; }
        public virtual void RemoveAccessRuleAll(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void RemoveAccessRuleSpecific(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual bool RemoveAuditRule(System.Security.AccessControl.AuditRule<T> rule) { throw null; }
        public virtual void RemoveAuditRuleAll(System.Security.AccessControl.AuditRule<T> rule) { }
        public virtual void RemoveAuditRuleSpecific(System.Security.AccessControl.AuditRule<T> rule) { }
        public virtual void ResetAccessRule(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void SetAccessRule(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void SetAuditRule(System.Security.AccessControl.AuditRule<T> rule) { }
    }
    public sealed partial class PrivilegeNotHeldException : System.UnauthorizedAccessException, System.Runtime.Serialization.ISerializable
    {
        public PrivilegeNotHeldException() { }
        public PrivilegeNotHeldException(string privilege) { }
        public PrivilegeNotHeldException(string privilege, System.Exception inner) { }
        public string PrivilegeName { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    public enum PropagationFlags
    {
        None = 0,
        NoPropagateInherit = 1,
        InheritOnly = 2,
    }
    public abstract partial class QualifiedAce : System.Security.AccessControl.KnownAce
    {
        internal QualifiedAce() { }
        public System.Security.AccessControl.AceQualifier AceQualifier { get { throw null; } }
        public bool IsCallback { get { throw null; } }
        public int OpaqueLength { get { throw null; } }
        public byte[] GetOpaque() { throw null; }
        public void SetOpaque(byte[] opaque) { }
    }
    public sealed partial class RawAcl : System.Security.AccessControl.GenericAcl
    {
        public RawAcl(byte revision, int capacity) { }
        public RawAcl(byte[] binaryForm, int offset) { }
        public override int BinaryLength { get { throw null; } }
        public override int Count { get { throw null; } }
        public override System.Security.AccessControl.GenericAce this[int index] { get { throw null; } set { } }
        public override byte Revision { get { throw null; } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public void InsertAce(int index, System.Security.AccessControl.GenericAce ace) { }
        public void RemoveAce(int index) { }
    }
    public sealed partial class RawSecurityDescriptor : System.Security.AccessControl.GenericSecurityDescriptor
    {
        public RawSecurityDescriptor(byte[] binaryForm, int offset) { }
        public RawSecurityDescriptor(System.Security.AccessControl.ControlFlags flags, System.Security.Principal.SecurityIdentifier owner, System.Security.Principal.SecurityIdentifier group, System.Security.AccessControl.RawAcl systemAcl, System.Security.AccessControl.RawAcl discretionaryAcl) { }
        public RawSecurityDescriptor(string sddlForm) { }
        public override System.Security.AccessControl.ControlFlags ControlFlags { get { throw null; } }
        public System.Security.AccessControl.RawAcl DiscretionaryAcl { get { throw null; } set { } }
        public override System.Security.Principal.SecurityIdentifier Group { get { throw null; } set { } }
        public override System.Security.Principal.SecurityIdentifier Owner { get { throw null; } set { } }
        public byte ResourceManagerControl { get { throw null; } set { } }
        public System.Security.AccessControl.RawAcl SystemAcl { get { throw null; } set { } }
        public void SetFlags(System.Security.AccessControl.ControlFlags flags) { }
    }
    public enum ResourceType
    {
        Unknown = 0,
        FileObject = 1,
        Service = 2,
        Printer = 3,
        RegistryKey = 4,
        LMShare = 5,
        KernelObject = 6,
        WindowObject = 7,
        DSObject = 8,
        DSObjectAll = 9,
        ProviderDefined = 10,
        WmiGuidObject = 11,
        RegistryWow6432Key = 12,
    }
    [System.FlagsAttribute]
    public enum SecurityInfos
    {
        Owner = 1,
        Group = 2,
        DiscretionaryAcl = 4,
        SystemAcl = 8,
    }
    public sealed partial class SystemAcl : System.Security.AccessControl.CommonAcl
    {
        public SystemAcl(bool isContainer, bool isDS, byte revision, int capacity) { }
        public SystemAcl(bool isContainer, bool isDS, int capacity) { }
        public SystemAcl(bool isContainer, bool isDS, System.Security.AccessControl.RawAcl rawAcl) { }
        public void AddAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void AddAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void AddAudit(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { }
        public bool RemoveAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { throw null; }
        public bool RemoveAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { throw null; }
        public bool RemoveAudit(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { throw null; }
        public void RemoveAuditSpecific(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void RemoveAuditSpecific(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void RemoveAuditSpecific(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { }
        public void SetAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void SetAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void SetAudit(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { }
    }
}

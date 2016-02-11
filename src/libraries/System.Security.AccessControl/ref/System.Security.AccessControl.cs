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
        Change = 2,
        None = 0,
        View = 1,
    }
    public enum AccessControlModification
    {
        Add = 0,
        Remove = 3,
        RemoveAll = 4,
        RemoveSpecific = 5,
        Reset = 2,
        Set = 1,
    }
    [System.FlagsAttribute]
    public enum AccessControlSections
    {
        Access = 2,
        All = 15,
        Audit = 1,
        Group = 8,
        None = 0,
        Owner = 4,
    }
    public enum AccessControlType
    {
        Allow = 0,
        Deny = 1,
    }
    public abstract partial class AccessRule : System.Security.AccessControl.AuthorizationRule
    {
        protected AccessRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags)) { }
        public System.Security.AccessControl.AccessControlType AccessControlType { get { return default(System.Security.AccessControl.AccessControlType); } }
    }
    public partial class AccessRule<T> : System.Security.AccessControl.AccessRule where T : struct
    {
        public AccessRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public AccessRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public AccessRule(string identity, T rights, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public AccessRule(string identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public T Rights { get { return default(T); } }
    }
    public sealed partial class AceEnumerator : System.Collections.IEnumerator
    {
        internal AceEnumerator() { }
        public System.Security.AccessControl.GenericAce Current { get { return default(System.Security.AccessControl.GenericAce); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
    }
    [System.FlagsAttribute]
    public enum AceFlags : byte
    {
        AuditFlags = (byte)192,
        ContainerInherit = (byte)2,
        FailedAccess = (byte)128,
        InheritanceFlags = (byte)15,
        Inherited = (byte)16,
        InheritOnly = (byte)8,
        None = (byte)0,
        NoPropagateInherit = (byte)4,
        ObjectInherit = (byte)1,
        SuccessfulAccess = (byte)64,
    }
    public enum AceQualifier
    {
        AccessAllowed = 0,
        AccessDenied = 1,
        SystemAlarm = 3,
        SystemAudit = 2,
    }
    public enum AceType : byte
    {
        AccessAllowed = (byte)0,
        AccessAllowedCallback = (byte)9,
        AccessAllowedCallbackObject = (byte)11,
        AccessAllowedCompound = (byte)4,
        AccessAllowedObject = (byte)5,
        AccessDenied = (byte)1,
        AccessDeniedCallback = (byte)10,
        AccessDeniedCallbackObject = (byte)12,
        AccessDeniedObject = (byte)6,
        MaxDefinedAceType = (byte)16,
        SystemAlarm = (byte)3,
        SystemAlarmCallback = (byte)14,
        SystemAlarmCallbackObject = (byte)16,
        SystemAlarmObject = (byte)8,
        SystemAudit = (byte)2,
        SystemAuditCallback = (byte)13,
        SystemAuditCallbackObject = (byte)15,
        SystemAuditObject = (byte)7,
    }
    [System.FlagsAttribute]
    public enum AuditFlags
    {
        Failure = 2,
        None = 0,
        Success = 1,
    }
    public abstract partial class AuditRule : System.Security.AccessControl.AuthorizationRule
    {
        protected AuditRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags auditFlags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags)) { }
        public System.Security.AccessControl.AuditFlags AuditFlags { get { return default(System.Security.AccessControl.AuditFlags); } }
    }
    public partial class AuditRule<T> : System.Security.AccessControl.AuditRule where T : struct
    {
        public AuditRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.AuditFlags flags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public AuditRule(System.Security.Principal.IdentityReference identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public AuditRule(string identity, T rights, System.Security.AccessControl.AuditFlags flags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public AuditRule(string identity, T rights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public T Rights { get { return default(T); } }
    }
    public abstract partial class AuthorizationRule
    {
        protected internal AuthorizationRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        protected internal int AccessMask { get { return default(int); } }
        public System.Security.Principal.IdentityReference IdentityReference { get { return default(System.Security.Principal.IdentityReference); } }
        public System.Security.AccessControl.InheritanceFlags InheritanceFlags { get { return default(System.Security.AccessControl.InheritanceFlags); } }
        public bool IsInherited { get { return default(bool); } }
        public System.Security.AccessControl.PropagationFlags PropagationFlags { get { return default(System.Security.AccessControl.PropagationFlags); } }
    }
    public sealed partial class AuthorizationRuleCollection
    {
        public AuthorizationRuleCollection() { }
        public System.Security.AccessControl.AuthorizationRule this[int index] { get { return default(System.Security.AccessControl.AuthorizationRule); } }
        public void AddRule(System.Security.AccessControl.AuthorizationRule rule) { }
        public void CopyTo(System.Security.AccessControl.AuthorizationRule[] rules, int index) { }
    }
    public sealed partial class CommonAce : System.Security.AccessControl.QualifiedAce
    {
        public CommonAce(System.Security.AccessControl.AceFlags flags, System.Security.AccessControl.AceQualifier qualifier, int accessMask, System.Security.Principal.SecurityIdentifier sid, bool isCallback, byte[] opaque) { }
        public override int BinaryLength { get { return default(int); } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public static int MaxOpaqueLength(bool isCallback) { return default(int); }
    }
    public abstract partial class CommonAcl : System.Security.AccessControl.GenericAcl
    {
        internal CommonAcl() { }
        public sealed override int BinaryLength { get { return default(int); } }
        public sealed override int Count { get { return default(int); } }
        public bool IsCanonical { get { return default(bool); } }
        public bool IsContainer { get { return default(bool); } }
        public bool IsDS { get { return default(bool); } }
        public sealed override System.Security.AccessControl.GenericAce this[int index] { get { return default(System.Security.AccessControl.GenericAce); } set { } }
        public sealed override byte Revision { get { return default(byte); } }
        public sealed override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public void Purge(System.Security.Principal.SecurityIdentifier sid) { }
        public void RemoveInheritedAces() { }
    }
    public abstract partial class CommonObjectSecurity : System.Security.AccessControl.ObjectSecurity
    {
        protected CommonObjectSecurity(bool isContainer) { }
        protected void AddAccessRule(System.Security.AccessControl.AccessRule rule) { }
        protected void AddAuditRule(System.Security.AccessControl.AuditRule rule) { }
        public System.Security.AccessControl.AuthorizationRuleCollection GetAccessRules(bool includeExplicit, bool includeInherited, System.Type targetType) { return default(System.Security.AccessControl.AuthorizationRuleCollection); }
        public System.Security.AccessControl.AuthorizationRuleCollection GetAuditRules(bool includeExplicit, bool includeInherited, System.Type targetType) { return default(System.Security.AccessControl.AuthorizationRuleCollection); }
        protected override bool ModifyAccess(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified) { modified = default(bool); return default(bool); }
        protected override bool ModifyAudit(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified) { modified = default(bool); return default(bool); }
        protected bool RemoveAccessRule(System.Security.AccessControl.AccessRule rule) { return default(bool); }
        protected void RemoveAccessRuleAll(System.Security.AccessControl.AccessRule rule) { }
        protected void RemoveAccessRuleSpecific(System.Security.AccessControl.AccessRule rule) { }
        protected bool RemoveAuditRule(System.Security.AccessControl.AuditRule rule) { return default(bool); }
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
        public override System.Security.AccessControl.ControlFlags ControlFlags { get { return default(System.Security.AccessControl.ControlFlags); } }
        public System.Security.AccessControl.DiscretionaryAcl DiscretionaryAcl { get { return default(System.Security.AccessControl.DiscretionaryAcl); } set { } }
        public override System.Security.Principal.SecurityIdentifier Group { get { return default(System.Security.Principal.SecurityIdentifier); } set { } }
        public bool IsContainer { get { return default(bool); } }
        public bool IsDiscretionaryAclCanonical { get { return default(bool); } }
        public bool IsDS { get { return default(bool); } }
        public bool IsSystemAclCanonical { get { return default(bool); } }
        public override System.Security.Principal.SecurityIdentifier Owner { get { return default(System.Security.Principal.SecurityIdentifier); } set { } }
        public System.Security.AccessControl.SystemAcl SystemAcl { get { return default(System.Security.AccessControl.SystemAcl); } set { } }
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
        public override int BinaryLength { get { return default(int); } }
        public System.Security.AccessControl.CompoundAceType CompoundAceType { get { return default(System.Security.AccessControl.CompoundAceType); } set { } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
    }
    public enum CompoundAceType
    {
        Impersonation = 1,
    }
    [System.FlagsAttribute]
    public enum ControlFlags
    {
        DiscretionaryAclAutoInherited = 1024,
        DiscretionaryAclAutoInheritRequired = 256,
        DiscretionaryAclDefaulted = 8,
        DiscretionaryAclPresent = 4,
        DiscretionaryAclProtected = 4096,
        DiscretionaryAclUntrusted = 64,
        GroupDefaulted = 2,
        None = 0,
        OwnerDefaulted = 1,
        RMControlValid = 16384,
        SelfRelative = 32768,
        ServerSecurity = 128,
        SystemAclAutoInherited = 2048,
        SystemAclAutoInheritRequired = 512,
        SystemAclDefaulted = 32,
        SystemAclPresent = 16,
        SystemAclProtected = 8192,
    }
    public sealed partial class CustomAce : System.Security.AccessControl.GenericAce
    {
        public static readonly int MaxOpaqueLength;
        public CustomAce(System.Security.AccessControl.AceType type, System.Security.AccessControl.AceFlags flags, byte[] opaque) { }
        public override int BinaryLength { get { return default(int); } }
        public int OpaqueLength { get { return default(int); } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public byte[] GetOpaque() { return default(byte[]); }
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
        public bool RemoveAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { return default(bool); }
        public bool RemoveAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { return default(bool); }
        public bool RemoveAccess(System.Security.AccessControl.AccessControlType accessType, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAccessRule rule) { return default(bool); }
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
        public System.Security.AccessControl.AceFlags AceFlags { get { return default(System.Security.AccessControl.AceFlags); } set { } }
        public System.Security.AccessControl.AceType AceType { get { return default(System.Security.AccessControl.AceType); } }
        public System.Security.AccessControl.AuditFlags AuditFlags { get { return default(System.Security.AccessControl.AuditFlags); } }
        public abstract int BinaryLength { get; }
        public System.Security.AccessControl.InheritanceFlags InheritanceFlags { get { return default(System.Security.AccessControl.InheritanceFlags); } }
        public bool IsInherited { get { return default(bool); } }
        public System.Security.AccessControl.PropagationFlags PropagationFlags { get { return default(System.Security.AccessControl.PropagationFlags); } }
        public System.Security.AccessControl.GenericAce Copy() { return default(System.Security.AccessControl.GenericAce); }
        public static System.Security.AccessControl.GenericAce CreateFromBinaryForm(byte[] binaryForm, int offset) { return default(System.Security.AccessControl.GenericAce); }
        public sealed override bool Equals(object o) { return default(bool); }
        public abstract void GetBinaryForm(byte[] binaryForm, int offset);
        public sealed override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.AccessControl.GenericAce left, System.Security.AccessControl.GenericAce right) { return default(bool); }
        public static bool operator !=(System.Security.AccessControl.GenericAce left, System.Security.AccessControl.GenericAce right) { return default(bool); }
    }
    public abstract partial class GenericAcl : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public static readonly byte AclRevision;
        public static readonly byte AclRevisionDS;
        public static readonly int MaxBinaryLength;
        protected GenericAcl() { }
        public abstract int BinaryLength { get; }
        public abstract int Count { get; }
        public bool IsSynchronized { get { return default(bool); } }
        public abstract System.Security.AccessControl.GenericAce this[int index] { get; set; }
        public abstract byte Revision { get; }
        public virtual object SyncRoot { get { return default(object); } }
        public void CopyTo(System.Security.AccessControl.GenericAce[] array, int index) { }
        public abstract void GetBinaryForm(byte[] binaryForm, int offset);
        public System.Security.AccessControl.AceEnumerator GetEnumerator() { return default(System.Security.AccessControl.AceEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public abstract partial class GenericSecurityDescriptor
    {
        protected GenericSecurityDescriptor() { }
        public int BinaryLength { get { return default(int); } }
        public abstract System.Security.AccessControl.ControlFlags ControlFlags { get; }
        public abstract System.Security.Principal.SecurityIdentifier Group { get; set; }
        public abstract System.Security.Principal.SecurityIdentifier Owner { get; set; }
        public static byte Revision { get { return default(byte); } }
        public void GetBinaryForm(byte[] binaryForm, int offset) { }
        public string GetSddlForm(System.Security.AccessControl.AccessControlSections includeSections) { return default(string); }
        public static bool IsSddlConversionSupported() { return default(bool); }
    }
    [System.FlagsAttribute]
    public enum InheritanceFlags
    {
        ContainerInherit = 1,
        None = 0,
        ObjectInherit = 2,
    }
    public abstract partial class KnownAce : System.Security.AccessControl.GenericAce
    {
        internal KnownAce() { }
        public int AccessMask { get { return default(int); } set { } }
        public System.Security.Principal.SecurityIdentifier SecurityIdentifier { get { return default(System.Security.Principal.SecurityIdentifier); } set { } }
    }
    public abstract partial class NativeObjectSecurity : System.Security.AccessControl.CommonObjectSecurity
    {
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType) : base(default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections) : base(default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base(default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base(default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections) : base(default(bool)) { }
        protected NativeObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base(default(bool)) { }
        protected sealed override void Persist(System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected void Persist(System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections, object exceptionContext) { }
        protected sealed override void Persist(string name, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected void Persist(string name, System.Security.AccessControl.AccessControlSections includeSections, object exceptionContext) { }
        protected internal delegate System.Exception ExceptionFromErrorCode(int errorCode, string name, System.Runtime.InteropServices.SafeHandle handle, object context);
    }
    public abstract partial class ObjectAccessRule : System.Security.AccessControl.AccessRule
    {
        protected ObjectAccessRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Guid objectType, System.Guid inheritedObjectType, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public System.Guid InheritedObjectType { get { return default(System.Guid); } }
        public System.Security.AccessControl.ObjectAceFlags ObjectFlags { get { return default(System.Security.AccessControl.ObjectAceFlags); } }
        public System.Guid ObjectType { get { return default(System.Guid); } }
    }
    public sealed partial class ObjectAce : System.Security.AccessControl.QualifiedAce
    {
        public ObjectAce(System.Security.AccessControl.AceFlags aceFlags, System.Security.AccessControl.AceQualifier qualifier, int accessMask, System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAceFlags flags, System.Guid type, System.Guid inheritedType, bool isCallback, byte[] opaque) { }
        public override int BinaryLength { get { return default(int); } }
        public System.Guid InheritedObjectAceType { get { return default(System.Guid); } set { } }
        public System.Security.AccessControl.ObjectAceFlags ObjectAceFlags { get { return default(System.Security.AccessControl.ObjectAceFlags); } set { } }
        public System.Guid ObjectAceType { get { return default(System.Guid); } set { } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public static int MaxOpaqueLength(bool isCallback) { return default(int); }
    }
    [System.FlagsAttribute]
    public enum ObjectAceFlags
    {
        InheritedObjectAceTypePresent = 2,
        None = 0,
        ObjectAceTypePresent = 1,
    }
    public abstract partial class ObjectAuditRule : System.Security.AccessControl.AuditRule
    {
        protected ObjectAuditRule(System.Security.Principal.IdentityReference identity, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Guid objectType, System.Guid inheritedObjectType, System.Security.AccessControl.AuditFlags auditFlags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public System.Guid InheritedObjectType { get { return default(System.Guid); } }
        public System.Security.AccessControl.ObjectAceFlags ObjectFlags { get { return default(System.Security.AccessControl.ObjectAceFlags); } }
        public System.Guid ObjectType { get { return default(System.Guid); } }
    }
    public abstract partial class ObjectSecurity
    {
        protected ObjectSecurity() { }
        protected ObjectSecurity(bool isContainer, bool isDS) { }
        protected ObjectSecurity(System.Security.AccessControl.CommonSecurityDescriptor securityDescriptor) { }
        public abstract System.Type AccessRightType { get; }
        protected bool AccessRulesModified { get { return default(bool); } set { } }
        public abstract System.Type AccessRuleType { get; }
        public bool AreAccessRulesCanonical { get { return default(bool); } }
        public bool AreAccessRulesProtected { get { return default(bool); } }
        public bool AreAuditRulesCanonical { get { return default(bool); } }
        public bool AreAuditRulesProtected { get { return default(bool); } }
        protected bool AuditRulesModified { get { return default(bool); } set { } }
        public abstract System.Type AuditRuleType { get; }
        protected bool GroupModified { get { return default(bool); } set { } }
        protected bool IsContainer { get { return default(bool); } }
        protected bool IsDS { get { return default(bool); } }
        protected bool OwnerModified { get { return default(bool); } set { } }
        public abstract System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type);
        public abstract System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags);
        public System.Security.Principal.IdentityReference GetGroup(System.Type targetType) { return default(System.Security.Principal.IdentityReference); }
        public System.Security.Principal.IdentityReference GetOwner(System.Type targetType) { return default(System.Security.Principal.IdentityReference); }
        public byte[] GetSecurityDescriptorBinaryForm() { return default(byte[]); }
        public string GetSecurityDescriptorSddlForm(System.Security.AccessControl.AccessControlSections includeSections) { return default(string); }
        public static bool IsSddlConversionSupported() { return default(bool); }
        protected abstract bool ModifyAccess(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified);
        public virtual bool ModifyAccessRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AccessRule rule, out bool modified) { modified = default(bool); return default(bool); }
        protected abstract bool ModifyAudit(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified);
        public virtual bool ModifyAuditRule(System.Security.AccessControl.AccessControlModification modification, System.Security.AccessControl.AuditRule rule, out bool modified) { modified = default(bool); return default(bool); }
        protected virtual void Persist(bool enableOwnershipPrivilege, string name, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected virtual void Persist(System.Runtime.InteropServices.SafeHandle handle, System.Security.AccessControl.AccessControlSections includeSections) { }
        protected virtual void Persist(string name, System.Security.AccessControl.AccessControlSections includeSections) { }
        public virtual void PurgeAccessRules(System.Security.Principal.IdentityReference identity) { }
        public virtual void PurgeAuditRules(System.Security.Principal.IdentityReference identity) { }
        protected void ReadLock() { }
        protected void ReadUnlock() { }
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
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType) : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle safeHandle, System.Security.AccessControl.AccessControlSections includeSections) : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, System.Runtime.InteropServices.SafeHandle safeHandle, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections) : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        protected ObjectSecurity(bool isContainer, System.Security.AccessControl.ResourceType resourceType, string name, System.Security.AccessControl.AccessControlSections includeSections, System.Security.AccessControl.NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext) : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        public override System.Type AccessRightType { get { return default(System.Type); } }
        public override System.Type AccessRuleType { get { return default(System.Type); } }
        public override System.Type AuditRuleType { get { return default(System.Type); } }
        public override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { return default(System.Security.AccessControl.AccessRule); }
        public virtual void AddAccessRule(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void AddAuditRule(System.Security.AccessControl.AuditRule<T> rule) { }
        public override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { return default(System.Security.AccessControl.AuditRule); }
        protected internal void Persist(System.Runtime.InteropServices.SafeHandle handle) { }
        protected internal void Persist(string name) { }
        public virtual bool RemoveAccessRule(System.Security.AccessControl.AccessRule<T> rule) { return default(bool); }
        public virtual void RemoveAccessRuleAll(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void RemoveAccessRuleSpecific(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual bool RemoveAuditRule(System.Security.AccessControl.AuditRule<T> rule) { return default(bool); }
        public virtual void RemoveAuditRuleAll(System.Security.AccessControl.AuditRule<T> rule) { }
        public virtual void RemoveAuditRuleSpecific(System.Security.AccessControl.AuditRule<T> rule) { }
        public virtual void ResetAccessRule(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void SetAccessRule(System.Security.AccessControl.AccessRule<T> rule) { }
        public virtual void SetAuditRule(System.Security.AccessControl.AuditRule<T> rule) { }
    }
    public sealed partial class PrivilegeNotHeldException : System.UnauthorizedAccessException
    {
        public PrivilegeNotHeldException() { }
        public PrivilegeNotHeldException(string privilege) { }
        public PrivilegeNotHeldException(string privilege, System.Exception inner) { }
        public string PrivilegeName { get { return default(string); } }
    }
    [System.FlagsAttribute]
    public enum PropagationFlags
    {
        InheritOnly = 2,
        None = 0,
        NoPropagateInherit = 1,
    }
    public abstract partial class QualifiedAce : System.Security.AccessControl.KnownAce
    {
        internal QualifiedAce() { }
        public System.Security.AccessControl.AceQualifier AceQualifier { get { return default(System.Security.AccessControl.AceQualifier); } }
        public bool IsCallback { get { return default(bool); } }
        public int OpaqueLength { get { return default(int); } }
        public byte[] GetOpaque() { return default(byte[]); }
        public void SetOpaque(byte[] opaque) { }
    }
    public sealed partial class RawAcl : System.Security.AccessControl.GenericAcl
    {
        public RawAcl(byte revision, int capacity) { }
        public RawAcl(byte[] binaryForm, int offset) { }
        public override int BinaryLength { get { return default(int); } }
        public override int Count { get { return default(int); } }
        public override System.Security.AccessControl.GenericAce this[int index] { get { return default(System.Security.AccessControl.GenericAce); } set { } }
        public override byte Revision { get { return default(byte); } }
        public override void GetBinaryForm(byte[] binaryForm, int offset) { }
        public void InsertAce(int index, System.Security.AccessControl.GenericAce ace) { }
        public void RemoveAce(int index) { }
    }
    public sealed partial class RawSecurityDescriptor : System.Security.AccessControl.GenericSecurityDescriptor
    {
        public RawSecurityDescriptor(byte[] binaryForm, int offset) { }
        public RawSecurityDescriptor(System.Security.AccessControl.ControlFlags flags, System.Security.Principal.SecurityIdentifier owner, System.Security.Principal.SecurityIdentifier group, System.Security.AccessControl.RawAcl systemAcl, System.Security.AccessControl.RawAcl discretionaryAcl) { }
        public RawSecurityDescriptor(string sddlForm) { }
        public override System.Security.AccessControl.ControlFlags ControlFlags { get { return default(System.Security.AccessControl.ControlFlags); } }
        public System.Security.AccessControl.RawAcl DiscretionaryAcl { get { return default(System.Security.AccessControl.RawAcl); } set { } }
        public override System.Security.Principal.SecurityIdentifier Group { get { return default(System.Security.Principal.SecurityIdentifier); } set { } }
        public override System.Security.Principal.SecurityIdentifier Owner { get { return default(System.Security.Principal.SecurityIdentifier); } set { } }
        public byte ResourceManagerControl { get { return default(byte); } set { } }
        public System.Security.AccessControl.RawAcl SystemAcl { get { return default(System.Security.AccessControl.RawAcl); } set { } }
        public void SetFlags(System.Security.AccessControl.ControlFlags flags) { }
    }
    public enum ResourceType
    {
        DSObject = 8,
        DSObjectAll = 9,
        FileObject = 1,
        KernelObject = 6,
        LMShare = 5,
        Printer = 3,
        ProviderDefined = 10,
        RegistryKey = 4,
        RegistryWow6432Key = 12,
        Service = 2,
        Unknown = 0,
        WindowObject = 7,
        WmiGuidObject = 11,
    }
    [System.FlagsAttribute]
    public enum SecurityInfos
    {
        DiscretionaryAcl = 4,
        Group = 2,
        Owner = 1,
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
        public bool RemoveAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { return default(bool); }
        public bool RemoveAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { return default(bool); }
        public bool RemoveAudit(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { return default(bool); }
        public void RemoveAuditSpecific(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void RemoveAuditSpecific(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void RemoveAuditSpecific(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { }
        public void SetAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags) { }
        public void SetAudit(System.Security.AccessControl.AuditFlags auditFlags, System.Security.Principal.SecurityIdentifier sid, int accessMask, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.ObjectAceFlags objectFlags, System.Guid objectType, System.Guid inheritedObjectType) { }
        public void SetAudit(System.Security.Principal.SecurityIdentifier sid, System.Security.AccessControl.ObjectAuditRule rule) { }
    }
}

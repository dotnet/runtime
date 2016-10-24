// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    [Flags]
    public enum NetworkInformationAccess
    {
        None = 0,
        Read = 1,
        Ping = 4
    };
    public sealed class NetworkInformationPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission
    {
        public NetworkInformationPermission(System.Security.Permissions.PermissionState state) { }
        public NetworkInformationPermission(NetworkInformationAccess access) { }
        public NetworkInformationAccess Access { get { throw null; } }
        public void AddPermission(NetworkInformationAccess access) { }
        public bool IsUnrestricted() { throw null; }
        public override System.Security.IPermission Copy() { throw null; }
        public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
        public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
        public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
        public override void FromXml(System.Security.SecurityElement securityElement) { }
        public override System.Security.SecurityElement ToXml() { throw null; }
    }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class NetworkInformationPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public NetworkInformationPermissionAttribute(System.Security.Permissions.SecurityAction action) : base(action) { }
        public string Access { get { throw null; } set { } }
        public override System.Security.IPermission CreatePermission() { throw null; }
    }
}
namespace System.Security {
  public abstract partial class CodeAccessPermission : System.Security.IPermission, System.Security.ISecurityEncodable, System.Security.IStackWalk {
    protected CodeAccessPermission() { }
    public void Assert() { }
    public abstract System.Security.IPermission Copy();
    public void Demand() { }
    [System.ObsoleteAttribute]
    public void Deny() { }
    public override bool Equals(object o) => base.Equals(o);
    public abstract void FromXml(System.Security.SecurityElement elem);
    public override int GetHashCode() => base.GetHashCode();
    public abstract System.Security.IPermission Intersect(System.Security.IPermission target);
    public abstract bool IsSubsetOf(System.Security.IPermission target);
    public void PermitOnly() { }
    public override string ToString() => base.ToString();
    public abstract System.Security.SecurityElement ToXml();
    public virtual System.Security.IPermission Union(System.Security.IPermission other) { throw null; }
  }
  public partial class HostProtectionException : System.SystemException {
    public HostProtectionException() { }
    protected HostProtectionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public HostProtectionException(string message) : base(message) { }
    public HostProtectionException(string message, System.Exception e) : base(message, e) { }
    public HostProtectionException(string message, System.Security.Permissions.HostProtectionResource protectedResources, System.Security.Permissions.HostProtectionResource demandedResources) { }
    public System.Security.Permissions.HostProtectionResource DemandedResources { get { throw null; } }
    public System.Security.Permissions.HostProtectionResource ProtectedResources { get { throw null; } }
    public override string ToString() => base.ToString();
  }
  public partial class HostSecurityManager {
    public HostSecurityManager() { }
    public virtual System.Security.Policy.PolicyLevel DomainPolicy { get { throw null; } }
    public virtual System.Security.HostSecurityManagerOptions Flags { get { throw null; } }
    public virtual System.Security.Policy.ApplicationTrust DetermineApplicationTrust(System.Security.Policy.Evidence applicationEvidence, System.Security.Policy.Evidence activatorEvidence, System.Security.Policy.TrustManagerContext context) { throw null; }
    public virtual System.Security.Policy.Evidence ProvideAppDomainEvidence(System.Security.Policy.Evidence inputEvidence) { throw null; }
    public virtual System.Security.Policy.Evidence ProvideAssemblyEvidence(System.Reflection.Assembly loadedAssembly, System.Security.Policy.Evidence inputEvidence) { throw null; }
    [System.ObsoleteAttribute]
    public virtual System.Security.PermissionSet ResolvePolicy(System.Security.Policy.Evidence evidence) { throw null; }
  }
  [System.FlagsAttribute]
  public enum HostSecurityManagerOptions {
    AllFlags = 31,
    HostAppDomainEvidence = 1,
    HostAssemblyEvidence = 4,
    HostDetermineApplicationTrust = 8,
    HostPolicyLevel = 2,
    HostResolvePolicy = 16,
    None = 0,
  }
  public partial interface IEvidenceFactory {
    System.Security.Policy.Evidence Evidence { get; }
  }
  public partial interface IPermission : System.Security.ISecurityEncodable {
    System.Security.IPermission Copy();
    void Demand();
    System.Security.IPermission Intersect(System.Security.IPermission target);
    bool IsSubsetOf(System.Security.IPermission target);
    System.Security.IPermission Union(System.Security.IPermission target);
  }
  public partial interface ISecurityEncodable {
    void FromXml(System.Security.SecurityElement e);
    System.Security.SecurityElement ToXml();
  }
  public partial interface ISecurityPolicyEncodable {
    void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level);
    System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level);
  }
  public partial interface IStackWalk {
    void Assert();
    void Demand();
    void Deny();
    void PermitOnly();
  }
  public sealed partial class NamedPermissionSet : System.Security.PermissionSet {
    public NamedPermissionSet(System.Security.NamedPermissionSet permSet) : base (default(System.Security.Permissions.PermissionState)) { }
    public NamedPermissionSet(string name) : base (default(System.Security.Permissions.PermissionState)) { }
    public NamedPermissionSet(string name, System.Security.Permissions.PermissionState state) : base (default(System.Security.Permissions.PermissionState)) { }
    public NamedPermissionSet(string name, System.Security.PermissionSet permSet) : base (default(System.Security.Permissions.PermissionState)) { }
    public string Description { get; set; }
    public string Name { get; set; }
    public override System.Security.PermissionSet Copy() { throw null; }
    public System.Security.NamedPermissionSet Copy(string name) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override void FromXml(System.Security.SecurityElement et) { }
    public override int GetHashCode() => base.GetHashCode();
    public override System.Security.SecurityElement ToXml() { throw null; }
  }
  public partial class PermissionSet : System.Collections.ICollection, System.Collections.IEnumerable, System.Runtime.Serialization.IDeserializationCallback, System.Security.ISecurityEncodable, System.Security.IStackWalk {
    public PermissionSet(System.Security.Permissions.PermissionState state) { }
    public PermissionSet(System.Security.PermissionSet permSet) { }
    public virtual int Count { get { throw null; } }
    public virtual bool IsReadOnly { get { throw null; } }
    public virtual bool IsSynchronized { get { throw null; } }
    public virtual object SyncRoot { get { throw null; } }
    public System.Security.IPermission AddPermission(System.Security.IPermission perm) { throw null; }
    public void Assert() { }
    public bool ContainsNonCodeAccessPermissions() { throw null; }
    [System.ObsoleteAttribute]
    public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat) { throw null; }
    public virtual System.Security.PermissionSet Copy() { throw null; }
    public virtual void CopyTo(System.Array array, int index) { }
    public void Demand() { }
    [System.ObsoleteAttribute]
    public void Deny() { }
    public override bool Equals(object o) => base.Equals(o);
    public virtual void FromXml(System.Security.SecurityElement et) { }
    public System.Collections.IEnumerator GetEnumerator() { throw null; }
    public override int GetHashCode() => base.GetHashCode();
    public System.Security.IPermission GetPermission(System.Type permClass) { throw null; }
    public System.Security.PermissionSet Intersect(System.Security.PermissionSet other) { throw null; }
    public bool IsEmpty() { throw null; }
    public bool IsSubsetOf(System.Security.PermissionSet target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public void PermitOnly() { }
    public System.Security.IPermission RemovePermission(System.Type permClass) { throw null; }
    public static void RevertAssert() { }
    public System.Security.IPermission SetPermission(System.Security.IPermission perm) { throw null; }
    void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
    public override string ToString() => base.ToString();
    public virtual System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.PermissionSet Union(System.Security.PermissionSet other) { throw null; }
  }
  public enum PolicyLevelType {
    AppDomain = 3,
    Enterprise = 2,
    Machine = 1,
    User = 0,
  }
  public sealed partial class SecurityContext : System.IDisposable {
    internal SecurityContext() { }
    public static System.Security.SecurityContext Capture() { throw null; }
    public System.Security.SecurityContext CreateCopy() { throw null; }
    public void Dispose() { }
    public static bool IsFlowSuppressed() { throw null; }
    public static bool IsWindowsIdentityFlowSuppressed() { throw null; }
    public static void RestoreFlow() { }
    public static void Run(System.Security.SecurityContext securityContext, System.Threading.ContextCallback callback, object state) { }
  }
  public enum SecurityContextSource {
    CurrentAppDomain = 0,
    CurrentAssembly = 1,
  }
  public sealed partial class SecurityElement {
    public SecurityElement(string tag) { }
    public SecurityElement(string tag, string text) { }
    public System.Collections.Hashtable Attributes { get; set; }
    public System.Collections.ArrayList Children { get; set; }
    public string Tag { get; set; }
    public string Text { get; set; }
    public void AddAttribute(string name, string value) { }
    public void AddChild(System.Security.SecurityElement child) { }
    public string Attribute(string name) { throw null; }
    public System.Security.SecurityElement Copy() { throw null; }
    public bool Equal(System.Security.SecurityElement other) { throw null; }
    public static string Escape(string str) { throw null; }
    public static System.Security.SecurityElement FromString(string xml) { throw null; }
    public static bool IsValidAttributeName(string name) { throw null; }
    public static bool IsValidAttributeValue(string value) { throw null; }
    public static bool IsValidTag(string tag) { throw null; }
    public static bool IsValidText(string text) { throw null; }
    public System.Security.SecurityElement SearchForChildByTag(string tag) { throw null; }
    public string SearchForTextOfTag(string tag) { throw null; }
    public override string ToString() => base.ToString();
  }
  public static partial class SecurityManager {
    [System.ObsoleteAttribute]
    public static bool CheckExecutionRights { get; set; }
    [System.ObsoleteAttribute]
    public static bool SecurityEnabled { get; set; }
    public static void GetZoneAndOrigin(out System.Collections.ArrayList zone, out System.Collections.ArrayList origin) { throw null; }
    [System.ObsoleteAttribute]
    public static bool IsGranted(System.Security.IPermission perm) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Security.Policy.PolicyLevel LoadPolicyLevelFromFile(string path, System.Security.PolicyLevelType type) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Security.Policy.PolicyLevel LoadPolicyLevelFromString(string str, System.Security.PolicyLevelType type) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Collections.IEnumerator PolicyHierarchy() { throw null; }
    [System.ObsoleteAttribute]
    public static System.Security.PermissionSet ResolvePolicy(System.Security.Policy.Evidence evidence) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Security.PermissionSet ResolvePolicy(System.Security.Policy.Evidence evidence, System.Security.PermissionSet reqdPset, System.Security.PermissionSet optPset, System.Security.PermissionSet denyPset, out System.Security.PermissionSet denied) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Security.PermissionSet ResolvePolicy(System.Security.Policy.Evidence[] evidences) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Collections.IEnumerator ResolvePolicyGroups(System.Security.Policy.Evidence evidence) { throw null; }
    [System.ObsoleteAttribute]
    public static System.Security.PermissionSet ResolveSystemPolicy(System.Security.Policy.Evidence evidence) { throw null; }
    [System.ObsoleteAttribute]
    public static void SavePolicy() { }
    [System.ObsoleteAttribute]
    public static void SavePolicyLevel(System.Security.Policy.PolicyLevel level) { }
  }
  public abstract partial class SecurityState {
    protected SecurityState() { }
    public abstract void EnsureState();
    public bool IsStateAvailable() { throw null; }
  }
  public enum SecurityZone {
    Internet = 3,
    Intranet = 1,
    MyComputer = 0,
    NoZone = -1,
    Trusted = 2,
    Untrusted = 4,
  }
  public sealed partial class XmlSyntaxException : System.SystemException {
    public XmlSyntaxException() { }
    public XmlSyntaxException(int lineNumber) { }
    public XmlSyntaxException(int lineNumber, string message) { }
    public XmlSyntaxException(string message) : base(message) { }
    public XmlSyntaxException(string message, System.Exception inner) : base(message, inner) { }
  }
}
namespace System.Security.Permissions {
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public abstract partial class CodeAccessSecurityAttribute : System.Security.Permissions.SecurityAttribute {
    protected CodeAccessSecurityAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
  }
  public sealed partial class EnvironmentPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public EnvironmentPermission(System.Security.Permissions.EnvironmentPermissionAccess flag, string pathList) { }
    public EnvironmentPermission(System.Security.Permissions.PermissionState state) { }
    public void AddPathList(System.Security.Permissions.EnvironmentPermissionAccess flag, string pathList) { }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public string GetPathList(System.Security.Permissions.EnvironmentPermissionAccess flag) { throw null; }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public void SetPathList(System.Security.Permissions.EnvironmentPermissionAccess flag, string pathList) { }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission other) { throw null; }
  }
  [System.FlagsAttribute]
  public enum EnvironmentPermissionAccess {
    AllAccess = 3,
    NoAccess = 0,
    Read = 1,
    Write = 2,
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class EnvironmentPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public EnvironmentPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public string All { get; set; }
    public string Read { get; set; }
    public string Write { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class FileDialogPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public FileDialogPermission(System.Security.Permissions.FileDialogPermissionAccess access) { }
    public FileDialogPermission(System.Security.Permissions.PermissionState state) { }
    public System.Security.Permissions.FileDialogPermissionAccess Access { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.FlagsAttribute]
  public enum FileDialogPermissionAccess {
    None = 0,
    Open = 1,
    OpenSave = 3,
    Save = 2,
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class FileDialogPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public FileDialogPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public bool Open { get; set; }
    public bool Save { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class FileIOPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public FileIOPermission(System.Security.Permissions.FileIOPermissionAccess access, string path) { }
    public FileIOPermission(System.Security.Permissions.FileIOPermissionAccess access, string[] pathList) { }
    public FileIOPermission(System.Security.Permissions.PermissionState state) { }
    public System.Security.Permissions.FileIOPermissionAccess AllFiles { get; set; }
    public System.Security.Permissions.FileIOPermissionAccess AllLocalFiles { get; set; }
    public void AddPathList(System.Security.Permissions.FileIOPermissionAccess access, string path) { }
    public void AddPathList(System.Security.Permissions.FileIOPermissionAccess access, string[] pathList) { }
    public override System.Security.IPermission Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override int GetHashCode() => base.GetHashCode();
    public string[] GetPathList(System.Security.Permissions.FileIOPermissionAccess access) { throw null; }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public void SetPathList(System.Security.Permissions.FileIOPermissionAccess access, string path) { }
    public void SetPathList(System.Security.Permissions.FileIOPermissionAccess access, string[] pathList) { }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission other) { throw null; }
  }
  [System.FlagsAttribute]
  public enum FileIOPermissionAccess {
    AllAccess = 15,
    Append = 4,
    NoAccess = 0,
    PathDiscovery = 8,
    Read = 1,
    Write = 2,
  }
  public sealed partial class GacIdentityPermission : System.Security.CodeAccessPermission {
    public GacIdentityPermission() { }
    public GacIdentityPermission(System.Security.Permissions.PermissionState state) { }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement securityElement) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class GacIdentityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public GacIdentityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)4205, AllowMultiple=true, Inherited=false)]
  public sealed partial class HostProtectionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public HostProtectionAttribute() : base (default(System.Security.Permissions.SecurityAction)) { }
    public HostProtectionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public bool ExternalProcessMgmt { get; set; }
    public bool ExternalThreading { get; set; }
    public bool MayLeakOnAbort { get; set; }
    public System.Security.Permissions.HostProtectionResource Resources { get; set; }
    public bool SecurityInfrastructure { get; set; }
    public bool SelfAffectingProcessMgmt { get; set; }
    public bool SelfAffectingThreading { get; set; }
    public bool SharedState { get; set; }
    public bool Synchronization { get; set; }
    public bool UI { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  [System.FlagsAttribute]
  public enum HostProtectionResource {
    All = 511,
    ExternalProcessMgmt = 4,
    ExternalThreading = 16,
    MayLeakOnAbort = 256,
    None = 0,
    SecurityInfrastructure = 64,
    SelfAffectingProcessMgmt = 8,
    SelfAffectingThreading = 32,
    SharedState = 2,
    Synchronization = 1,
    UI = 128,
  }
  public partial interface IUnrestrictedPermission {
    bool IsUnrestricted();
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class PermissionSetAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public PermissionSetAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public string File { get; set; }
    public string Hex { get; set; }
    public string Name { get; set; }
    public bool UnicodeEncoded { get; set; }
    public string XML { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
    public System.Security.PermissionSet CreatePermissionSet() { throw null; }
  }
  public enum PermissionState {
    None = 0,
    Unrestricted = 1,
  }
  public sealed partial class PrincipalPermission : System.Security.IPermission, System.Security.ISecurityEncodable, System.Security.Permissions.IUnrestrictedPermission {
    public PrincipalPermission(System.Security.Permissions.PermissionState state) { }
    public PrincipalPermission(string name, string role) { }
    public PrincipalPermission(string name, string role, bool isAuthenticated) { }
    public System.Security.IPermission Copy() { throw null; }
    public void Demand() { }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement elem) { }
    public override int GetHashCode() => base.GetHashCode();
    public System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.IPermission Union(System.Security.IPermission other) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)68, AllowMultiple=true, Inherited=false)]
  public sealed partial class PrincipalPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public PrincipalPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public bool Authenticated { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class PublisherIdentityPermission : System.Security.CodeAccessPermission {
    public PublisherIdentityPermission(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { }
    public PublisherIdentityPermission(System.Security.Permissions.PermissionState state) { }
    public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class PublisherIdentityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public PublisherIdentityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public string CertFile { get; set; }
    public string SignedFile { get; set; }
    public string X509Certificate { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class ReflectionPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public ReflectionPermission(System.Security.Permissions.PermissionState state) { }
    public ReflectionPermission(System.Security.Permissions.ReflectionPermissionFlag flag) { }
    public System.Security.Permissions.ReflectionPermissionFlag Flags { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission other) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class ReflectionPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public ReflectionPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public System.Security.Permissions.ReflectionPermissionFlag Flags { get; set; }
    public bool MemberAccess { get; set; }
    [System.ObsoleteAttribute]
    public bool ReflectionEmit { get; set; }
    public bool RestrictedMemberAccess { get; set; }
    [System.ObsoleteAttribute("not enforced in 2.0+")]
    public bool TypeInformation { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  [System.FlagsAttribute]
  public enum ReflectionPermissionFlag {
    [System.ObsoleteAttribute]
    AllFlags = 7,
    MemberAccess = 2,
    NoFlags = 0,
    [System.ObsoleteAttribute]
    ReflectionEmit = 4,
    RestrictedMemberAccess = 8,
    [System.ObsoleteAttribute("not used anymore")]
    TypeInformation = 1,
  }
  public sealed partial class RegistryPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public RegistryPermission(System.Security.Permissions.PermissionState state) { }
    public RegistryPermission(System.Security.Permissions.RegistryPermissionAccess access, System.Security.AccessControl.AccessControlActions control, string pathList) { }
    public RegistryPermission(System.Security.Permissions.RegistryPermissionAccess access, string pathList) { }
    public void AddPathList(System.Security.Permissions.RegistryPermissionAccess access, string pathList) { }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement elem) { }
    public string GetPathList(System.Security.Permissions.RegistryPermissionAccess access) { throw null; }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public void SetPathList(System.Security.Permissions.RegistryPermissionAccess access, string pathList) { }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission other) { throw null; }
  }
  [System.FlagsAttribute]
  public enum RegistryPermissionAccess {
    AllAccess = 7,
    Create = 4,
    NoAccess = 0,
    Read = 1,
    Write = 2,
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class RegistryPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public RegistryPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    [System.ObsoleteAttribute]
    public string All { get; set; }
    public string ChangeAccessControl { get; set; }
    public string Create { get; set; }
    public string Read { get; set; }
    public string ViewAccessControl { get; set; }
    public string ViewAndModify { get; set; }
    public string Write { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public enum SecurityAction {
    Assert = 3,
    Demand = 2,
    [System.ObsoleteAttribute]
    Deny = 4,
    InheritanceDemand = 7,
    LinkDemand = 6,
    PermitOnly = 5,
    [System.ObsoleteAttribute]
    RequestMinimum = 8,
    [System.ObsoleteAttribute]
    RequestOptional = 9,
    [System.ObsoleteAttribute]
    RequestRefuse = 10,
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public abstract partial class SecurityAttribute : System.Attribute {
    protected SecurityAttribute(System.Security.Permissions.SecurityAction action) { }
    public System.Security.Permissions.SecurityAction Action { get; set; }
    public bool Unrestricted { get; set; }
    public abstract System.Security.IPermission CreatePermission();
  }
  public sealed partial class SecurityPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public SecurityPermission(System.Security.Permissions.PermissionState state) { }
    public SecurityPermission(System.Security.Permissions.SecurityPermissionFlag flag) { }
    public System.Security.Permissions.SecurityPermissionFlag Flags { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class SecurityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public SecurityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public bool Assertion { get; set; }
    public bool BindingRedirects { get; set; }
    public bool ControlAppDomain { get; set; }
    public bool ControlDomainPolicy { get; set; }
    public bool ControlEvidence { get; set; }
    public bool ControlPolicy { get; set; }
    public bool ControlPrincipal { get; set; }
    public bool ControlThread { get; set; }
    public bool Execution { get; set; }
    public System.Security.Permissions.SecurityPermissionFlag Flags { get; set; }
    public bool Infrastructure { get; set; }
    public bool RemotingConfiguration { get; set; }
    public bool SerializationFormatter { get; set; }
    public bool SkipVerification { get; set; }
    public bool UnmanagedCode { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  [System.FlagsAttribute]
  public enum SecurityPermissionFlag {
    AllFlags = 16383,
    Assertion = 1,
    BindingRedirects = 8192,
    ControlAppDomain = 1024,
    ControlDomainPolicy = 256,
    ControlEvidence = 32,
    ControlPolicy = 64,
    ControlPrincipal = 512,
    ControlThread = 16,
    Execution = 8,
    Infrastructure = 4096,
    NoFlags = 0,
    RemotingConfiguration = 2048,
    SerializationFormatter = 128,
    SkipVerification = 4,
    UnmanagedCode = 2,
  }
  public sealed partial class SiteIdentityPermission : System.Security.CodeAccessPermission {
    public SiteIdentityPermission(System.Security.Permissions.PermissionState state) { }
    public SiteIdentityPermission(string site) { }
    public string Site { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class SiteIdentityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public SiteIdentityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public string Site { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class StrongNameIdentityPermission : System.Security.CodeAccessPermission {
    public StrongNameIdentityPermission(System.Security.Permissions.PermissionState state) { }
    public StrongNameIdentityPermission(System.Security.Permissions.StrongNamePublicKeyBlob blob, string name, System.Version version) { }
    public string Name { get; set; }
    public System.Security.Permissions.StrongNamePublicKeyBlob PublicKey { get; set; }
    public System.Version Version { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement e) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class StrongNameIdentityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public StrongNameIdentityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public string Name { get; set; }
    public string PublicKey { get; set; }
    public string Version { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class StrongNamePublicKeyBlob {
    public StrongNamePublicKeyBlob(byte[] publicKey) { }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class TypeDescriptorPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public TypeDescriptorPermission(System.Security.Permissions.PermissionState state) { }
    public TypeDescriptorPermission(System.Security.Permissions.TypeDescriptorPermissionFlags flag) { }
    public System.Security.Permissions.TypeDescriptorPermissionFlags Flags { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement securityElement) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.FlagsAttribute]
  public enum TypeDescriptorPermissionFlags {
    NoFlags = 0,
    RestrictedRegistrationAccess = 1,
  }
  public sealed partial class UIPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission {
    public UIPermission(System.Security.Permissions.PermissionState state) { }
    public UIPermission(System.Security.Permissions.UIPermissionClipboard clipboardFlag) { }
    public UIPermission(System.Security.Permissions.UIPermissionWindow windowFlag) { }
    public UIPermission(System.Security.Permissions.UIPermissionWindow windowFlag, System.Security.Permissions.UIPermissionClipboard clipboardFlag) { }
    public System.Security.Permissions.UIPermissionClipboard Clipboard { get; set; }
    public System.Security.Permissions.UIPermissionWindow Window { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public bool IsUnrestricted() { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class UIPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public UIPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public System.Security.Permissions.UIPermissionClipboard Clipboard { get; set; }
    public System.Security.Permissions.UIPermissionWindow Window { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public enum UIPermissionClipboard {
    AllClipboard = 2,
    NoClipboard = 0,
    OwnClipboard = 1,
  }
  public enum UIPermissionWindow {
    AllWindows = 3,
    NoWindows = 0,
    SafeSubWindows = 1,
    SafeTopLevelWindows = 2,
  }
  public sealed partial class UrlIdentityPermission : System.Security.CodeAccessPermission {
    public UrlIdentityPermission(System.Security.Permissions.PermissionState state) { }
    public UrlIdentityPermission(string site) { }
    public string Url { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class UrlIdentityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public UrlIdentityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public string Url { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
  public sealed partial class ZoneIdentityPermission : System.Security.CodeAccessPermission {
    public ZoneIdentityPermission(System.Security.Permissions.PermissionState state) { }
    public ZoneIdentityPermission(System.Security.SecurityZone zone) { }
    public System.Security.SecurityZone SecurityZone { get; set; }
    public override System.Security.IPermission Copy() { throw null; }
    public override void FromXml(System.Security.SecurityElement esd) { }
    public override System.Security.IPermission Intersect(System.Security.IPermission target) { throw null; }
    public override bool IsSubsetOf(System.Security.IPermission target) { throw null; }
    public override System.Security.SecurityElement ToXml() { throw null; }
    public override System.Security.IPermission Union(System.Security.IPermission target) { throw null; }
  }
  [System.AttributeUsageAttribute((System.AttributeTargets)109, AllowMultiple=true, Inherited=false)]
  public sealed partial class ZoneIdentityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute {
    public ZoneIdentityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    public System.Security.SecurityZone Zone { get; set; }
    public override System.Security.IPermission CreatePermission() { throw null; }
  }
}
namespace System.Security.Policy {
  public sealed partial class AllMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public AllMembershipCondition() { }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class ApplicationDirectory : System.Security.Policy.EvidenceBase {
    public ApplicationDirectory(string name) { }
    public string Directory { get { throw null; } }
    public object Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class ApplicationDirectoryMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public ApplicationDirectoryMembershipCondition() { }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class ApplicationTrust : System.Security.Policy.EvidenceBase, System.Security.ISecurityEncodable {
    public ApplicationTrust() { }
    public ApplicationTrust(System.Security.PermissionSet defaultGrantSet, System.Collections.Generic.IEnumerable<System.Security.Policy.StrongName> fullTrustAssemblies) { }
    public System.Security.Policy.PolicyStatement DefaultGrantSet { get; set; }
    public object ExtraInfo { get; set; }
    public System.Collections.Generic.IList<System.Security.Policy.StrongName> FullTrustAssemblies { get { throw null; } }
    public bool IsApplicationTrustedToRun { get; set; }
    public bool Persist { get; set; }
    public void FromXml(System.Security.SecurityElement element) { }
    public System.Security.SecurityElement ToXml() { throw null; }
  }
  public sealed partial class ApplicationTrustCollection : System.Collections.ICollection, System.Collections.IEnumerable {
    internal ApplicationTrustCollection() { }
    public int Count { get { throw null; } }
    public bool IsSynchronized { get { throw null; } }
    public System.Security.Policy.ApplicationTrust this[int index] { get { throw null; } }
    public System.Security.Policy.ApplicationTrust this[string appFullName] { get { throw null; } }
    public object SyncRoot { get { throw null; } }
    public int Add(System.Security.Policy.ApplicationTrust trust) { throw null; }
    public void AddRange(System.Security.Policy.ApplicationTrust[] trusts) { }
    public void AddRange(System.Security.Policy.ApplicationTrustCollection trusts) { }
    public void Clear() { }
    public void CopyTo(System.Security.Policy.ApplicationTrust[] array, int index) { }
    public System.Security.Policy.ApplicationTrustEnumerator GetEnumerator() { throw null; }
    public void Remove(System.Security.Policy.ApplicationTrust trust) { }
    public void RemoveRange(System.Security.Policy.ApplicationTrust[] trusts) { }
    public void RemoveRange(System.Security.Policy.ApplicationTrustCollection trusts) { }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
  }
  public sealed partial class ApplicationTrustEnumerator : System.Collections.IEnumerator {
    internal ApplicationTrustEnumerator() { }
    public System.Security.Policy.ApplicationTrust Current { get { throw null; } }
    object System.Collections.IEnumerator.Current { get { throw null; } }
    public bool MoveNext() { throw null; }
    public void Reset() { }
  }
  public enum ApplicationVersionMatch {
    MatchAllVersions = 1,
    MatchExactVersion = 0,
  }
  public partial class CodeConnectAccess {
    public static readonly string AnyScheme;
    public static readonly int DefaultPort;
    public static readonly int OriginPort;
    public static readonly string OriginScheme;
    public CodeConnectAccess(string allowScheme, int allowPort) { }
    public int Port { get { throw null; } }
    public string Scheme { get { throw null; } }
    public static System.Security.Policy.CodeConnectAccess CreateAnySchemeAccess(int allowPort) { throw null; }
    public static System.Security.Policy.CodeConnectAccess CreateOriginSchemeAccess(int allowPort) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
  }
  public abstract partial class CodeGroup {
    protected CodeGroup(System.Security.Policy.IMembershipCondition membershipCondition, System.Security.Policy.PolicyStatement policy) { }
    public virtual string AttributeString { get { throw null; } }
    public System.Collections.IList Children { get; set; }
    public string Description { get; set; }
    public System.Security.Policy.IMembershipCondition MembershipCondition { get; set; }
    public abstract string MergeLogic { get; }
    public string Name { get; set; }
    public virtual string PermissionSetName { get { throw null; } }
    public System.Security.Policy.PolicyStatement PolicyStatement { get; set; }
    public void AddChild(System.Security.Policy.CodeGroup group) { }
    public abstract System.Security.Policy.CodeGroup Copy();
    protected virtual void CreateXml(System.Security.SecurityElement element, System.Security.Policy.PolicyLevel level) { }
    public override bool Equals(object o) => base.Equals(o);
    public bool Equals(System.Security.Policy.CodeGroup cg, bool compareChildren) { throw null; }
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    protected virtual void ParseXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public void RemoveChild(System.Security.Policy.CodeGroup group) { }
    public abstract System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence);
    public abstract System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence);
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class Evidence : System.Collections.ICollection, System.Collections.IEnumerable {
    public Evidence() { }
    [System.ObsoleteAttribute]
    public Evidence(object[] hostEvidence, object[] assemblyEvidence) { }
    public Evidence(System.Security.Policy.Evidence evidence) { }
    [System.ObsoleteAttribute]
    public int Count { get { throw null; } }
    public bool IsReadOnly { get { throw null; } }
    public bool IsSynchronized { get { throw null; } }
    public bool Locked { get; set; }
    public object SyncRoot { get { throw null; } }
    [System.ObsoleteAttribute]
    public void AddAssembly(object id) { }
    [System.ObsoleteAttribute]
    public void AddHost(object id) { }
    public void Clear() { }
    public System.Security.Policy.Evidence Clone() { throw null; }
    [System.ObsoleteAttribute]
    public void CopyTo(System.Array array, int index) { }
    public System.Collections.IEnumerator GetAssemblyEnumerator() { throw null; }
    [System.ObsoleteAttribute]
    public System.Collections.IEnumerator GetEnumerator() { throw null; }
    public System.Collections.IEnumerator GetHostEnumerator() { throw null; }
    public void Merge(System.Security.Policy.Evidence evidence) { }
    public void RemoveType(System.Type t) { }
  }
  public abstract partial class EvidenceBase {
    protected EvidenceBase() { }
    public virtual System.Security.Policy.EvidenceBase Clone() { throw null; }
  }
  public sealed partial class FileCodeGroup : System.Security.Policy.CodeGroup {
    public FileCodeGroup(System.Security.Policy.IMembershipCondition membershipCondition, System.Security.Permissions.FileIOPermissionAccess access) : base (default(System.Security.Policy.IMembershipCondition), default(System.Security.Policy.PolicyStatement)) { }
    public override string AttributeString { get { throw null; } }
    public override string MergeLogic { get { throw null; } }
    public override string PermissionSetName { get { throw null; } }
    public override System.Security.Policy.CodeGroup Copy() { throw null; }
    protected override void CreateXml(System.Security.SecurityElement element, System.Security.Policy.PolicyLevel level) { }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    protected override void ParseXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { throw null; }
    public override System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { throw null; }
  }
  public sealed partial class FirstMatchCodeGroup : System.Security.Policy.CodeGroup {
    public FirstMatchCodeGroup(System.Security.Policy.IMembershipCondition membershipCondition, System.Security.Policy.PolicyStatement policy) : base (default(System.Security.Policy.IMembershipCondition), default(System.Security.Policy.PolicyStatement)) { }
    public override string MergeLogic { get { throw null; } }
    public override System.Security.Policy.CodeGroup Copy() { throw null; }
    public override System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { throw null; }
    public override System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { throw null; }
  }
  public sealed partial class GacInstalled : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory {
    public GacInstalled() { }
    public object Copy() { throw null; }
    public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class GacMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public GacMembershipCondition() { }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class Hash : System.Security.Policy.EvidenceBase, System.Runtime.Serialization.ISerializable {
    public Hash(System.Reflection.Assembly assembly) { }
    public byte[] MD5 { get { throw null; } }
    public byte[] SHA1 { get { throw null; } }
    public static System.Security.Policy.Hash CreateMD5(byte[] md5) { throw null; }
    public static System.Security.Policy.Hash CreateSHA1(byte[] sha1) { throw null; }
    public byte[] GenerateHash(System.Security.Cryptography.HashAlgorithm hashAlg) { throw null; }
    public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public override string ToString() => base.ToString();
  }
  public sealed partial class HashMembershipCondition : System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable, System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public HashMembershipCondition(System.Security.Cryptography.HashAlgorithm hashAlg, byte[] value) { }
    public System.Security.Cryptography.HashAlgorithm HashAlgorithm { get; set; }
    public byte[] HashValue { get; set; }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
    void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public partial interface IIdentityPermissionFactory {
    System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence);
  }
  public partial interface IMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable {
    bool Check(System.Security.Policy.Evidence evidence);
    System.Security.Policy.IMembershipCondition Copy();
    bool Equals(object obj);
    string ToString();
  }
  public sealed partial class NetCodeGroup : System.Security.Policy.CodeGroup {
    public static readonly string AbsentOriginScheme;
    public static readonly string AnyOtherOriginScheme;
    public NetCodeGroup(System.Security.Policy.IMembershipCondition membershipCondition) : base (default(System.Security.Policy.IMembershipCondition), default(System.Security.Policy.PolicyStatement)) { }
    public override string AttributeString { get { throw null; } }
    public override string MergeLogic { get { throw null; } }
    public override string PermissionSetName { get { throw null; } }
    public void AddConnectAccess(string originScheme, System.Security.Policy.CodeConnectAccess connectAccess) { }
    public override System.Security.Policy.CodeGroup Copy() { throw null; }
    protected override void CreateXml(System.Security.SecurityElement element, System.Security.Policy.PolicyLevel level) { }
    public override bool Equals(object o) => base.Equals(o);
    public System.Collections.DictionaryEntry[] GetConnectAccessRules() { throw null; }
    public override int GetHashCode() => base.GetHashCode();
    protected override void ParseXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public void ResetConnectAccess() { }
    public override System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { throw null; }
    public override System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { throw null; }
  }
  public sealed partial class PermissionRequestEvidence : System.Security.Policy.EvidenceBase {
    public PermissionRequestEvidence(System.Security.PermissionSet request, System.Security.PermissionSet optional, System.Security.PermissionSet denied) { }
    public System.Security.PermissionSet DeniedPermissions { get { throw null; } }
    public System.Security.PermissionSet OptionalPermissions { get { throw null; } }
    public System.Security.PermissionSet RequestedPermissions { get { throw null; } }
    public System.Security.Policy.PermissionRequestEvidence Copy() { throw null; }
    public override string ToString() => base.ToString();
  }
  public partial class PolicyException : System.SystemException {
    public PolicyException() { }
    protected PolicyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    public PolicyException(string message) : base(message) { }
    public PolicyException(string message, System.Exception exception) : base(message, exception) { }
  }
  public sealed partial class PolicyLevel {
    internal PolicyLevel() { }
    [System.ObsoleteAttribute]
    public System.Collections.IList FullTrustAssemblies { get { throw null; } }
    public string Label { get { throw null; } }
    public System.Collections.IList NamedPermissionSets { get { throw null; } }
    public System.Security.Policy.CodeGroup RootCodeGroup { get; set; }
    public string StoreLocation { get { throw null; } }
    public System.Security.PolicyLevelType Type { get { throw null; } }
    [System.ObsoleteAttribute]
    public void AddFullTrustAssembly(System.Security.Policy.StrongName sn) { }
    [System.ObsoleteAttribute]
    public void AddFullTrustAssembly(System.Security.Policy.StrongNameMembershipCondition snMC) { }
    public void AddNamedPermissionSet(System.Security.NamedPermissionSet permSet) { }
    public System.Security.NamedPermissionSet ChangeNamedPermissionSet(string name, System.Security.PermissionSet pSet) { throw null; }
    public static System.Security.Policy.PolicyLevel CreateAppDomainLevel() { throw null; }
    public void FromXml(System.Security.SecurityElement e) { }
    public System.Security.NamedPermissionSet GetNamedPermissionSet(string name) { throw null; }
    public void Recover() { }
    [System.ObsoleteAttribute]
    public void RemoveFullTrustAssembly(System.Security.Policy.StrongName sn) { }
    [System.ObsoleteAttribute]
    public void RemoveFullTrustAssembly(System.Security.Policy.StrongNameMembershipCondition snMC) { }
    public System.Security.NamedPermissionSet RemoveNamedPermissionSet(System.Security.NamedPermissionSet permSet) { throw null; }
    public System.Security.NamedPermissionSet RemoveNamedPermissionSet(string name) { throw null; }
    public void Reset() { }
    public System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.SecurityElement ToXml() { throw null; }
  }
  public sealed partial class PolicyStatement : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable {
    public PolicyStatement(System.Security.PermissionSet permSet) { }
    public PolicyStatement(System.Security.PermissionSet permSet, System.Security.Policy.PolicyStatementAttribute attributes) { }
    public System.Security.Policy.PolicyStatementAttribute Attributes { get; set; }
    public string AttributeString { get { throw null; } }
    public System.Security.PermissionSet PermissionSet { get; set; }
    public System.Security.Policy.PolicyStatement Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement et) { }
    public void FromXml(System.Security.SecurityElement et, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  [System.FlagsAttribute]
  public enum PolicyStatementAttribute {
    All = 3,
    Exclusive = 1,
    LevelFinal = 2,
    Nothing = 0,
  }
  public sealed partial class Publisher : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory {
    public Publisher(System.Security.Cryptography.X509Certificates.X509Certificate cert) { }
    public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get { throw null; } }
    public object Copy() { throw null; }
    public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class PublisherMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public PublisherMembershipCondition(System.Security.Cryptography.X509Certificates.X509Certificate certificate) { }
    public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get; set; }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class Site : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory {
    public Site(string name) { }
    public string Name { get { throw null; } }
    public object Copy() { throw null; }
    public static System.Security.Policy.Site CreateFromUrl(string url) { throw null; }
    public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class SiteMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public SiteMembershipCondition(string site) { }
    public string Site { get; set; }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class StrongName : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory {
    public StrongName(System.Security.Permissions.StrongNamePublicKeyBlob blob, string name, System.Version version) { }
    public string Name { get { throw null; } }
    public System.Security.Permissions.StrongNamePublicKeyBlob PublicKey { get { throw null; } }
    public System.Version Version { get { throw null; } }
    public object Copy() { throw null; }
    public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class StrongNameMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public StrongNameMembershipCondition(System.Security.Permissions.StrongNamePublicKeyBlob blob, string name, System.Version version) { }
    public string Name { get; set; }
    public System.Security.Permissions.StrongNamePublicKeyBlob PublicKey { get; set; }
    public System.Version Version { get; set; }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public partial class TrustManagerContext {
    public TrustManagerContext() { }
    public TrustManagerContext(System.Security.Policy.TrustManagerUIContext uiContext) { }
    public virtual bool IgnorePersistedDecision { get; set; }
    public virtual bool KeepAlive { get; set; }
    public virtual bool NoPrompt { get; set; }
    public virtual bool Persist { get; set; }
    public virtual System.Security.Policy.TrustManagerUIContext UIContext { get; set; }
  }
  public enum TrustManagerUIContext {
    Install = 0,
    Run = 2,
    Upgrade = 1,
  }
  public sealed partial class UnionCodeGroup : System.Security.Policy.CodeGroup {
    public UnionCodeGroup(System.Security.Policy.IMembershipCondition membershipCondition, System.Security.Policy.PolicyStatement policy) : base (default(System.Security.Policy.IMembershipCondition), default(System.Security.Policy.PolicyStatement)) { }
    public override string MergeLogic { get { throw null; } }
    public override System.Security.Policy.CodeGroup Copy() { throw null; }
    public override System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { throw null; }
    public override System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { throw null; }
  }
  public sealed partial class Url : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory {
    public Url(string name) { }
    public string Value { get { throw null; } }
    public object Copy() { throw null; }
    public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class UrlMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public UrlMembershipCondition(string url) { }
    public string Url { get; set; }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
  public sealed partial class Zone : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory {
    public Zone(System.Security.SecurityZone zone) { }
    public System.Security.SecurityZone SecurityZone { get { throw null; } }
    public object Copy() { throw null; }
    public static System.Security.Policy.Zone CreateFromUrl(string url) { throw null; }
    public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
  }
  public sealed partial class ZoneMembershipCondition : System.Security.ISecurityEncodable, System.Security.ISecurityPolicyEncodable, System.Security.Policy.IMembershipCondition {
    public ZoneMembershipCondition(System.Security.SecurityZone zone) { }
    public System.Security.SecurityZone SecurityZone { get; set; }
    public bool Check(System.Security.Policy.Evidence evidence) { throw null; }
    public System.Security.Policy.IMembershipCondition Copy() { throw null; }
    public override bool Equals(object o) => base.Equals(o);
    public void FromXml(System.Security.SecurityElement e) { }
    public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level) { }
    public override int GetHashCode() => base.GetHashCode();
    public override string ToString() => base.ToString();
    public System.Security.SecurityElement ToXml() { throw null; }
    public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level) { throw null; }
  }
}

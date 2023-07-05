// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32
{
    public static partial class Registry
    {
        public static readonly Microsoft.Win32.RegistryKey ClassesRoot;
        public static readonly Microsoft.Win32.RegistryKey CurrentConfig;
        public static readonly Microsoft.Win32.RegistryKey CurrentUser;
        public static readonly Microsoft.Win32.RegistryKey LocalMachine;
        public static readonly Microsoft.Win32.RegistryKey PerformanceData;
        public static readonly Microsoft.Win32.RegistryKey Users;
        public static object? GetValue(string keyName, string? valueName, object? defaultValue) { throw null; }
        public static void SetValue(string keyName, string? valueName, object value) { }
        public static void SetValue(string keyName, string? valueName, object value, Microsoft.Win32.RegistryValueKind valueKind) { }
    }
    public enum RegistryHive
    {
        ClassesRoot = -2147483648,
        CurrentUser = -2147483647,
        LocalMachine = -2147483646,
        Users = -2147483645,
        PerformanceData = -2147483644,
        CurrentConfig = -2147483643,
    }
    public sealed partial class RegistryKey : System.MarshalByRefObject, System.IDisposable
    {
        internal RegistryKey() { }
        public Microsoft.Win32.SafeHandles.SafeRegistryHandle Handle { get { throw null; } }
        public string Name { get { throw null; } }
        public int SubKeyCount { get { throw null; } }
        public int ValueCount { get { throw null; } }
        public Microsoft.Win32.RegistryView View { get { throw null; } }
        public void Close() { }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey) { throw null; }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck) { throw null; }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck, Microsoft.Win32.RegistryOptions registryOptions) { throw null; }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck, Microsoft.Win32.RegistryOptions registryOptions, System.Security.AccessControl.RegistrySecurity? registrySecurity) { throw null; }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck, System.Security.AccessControl.RegistrySecurity? registrySecurity) { throw null; }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, bool writable) { throw null; }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, bool writable, Microsoft.Win32.RegistryOptions options) { throw null; }
        public void DeleteSubKey(string subkey) { }
        public void DeleteSubKey(string subkey, bool throwOnMissingSubKey) { }
        public void DeleteSubKeyTree(string subkey) { }
        public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey) { }
        public void DeleteValue(string name) { }
        public void DeleteValue(string name, bool throwOnMissingValue) { }
        public void Dispose() { }
        public void Flush() { }
        public static Microsoft.Win32.RegistryKey FromHandle(Microsoft.Win32.SafeHandles.SafeRegistryHandle handle) { throw null; }
        public static Microsoft.Win32.RegistryKey FromHandle(Microsoft.Win32.SafeHandles.SafeRegistryHandle handle, Microsoft.Win32.RegistryView view) { throw null; }
        public System.Security.AccessControl.RegistrySecurity GetAccessControl() { throw null; }
        public System.Security.AccessControl.RegistrySecurity GetAccessControl(System.Security.AccessControl.AccessControlSections includeSections) { throw null; }
        public string[] GetSubKeyNames() { throw null; }
        public object? GetValue(string? name) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("defaultValue")]
        public object? GetValue(string? name, object? defaultValue) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("defaultValue")]
        public object? GetValue(string? name, object? defaultValue, Microsoft.Win32.RegistryValueOptions options) { throw null; }
        public Microsoft.Win32.RegistryValueKind GetValueKind(string? name) { throw null; }
        public string[] GetValueNames() { throw null; }
        public static Microsoft.Win32.RegistryKey OpenBaseKey(Microsoft.Win32.RegistryHive hKey, Microsoft.Win32.RegistryView view) { throw null; }
        public static Microsoft.Win32.RegistryKey OpenRemoteBaseKey(Microsoft.Win32.RegistryHive hKey, string machineName) { throw null; }
        public static Microsoft.Win32.RegistryKey OpenRemoteBaseKey(Microsoft.Win32.RegistryHive hKey, string machineName, Microsoft.Win32.RegistryView view) { throw null; }
        public Microsoft.Win32.RegistryKey? OpenSubKey(string name) { throw null; }
        public Microsoft.Win32.RegistryKey? OpenSubKey(string name, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck) { throw null; }
        public Microsoft.Win32.RegistryKey? OpenSubKey(string name, Microsoft.Win32.RegistryKeyPermissionCheck permissionCheck, System.Security.AccessControl.RegistryRights rights) { throw null; }
        public Microsoft.Win32.RegistryKey? OpenSubKey(string name, bool writable) { throw null; }
        public Microsoft.Win32.RegistryKey? OpenSubKey(string name, System.Security.AccessControl.RegistryRights rights) { throw null; }
        public void SetAccessControl(System.Security.AccessControl.RegistrySecurity registrySecurity) { }
        public void SetValue(string? name, object value) { }
        public void SetValue(string? name, object value, Microsoft.Win32.RegistryValueKind valueKind) { }
        public override string ToString() { throw null; }
    }
    public enum RegistryKeyPermissionCheck
    {
        Default = 0,
        ReadSubTree = 1,
        ReadWriteSubTree = 2,
    }
    [System.FlagsAttribute]
    public enum RegistryOptions
    {
        None = 0,
        Volatile = 1,
    }
    public enum RegistryValueKind
    {
        None = -1,
        Unknown = 0,
        String = 1,
        ExpandString = 2,
        Binary = 3,
        DWord = 4,
        MultiString = 7,
        QWord = 11,
    }
    [System.FlagsAttribute]
    public enum RegistryValueOptions
    {
        None = 0,
        DoNotExpandEnvironmentNames = 1,
    }
    public enum RegistryView
    {
        Default = 0,
        Registry64 = 256,
        Registry32 = 512,
    }
}
namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeRegistryHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeRegistryHandle() : base (default(bool)) {}
        public SafeRegistryHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.Security.AccessControl
{
    public sealed partial class RegistryAccessRule : System.Security.AccessControl.AccessRule
    {
        public RegistryAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public RegistryAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public RegistryAccessRule(string identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public RegistryAccessRule(string identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public System.Security.AccessControl.RegistryRights RegistryRights { get { throw null; } }
    }
    public sealed partial class RegistryAuditRule : System.Security.AccessControl.AuditRule
    {
        public RegistryAuditRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public RegistryAuditRule(string identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public System.Security.AccessControl.RegistryRights RegistryRights { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum RegistryRights
    {
        QueryValues = 1,
        SetValue = 2,
        CreateSubKey = 4,
        EnumerateSubKeys = 8,
        Notify = 16,
        CreateLink = 32,
        Delete = 65536,
        ReadPermissions = 131072,
        WriteKey = 131078,
        ExecuteKey = 131097,
        ReadKey = 131097,
        ChangePermissions = 262144,
        TakeOwnership = 524288,
        FullControl = 983103,
    }
    public sealed partial class RegistrySecurity : System.Security.AccessControl.NativeObjectSecurity
    {
        public RegistrySecurity() : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        public override System.Type AccessRightType { get { throw null; } }
        public override System.Type AccessRuleType { get { throw null; } }
        public override System.Type AuditRuleType { get { throw null; } }
        public override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { throw null; }
        public void AddAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void AddAuditRule(System.Security.AccessControl.RegistryAuditRule rule) { }
        public override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { throw null; }
        public bool RemoveAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { throw null; }
        public void RemoveAccessRuleAll(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void RemoveAccessRuleSpecific(System.Security.AccessControl.RegistryAccessRule rule) { }
        public bool RemoveAuditRule(System.Security.AccessControl.RegistryAuditRule rule) { throw null; }
        public void RemoveAuditRuleAll(System.Security.AccessControl.RegistryAuditRule rule) { }
        public void RemoveAuditRuleSpecific(System.Security.AccessControl.RegistryAuditRule rule) { }
        public void ResetAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void SetAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void SetAuditRule(System.Security.AccessControl.RegistryAuditRule rule) { }
    }
}

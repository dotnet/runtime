// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32
{
    [System.Security.SecurityCriticalAttribute]
    public static partial class RegistryAclExtensions
    {
        public static System.Security.AccessControl.RegistrySecurity GetAccessControl(this Microsoft.Win32.RegistryKey key) { return default(System.Security.AccessControl.RegistrySecurity); }
        public static System.Security.AccessControl.RegistrySecurity GetAccessControl(this Microsoft.Win32.RegistryKey key, System.Security.AccessControl.AccessControlSections includeSections) { return default(System.Security.AccessControl.RegistrySecurity); }
        public static void SetAccessControl(this Microsoft.Win32.RegistryKey key, System.Security.AccessControl.RegistrySecurity registrySecurity) { }
    }
}
namespace System.Security.AccessControl
{
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class RegistryAccessRule : System.Security.AccessControl.AccessRule
    {
        public RegistryAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public RegistryAccessRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public RegistryAccessRule(string identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public RegistryAccessRule(string identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public System.Security.AccessControl.RegistryRights RegistryRights { get { return default(System.Security.AccessControl.RegistryRights); } }
    }
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class RegistryAuditRule : System.Security.AccessControl.AuditRule
    {
        public RegistryAuditRule(System.Security.Principal.IdentityReference identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public RegistryAuditRule(string identity, System.Security.AccessControl.RegistryRights registryRights, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) : base(default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public System.Security.AccessControl.RegistryRights RegistryRights { get { return default(System.Security.AccessControl.RegistryRights); } }
    }
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class RegistrySecurity : System.Security.AccessControl.NativeObjectSecurity
    {
        public RegistrySecurity() : base(default(bool), default(System.Security.AccessControl.ResourceType)) { }
        public override System.Type AccessRightType { get { return default(System.Type); } }
        public override System.Type AccessRuleType { get { return default(System.Type); } }
        public override System.Type AuditRuleType { get { return default(System.Type); } }
        public override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { return default(System.Security.AccessControl.AccessRule); }
        public void AddAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void AddAuditRule(System.Security.AccessControl.RegistryAuditRule rule) { }
        public override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { return default(System.Security.AccessControl.AuditRule); }
        public bool RemoveAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { return default(bool); }
        public void RemoveAccessRuleAll(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void RemoveAccessRuleSpecific(System.Security.AccessControl.RegistryAccessRule rule) { }
        public bool RemoveAuditRule(System.Security.AccessControl.RegistryAuditRule rule) { return default(bool); }
        public void RemoveAuditRuleAll(System.Security.AccessControl.RegistryAuditRule rule) { }
        public void RemoveAuditRuleSpecific(System.Security.AccessControl.RegistryAuditRule rule) { }
        public void ResetAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void SetAccessRule(System.Security.AccessControl.RegistryAccessRule rule) { }
        public void SetAuditRule(System.Security.AccessControl.RegistryAuditRule rule) { }
    }
}

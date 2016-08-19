// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class PolicyLevel
    {
        internal PolicyLevel() { }
        [System.ObsoleteAttribute("All GACed assemblies are now fully trusted and all permissions now succeed on fully trusted code.")]
        public System.Collections.IList FullTrustAssemblies { get { return default(System.Collections.IList); } }
        public string Label { get { return default(string); } }
        public System.Collections.IList NamedPermissionSets { get { return default(System.Collections.IList); } }
        public System.Security.Policy.CodeGroup RootCodeGroup { get; set; }
        public string StoreLocation { get { return default(string); } }
        public System.Security.PolicyLevelType Type { get { return default(System.Security.PolicyLevelType); } }
        [System.ObsoleteAttribute("All GACed assemblies are now fully trusted and all permissions now succeed on fully trusted code.")]
        public void AddFullTrustAssembly(System.Security.Policy.StrongName sn) { }
        [System.ObsoleteAttribute("All GACed assemblies are now fully trusted and all permissions now succeed on fully trusted code.")]
        public void AddFullTrustAssembly(System.Security.Policy.StrongNameMembershipCondition snMC) { }
        public void AddNamedPermissionSet(System.Security.NamedPermissionSet permSet) { }
        public System.Security.NamedPermissionSet ChangeNamedPermissionSet(string name, System.Security.PermissionSet pSet) { return default(System.Security.NamedPermissionSet); }
        public static System.Security.Policy.PolicyLevel CreateAppDomainLevel() { return default(System.Security.Policy.PolicyLevel); }
        public System.Security.NamedPermissionSet GetNamedPermissionSet(string name) { return default(System.Security.NamedPermissionSet); }
        public void Recover() { }
        [System.ObsoleteAttribute("All GACed assemblies are now fully trusted and all permissions now succeed on fully trusted code.")]
        public void RemoveFullTrustAssembly(System.Security.Policy.StrongName sn) { }
        [System.ObsoleteAttribute("All GACed assemblies are now fully trusted and all permissions now succeed on fully trusted code.")]
        public void RemoveFullTrustAssembly(System.Security.Policy.StrongNameMembershipCondition snMC) { }
        public System.Security.NamedPermissionSet RemoveNamedPermissionSet(System.Security.NamedPermissionSet permSet) { return default(System.Security.NamedPermissionSet); }
        public System.Security.NamedPermissionSet RemoveNamedPermissionSet(string name) { return default(System.Security.NamedPermissionSet); }
        public void Reset() { }
        public System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { return default(System.Security.Policy.PolicyStatement); }
        public System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { return default(System.Security.Policy.CodeGroup); }
    }
}

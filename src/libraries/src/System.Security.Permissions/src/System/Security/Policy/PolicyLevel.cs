// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Security.Policy
{
    public sealed partial class PolicyLevel
    {
        internal PolicyLevel() { }
        [Obsolete("Because all GAC assemblies always get full trust, the full trust list is no longer meaningful. You should install any assemblies that are used in security policy in the GAC to ensure they are trusted.")]
        public IList FullTrustAssemblies { get { return default(IList); } }
        public string Label { get { return null; } }
        public IList NamedPermissionSets { get { return default(IList); } }
        public CodeGroup RootCodeGroup { get; set; }
        public string StoreLocation { get { return null; } }
        public PolicyLevelType Type { get { return default(PolicyLevelType); } }
        [Obsolete("Because all GAC assemblies always get full trust, the full trust list is no longer meaningful. You should install any assemblies that are used in security policy in the GAC to ensure they are trusted.")]
        public void AddFullTrustAssembly(StrongName sn) { }
        [Obsolete("Because all GAC assemblies always get full trust, the full trust list is no longer meaningful. You should install any assemblies that are used in security policy in the GAC to ensure they are trusted.")]
        public void AddFullTrustAssembly(StrongNameMembershipCondition snMC) { }
        public void AddNamedPermissionSet(NamedPermissionSet permSet) { }
        public NamedPermissionSet ChangeNamedPermissionSet(string name, PermissionSet pSet) { return default(NamedPermissionSet); }
        [Obsolete("AppDomain policy levels are obsolete. See https://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static PolicyLevel CreateAppDomainLevel() { return default(PolicyLevel); }
        public void FromXml(SecurityElement e) { }
        public NamedPermissionSet GetNamedPermissionSet(string name) { return default(NamedPermissionSet); }
        public void Recover() { }
        [Obsolete("Because all GAC assemblies always get full trust, the full trust list is no longer meaningful. You should install any assemblies that are used in security policy in the GAC to ensure they are trusted.")]
        public void RemoveFullTrustAssembly(StrongName sn) { }
        [Obsolete("Because all GAC assemblies always get full trust, the full trust list is no longer meaningful. You should install any assemblies that are used in security policy in the GAC to ensure they are trusted.")]
        public void RemoveFullTrustAssembly(StrongNameMembershipCondition snMC) { }
        public NamedPermissionSet RemoveNamedPermissionSet(NamedPermissionSet permSet) { return default(NamedPermissionSet); }
        public NamedPermissionSet RemoveNamedPermissionSet(string name) { return default(NamedPermissionSet); }
        public void Reset() { }
        public PolicyStatement Resolve(Evidence evidence) { return default(PolicyStatement); }
        public CodeGroup ResolveMatchingCodeGroups(Evidence evidence) { return default(CodeGroup); }
        public SecurityElement ToXml() { return default(SecurityElement); }
    }
}

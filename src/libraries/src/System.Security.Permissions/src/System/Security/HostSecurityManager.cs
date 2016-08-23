// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public partial class HostSecurityManager
    {
        public HostSecurityManager() { }
        public virtual System.Security.Policy.PolicyLevel DomainPolicy { get { return default(System.Security.Policy.PolicyLevel); } }
        public virtual System.Security.HostSecurityManagerOptions Flags { get { return default(System.Security.HostSecurityManagerOptions); } }
        public virtual System.Security.Policy.ApplicationTrust DetermineApplicationTrust(System.Security.Policy.Evidence applicationEvidence, System.Security.Policy.Evidence activatorEvidence, System.Security.Policy.TrustManagerContext context) { return default(System.Security.Policy.ApplicationTrust); }
        public virtual System.Security.Policy.Evidence ProvideAppDomainEvidence(System.Security.Policy.Evidence inputEvidence) { return default(System.Security.Policy.Evidence); }
        public virtual System.Security.Policy.Evidence ProvideAssemblyEvidence(System.Reflection.Assembly loadedAssembly, System.Security.Policy.Evidence inputEvidence) { return default(System.Security.Policy.Evidence); }
        [System.ObsoleteAttribute]
        public virtual System.Security.PermissionSet ResolvePolicy(System.Security.Policy.Evidence evidence) { return default(System.Security.PermissionSet); }
    }
}

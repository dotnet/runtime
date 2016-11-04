// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;

namespace System.Security
{
    [Serializable]
    public partial class HostSecurityManager
    {
        public HostSecurityManager() { }
        public virtual PolicyLevel DomainPolicy { get { return default(PolicyLevel); } }
        public virtual HostSecurityManagerOptions Flags { get { return default(HostSecurityManagerOptions); } }
        public virtual ApplicationTrust DetermineApplicationTrust(Evidence applicationEvidence, Evidence activatorEvidence, TrustManagerContext context) { return default(ApplicationTrust); }
        public virtual Evidence ProvideAppDomainEvidence(Evidence inputEvidence) { return default(Evidence); }
        public virtual Evidence ProvideAssemblyEvidence(System.Reflection.Assembly loadedAssembly, Evidence inputEvidence) { return default(Evidence); }
        [Obsolete]
        public virtual PermissionSet ResolvePolicy(Evidence evidence) { return default(PermissionSet); }
    }
}

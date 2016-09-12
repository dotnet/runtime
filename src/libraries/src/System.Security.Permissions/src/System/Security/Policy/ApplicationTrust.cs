// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class ApplicationTrust : System.Security.Policy.EvidenceBase, System.Security.ISecurityEncodable
    {
        public ApplicationTrust() { }
        public ApplicationTrust(System.Security.PermissionSet defaultGrantSet, System.Collections.Generic.IEnumerable<System.Security.Policy.StrongName> fullTrustAssemblies) { }
        public System.Security.Policy.PolicyStatement DefaultGrantSet { get; set; }
        public object ExtraInfo { get; set; }
        public System.Collections.Generic.IList<System.Security.Policy.StrongName> FullTrustAssemblies { get { return default(System.Collections.Generic.IList<System.Security.Policy.StrongName>); } }
        public bool IsApplicationTrustedToRun { get; set; }
        public bool Persist { get; set; }
        public void FromXml(SecurityElement element) { }
        public SecurityElement ToXml() { return default(SecurityElement); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Security.Policy
{
    [Serializable]
    public sealed partial class ApplicationTrust : EvidenceBase, ISecurityEncodable
    {
        public ApplicationTrust() { }
        public ApplicationTrust(PermissionSet defaultGrantSet, IEnumerable<StrongName> fullTrustAssemblies) { }
        public PolicyStatement DefaultGrantSet { get; set; }
        public object ExtraInfo { get; set; }
        public IList<StrongName> FullTrustAssemblies { get { return default(IList<StrongName>); } }
        public bool IsApplicationTrustedToRun { get; set; }
        public bool Persist { get; set; }
        public void FromXml(SecurityElement element) { }
        public SecurityElement ToXml() { return default(SecurityElement); }
    }
}

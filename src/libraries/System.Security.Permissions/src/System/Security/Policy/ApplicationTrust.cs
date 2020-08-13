// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Security.Policy
{
    public sealed partial class ApplicationTrust : EvidenceBase, ISecurityEncodable
    {
        public ApplicationTrust() { }
        public ApplicationTrust(ApplicationIdentity identity) { }
#if NET50_OBSOLETIONS
        [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        public ApplicationTrust(PermissionSet defaultGrantSet, IEnumerable<StrongName> fullTrustAssemblies) { }
        public ApplicationIdentity ApplicationIdentity { get; set; }
        public PolicyStatement DefaultGrantSet { get; set; }
        public object ExtraInfo { get; set; }
#if NET50_OBSOLETIONS
        [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        public IList<StrongName> FullTrustAssemblies { get { return default(IList<StrongName>); } }
        public bool IsApplicationTrustedToRun { get; set; }
        public bool Persist { get; set; }
        public void FromXml(SecurityElement element) { }
        public SecurityElement ToXml() { return default(SecurityElement); }
    }
}

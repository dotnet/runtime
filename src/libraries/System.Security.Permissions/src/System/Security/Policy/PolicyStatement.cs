// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Policy
{
    public sealed partial class PolicyStatement : ISecurityEncodable, ISecurityPolicyEncodable
    {
#if NET5_0_OR_GREATER
        [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        public PolicyStatement(PermissionSet permSet) { }
#if NET5_0_OR_GREATER
        [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        public PolicyStatement(PermissionSet permSet, PolicyStatementAttribute attributes) { }
        public PolicyStatementAttribute Attributes { get; set; }
        public string AttributeString { get { return null; } }
        public PermissionSet PermissionSet { get; set; }
        public PolicyStatement Copy() { return this; }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement et) { }
        public void FromXml(SecurityElement et, PolicyLevel level) { }
        public override int GetHashCode() => base.GetHashCode();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public SecurityElement ToXml(PolicyLevel level) { return default(SecurityElement); }
    }
}

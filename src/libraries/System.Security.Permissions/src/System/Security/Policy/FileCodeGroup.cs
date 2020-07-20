// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Policy
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class FileCodeGroup : CodeGroup
    {
        public FileCodeGroup(IMembershipCondition membershipCondition, Permissions.FileIOPermissionAccess access) : base(default(IMembershipCondition), default(PolicyStatement)) { }
        public override string AttributeString { get { return null; } }
        public override string MergeLogic { get { return null; } }
        public override string PermissionSetName { get { return null; } }
        public override CodeGroup Copy() { return default(CodeGroup); }
        protected override void CreateXml(SecurityElement element, PolicyLevel level) { }
        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();
        protected override void ParseXml(SecurityElement e, PolicyLevel level) { }
        public override PolicyStatement Resolve(Evidence evidence) { return default(PolicyStatement); }
        public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence) { return default(CodeGroup); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class NetCodeGroup : CodeGroup
    {
        public static readonly string AbsentOriginScheme;
        public static readonly string AnyOtherOriginScheme;
        public NetCodeGroup(IMembershipCondition membershipCondition) : base(default(IMembershipCondition), default(PolicyStatement)) { }
        public override string AttributeString { get { return null; } }
        public override string MergeLogic { get { return null; } }
        public override string PermissionSetName { get { return null; } }
        public void AddConnectAccess(string originScheme, CodeConnectAccess connectAccess) { }
        public override CodeGroup Copy() { return default(CodeGroup); }
        protected override void CreateXml(SecurityElement element, PolicyLevel level) { }
        public override bool Equals(object o) => base.Equals(o);
        public System.Collections.DictionaryEntry[] GetConnectAccessRules() { return default(System.Collections.DictionaryEntry[]); }
        public override int GetHashCode() => base.GetHashCode();
        protected override void ParseXml(SecurityElement e, PolicyLevel level) { }
        public void ResetConnectAccess() { }
        public override PolicyStatement Resolve(Evidence evidence) { return default(PolicyStatement); }
        public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence) { return default(CodeGroup); }
    }
}

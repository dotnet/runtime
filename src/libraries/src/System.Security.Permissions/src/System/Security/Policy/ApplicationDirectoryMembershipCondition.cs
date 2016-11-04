// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    [Serializable]
    public sealed partial class ApplicationDirectoryMembershipCondition : ISecurityEncodable, ISecurityPolicyEncodable, IMembershipCondition
    {
        public ApplicationDirectoryMembershipCondition() { }
        public bool Check(Evidence evidence) { return false; }
        public IMembershipCondition Copy() { return default(IMembershipCondition); }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement e) { }
        public void FromXml(SecurityElement e, PolicyLevel level) { }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public SecurityElement ToXml(PolicyLevel level) { return default(SecurityElement); }
    }
}

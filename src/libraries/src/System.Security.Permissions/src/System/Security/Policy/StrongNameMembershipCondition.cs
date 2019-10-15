// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.Security.Policy
{
    public sealed partial class StrongNameMembershipCondition : ISecurityEncodable, ISecurityPolicyEncodable, IMembershipCondition
    {
        public StrongNameMembershipCondition(StrongNamePublicKeyBlob blob, string name, Version version) { }
        public string Name { get; set; }
        public StrongNamePublicKeyBlob PublicKey { get; set; }
        public Version Version { get; set; }
        public bool Check(Evidence evidence) { return false; }
        public IMembershipCondition Copy() { return this; }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement e) { }
        public void FromXml(SecurityElement e, PolicyLevel level) { }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public SecurityElement ToXml(PolicyLevel level) { return default(SecurityElement); }
    }
}

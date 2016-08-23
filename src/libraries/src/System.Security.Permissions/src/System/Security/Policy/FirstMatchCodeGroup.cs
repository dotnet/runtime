// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class FirstMatchCodeGroup : System.Security.Policy.CodeGroup
    {
        public FirstMatchCodeGroup(System.Security.Policy.IMembershipCondition membershipCondition, System.Security.Policy.PolicyStatement policy) : base(default(System.Security.Policy.IMembershipCondition), default(System.Security.Policy.PolicyStatement)) { }
        public override string MergeLogic { get { return null; } }
        public override System.Security.Policy.CodeGroup Copy() { return default(System.Security.Policy.CodeGroup); }
        public override System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence) { return default(System.Security.Policy.PolicyStatement); }
        public override System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence) { return default(System.Security.Policy.CodeGroup); }
    }
}

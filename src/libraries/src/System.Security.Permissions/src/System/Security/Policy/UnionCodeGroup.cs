// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    [Serializable]
    public sealed partial class UnionCodeGroup : CodeGroup
    {
        public UnionCodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy) : base(default(IMembershipCondition), default(PolicyStatement)) { }
        public override string MergeLogic { get { return null; } }
        public override CodeGroup Copy() { return default(CodeGroup); }
        public override PolicyStatement Resolve(Evidence evidence) { return default(PolicyStatement); }
        public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence) { return default(CodeGroup); }
    }
}

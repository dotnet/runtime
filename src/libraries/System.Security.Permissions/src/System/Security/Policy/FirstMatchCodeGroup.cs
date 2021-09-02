// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Policy
{
    [Obsolete("Code Access Security is not supported or honored by the runtime.")]
    public sealed partial class FirstMatchCodeGroup : CodeGroup
    {
        public FirstMatchCodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy) : base(default(IMembershipCondition), default(PolicyStatement)) { }
        public override string MergeLogic { get { return null; } }
        public override CodeGroup Copy() { return default(CodeGroup); }
        public override PolicyStatement Resolve(Evidence evidence) { return default(PolicyStatement); }
        public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence) { return default(CodeGroup); }
    }
}

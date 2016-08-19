// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public abstract partial class CodeGroup
    {
        protected CodeGroup(System.Security.Policy.IMembershipCondition membershipCondition, System.Security.Policy.PolicyStatement policy) { }
        public virtual string AttributeString { get { return null; } }
        public System.Collections.IList Children { get; set; }
        public string Description { get; set; }
        public System.Security.Policy.IMembershipCondition MembershipCondition { get; set; }
        public abstract string MergeLogic { get; }
        public string Name { get; set; }
        public virtual string PermissionSetName { get { return null; } }
        public System.Security.Policy.PolicyStatement PolicyStatement { get; set; }
        public void AddChild(System.Security.Policy.CodeGroup group) { }
        public abstract System.Security.Policy.CodeGroup Copy();
        public override bool Equals(object o) => base.Equals(o);
        public bool Equals(System.Security.Policy.CodeGroup cg, bool compareChildren) { return false; }
        public override int GetHashCode() => base.GetHashCode();
        public void RemoveChild(System.Security.Policy.CodeGroup group) { }
        public abstract System.Security.Policy.PolicyStatement Resolve(System.Security.Policy.Evidence evidence);
        public abstract System.Security.Policy.CodeGroup ResolveMatchingCodeGroups(System.Security.Policy.Evidence evidence);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Security.Policy
{
    [Serializable]
    public abstract partial class CodeGroup
    {
        protected CodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy) { }
        public virtual string AttributeString { get { return null; } }
        public IList Children { get; set; }
        public string Description { get; set; }
        public IMembershipCondition MembershipCondition { get; set; }
        public abstract string MergeLogic { get; }
        public string Name { get; set; }
        public virtual string PermissionSetName { get { return null; } }
        public PolicyStatement PolicyStatement { get; set; }
        public void AddChild(CodeGroup group) { }
        public abstract CodeGroup Copy();
        protected virtual void CreateXml(SecurityElement element, PolicyLevel level) { }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement e) { }
        public void FromXml(SecurityElement e, PolicyLevel level) { }
        public bool Equals(CodeGroup cg, bool compareChildren) { return false; }
        public override int GetHashCode() => base.GetHashCode();
        protected virtual void ParseXml(SecurityElement e, PolicyLevel level) { }
        public void RemoveChild(CodeGroup group) { }
        public abstract PolicyStatement Resolve(Evidence evidence);
        public abstract CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
        public SecurityElement ToXml() { return default(SecurityElement); }
        public SecurityElement ToXml(PolicyLevel level) { return default(SecurityElement); }
    }
}

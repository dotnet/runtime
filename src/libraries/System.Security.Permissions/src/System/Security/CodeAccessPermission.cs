// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    [Serializable]
    public abstract partial class CodeAccessPermission : IPermission, ISecurityEncodable, IStackWalk
    {
        protected CodeAccessPermission() { }
        public void Assert() { }
        public abstract IPermission Copy();
        public void Demand() { }
        [Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public void Deny() { throw new NotSupportedException(); }
        public override bool Equals(object obj) => base.Equals(obj);
        public abstract void FromXml(SecurityElement elem);
        public override int GetHashCode() => base.GetHashCode();
        public abstract IPermission Intersect(IPermission target);
        public abstract bool IsSubsetOf(IPermission target);
        public void PermitOnly() { throw new PlatformNotSupportedException(); }
        public override string ToString() => base.ToString();
        public abstract SecurityElement ToXml();
        public virtual IPermission Union(IPermission other) { return default(IPermission); }
    }
}
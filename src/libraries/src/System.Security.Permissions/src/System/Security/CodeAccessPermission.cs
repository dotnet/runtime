// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public abstract partial class CodeAccessPermission : System.Security.IPermission, System.Security.ISecurityEncodable, System.Security.IStackWalk
    {
        protected CodeAccessPermission() { }
        public void Assert() { }
        public abstract System.Security.IPermission Copy();
        public void Demand() { }
        [System.ObsoleteAttribute("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public void Deny() { throw new System.NotSupportedException(); }
        public override bool Equals(object obj) => base.Equals(obj);
        public abstract void FromXml(SecurityElement elem);
        public override int GetHashCode() => base.GetHashCode();
        public abstract System.Security.IPermission Intersect(System.Security.IPermission target);
        public abstract bool IsSubsetOf(System.Security.IPermission target);
        public void PermitOnly() { throw new System.PlatformNotSupportedException(); }
        public override string ToString() => base.ToString();
        public abstract SecurityElement ToXml();
        public virtual System.Security.IPermission Union(System.Security.IPermission other) { return default(System.Security.IPermission); }
    }
}
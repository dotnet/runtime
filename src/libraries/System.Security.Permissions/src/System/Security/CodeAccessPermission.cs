// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public abstract partial class CodeAccessPermission : IPermission, ISecurityEncodable, IStackWalk
    {
        protected CodeAccessPermission() { }
        public void Assert() { }
        public abstract IPermission Copy();
        public void Demand() { }
        [Obsolete]
        public void Deny() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public override bool Equals(object obj) => base.Equals(obj);
        public abstract void FromXml(SecurityElement elem);
        public override int GetHashCode() => base.GetHashCode();
        public abstract IPermission Intersect(IPermission target);
        public abstract bool IsSubsetOf(IPermission target);
        public void PermitOnly() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public static void RevertAll() { }
        public static void RevertAssert() { }
        [Obsolete]
        public static void RevertDeny() { }
        public static void RevertPermitOnly() { }
        public override string ToString() => base.ToString();
        public abstract SecurityElement ToXml();
        public virtual IPermission Union(IPermission other) { return default(IPermission); }
    }
}
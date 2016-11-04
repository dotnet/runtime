// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    //TODO #9641: Implement PrincipalPermission with real behavior
    [Serializable]
    public sealed partial class PrincipalPermission : IPermission, ISecurityEncodable, IUnrestrictedPermission
    {
        public PrincipalPermission(PermissionState state) { }
        public PrincipalPermission(string name, string role) { }
        public PrincipalPermission(string name, string role, bool isAuthenticated) { }
        public IPermission Copy() { return default(IPermission); }
        public void Demand() { throw new SecurityException(); }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement elem) { }
        public override int GetHashCode() => base.GetHashCode();
        public IPermission Intersect(IPermission target) { return default(IPermission); }
        public bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public override string ToString() => base.ToString();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public IPermission Union(IPermission other) { return default(IPermission); }
    }
}

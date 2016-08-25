// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    //TODO implement PrincipalPermission
    public sealed partial class PrincipalPermission : System.Security.IPermission, System.Security.ISecurityEncodable, System.Security.Permissions.IUnrestrictedPermission
    {
        public PrincipalPermission(System.Security.Permissions.PermissionState state) { }
        public PrincipalPermission(string name, string role) { }
        public PrincipalPermission(string name, string role, bool isAuthenticated) { }
        public System.Security.IPermission Copy() { return default(System.Security.IPermission); }
        public void Demand() { throw new System.Security.SecurityException(); }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement elem) { }
        public override int GetHashCode() => base.GetHashCode();
        public System.Security.IPermission Intersect(System.Security.IPermission target) { return default(System.Security.IPermission); }
        public bool IsSubsetOf(System.Security.IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public override string ToString() => base.ToString();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public System.Security.IPermission Union(System.Security.IPermission other) { return default(System.Security.IPermission); }
    }
}

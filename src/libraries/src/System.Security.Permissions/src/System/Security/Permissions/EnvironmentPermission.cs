// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed partial class EnvironmentPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission
    {
        public EnvironmentPermission(System.Security.Permissions.EnvironmentPermissionAccess flag, string pathList) { }
        public EnvironmentPermission(System.Security.Permissions.PermissionState state) { }
        public void AddPathList(System.Security.Permissions.EnvironmentPermissionAccess flag, string pathList) { }
        public override System.Security.IPermission Copy() { return default(System.Security.IPermission); }
        public override void FromXml(SecurityElement esd) { }
        public string GetPathList(System.Security.Permissions.EnvironmentPermissionAccess flag) { return null; }
        public override System.Security.IPermission Intersect(System.Security.IPermission target) { return default(System.Security.IPermission); }
        public override bool IsSubsetOf(System.Security.IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void SetPathList(System.Security.Permissions.EnvironmentPermissionAccess flag, string pathList) { }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override System.Security.IPermission Union(System.Security.IPermission other) { return default(System.Security.IPermission); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed partial class RegistryPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission
    {
        public RegistryPermission(System.Security.Permissions.PermissionState state) { }
        public RegistryPermission(System.Security.Permissions.RegistryPermissionAccess access, System.Security.AccessControl.AccessControlActions control, string pathList) { }
        public RegistryPermission(System.Security.Permissions.RegistryPermissionAccess access, string pathList) { }
        public void AddPathList(System.Security.Permissions.RegistryPermissionAccess access, string pathList) { }
        public override System.Security.IPermission Copy() { return default(System.Security.IPermission); }
        public override void FromXml(SecurityElement elem) { }
        public string GetPathList(System.Security.Permissions.RegistryPermissionAccess access) { return null; }
        public override System.Security.IPermission Intersect(System.Security.IPermission target) { return default(System.Security.IPermission); }
        public override bool IsSubsetOf(System.Security.IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void SetPathList(System.Security.Permissions.RegistryPermissionAccess access, string pathList) { }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override System.Security.IPermission Union(System.Security.IPermission other) { return default(System.Security.IPermission); }
    }
}

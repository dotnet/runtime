// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed partial class FileIOPermission : System.Security.CodeAccessPermission, System.Security.Permissions.IUnrestrictedPermission
    {
        public FileIOPermission(System.Security.Permissions.FileIOPermissionAccess access, string path) { }
        public FileIOPermission(System.Security.Permissions.FileIOPermissionAccess access, string[] pathList) { }
        public FileIOPermission(System.Security.Permissions.PermissionState state) { }
        public System.Security.Permissions.FileIOPermissionAccess AllFiles { get; set; }
        public System.Security.Permissions.FileIOPermissionAccess AllLocalFiles { get; set; }
        public void AddPathList(System.Security.Permissions.FileIOPermissionAccess access, string path) { }
        public void AddPathList(System.Security.Permissions.FileIOPermissionAccess access, string[] pathList) { }
        public override System.Security.IPermission Copy() { return this; }
        public override bool Equals(object o) => base.Equals(o);
        public override void FromXml(SecurityElement esd) { }
        public override int GetHashCode() => base.GetHashCode();
        public string[] GetPathList(System.Security.Permissions.FileIOPermissionAccess access) { return null; }
        public override System.Security.IPermission Intersect(System.Security.IPermission target) { return default(System.Security.IPermission); }
        public override bool IsSubsetOf(System.Security.IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void SetPathList(System.Security.Permissions.FileIOPermissionAccess access, string path) { }
        public void SetPathList(System.Security.Permissions.FileIOPermissionAccess access, string[] pathList) { }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override System.Security.IPermission Union(System.Security.IPermission other) { return default(System.Security.IPermission); }
    }
}

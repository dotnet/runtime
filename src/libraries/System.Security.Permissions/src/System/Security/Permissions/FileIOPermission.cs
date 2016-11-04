// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    public sealed partial class FileIOPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public FileIOPermission(FileIOPermissionAccess access, string path) { }
        public FileIOPermission(FileIOPermissionAccess access, string[] pathList) { }
        public FileIOPermission(PermissionState state) { }
        public FileIOPermissionAccess AllFiles { get; set; }
        public FileIOPermissionAccess AllLocalFiles { get; set; }
        public void AddPathList(FileIOPermissionAccess access, string path) { }
        public void AddPathList(FileIOPermissionAccess access, string[] pathList) { }
        public override IPermission Copy() { return this; }
        public override bool Equals(object o) => base.Equals(o);
        public override void FromXml(SecurityElement esd) { }
        public override int GetHashCode() => base.GetHashCode();
        public string[] GetPathList(FileIOPermissionAccess access) { return null; }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void SetPathList(FileIOPermissionAccess access, string path) { }
        public void SetPathList(FileIOPermissionAccess access, string[] pathList) { }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission other) { return default(IPermission); }
    }
}

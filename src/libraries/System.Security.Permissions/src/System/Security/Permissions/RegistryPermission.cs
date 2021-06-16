// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.AccessControl;

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class RegistryPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public RegistryPermission(PermissionState state) { }
        public RegistryPermission(RegistryPermissionAccess access, AccessControlActions control, string pathList) { }
        public RegistryPermission(RegistryPermissionAccess access, string pathList) { }
        public void AddPathList(RegistryPermissionAccess access, string pathList) { }
        public void AddPathList(RegistryPermissionAccess access, AccessControlActions actions, string pathList) { }
        public override IPermission Copy() { return default(IPermission); }
        public override void FromXml(SecurityElement elem) { }
        public string GetPathList(RegistryPermissionAccess access) { return null; }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void SetPathList(RegistryPermissionAccess access, string pathList) { }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission other) { return default(IPermission); }
    }
}

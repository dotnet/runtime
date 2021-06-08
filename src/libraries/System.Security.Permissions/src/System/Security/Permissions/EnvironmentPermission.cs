// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class EnvironmentPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public EnvironmentPermission(EnvironmentPermissionAccess flag, string pathList) { }
        public EnvironmentPermission(PermissionState state) { }
        public void AddPathList(EnvironmentPermissionAccess flag, string pathList) { }
        public override IPermission Copy() { return default(IPermission); }
        public override void FromXml(SecurityElement esd) { }
        public string GetPathList(EnvironmentPermissionAccess flag) { return null; }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void SetPathList(EnvironmentPermissionAccess flag, string pathList) { }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission other) { return default(IPermission); }
    }
}

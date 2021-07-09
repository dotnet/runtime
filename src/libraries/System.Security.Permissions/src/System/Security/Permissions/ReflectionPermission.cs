// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class ReflectionPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public ReflectionPermission(PermissionState state) { }
        public ReflectionPermission(ReflectionPermissionFlag flag) { }
        public ReflectionPermissionFlag Flags { get; set; }
        public override IPermission Copy() { return this; }
        public override void FromXml(SecurityElement esd) { }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission other) { return default(IPermission); }
    }
}

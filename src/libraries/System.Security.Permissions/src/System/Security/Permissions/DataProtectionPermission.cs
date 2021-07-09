// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class DataProtectionPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public DataProtectionPermission(PermissionState state) { }
        public DataProtectionPermission(DataProtectionPermissionFlags flag) { }
        public bool IsUnrestricted() => false;
        public DataProtectionPermissionFlags Flags { get; set; }
        public override IPermission Copy() { return null; }
        public override IPermission Union(IPermission target) { return null; }
        public override IPermission Intersect(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) => false;
        public override void FromXml(SecurityElement securityElement) { }
        public override SecurityElement ToXml() { return null; }
    }
}

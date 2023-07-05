// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NETCOREAPP
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class KeyContainerPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public KeyContainerPermission(PermissionState state) { }
        public KeyContainerPermission(KeyContainerPermissionFlags flags) { }
        public KeyContainerPermission(KeyContainerPermissionFlags flags, KeyContainerPermissionAccessEntry[] accessList) { }
        public KeyContainerPermissionFlags Flags { get; }
        public KeyContainerPermissionAccessEntryCollection AccessEntries { get; }
        public bool IsUnrestricted() { return false; }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public override IPermission Intersect(IPermission target) { return null; }
        public override IPermission Union(IPermission target) { return null; }
        public override IPermission Copy() { return null; }
        public override SecurityElement ToXml() { return null; }
        public override void FromXml(SecurityElement securityElement) { }
    }
}

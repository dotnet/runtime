// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class IsolatedStorageFilePermission : IsolatedStoragePermission
    {
        public IsolatedStorageFilePermission(PermissionState state) : base(state) { }
        public override IPermission Union(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public override IPermission Intersect(IPermission target) { return null; }
        public override IPermission Copy() { return null; }
        public override SecurityElement ToXml() { return null; }
    }
}

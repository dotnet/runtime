// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.Drawing.Printing
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class PrintingPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public PrintingPermission(PrintingPermissionLevel printingLevel) { }
        public PrintingPermission(PermissionState state) { }
        public PrintingPermissionLevel Level { get; set; }
        public override IPermission Copy() => null;
        public override void FromXml(SecurityElement element) { }
        public override IPermission Intersect(IPermission target) => null;
        public override bool IsSubsetOf(IPermission target) => false;
        public bool IsUnrestricted() => false;
        public override SecurityElement ToXml() => null;
        public override IPermission Union(IPermission target) => null;
    }
}

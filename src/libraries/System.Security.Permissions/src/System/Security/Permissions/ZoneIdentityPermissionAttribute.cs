// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage((AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class ZoneIdentityPermissionAttribute : CodeAccessSecurityAttribute
    {
        public ZoneIdentityPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public SecurityZone Zone { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

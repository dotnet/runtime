// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage((AttributeTargets)(68), AllowMultiple = true, Inherited = false)]
    public sealed partial class PrincipalPermissionAttribute : CodeAccessSecurityAttribute
    {
#if NET50_OBSOLETIONS
        [Obsolete(Obsoletions.PrincipalPermissionAttributeMessage, error: true, DiagnosticId = Obsoletions.PrincipalPermissionAttributeDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        public PrincipalPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public bool Authenticated { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

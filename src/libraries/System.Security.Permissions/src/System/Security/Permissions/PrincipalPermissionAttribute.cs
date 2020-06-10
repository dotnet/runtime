// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [AttributeUsage((AttributeTargets)(68), AllowMultiple = true, Inherited = false)]
    public sealed partial class PrincipalPermissionAttribute : CodeAccessSecurityAttribute
    {
#if CAS_OBSOLETIONS
        [Obsolete("PrincipalPermissionAttribute is not honored by the runtime and must not be used.", error: true, DiagnosticId = "MSLIB0002", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        public PrincipalPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public bool Authenticated { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

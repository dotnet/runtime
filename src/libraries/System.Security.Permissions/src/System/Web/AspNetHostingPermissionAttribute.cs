// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.Web
{
#if NETCOREAPP
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class AspNetHostingPermissionAttribute : CodeAccessSecurityAttribute
    {
        public AspNetHostingPermissionAttribute(SecurityAction action) : base(action) { }
        public AspNetHostingPermissionLevel Level { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}

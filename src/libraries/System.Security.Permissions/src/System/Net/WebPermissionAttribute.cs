// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.Net
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class |
        AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class WebPermissionAttribute : CodeAccessSecurityAttribute
    {
        public WebPermissionAttribute(SecurityAction action) : base(action) { }
        public string Accept { get { return null; } set { } }
        public string AcceptPattern { get { return null; } set { } }
        public string Connect { get { return null; } set { } }
        public string ConnectPattern { get { return null; } set { } }
        public override IPermission CreatePermission() { return null; }
    }
}

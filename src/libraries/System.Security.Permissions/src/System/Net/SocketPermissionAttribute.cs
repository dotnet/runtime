// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.Net
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class |
        AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class SocketPermissionAttribute : CodeAccessSecurityAttribute
    {
        public SocketPermissionAttribute(SecurityAction action) : base(action) { }
        public string Access { get { return null; } set { } }
        public string Host { get { return null; } set { } }
        public string Port { get { return null; } set { } }
        public string Transport { get { return null; } set { } }
        public override IPermission CreatePermission() { return null; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Security;
using System.Security.Permissions;

namespace System.Data.Odbc
{
#if NETCOREAPP
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class OdbcPermissionAttribute : DBDataPermissionAttribute
    {
        public OdbcPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public override IPermission CreatePermission() { return null; }
    }
}

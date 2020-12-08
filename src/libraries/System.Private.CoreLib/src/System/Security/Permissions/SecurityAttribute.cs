// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [AttributeUsage((AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public abstract partial class SecurityAttribute : Attribute
    {
        protected SecurityAttribute(SecurityAction action) { }
        public SecurityAction Action { get; set; }
        public bool Unrestricted { get; set; }
        public abstract IPermission? CreatePermission();
    }
}

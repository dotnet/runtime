// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage((AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class PublisherIdentityPermissionAttribute : CodeAccessSecurityAttribute
    {
        public PublisherIdentityPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public string CertFile { get; set; }
        public string SignedFile { get; set; }
        public string X509Certificate { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

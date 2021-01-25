// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public abstract class IsolatedStoragePermissionAttribute : CodeAccessSecurityAttribute
    {
        protected IsolatedStoragePermissionAttribute(SecurityAction action) : base(action) { }
        public long UserQuota { get; set; }
        public IsolatedStorageContainment UsageAllowed { get; set; }
    }
}

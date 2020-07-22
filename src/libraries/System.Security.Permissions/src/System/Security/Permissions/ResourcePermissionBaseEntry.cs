// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public class ResourcePermissionBaseEntry
    {
        public ResourcePermissionBaseEntry() { }
        public ResourcePermissionBaseEntry(int permissionAccess, string[] permissionAccessPath) { }
        public int PermissionAccess { get; }
        public string[] PermissionAccessPath { get; }
    }
}

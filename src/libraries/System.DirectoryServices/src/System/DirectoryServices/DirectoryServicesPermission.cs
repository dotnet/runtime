// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.DirectoryServices
{
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public sealed class DirectoryServicesPermission : ResourcePermissionBase
    {
        public DirectoryServicesPermission() { }
        public DirectoryServicesPermission(DirectoryServicesPermissionEntry[]? permissionAccessEntries) { }
        public DirectoryServicesPermission(PermissionState state) { }
        public DirectoryServicesPermission(DirectoryServicesPermissionAccess permissionAccess, string? path) { }
        public DirectoryServicesPermissionEntryCollection? PermissionEntries { get; }
    }
}

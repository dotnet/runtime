// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.DirectoryServices
{
#if CAS_OBSOLETIONS
    [Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
    public sealed class DirectoryServicesPermission : ResourcePermissionBase
    {
        public DirectoryServicesPermission() { }
        public DirectoryServicesPermission(DirectoryServicesPermissionEntry[] permissionAccessEntries) { }
        public DirectoryServicesPermission(PermissionState state) { }
        public DirectoryServicesPermission(DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public DirectoryServicesPermissionEntryCollection PermissionEntries { get; }
    }
}

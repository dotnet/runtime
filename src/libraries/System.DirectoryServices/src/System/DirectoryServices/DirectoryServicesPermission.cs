// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.DirectoryServices
{
#pragma warning disable SYSLIB0003
    // Conditionally marking this type as obsolete in .NET 5+ will require diverging its net5.0 build from netstandard2.0
    // https://github.com/dotnet/runtime/issues/39413
    public sealed class DirectoryServicesPermission : ResourcePermissionBase
    {
        public DirectoryServicesPermission() { }
        public DirectoryServicesPermission(DirectoryServicesPermissionEntry[] permissionAccessEntries) { }
        public DirectoryServicesPermission(PermissionState state) { }
        public DirectoryServicesPermission(DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public DirectoryServicesPermissionEntryCollection PermissionEntries { get; }
    }
#pragma warning restore SYSLIB0003
}

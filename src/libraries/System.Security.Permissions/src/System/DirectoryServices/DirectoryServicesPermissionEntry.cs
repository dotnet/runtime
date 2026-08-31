// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices
{
    public class DirectoryServicesPermissionEntry
    {
        public DirectoryServicesPermissionEntry(DirectoryServicesPermissionAccess permissionAccess, string? path) { }
        public string? Path { get; }
        public DirectoryServicesPermissionAccess PermissionAccess { get; }
    }
}

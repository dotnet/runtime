// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;
 
namespace System.DirectoryServices
{
    public sealed class DirectoryServicesPermission : ResourcePermissionBase
    {
        public DirectoryServicesPermission() { }
        public DirectoryServicesPermission(DirectoryServicesPermissionEntry[] permissionAccessEntries) { }
        public DirectoryServicesPermission(PermissionState state) { }
        public DirectoryServicesPermission(DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public DirectoryServicesPermissionEntryCollection PermissionEntries { get; }
    }
}
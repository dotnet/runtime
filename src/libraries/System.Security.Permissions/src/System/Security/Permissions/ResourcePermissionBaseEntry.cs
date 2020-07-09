// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
    public class ResourcePermissionBaseEntry
    {
        public ResourcePermissionBaseEntry() { }
        public ResourcePermissionBaseEntry(int permissionAccess, string[] permissionAccessPath) { }
        public int PermissionAccess { get; }
        public string[] PermissionAccessPath { get; }
    }
}

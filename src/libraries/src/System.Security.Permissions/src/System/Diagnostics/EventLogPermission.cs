// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.Diagnostics
{
    public sealed class EventLogPermission : ResourcePermissionBase
    {
        public EventLogPermission() { }
        public EventLogPermission(EventLogPermissionAccess permissionAccess, string machineName) { }
        public EventLogPermission(EventLogPermissionEntry[] permissionAccessEntries) { }
        public EventLogPermission(PermissionState state) { }
        public EventLogPermissionEntryCollection PermissionEntries { get; }
    }
}

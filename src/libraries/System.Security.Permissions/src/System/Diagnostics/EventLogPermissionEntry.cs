// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public class EventLogPermissionEntry
    {
        public EventLogPermissionEntry(EventLogPermissionAccess permissionAccess, string machineName) { }
        public string MachineName { get { return null; } }
        public EventLogPermissionAccess PermissionAccess { get; }
    }
}

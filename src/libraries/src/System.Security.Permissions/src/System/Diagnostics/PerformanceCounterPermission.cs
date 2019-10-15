// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.Diagnostics
{
    public sealed class PerformanceCounterPermission : ResourcePermissionBase
    {
        public PerformanceCounterPermission() { }
        public PerformanceCounterPermission(PerformanceCounterPermissionAccess permissionAccess, string machineName, string categoryName) { }
        public PerformanceCounterPermission(PerformanceCounterPermissionEntry[] permissionAccessEntries) { }
        public PerformanceCounterPermission(PermissionState state) { }
        public PerformanceCounterPermissionEntryCollection PermissionEntries { get { return null; } }
    }
}

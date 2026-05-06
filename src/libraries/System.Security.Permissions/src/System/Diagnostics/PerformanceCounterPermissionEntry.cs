// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public class PerformanceCounterPermissionEntry
    {
        public PerformanceCounterPermissionEntry(PerformanceCounterPermissionAccess permissionAccess, string machineName, string categoryName) { }
        public string CategoryName { get { return null; } }
        public string MachineName { get { return null; } }
        public PerformanceCounterPermissionAccess PermissionAccess { get; }
    }
}

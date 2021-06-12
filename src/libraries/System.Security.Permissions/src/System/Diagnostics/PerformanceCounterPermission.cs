// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.Diagnostics
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class PerformanceCounterPermission : ResourcePermissionBase
    {
        public PerformanceCounterPermission() { }
        public PerformanceCounterPermission(PerformanceCounterPermissionAccess permissionAccess, string machineName, string categoryName) { }
        public PerformanceCounterPermission(PerformanceCounterPermissionEntry[] permissionAccessEntries) { }
        public PerformanceCounterPermission(PermissionState state) { }
        public PerformanceCounterPermissionEntryCollection PermissionEntries { get { return null; } }
    }
}

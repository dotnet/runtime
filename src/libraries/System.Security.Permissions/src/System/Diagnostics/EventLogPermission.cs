// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.Diagnostics
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class EventLogPermission : ResourcePermissionBase
    {
        public EventLogPermission() { }
        public EventLogPermission(EventLogPermissionAccess permissionAccess, string machineName) { }
        public EventLogPermission(EventLogPermissionEntry[] permissionAccessEntries) { }
        public EventLogPermission(PermissionState state) { }
        public EventLogPermissionEntryCollection PermissionEntries { get; }
    }
}

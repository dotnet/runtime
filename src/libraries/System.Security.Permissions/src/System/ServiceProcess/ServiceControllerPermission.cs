// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.ServiceProcess
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class ServiceControllerPermission : ResourcePermissionBase
    {
        public ServiceControllerPermission() { }
        public ServiceControllerPermission(PermissionState state) : base(state) { }
        public ServiceControllerPermission(ServiceControllerPermissionAccess permissionAccess, string machineName, string serviceName) { }
        public ServiceControllerPermission(ServiceControllerPermissionEntry[] permissionAccessEntries) { }
        public ServiceControllerPermissionEntryCollection PermissionEntries { get => null; }
    }
}

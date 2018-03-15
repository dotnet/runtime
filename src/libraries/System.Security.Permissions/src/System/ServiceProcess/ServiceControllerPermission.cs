// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.ServiceProcess
{
    public sealed class ServiceControllerPermission : ResourcePermissionBase
    {
        public ServiceControllerPermission() { }
        public ServiceControllerPermission(PermissionState state) : base(state) { }
        public ServiceControllerPermission(ServiceControllerPermissionAccess permissionAccess, string machineName, string serviceName) { }
        public ServiceControllerPermission(ServiceControllerPermissionEntry[] permissionAccessEntries) { }
        public ServiceControllerPermissionEntryCollection PermissionEntries { get => null; }
    }
}

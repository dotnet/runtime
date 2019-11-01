// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public class ServiceControllerPermissionEntry
    {
        public ServiceControllerPermissionEntry() { }
        public ServiceControllerPermissionEntry(ServiceControllerPermissionAccess permissionAccess, string machineName, string serviceName) { }
        public string MachineName { get => null; }
        public ServiceControllerPermissionAccess PermissionAccess { get => default(ServiceControllerPermissionAccess); }
        public string ServiceName { get => null; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.ServiceProcess
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Event, AllowMultiple = true, Inherited = false )]
    public class ServiceControllerPermissionAttribute : CodeAccessSecurityAttribute
    {
        public ServiceControllerPermissionAttribute(SecurityAction action): base(action) { }
        public string MachineName { get => null; set { } }
        public ServiceControllerPermissionAccess PermissionAccess { get => default(ServiceControllerPermissionAccess); set { } }
        public string ServiceName { get => null; set { } }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

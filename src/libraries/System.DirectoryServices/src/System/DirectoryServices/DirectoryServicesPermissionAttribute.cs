// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.DirectoryServices
{
#if CAS_OBSOLETIONS
    [Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Event,
        AllowMultiple = true, Inherited = false)]
    public class DirectoryServicesPermissionAttribute : CodeAccessSecurityAttribute
    {
        public DirectoryServicesPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public DirectoryServicesPermissionAccess PermissionAccess { get; set; }
        public string Path { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

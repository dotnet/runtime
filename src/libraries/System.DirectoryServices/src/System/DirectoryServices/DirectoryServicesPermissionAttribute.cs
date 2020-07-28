// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.DirectoryServices
{
#pragma warning disable SYSLIB0003
    // Conditionally marking this type as obsolete in .NET 5+ will require diverging its net5.0 build from netstandard2.0
    // https://github.com/dotnet/runtime/issues/39413
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
#pragma warning restore SYSLIB0003
}

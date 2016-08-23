// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class EnvironmentPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public EnvironmentPermissionAttribute(System.Security.Permissions.SecurityAction action) : base(default(System.Security.Permissions.SecurityAction)) { }
        public string All { get; set; }
        public string Read { get; set; }
        public string Write { get; set; }
        public override System.Security.IPermission CreatePermission() { return default(System.Security.IPermission); }
    }
}

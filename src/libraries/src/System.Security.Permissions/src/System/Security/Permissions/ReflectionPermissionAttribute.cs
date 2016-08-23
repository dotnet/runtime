// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class ReflectionPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public ReflectionPermissionAttribute(System.Security.Permissions.SecurityAction action) : base(default(System.Security.Permissions.SecurityAction)) { }
        public System.Security.Permissions.ReflectionPermissionFlag Flags { get; set; }
        public bool MemberAccess { get; set; }
        [System.ObsoleteAttribute("This permission is no longer used by the CLR.")]
        public bool ReflectionEmit { get; set; }
        public bool RestrictedMemberAccess { get; set; }
        [System.ObsoleteAttribute("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool TypeInformation { get; set; }
        public override System.Security.IPermission CreatePermission() { return default(System.Security.IPermission); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [AttributeUsage((AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class ReflectionPermissionAttribute : CodeAccessSecurityAttribute
    {
        public ReflectionPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public ReflectionPermissionFlag Flags { get; set; }
        public bool MemberAccess { get; set; }
        [Obsolete("This permission is no longer used by the CLR.")]
        public bool ReflectionEmit { get; set; }
        public bool RestrictedMemberAccess { get; set; }
        [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool TypeInformation { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

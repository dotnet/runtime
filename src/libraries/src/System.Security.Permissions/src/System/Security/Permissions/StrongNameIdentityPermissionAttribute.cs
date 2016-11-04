// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    [AttributeUsage((AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class StrongNameIdentityPermissionAttribute : CodeAccessSecurityAttribute
    {
        public StrongNameIdentityPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string Version { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}

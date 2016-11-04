// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    [AttributeUsage((AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class PermissionSetAttribute : CodeAccessSecurityAttribute
    {
        public PermissionSetAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public string File { get; set; }
        public string Hex { get; set; }
        public string Name { get; set; }
        public bool UnicodeEncoded { get; set; }
        public string XML { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
        public PermissionSet CreatePermissionSet() { return default(PermissionSet); }
    }
}

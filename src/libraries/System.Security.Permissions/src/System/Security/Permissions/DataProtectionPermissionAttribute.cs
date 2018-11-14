// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class
     | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )]
    public sealed class DataProtectionPermissionAttribute : CodeAccessSecurityAttribute
    {
        public DataProtectionPermissionAttribute (SecurityAction action) : base(default(SecurityAction)) { }
        public DataProtectionPermissionFlags Flags { get; set; }
        public bool ProtectData { get; set; }
        public bool UnprotectData { get; set; }
        public bool ProtectMemory { get; set; }
        public bool UnprotectMemory { get; set; }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}
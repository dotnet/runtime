// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class TypeDescriptorPermissionAttribute : CodeAccessSecurityAttribute
    {
        public TypeDescriptorPermissionAttribute(SecurityAction action) : base(action) { }
        public TypeDescriptorPermissionFlags Flags { get; set; }
        public bool RestrictedRegistrationAccess { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}

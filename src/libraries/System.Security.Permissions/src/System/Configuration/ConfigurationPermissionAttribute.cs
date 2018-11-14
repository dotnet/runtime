// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    sealed public class ConfigurationPermissionAttribute : CodeAccessSecurityAttribute
    {
        public ConfigurationPermissionAttribute(SecurityAction action) : base(default(SecurityAction)) { }
        public override IPermission CreatePermission() { return default(IPermission); }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Web
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false )]
    sealed public class AspNetHostingPermissionAttribute : CodeAccessSecurityAttribute
    {
        public AspNetHostingPermissionAttribute(SecurityAction action) : base(action) { }
        public AspNetHostingPermissionLevel Level { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.Drawing.Printing
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class PrintingPermissionAttribute : CodeAccessSecurityAttribute
    {
        public PrintingPermissionAttribute(SecurityAction action) : base(action) { }
        public PrintingPermissionLevel Level { get; set; }
        public override System.Security.IPermission CreatePermission() { return null; }
    }
}

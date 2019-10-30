// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Net.NetworkInformation
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class NetworkInformationPermissionAttribute : CodeAccessSecurityAttribute
    {
        public NetworkInformationPermissionAttribute(SecurityAction action) : base(action) { }
        public string Access { get; set; }
        public override IPermission CreatePermission() => null;
    }
}

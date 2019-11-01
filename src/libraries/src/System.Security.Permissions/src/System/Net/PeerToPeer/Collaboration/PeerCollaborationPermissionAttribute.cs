// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Net.PeerToPeer.Collaboration
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
       AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class PeerCollaborationPermissionAttribute : CodeAccessSecurityAttribute
    {
        public PeerCollaborationPermissionAttribute(SecurityAction action) : base(action) { }
        public override IPermission CreatePermission() { return null; }
    }
}

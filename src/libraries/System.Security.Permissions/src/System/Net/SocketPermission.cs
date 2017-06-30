// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Net
{
    public sealed class SocketPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public const int AllPorts = -1;
        public SocketPermission(NetworkAccess access, TransportType transport, string hostName, int portNumber) { }
        public SocketPermission(PermissionState state) { }
        public System.Collections.IEnumerator AcceptList { get { return null; } }
        public System.Collections.IEnumerator ConnectList { get { return null; } }
        public void AddPermission(NetworkAccess access, TransportType transport, string hostName, int portNumber) { }
        public override IPermission Copy() { return null; }
        public override void FromXml(SecurityElement securityElement) { }
        public override IPermission Intersect(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) => false;
        public bool IsUnrestricted() => false;
        public override SecurityElement ToXml() { return null; }
        public override IPermission Union(IPermission target) { return null; }
    }
}

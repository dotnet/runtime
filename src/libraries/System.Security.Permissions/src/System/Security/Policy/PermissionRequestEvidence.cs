// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class PermissionRequestEvidence : System.Security.Policy.EvidenceBase
    {
        public PermissionRequestEvidence(System.Security.PermissionSet request, System.Security.PermissionSet optional, System.Security.PermissionSet denied) { }
        public System.Security.PermissionSet DeniedPermissions { get { return default(System.Security.PermissionSet); } }
        public System.Security.PermissionSet OptionalPermissions { get { return default(System.Security.PermissionSet); } }
        public System.Security.PermissionSet RequestedPermissions { get { return default(System.Security.PermissionSet); } }
        public System.Security.Policy.PermissionRequestEvidence Copy() { return default(System.Security.Policy.PermissionRequestEvidence); }
        public override string ToString() => base.ToString();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    [Obsolete("This type is obsolete. See https://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
    public sealed partial class PermissionRequestEvidence : EvidenceBase
    {
        public PermissionRequestEvidence(PermissionSet request, PermissionSet optional, PermissionSet denied) { }
        public PermissionSet DeniedPermissions { get { return default(PermissionSet); } }
        public PermissionSet OptionalPermissions { get { return default(PermissionSet); } }
        public PermissionSet RequestedPermissions { get { return default(PermissionSet); } }
        public PermissionRequestEvidence Copy() { return default(PermissionRequestEvidence); }
        public override string ToString() => base.ToString();
    }
}

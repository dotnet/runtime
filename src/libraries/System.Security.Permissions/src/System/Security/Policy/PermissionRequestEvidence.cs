// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Policy
{
    [Obsolete("Code Access Security is not supported or honored by the runtime.")]
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed class DataProtectionPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public DataProtectionPermission(PermissionState state) { }
        public DataProtectionPermission(DataProtectionPermissionFlags flag) { }
        public bool IsUnrestricted() => false;
        public DataProtectionPermissionFlags Flags { get; set; }
        public override IPermission Copy() { return null; }
        public override IPermission Union(IPermission target) { return null; }
        public override IPermission Intersect(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) => false;
        public override void FromXml(SecurityElement securityElement) { }
        public override SecurityElement ToXml() { return null; }
    }
}
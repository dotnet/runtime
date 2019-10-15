// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed class KeyContainerPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public KeyContainerPermission(PermissionState state) { }
        public KeyContainerPermission(KeyContainerPermissionFlags flags) { }
        public KeyContainerPermission(KeyContainerPermissionFlags flags, KeyContainerPermissionAccessEntry[] accessList) { }
        public KeyContainerPermissionFlags Flags { get; }
        public KeyContainerPermissionAccessEntryCollection AccessEntries { get; }
        public bool IsUnrestricted() { return false; }
        private bool IsEmpty() { return false; }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public override IPermission Intersect(IPermission target) { return null; }
        public override IPermission Union(IPermission target) { return null; }
        public override IPermission Copy() { return null; }
        public override SecurityElement ToXml() { return null; }
        public override void FromXml(SecurityElement securityElement) { }
    }
}
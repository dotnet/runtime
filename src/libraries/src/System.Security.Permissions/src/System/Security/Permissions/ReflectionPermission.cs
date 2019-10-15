// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed partial class ReflectionPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public ReflectionPermission(PermissionState state) { }
        public ReflectionPermission(ReflectionPermissionFlag flag) { }
        public ReflectionPermissionFlag Flags { get; set; }
        public override IPermission Copy() { return this; }
        public override void FromXml(SecurityElement esd) { }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission other) { return default(IPermission); }
    }
}

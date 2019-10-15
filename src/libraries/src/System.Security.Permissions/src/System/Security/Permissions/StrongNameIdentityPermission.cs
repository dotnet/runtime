// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed partial class StrongNameIdentityPermission : CodeAccessPermission
    {
        public StrongNameIdentityPermission(PermissionState state) { }
        public StrongNameIdentityPermission(StrongNamePublicKeyBlob blob, string name, Version version) { }
        public string Name { get; set; }
        public StrongNamePublicKeyBlob PublicKey { get; set; }
        public Version Version { get; set; }
        public override IPermission Copy() { return this; }
        public override void FromXml(SecurityElement e) { }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission target) { return default(IPermission); }
    }
}

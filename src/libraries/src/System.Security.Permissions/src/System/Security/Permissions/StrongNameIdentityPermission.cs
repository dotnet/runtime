// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed partial class StrongNameIdentityPermission : System.Security.CodeAccessPermission
    {
        public StrongNameIdentityPermission(System.Security.Permissions.PermissionState state) { }
        public StrongNameIdentityPermission(System.Security.Permissions.StrongNamePublicKeyBlob blob, string name, System.Version version) { }
        public string Name { get; set; }
        public System.Security.Permissions.StrongNamePublicKeyBlob PublicKey { get; set; }
        public System.Version Version { get; set; }
        public override System.Security.IPermission Copy() { return this; }
        public override void FromXml(SecurityElement e) { }
        public override System.Security.IPermission Intersect(System.Security.IPermission target) { return default(System.Security.IPermission); }
        public override bool IsSubsetOf(System.Security.IPermission target) { return false; }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override System.Security.IPermission Union(System.Security.IPermission target) { return default(System.Security.IPermission); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public sealed partial class NamedPermissionSet : System.Security.PermissionSet
    {
        public NamedPermissionSet(System.Security.NamedPermissionSet permSet) : base(default(System.Security.Permissions.PermissionState)) { }
        public NamedPermissionSet(string name) : base(default(System.Security.Permissions.PermissionState)) { }
        public NamedPermissionSet(string name, System.Security.Permissions.PermissionState state) : base(default(System.Security.Permissions.PermissionState)) { }
        public NamedPermissionSet(string name, System.Security.PermissionSet permSet) : base(default(System.Security.Permissions.PermissionState)) { }
        public string Description { get; set; }
        public string Name { get; set; }
        public override System.Security.PermissionSet Copy() { return default(System.Security.PermissionSet); }
        public System.Security.NamedPermissionSet Copy(string name) { return default(System.Security.NamedPermissionSet); }
        public override bool Equals(object o) => base.Equals(o);
        public override void FromXml(SecurityElement et) { }
        public override int GetHashCode() => base.GetHashCode();
        public override SecurityElement ToXml() { return default(SecurityElement); }
    }
}

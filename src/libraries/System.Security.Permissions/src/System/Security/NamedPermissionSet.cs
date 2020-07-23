// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.Security
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class NamedPermissionSet : PermissionSet
    {
        public NamedPermissionSet(NamedPermissionSet permSet) : base(default(PermissionState)) { }
        public NamedPermissionSet(string name) : base(default(PermissionState)) { }
        public NamedPermissionSet(string name, PermissionState state) : base(default(PermissionState)) { }
        public NamedPermissionSet(string name, PermissionSet permSet) : base(default(PermissionState)) { }
        public string Description { get; set; }
        public string Name { get; set; }
        public override PermissionSet Copy() { return default(PermissionSet); }
        public NamedPermissionSet Copy(string name) { return default(NamedPermissionSet); }
        public override bool Equals(object o) => base.Equals(o);
        public override void FromXml(SecurityElement et) { }
        public override int GetHashCode() => base.GetHashCode();
        public override SecurityElement ToXml() { return default(SecurityElement); }
    }
}

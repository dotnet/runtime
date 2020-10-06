// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Xaml.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class XamlLoadPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public XamlLoadPermission(PermissionState state) { }
        public XamlLoadPermission(XamlAccessLevel allowedAccess) { }
        public XamlLoadPermission(IEnumerable<XamlAccessLevel> allowedAccess) { }
        [ComVisible(false)]
        public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
        [ComVisible(false)]
        public override int GetHashCode() { return base.GetHashCode(); }
        public IList<XamlAccessLevel> AllowedAccess { get; private set; } = new ReadOnlyCollection<XamlAccessLevel>(Array.Empty<XamlAccessLevel>());
        public override IPermission Copy() { return new XamlLoadPermission(PermissionState.Unrestricted); }
        public override void FromXml(SecurityElement elem) { }
        public bool Includes(XamlAccessLevel requestedAccess) { return true; }
        public override IPermission Intersect(IPermission target) { return new XamlLoadPermission(PermissionState.Unrestricted); }
        public override bool IsSubsetOf(IPermission target) { return true; }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission other) { return new XamlLoadPermission(PermissionState.Unrestricted); }
        public bool IsUnrestricted() { return true; }
    }
}

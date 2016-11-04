// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    public sealed partial class UIPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public UIPermission(PermissionState state) { }
        public UIPermission(UIPermissionClipboard clipboardFlag) { }
        public UIPermission(UIPermissionWindow windowFlag) { }
        public UIPermission(UIPermissionWindow windowFlag, UIPermissionClipboard clipboardFlag) { }
        public UIPermissionClipboard Clipboard { get; set; }
        public UIPermissionWindow Window { get; set; }
        public override IPermission Copy() { return this; }
        public override void FromXml(SecurityElement esd) { }
        public override IPermission Intersect(IPermission target) { return default(IPermission); }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override IPermission Union(IPermission target) { return default(IPermission); }
    }
}

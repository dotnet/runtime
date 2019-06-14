// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace System.Security.Permissions
{
    public enum WebBrowserPermissionLevel
    {
        None,
        Safe,
        Unrestricted
    }
    sealed public class WebBrowserPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public WebBrowserPermission() { }
        public WebBrowserPermission(PermissionState state) { }
        public WebBrowserPermission(WebBrowserPermissionLevel webBrowserPermissionLevel) { }
        public bool IsUnrestricted() { return true; }
        public override bool IsSubsetOf(IPermission target) { return true; }
        public override IPermission Intersect(IPermission target) { return new WebBrowserPermission(); }
        public override IPermission Union(IPermission target) { return new WebBrowserPermission(); }
        public override IPermission Copy() { return new WebBrowserPermission(); }
        public override SecurityElement ToXml() { return default(SecurityElement); }
        public override void FromXml(SecurityElement securityElement) { }
        public WebBrowserPermissionLevel Level { get { return WebBrowserPermissionLevel.Unrestricted; } set { } }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    sealed public class WebBrowserPermissionAttribute : CodeAccessSecurityAttribute
    {
        public WebBrowserPermissionAttribute(SecurityAction action) : base(action) { }
        public override IPermission CreatePermission() { return new WebBrowserPermission(); }
        public WebBrowserPermissionLevel Level { get { return WebBrowserPermissionLevel.Unrestricted; } set { } }
    }
}

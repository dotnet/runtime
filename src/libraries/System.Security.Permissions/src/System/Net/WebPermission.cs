// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Security;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace System.Net
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class WebPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public WebPermission() { }
        public WebPermission(NetworkAccess access, string uriString) { }
        public WebPermission(NetworkAccess access, Regex uriRegex) { }
        public WebPermission(PermissionState state) { }
        public IEnumerator AcceptList { get { return null; } }
        public IEnumerator ConnectList { get { return null; } }
        public void AddPermission(NetworkAccess access, string uriString) { }
        public void AddPermission(NetworkAccess access, Regex uriRegex) { }
        public override IPermission Copy() { return null; }
        public override void FromXml(SecurityElement securityElement) { }
        public override IPermission Intersect(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) => false;
        public bool IsUnrestricted() => false;
        public override SecurityElement ToXml() { return null; }
        public override IPermission Union(IPermission target) { return null; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.Security.Policy
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed partial class StrongName : EvidenceBase, IIdentityPermissionFactory
    {
        public StrongName(StrongNamePublicKeyBlob blob, string name, Version version) { }
        public string Name { get { return null; } }
        public StrongNamePublicKeyBlob PublicKey { get { return default(StrongNamePublicKeyBlob); } }
        public Version Version { get { return default(Version); } }
        public object Copy() { return null; }
        public IPermission CreateIdentityPermission(Evidence evidence) { return default(IPermission); }
        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
    }
}

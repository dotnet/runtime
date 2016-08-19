// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class Publisher : System.Security.Policy.EvidenceBase, System.Security.Policy.IIdentityPermissionFactory
    {
        public Publisher(System.Security.Cryptography.X509Certificates.X509Certificate cert) { }
        public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get { return default(System.Security.Cryptography.X509Certificates.X509Certificate); } }
        public object Copy() { return null; }
        public System.Security.IPermission CreateIdentityPermission(System.Security.Policy.Evidence evidence) { return default(System.Security.IPermission); }
        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
    }
}

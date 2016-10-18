// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace System.Security.Policy
{
    [Serializable]
    public sealed partial class Publisher : EvidenceBase, IIdentityPermissionFactory
    {
        public Publisher(X509Certificate cert) { }
        public X509Certificate Certificate { get { return default(X509Certificate); } }
        public object Copy() { return null; }
        public IPermission CreateIdentityPermission(Evidence evidence) { return default(IPermission); }
        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
    }
}

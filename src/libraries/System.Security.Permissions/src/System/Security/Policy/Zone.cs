// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class Zone : EvidenceBase, IIdentityPermissionFactory
    {
        public Zone(SecurityZone zone) { }
        public SecurityZone SecurityZone { get { return default(SecurityZone); } }
        public object Copy() { return null; }
        public static Zone CreateFromUrl(string url) { return default(Zone); }
        public IPermission CreateIdentityPermission(Evidence evidence) { return default(IPermission); }
        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
    }
}

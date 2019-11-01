// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Data.OracleClient
{
    public sealed class OraclePermission : CodeAccessPermission, IUnrestrictedPermission
    {
        public OraclePermission(PermissionState state) { }
        public bool AllowBlankPassword { get; set; }
        public void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior) { }
        public override IPermission Copy() { return null; }
        public override void FromXml(SecurityElement securityElement) { }
        public override IPermission Intersect(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) => false;
        public bool IsUnrestricted() => false;
        public override SecurityElement ToXml() { return null; }
        public override IPermission Union(IPermission target) { return null; }
    }
}

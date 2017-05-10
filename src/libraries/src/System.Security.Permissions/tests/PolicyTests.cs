// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class PolicyTests
    {
        [Fact]
        public static void PolicyExceptionCallMethods()
        {
            PolicyException pe = new PolicyException();
            pe = new PolicyException("test");
        }

        [Fact]
        public static void PolicyLevelCallMethods()
        {
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            NamedPermissionSet nps = new NamedPermissionSet("test");
            pl.AddNamedPermissionSet(nps);
            nps = pl.ChangeNamedPermissionSet("test", new PermissionSet(new Permissions.PermissionState()));
            PolicyLevel.CreateAppDomainLevel();
            nps = pl.GetNamedPermissionSet("test");
            pl.Recover();
            NamedPermissionSet nps2 = pl.RemoveNamedPermissionSet(nps);
            nps2 = pl.RemoveNamedPermissionSet("test");
            pl.Reset();
            Evidence evidence = new Evidence();
            PolicyStatement ps = pl.Resolve(evidence);
            CodeGroup cg = pl.ResolveMatchingCodeGroups(evidence);
            SecurityElement se = new SecurityElement("");
            pl.FromXml(se);
            se = pl.ToXml();
        }

        [Fact]
        public static void PolicyStatementCallMethods()
        {
            PolicyStatement ps = new PolicyStatement(new PermissionSet(new PermissionState()));
            PolicyStatement ps2 = ps.Copy();
            bool equals = ps.Equals(ps2);
            int hash = ps.GetHashCode();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            ps.FromXml(se);
            ps.FromXml(se, pl);
            se = ps.ToXml();
            se = ps.ToXml(pl);
        }
    }
}

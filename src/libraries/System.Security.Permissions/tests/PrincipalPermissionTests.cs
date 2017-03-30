// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class PrincipalPermissionTest
    {
        [Fact]
        public void PermissionStateNone()
        {
            PrincipalPermission p = new PrincipalPermission(PermissionState.None);
            const string className = "System.Security.Permissions.PrincipalPermission, ";
            Assert.True(!p.IsUnrestricted());
            PrincipalPermission copy = (PrincipalPermission)p.Copy();
            Assert.Equal(p.IsUnrestricted(), copy.IsUnrestricted());
            SecurityElement se = p.ToXml();
            Assert.True((se.Attributes["class"] as string).StartsWith(className));
            Assert.Equal("1", (se.Attributes["version"] as string));
        }

        [Fact]
        public void PermissionStateUnrestricted()
        {
            PrincipalPermission p = new PrincipalPermission(PermissionState.Unrestricted);
            Assert.True(p.IsUnrestricted());
            PrincipalPermission copy = (PrincipalPermission)p.Copy();
            Assert.Equal(p.IsUnrestricted(), copy.IsUnrestricted());
            // Note: Unrestricted isn't shown in XML
        }

        [Fact]
        public void Name()
        {
            const string userName = "UniqueUserName1";
            PrincipalPermission p = new PrincipalPermission(userName, null);
            Assert.True(!p.IsUnrestricted());
            SecurityElement pele = (SecurityElement)(p.ToXml().Children[0]);
            Assert.Contains(userName, pele.ToString());
        }

        [Fact]
        public void UnauthenticatedName()
        {
            const string userName = "UniqueUserName1";
            PrincipalPermission p = new PrincipalPermission(userName, null, false);
            Assert.True(!p.IsUnrestricted());
            SecurityElement pele = (SecurityElement)(p.ToXml().Children[0]);
            Assert.Contains(userName, pele.ToString());
        }

        [Fact]
        public void Role()
        {
            const string roleName = "ThisIsARoleName";
            PrincipalPermission p = new PrincipalPermission(null, roleName);
            Assert.True(!p.IsUnrestricted());
            SecurityElement pele = (SecurityElement)(p.ToXml().Children[0]);
            Assert.Contains(roleName, pele.ToString());
        }

        [Fact]
        public void UnauthenticatedRole()
        {
            const string roleName = "ThisIsARoleName";
            PrincipalPermission p = new PrincipalPermission(null, roleName, false);
            Assert.True(!p.IsUnrestricted());
            SecurityElement pele = (SecurityElement)(p.ToXml().Children[0]);
            Assert.Contains(roleName, pele.ToString());
        }

        [Fact]
        public void NameRole()
        {
            const string userName = "UniqueUserName1";
            const string roleName = "ThisIsARoleName";
            PrincipalPermission p = new PrincipalPermission(userName, roleName);
            Assert.True(!p.IsUnrestricted());
            SecurityElement pele = (SecurityElement)(p.ToXml().Children[0]);
            Assert.Contains(userName, pele.ToString());
            Assert.Contains(roleName, pele.ToString());
        }

        [Fact]
        public void UnauthenticatedNameRole()
        {
            const string userName = "UniqueUserName1";
            const string roleName = "ThisIsARoleName";
            PrincipalPermission p = new PrincipalPermission(userName, roleName, false);
            Assert.True(!p.IsUnrestricted());
            SecurityElement pele = (SecurityElement)(p.ToXml().Children[0]);
            Assert.Contains(userName, pele.ToString());
            Assert.Contains(roleName, pele.ToString());
        }

        [Fact]
        public void AuthenticatedNullNull()
        {
            PrincipalPermission p = new PrincipalPermission(null, null, true);
            Assert.True(p.IsUnrestricted());
        }

        [Fact]
        public void FromXmlNull()
        {
            PrincipalPermission p = new PrincipalPermission(PermissionState.None);
            Assert.Throws<ArgumentNullException>(() => p.FromXml(null));
        }

        [Fact]
        public void FromXmlInvalidPermission()
        {
            PrincipalPermission p = new PrincipalPermission(PermissionState.None);
            SecurityElement se = p.ToXml();
            // can't modify - so we create our own
            SecurityElement se2 = new SecurityElement("IInvalidPermission", se.Text);
            se2.AddAttribute("class", se.Attribute("class"));
            se2.AddAttribute("version", se.Attribute("version"));
            Assert.Throws<ArgumentException>(() => p.FromXml(se2));
        }

        [Fact]
        public void FromXmlWrongVersion()
        {
            PrincipalPermission p = new PrincipalPermission(PermissionState.None);
            SecurityElement se = p.ToXml();
            // can't modify - so we create our own
            SecurityElement se2 = new SecurityElement(se.Tag, se.Text);
            se2.AddAttribute("class", se.Attribute("class"));
            se2.AddAttribute("version", "2");
            Assert.Throws<ArgumentException>(() => p.FromXml(se2));
        }

        [Fact]
        public void FromXml()
        {
            PrincipalPermission p = new PrincipalPermission(PermissionState.None);
            SecurityElement se = p.ToXml();
            Assert.NotNull(se);

            PrincipalPermission p2 = (PrincipalPermission)p.Copy();
            p2.FromXml(se);
            Assert.Equal(p.ToString(), p2.ToString());

            string className = (string)se.Attributes["class"];
            string version = (string)se.Attributes["version"];

            SecurityElement se2 = new SecurityElement(se.Tag);
            se2.AddAttribute("class", className);
            se2.AddAttribute("version", version);
            p2.FromXml(se2);

            SecurityElement sec = new SecurityElement("Identity");
            sec.AddAttribute("Authenticated", "true");
            se2.AddChild(sec);
            p2.FromXml(se2);
            Assert.True(p2.IsUnrestricted());
        }

        [Fact]
        public void UnionWithNull()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", null);
            PrincipalPermission p2 = null;
            PrincipalPermission p3 = (PrincipalPermission)p1.Union(p2);
            Assert.Equal(p1.ToXml().ToString(), p3.ToXml().ToString());
        }

        [Fact]
        public void UnionWithUnrestricted()
        {
            PrincipalPermission p1 = new PrincipalPermission(PermissionState.Unrestricted);
            PrincipalPermission p2 = new PrincipalPermission("user", "role");
            PrincipalPermission p3 = (PrincipalPermission)p1.Union(p2);
            Assert.True(p3.IsUnrestricted());
            p3 = (PrincipalPermission)p2.Union(p1);
            Assert.True(p3.IsUnrestricted());
        }

        [Fact]
        public void Union()
        {
            PrincipalPermission p1 = new PrincipalPermission("user A", "role A");
            PrincipalPermission p2 = new PrincipalPermission("user B", "role B", false);
            PrincipalPermission p3 = (PrincipalPermission)p1.Union(p2);
            string union = p3.ToString();
            Assert.Contains("user A", union);
            Assert.Contains("user B", union);
            Assert.Contains("role A", union);
            Assert.Contains("role B", union);
        }

        [Fact]
        public void UnionWithBadPermission()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", null);
            EnvironmentPermission ep2 = new EnvironmentPermission(PermissionState.Unrestricted);
            Assert.Throws<ArgumentException>(() => p1.Union(ep2));
        }

        [Fact]
        public void IntersectWithNull()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", "role");
            PrincipalPermission p2 = null;
            PrincipalPermission p3 = (PrincipalPermission)p1.Intersect(p2);
            Assert.Null(p3);
        }

        [Fact]
        public void IntersectWithUnrestricted()
        {
            PrincipalPermission p1 = new PrincipalPermission(PermissionState.Unrestricted);
            PrincipalPermission p2 = new PrincipalPermission("user", "role");
            PrincipalPermission p3 = (PrincipalPermission)p1.Intersect(p2);
            Assert.True(!p3.IsUnrestricted());
            Assert.Equal(p2.ToXml().ToString(), p3.ToXml().ToString());
            p3 = (PrincipalPermission)p2.Intersect(p1);
            Assert.True(!p3.IsUnrestricted());
            Assert.Equal(p2.ToXml().ToString(), p3.ToXml().ToString());
        }

        [Fact]
        public void Intersect_NoIntersection()
        {
            // no intersection
            PrincipalPermission p1 = new PrincipalPermission("user A", "role 1");
            PrincipalPermission p2 = new PrincipalPermission("user B", "role 2");
            PrincipalPermission p3 = (PrincipalPermission)p1.Intersect(p2);
            Assert.Null(p3);
        }

        [Fact]
        public void Intersect_IntersectionInRole()
        {
            PrincipalPermission p1 = new PrincipalPermission("user A", "role 1");
            PrincipalPermission p2 = new PrincipalPermission("user C", "role 1");
            PrincipalPermission p3 = (PrincipalPermission)p2.Intersect(p1);
            Assert.NotNull(p3);

            string intersection = p3.ToString();
            Assert.DoesNotContain("user A", intersection);
            Assert.DoesNotContain("user C", intersection);
            Assert.Contains("role 1", intersection);
        }

        [Fact]
        public void Intersect_IntersectionInRoleWithoutAuthentication()
        {
            PrincipalPermission p1 = new PrincipalPermission("user A", "role 1");
            PrincipalPermission p2 = new PrincipalPermission("user C", "role 1", false);
            PrincipalPermission p3 = (PrincipalPermission)p2.Intersect(p1);
            Assert.Null(p3);
        }

        [Fact]
        public void IntersectNullName()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", "role");
            PrincipalPermission p2 = new PrincipalPermission(null, "role");
            PrincipalPermission p3 = (PrincipalPermission)p1.Intersect(p2);
            Assert.Equal(p1.ToString(), p3.ToString());
            p3 = (PrincipalPermission)p2.Intersect(p1);
            Assert.Equal(p1.ToString(), p3.ToString());
        }

        [Fact]
        public void IntersectNullRole()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", "role");
            PrincipalPermission p2 = new PrincipalPermission("user", null);
            PrincipalPermission p3 = (PrincipalPermission)p1.Intersect(p2);
            Assert.Equal(p1.ToString(), p3.ToString());
            p3 = (PrincipalPermission)p2.Intersect(p1);
            Assert.Equal(p1.ToString(), p3.ToString());
        }

        [Fact]
        public void IntersectWithBadPermission()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", null);
            EnvironmentPermission ep2 = new EnvironmentPermission(PermissionState.Unrestricted);
            Assert.Throws<ArgumentException>(() => p1.Intersect(ep2));
        }

        [Fact]
        public void IsSubsetOfNull()
        {
            PrincipalPermission p = new PrincipalPermission("user", null);
            Assert.True(!p.IsSubsetOf(null));

            p = new PrincipalPermission(PermissionState.None);
            Assert.True(p.IsSubsetOf(null));

            p = new PrincipalPermission(PermissionState.Unrestricted);
            Assert.True(!p.IsSubsetOf(null));
        }

        [Fact]
        public void IsSubsetOfNone()
        {
            PrincipalPermission none = new PrincipalPermission(PermissionState.None);
            PrincipalPermission p = new PrincipalPermission("user", null);
            Assert.True(!p.IsSubsetOf(none));

            p = new PrincipalPermission(PermissionState.None);
            Assert.True(p.IsSubsetOf(none));

            p = new PrincipalPermission(PermissionState.Unrestricted);
            Assert.True(!p.IsSubsetOf(none));
        }

        [Fact]
        public void IsSubsetOfUnrestricted()
        {
            PrincipalPermission p1 = new PrincipalPermission(PermissionState.Unrestricted);
            PrincipalPermission p2 = new PrincipalPermission("user", "role", false);
            Assert.True(!p1.IsSubsetOf(p2));
            Assert.True(p2.IsSubsetOf(p1));
        }

        [Fact]
        public void IsSubsetOf()
        {
            PrincipalPermission p1 = new PrincipalPermission("user A", "role 1");
            PrincipalPermission p2 = new PrincipalPermission(null, "role 1");
            Assert.True(p1.IsSubsetOf(p2));
            Assert.True(!p2.IsSubsetOf(p1));

            PrincipalPermission p3 = new PrincipalPermission("user A", "role 1", false);
            Assert.True(!p3.IsSubsetOf(p1));
            Assert.True(!p1.IsSubsetOf(p3));

            PrincipalPermission p4 = new PrincipalPermission(null, null, true); // unrestricted
            Assert.True(!p4.IsSubsetOf(p1));
            Assert.True(p1.IsSubsetOf(p4));

            PrincipalPermission p5 = new PrincipalPermission("user A", null);
            Assert.True(p1.IsSubsetOf(p5));
            Assert.True(!p5.IsSubsetOf(p1));
        }

        [Fact]
        public void IsSubsetOfBadPermission()
        {
            PrincipalPermission p1 = new PrincipalPermission("user", null);
            EnvironmentPermission ep2 = new EnvironmentPermission(PermissionState.Unrestricted);
            Assert.Throws<ArgumentException>(() => p1.IsSubsetOf(ep2));
        }
    }
}
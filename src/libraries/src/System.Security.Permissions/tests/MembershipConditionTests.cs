// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class MembershipConditionTests
    {
        [Fact]
        public static void AllMembershipConditionCallMethods()
        {
            Policy.AllMembershipCondition amc = new Policy.AllMembershipCondition();
            bool check = amc.Check(new Policy.Evidence());
            Policy.IMembershipCondition imc = amc.Copy();
            check = amc.Equals(new object());
            int hash = amc.GetHashCode();
            string str = amc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            amc.FromXml(se);
            amc.FromXml(se, pl);
            se = amc.ToXml();
            se = amc.ToXml(pl);
        }
        [Fact]
        public static void ApplicationDirectoryMembershipConditionCallMethods()
        {
            Policy.ApplicationDirectoryMembershipCondition admc = new Policy.ApplicationDirectoryMembershipCondition();
            bool check = admc.Check(new Policy.Evidence());
            Policy.IMembershipCondition obj = admc.Copy();
            check = admc.Equals(new object());
            int hash = admc.GetHashCode();
            string str = admc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            admc.FromXml(se);
            admc.FromXml(se, pl);
            se = admc.ToXml();
            se = admc.ToXml(pl);
        }
        [Fact]
        public static void GacMembershipConditionCallMethods()
        {
            Policy.GacMembershipCondition gmc = new Policy.GacMembershipCondition();
            bool check = gmc.Check(new Policy.Evidence());
            Policy.IMembershipCondition obj = gmc.Copy();
            check = gmc.Equals(new object());
            int hash = gmc.GetHashCode();
            string str = gmc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            gmc.FromXml(se);
            gmc.FromXml(se, pl);
            se = gmc.ToXml();
            se = gmc.ToXml(pl);
        }
        [Fact]
        public static void HashMembershipConditionCallMethods()
        {
            Policy.HashMembershipCondition hmc = new Policy.HashMembershipCondition(Cryptography.SHA1.Create(), new byte[1]);
            bool check = hmc.Check(new Policy.Evidence());
            Policy.IMembershipCondition obj = hmc.Copy();
            check = hmc.Equals(new object());
            int hash = hmc.GetHashCode();
            string str = hmc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            hmc.FromXml(se);
            hmc.FromXml(se, pl);
            se = hmc.ToXml();
            se = hmc.ToXml(pl);
        }
        [Fact]
        public static void PublisherMembershipConditionCallMethods()
        {
            Policy.PublisherMembershipCondition pmc = new Policy.PublisherMembershipCondition(new System.Security.Cryptography.X509Certificates.X509Certificate());
            bool check = pmc.Check(new Policy.Evidence());
            Policy.IMembershipCondition obj = pmc.Copy();
            check = pmc.Equals(new object());
            int hash = pmc.GetHashCode();
            string str = pmc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            pmc.FromXml(se);
            pmc.FromXml(se, pl);
            se = pmc.ToXml();
            se = pmc.ToXml(pl);
        }
        [Fact]
        public static void SiteMembershipConditionCallMethods()
        {
            Policy.SiteMembershipCondition smc = new Policy.SiteMembershipCondition("test");
            bool check = smc.Check(new Policy.Evidence());
            Policy.IMembershipCondition obj = smc.Copy();
            check = smc.Equals(new object());
            int hash = smc.GetHashCode();
            string str = smc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            smc.FromXml(se);
            smc.FromXml(se, pl);
            se = smc.ToXml();
            se = smc.ToXml(pl);
        }
        [Fact]
        public static void StrongNameMembershipConditionCallMethods()
        {
            Policy.StrongNameMembershipCondition snmc = new Policy.StrongNameMembershipCondition(new StrongNamePublicKeyBlob(new byte[1]), "test", new Version(0, 1));
            bool check = snmc.Check(new Policy.Evidence());
            Policy.IMembershipCondition obj = snmc.Copy();
            check = snmc.Equals(new object());
            int hash = snmc.GetHashCode();
            string str = snmc.ToString();
            SecurityElement se = new SecurityElement("");
            Policy.PolicyLevel pl = (Policy.PolicyLevel)Activator.CreateInstance(typeof(Policy.PolicyLevel), true);
            snmc.FromXml(se);
            snmc.FromXml(se, pl);
            se = snmc.ToXml();
            se = snmc.ToXml(pl);
        }
    }
}

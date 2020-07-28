// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class MembershipConditionTests
    {
        [Fact]
        public static void AllMembershipConditionCallMethods()
        {
            AllMembershipCondition amc = new AllMembershipCondition();
            bool check = amc.Check(new Evidence());
            IMembershipCondition imc = amc.Copy();
            check = amc.Equals(new object());
            int hash = amc.GetHashCode();
            string str = amc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            amc.FromXml(se);
            amc.FromXml(se, pl);
            se = amc.ToXml();
            se = amc.ToXml(pl);
        }

        [Fact]
        public static void ApplicationDirectoryMembershipConditionCallMethods()
        {
            ApplicationDirectoryMembershipCondition admc = new ApplicationDirectoryMembershipCondition();
            bool check = admc.Check(new Evidence());
            IMembershipCondition obj = admc.Copy();
            check = admc.Equals(new object());
            int hash = admc.GetHashCode();
            string str = admc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            admc.FromXml(se);
            admc.FromXml(se, pl);
            se = admc.ToXml();
            se = admc.ToXml(pl);
        }

        [Fact]
        public static void GacMembershipConditionCallMethods()
        {
            GacMembershipCondition gmc = new GacMembershipCondition();
            bool check = gmc.Check(new Evidence());
            IMembershipCondition obj = gmc.Copy();
            check = gmc.Equals(new object());
            int hash = gmc.GetHashCode();
            string str = gmc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            gmc.FromXml(se);
            gmc.FromXml(se, pl);
            se = gmc.ToXml();
            se = gmc.ToXml(pl);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Browser)] // System.Security.Cryptography.Algorithms is not supported on this platform.
        public static void HashMembershipConditionCallMethods()
        {
            HashMembershipCondition hmc = new HashMembershipCondition(Cryptography.SHA1.Create(), new byte[1]);
            bool check = hmc.Check(new Evidence());
            IMembershipCondition obj = hmc.Copy();
            check = hmc.Equals(new object());
            int hash = hmc.GetHashCode();
            string str = hmc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            hmc.FromXml(se);
            hmc.FromXml(se, pl);
            se = hmc.ToXml();
            se = hmc.ToXml(pl);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Browser)] // System.Security.Cryptography.X509Certificates is not supported on this platform.
        public static void PublisherMembershipConditionCallMethods()
        {
            PublisherMembershipCondition pmc = new PublisherMembershipCondition(new System.Security.Cryptography.X509Certificates.X509Certificate());
            bool check = pmc.Check(new Evidence());
            IMembershipCondition obj = pmc.Copy();
            check = pmc.Equals(new object());
            int hash = pmc.GetHashCode();
            string str = pmc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            pmc.FromXml(se);
            pmc.FromXml(se, pl);
            se = pmc.ToXml();
            se = pmc.ToXml(pl);
        }

        [Fact]
        public static void SiteMembershipConditionCallMethods()
        {
            SiteMembershipCondition smc = new SiteMembershipCondition("test");
            bool check = smc.Check(new Evidence());
            IMembershipCondition obj = smc.Copy();
            check = smc.Equals(new object());
            int hash = smc.GetHashCode();
            string str = smc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            smc.FromXml(se);
            smc.FromXml(se, pl);
            se = smc.ToXml();
            se = smc.ToXml(pl);
        }

        [Fact]
        public static void StrongNameMembershipConditionCallMethods()
        {
            StrongNameMembershipCondition snmc = new StrongNameMembershipCondition(new StrongNamePublicKeyBlob(new byte[1]), "test", new Version(0, 1));
            bool check = snmc.Check(new Evidence());
            IMembershipCondition obj = snmc.Copy();
            check = snmc.Equals(new object());
            int hash = snmc.GetHashCode();
            string str = snmc.ToString();
            SecurityElement se = new SecurityElement("");
            PolicyLevel pl = (PolicyLevel)FormatterServices.GetUninitializedObject(typeof(PolicyLevel));
            snmc.FromXml(se);
            snmc.FromXml(se, pl);
            se = snmc.ToXml();
            se = snmc.ToXml(pl);
        }
    }
}

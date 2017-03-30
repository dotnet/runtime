// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class EvidenceBaseTests
    {
        [Fact]
        public static void ApplicationDirectoryCallMethods()
        {
            ApplicationDirectory ad = new ApplicationDirectory("test");
            object obj = ad.Copy();
            bool check = ad.Equals(new object());
            int hash = ad.GetHashCode();
            string str = ad.ToString();
        }

        [Fact]
        public static void ApplicationTrustCallMethods()
        {
            ApplicationTrust at = new ApplicationTrust();
            SecurityElement se = new SecurityElement("");
            at.FromXml(se);
            se = at.ToXml();
        }

        [Fact]
        public static void GacInstalledCallMethods()
        {
            GacInstalled gi = new GacInstalled();
            object obj = gi.Copy();
            IPermission ip = gi.CreateIdentityPermission(new Evidence());
            bool check = gi.Equals(new object());
            int hash = gi.GetHashCode();
            string str = gi.ToString();
        }

        [Fact]
        public static void HashCallMethods()
        {
            Hash hash = new Hash(Reflection.Assembly.Load(new Reflection.AssemblyName("System.Reflection")));
            byte[] barr = hash.GenerateHash(Cryptography.SHA1.Create());
            string str = hash.ToString();
            hash = Hash.CreateMD5(new byte[1]);
            hash = Hash.CreateSHA1(new byte[1]);
        }

        [Fact]
        public static void PermissionRequestEvidenceCallMethods()
        {
            PermissionSet ps = new PermissionSet(new PermissionState());
            PermissionRequestEvidence pre = new PermissionRequestEvidence(ps, ps, ps);
            PermissionRequestEvidence obj = pre.Copy();
            string str = ps.ToString();
            SecurityElement se = new SecurityElement("");
            ps.FromXml(se);
            se = ps.ToXml();
        }
    }
}

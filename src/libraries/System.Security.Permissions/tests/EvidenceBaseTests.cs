// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class EvidenceBaseTests
    {
        [Fact]
        public static void ApplicationDirectoryCallMethods()
        {
            Policy.ApplicationDirectory ad = new Policy.ApplicationDirectory("test");
            object obj = ad.Copy();
            bool check = ad.Equals(new object());
            int hash = ad.GetHashCode();
            string str = ad.ToString();
        }
        [Fact]
        public static void ApplicationTrustCallMethods()
        {
            Policy.ApplicationTrust at = new Policy.ApplicationTrust();
            SecurityElement se = new SecurityElement("");
            at.FromXml(se);
            se = at.ToXml();
        }
        [Fact]
        public static void GacInstalledCallMethods()
        {
            Policy.GacInstalled gi = new Policy.GacInstalled();
            object obj = gi.Copy();
            IPermission ip = gi.CreateIdentityPermission(new Policy.Evidence());
            bool check = gi.Equals(new object());
            int hash = gi.GetHashCode();
            string str = gi.ToString();
        }
        [Fact]
        public static void HashCallMethods()
        {
            Policy.Hash hash = new Policy.Hash(Reflection.Assembly.Load(new Reflection.AssemblyName("System.Reflection")));
            byte[] barr = hash.GenerateHash(Cryptography.SHA1.Create());
            string str = hash.ToString();
            hash = Policy.Hash.CreateMD5(new byte[1]);
            hash = Policy.Hash.CreateSHA1(new byte[1]);
        }
        [Fact]
        public static void PermissionRequestEvidenceCallMethods()
        {
            PermissionSet ps = new PermissionSet(new PermissionState());
            Policy.PermissionRequestEvidence pre = new Policy.PermissionRequestEvidence(ps, ps, ps);
            Policy.PermissionRequestEvidence obj = pre.Copy();
            string str = ps.ToString();
            SecurityElement se = new SecurityElement("");
            ps.FromXml(se);
            se = ps.ToXml();
        }
    }
}

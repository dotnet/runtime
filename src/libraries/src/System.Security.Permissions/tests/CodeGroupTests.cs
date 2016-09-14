// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class CodeGroupTests
    {
        [Fact]
        public static void FileCodeGroupCallMethods()
        {
            Policy.FileCodeGroup fcg = new Policy.FileCodeGroup(new Policy.GacMembershipCondition(), new FileIOPermissionAccess());
            Policy.CodeGroup cg = fcg.Copy();
            bool equals = fcg.Equals(new object());
            int hash = fcg.GetHashCode();
            Policy.PolicyStatement ps = fcg.Resolve(new Policy.Evidence());
            cg = fcg.ResolveMatchingCodeGroups(new Policy.Evidence());
        }
        [Fact]
        public static void FirstMatchCodeGroupCallMethods()
        {
            Policy.FirstMatchCodeGroup fmcg = new Policy.FirstMatchCodeGroup(new Policy.GacMembershipCondition(), new Policy.PolicyStatement(new PermissionSet(new PermissionState())));
            Policy.CodeGroup cg = fmcg.Copy();
            Policy.PolicyStatement ps = fmcg.Resolve(new Policy.Evidence());
            cg = fmcg.ResolveMatchingCodeGroups(new Policy.Evidence());
        }
        [Fact]
        public static void NetCodeGroupCallMethods()
        {
            Policy.NetCodeGroup ncg = new Policy.NetCodeGroup(new Policy.GacMembershipCondition());
            string teststring = Policy.NetCodeGroup.AbsentOriginScheme;
            teststring = Policy.NetCodeGroup.AnyOtherOriginScheme;
            ncg.AddConnectAccess("test", new Policy.CodeConnectAccess("test", 0));
            Policy.CodeGroup cg = ncg.Copy();
            bool equals = ncg.Equals(new object());
            System.Collections.DictionaryEntry[] de = ncg.GetConnectAccessRules();
            int hash = ncg.GetHashCode();
            ncg.ResetConnectAccess();
            Policy.PolicyStatement ps = ncg.Resolve(new Policy.Evidence());
            cg = ncg.ResolveMatchingCodeGroups(new Policy.Evidence());
        }
        [Fact]
        public static void UnionCodeGroupCallMethods()
        {
            Policy.UnionCodeGroup ucg = new Policy.UnionCodeGroup(new Policy.GacMembershipCondition(), new Policy.PolicyStatement(new PermissionSet(new PermissionState())));
            Policy.CodeGroup cg = ucg.Copy();
            Policy.PolicyStatement ps = ucg.Resolve(new Policy.Evidence());
            cg = ucg.ResolveMatchingCodeGroups(new Policy.Evidence());
        }
    }
}

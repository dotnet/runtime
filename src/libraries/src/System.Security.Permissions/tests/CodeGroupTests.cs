// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class CodeGroupTests
    {
        [Fact]
        public static void FileCodeGroupCallMethods()
        {
            FileCodeGroup fcg = new FileCodeGroup(new GacMembershipCondition(), new FileIOPermissionAccess());
            CodeGroup cg = fcg.Copy();
            bool equals = fcg.Equals(new object());
            int hash = fcg.GetHashCode();
            PolicyStatement ps = fcg.Resolve(new Evidence());
            cg = fcg.ResolveMatchingCodeGroups(new Evidence());
        }

        [Fact]
        public static void FirstMatchCodeGroupCallMethods()
        {
            FirstMatchCodeGroup fmcg = new FirstMatchCodeGroup(new GacMembershipCondition(), new PolicyStatement(new PermissionSet(new PermissionState())));
            CodeGroup cg = fmcg.Copy();
            PolicyStatement ps = fmcg.Resolve(new Evidence());
            cg = fmcg.ResolveMatchingCodeGroups(new Evidence());
        }

        [Fact]
        public static void NetCodeGroupCallMethods()
        {
            NetCodeGroup ncg = new NetCodeGroup(new GacMembershipCondition());
            string teststring = NetCodeGroup.AbsentOriginScheme;
            teststring = NetCodeGroup.AnyOtherOriginScheme;
            ncg.AddConnectAccess("test", new CodeConnectAccess("test", 0));
            CodeGroup cg = ncg.Copy();
            bool equals = ncg.Equals(new object());
            System.Collections.DictionaryEntry[] de = ncg.GetConnectAccessRules();
            int hash = ncg.GetHashCode();
            ncg.ResetConnectAccess();
            PolicyStatement ps = ncg.Resolve(new Evidence());
            cg = ncg.ResolveMatchingCodeGroups(new Evidence());
        }

        [Fact]
        public static void UnionCodeGroupCallMethods()
        {
            UnionCodeGroup ucg = new UnionCodeGroup(new GacMembershipCondition(), new PolicyStatement(new PermissionSet(new PermissionState())));
            CodeGroup cg = ucg.Copy();
            PolicyStatement ps = ucg.Resolve(new Evidence());
            cg = ucg.ResolveMatchingCodeGroups(new Evidence());
        }
    }
}

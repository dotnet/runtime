// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public class GroupPrincipalTest : PrincipalTest
    {
        [Fact]
        public void GroupPrincipalConstructorTest()
        {
            if (DomainContext == null)
            {
                return;
            }

            GroupPrincipal group = new GroupPrincipal(DomainContext);
            group.Dispose();
        }

        public override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            return new GroupPrincipal(context, name);
        }

        [Fact]
        public void IsMemberOfTest()
        {
            if (DomainContext == null)
            {
                return;
            }

            using (GroupPrincipal group = GroupPrincipal.FindByIdentity(DomainContext, "TestLargeGroup"))
            {
                Assert.True(UserPrincipal.FindByIdentity(DomainContext, "user1499-LargeGroup").IsMemberOf(group));
                Assert.True(UserPrincipal.FindByIdentity(DomainContext, "user1500-LargeGroup").IsMemberOf(group));
                Assert.True(UserPrincipal.FindByIdentity(DomainContext, "user1501-LargeGroup").IsMemberOf(group));
                Assert.True(UserPrincipal.FindByIdentity(DomainContext, "user3000-LargeGroup").IsMemberOf(group));
                Assert.True(UserPrincipal.FindByIdentity(DomainContext, "user3001-LargeGroup").IsMemberOf(group));
                Assert.False(UserPrincipal.FindByIdentity(DomainContext, "userNotInLargeGroup").IsMemberOf(group));
            }
        }

        private void CreateManyUsersInGroup(GroupPrincipal group)
        {
            for (int i = 1; i < 3002; i++)
            {
                string name = $"user{i:0000}-LargeGroup";
                UserPrincipal user = new UserPrincipal(DomainContext, name, "Adrumble@6", false);
                user.Save();
                group.Members.Add(user);
            }
            group.Save();
        }

        public override Principal CreateExtendedPrincipal(PrincipalContext context, string name)
        {
            throw new NotImplementedException();
        }

        public override Principal FindExtendedPrincipal(PrincipalContext context, string name)
        {
            throw new NotImplementedException();
        }
    }
}

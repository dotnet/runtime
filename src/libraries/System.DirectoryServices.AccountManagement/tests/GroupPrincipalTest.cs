// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.DirectoryServices.AccountManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AccountManagementUnitTests
{
    /// <summary>
    ///This is a test class for GroupPrincipalTest and is intended
    ///to contain all GroupPrincipalTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GroupPrincipalTest : PrincipalTest
    {
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for GroupPrincipal Constructor
        ///</summary>
        [TestMethod()]
        public void GroupPrincipalConstructorTest()
        {
            GroupPrincipal group = new GroupPrincipal(domainContext);
            group.Dispose();
            Assert.Inconclusive("TODO: Implement code to verify target");
        }

        internal override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            return new GroupPrincipal(context, name);
        }

        /// <summary>
        /// Testing IsMemberOf including large groups
        /// Right now I assume max is 1500 but test might be modified to test different settings
        /// Also assuming that the group and members were created - this should be done dynamically (but have to think about test performance, we don't want it to delay all tests)
        /// Maybe it's better to test ADStoreCtx.IsMemberOfInStore directly? (and also SAMStoreCTX - I still don't know what are the behavioural differences)
        ///</summary>
        [TestMethod()]
        public void IsMemberOfTest()
        {
            using (GroupPrincipal group = GroupPrincipal.FindByIdentity(domainContext, "TestLargeGroup"))
            {
                //CreateManyUsersInGroup(group);

                Assert.IsTrue(UserPrincipal.FindByIdentity(domainContext, "user1499-LargeGroup").IsMemberOf(group));
                Assert.IsTrue(UserPrincipal.FindByIdentity(domainContext, "user1500-LargeGroup").IsMemberOf(group));
                Assert.IsTrue(UserPrincipal.FindByIdentity(domainContext, "user1501-LargeGroup").IsMemberOf(group));
                Assert.IsTrue(UserPrincipal.FindByIdentity(domainContext, "user3000-LargeGroup").IsMemberOf(group));
                Assert.IsTrue(UserPrincipal.FindByIdentity(domainContext, "user3001-LargeGroup").IsMemberOf(group));
                Assert.IsFalse(UserPrincipal.FindByIdentity(domainContext, "userNotInLargeGroup").IsMemberOf(group));
            }
        }

        private void CreateManyUsersInGroup(GroupPrincipal group)
        {
            for (int i = 1; i < 3002; i++)
            {
                string name = String.Format("user{0:0000}-LargeGroup", i);
                UserPrincipal user = new UserPrincipal(domainContext, name, "Adrumble@6", false);
                user.Save();
                group.Members.Add(user);
            }
            group.Save();
        }

        internal override Principal CreateExtendedPrincipal(PrincipalContext context, string name)
        {
            Assert.Inconclusive("TODO: Implement code to verify target");
            throw new System.NotImplementedException();
        }

        internal override Principal FindExtendedPrincipal(PrincipalContext context, string name)
        {
            throw new System.NotImplementedException();
        }
    }
}

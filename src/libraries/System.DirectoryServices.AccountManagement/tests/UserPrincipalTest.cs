// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.DirectoryServices.AccountManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccountManagementUnitTests
{
    /// <summary>
    ///This is a test class for UserPrincipalTest and is intended
    ///to contain all UserPrincipalTest Unit Tests
    ///</summary>
    [TestClass()]
    public class UserPrincipalTest : PrincipalTest
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

        internal override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            UserPrincipal user = new UserPrincipal(context);
            user.Name = name;
            return user;
        }

        internal override Principal CreateExtendedPrincipal(PrincipalContext context, string name)
        {
            ExtendedUserPrincipal user = new ExtendedUserPrincipal(context);
            user.Name = name;
            return user;
        }

        internal override Principal FindExtendedPrincipal(PrincipalContext context, string name)
        {
            return ExtendedUserPrincipal.FindByIdentity(context, name);
        }

        /// <summary>
        ///A test for UserPrincipal Constructor
        ///</summary>
        [TestMethod()]
        public void UserPrincipalConstructorTest()
        {
            UserPrincipal user = new UserPrincipal(domainContext);
            user.Dispose();
            Assert.Inconclusive("TODO: Implement code to verify target");
        }

        [TestMethod()]
        public void ComputedUACCheck()
        {
            string username = "Administrator";
            string password = "Adrumble@6";
            //TODO: don't assume it exists, create it if its not
            string OU = "TestNull";
            string baseDomain = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split(new char[] { '\\' })[1] + "-TEST";
            string domain = System.String.Format("{0}.nttest.microsoft.com", baseDomain);
            string container = System.String.Format("ou={0},dc={1},dc=nttest,dc=microsoft,dc=com", OU, baseDomain);

            PrincipalContext context = new PrincipalContext(ContextType.Domain, domain, container, username, password);
            UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, "good");

            // set the wrong password to force account lockout
            // Is there a way of doing it programmatically except for NetUserSetInfo? (managed code)
            context.ValidateCredentials("good", "wrong password");

            //verify that the account is locked out
            Assert.IsTrue(user.IsAccountLockedOut(), "trying wrong credentials did not lock the account");

            // if uac is not set correctly, this call might clear the lockout
            user.SmartcardLogonRequired = false;
            user.Save();

            //verify that the account is still locked out
            Assert.IsTrue(user.IsAccountLockedOut(), "the account is no longer locked out after writing setting SmartCardLogonRequired");
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.DirectoryServices.AccountManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccountManagementUnitTests
{
    /// <summary>
    ///This is a test class for PrincipalTest and is intended
    ///to contain all PrincipalTest Unit Tests
    ///</summary>
    [TestClass()]
    abstract public class PrincipalTest
    {
        private TestContext _testContextInstance;
        protected PrincipalContext domainContext;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return _testContextInstance;
            }
            set
            {
                _testContextInstance = value;
            }
        }

        #region Additional test attributes

        [TestInitialize()]
        public void PrincipalTestInitialize()
        {
            RefreshContext();
        }

        private void RefreshContext()
        {
            string username = "Administrator";
            string password = "Adrumble@6";

            //TODO: don't assume it exists, create it if its not
            string OU = "Tests";
            string baseDomain = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split(new char[] { '\\' })[1] + "-TEST";
            string domain = String.Format("{0}.nttest.microsoft.com", baseDomain);
            string container = String.Format("ou={0},dc={1},dc=nttest,dc=microsoft,dc=com", OU, baseDomain);

            if (domainContext != null)
            {
                domainContext.Dispose();
            }

            domainContext = new PrincipalContext(ContextType.Domain, domain, container, username, password);
        }

        //Use ClassCleanup to run code after all tests in a class have run
        [TestCleanup()]
        public void PrincipalTestCleanup()
        {
            if (domainContext != null)
            {
                domainContext.Dispose();
            }
        }

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
        ///  testing user creation
        ///  right now we just test that if trying to add an existing user it causes it to be deleted
        [TestMethod()]
        public void AddExistingPrincipal()
        {
            // use new GUID for the user name so we be sure this user does not exist yet
            string name = Guid.NewGuid().ToString();
            using (Principal principal = CreatePrincipal(domainContext, name))
            {
                principal.Save();
            }

            Assert.IsNotNull(Principal.FindByIdentity(domainContext, name), "Could not create principal");

            // this previously caused the user to be deleted. it is still expected to throw an exception, but not delete the user
            bool exceptionThrown = false;
            try
            {
                using (Principal principal = CreatePrincipal(domainContext, name))
                {
                    principal.Save();
                }
            }
            catch (PrincipalExistsException)
            {
                exceptionThrown = true;
            }

            // validate that we correctly throw an exception when trying to add an existing principal
            Assert.IsTrue(exceptionThrown);

            // validate that we did not delete incorrectly delete the first principal
            using (Principal principal2 = Principal.FindByIdentity(domainContext, name))
            {
                Assert.IsNotNull(principal2, "Existing principal was deleted");

                // explicitly delete the user and check it was really deleted
                principal2.Delete();
            }

            // ensure we cleaned up the test principal
            Assert.IsNull(Principal.FindByIdentity(domainContext, name), "Cleanup failed - principal still exists");
        }


        /// <summary>
        /// 
        /// </summary>
        [TestMethod()]
        public void TestExtendedPrincipal()
        {
            // to improve this, we might want to generate random sequences
            byte[] writtenArray = { 10, 20, 30 };
            byte[] readArray;

            string name = Guid.NewGuid().ToString();
            using (Principal principal = CreateExtendedPrincipal(domainContext, name))
            {
                IExtendedPrincipalTest extendedPrincipal = (IExtendedPrincipalTest)principal;
                extendedPrincipal.ByteArrayExtension = writtenArray;
                principal.Save();
            }

            RefreshContext();

            using (Principal principal = FindExtendedPrincipal(domainContext, name))
            {
                IExtendedPrincipalTest extendedPrincipal = (IExtendedPrincipalTest)principal;
                readArray = extendedPrincipal.ByteArrayExtension;
                principal.Delete();
            }

            CollectionAssert.AreEqual(writtenArray, readArray);
        }

        private void RefreshDomainContext()
        {
            domainContext.Dispose();
        }

        /// <summary>
        /// derived classes will create concrete instances
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal abstract Principal CreatePrincipal(PrincipalContext context, string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal abstract Principal CreateExtendedPrincipal(PrincipalContext context, string name);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal abstract Principal FindExtendedPrincipal(PrincipalContext context, string name);
    }

    internal interface IExtendedPrincipalTest
    {
        object ObjectExtension { get; set; }
        byte[] ByteArrayExtension { get; set; }
        object[] ObjectArrayExtension { get; set; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.DirectoryServices.AccountManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccountManagementUnitTests
{
    /// <summary>
    ///This is a test class for ComputerPrincipalTest and is intended
    ///to contain all ComputerPrincipalTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ComputerPrincipalTest : PrincipalTest
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
        ///A test for ComputerPrincipal Constructor
        ///</summary>
        [TestMethod()]
        public void ComputerPrincipalConstructorTest()
        {
            ComputerPrincipal computer = new ComputerPrincipal(domainContext);
            computer.Dispose();
            Assert.Inconclusive("TODO: Implement code to verify target");
        }

        internal override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            ComputerPrincipal computer = new ComputerPrincipal(context);
            computer.Name = name;
            return computer;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public abstract class PrincipalTest : IDisposable
    {
        public PrincipalContext DomainContext { get; private set; }

        public PrincipalTest() => RefreshContext();

        private void RefreshContext()
        {
            string username = "Administrator";
            string password = "Adrumble@6";

            string OU = "Tests";
            string baseDomain = WindowsIdentity.GetCurrent().Name.Split(new char[] { '\\' })[1] + "-TEST";
            string domain = $"{baseDomain}.nttest.microsoft.com";
            string container = $"ou={OU},dc={baseDomain},dc=nttest,dc=microsoft,dc=com";
            DomainContext?.Dispose();
            try
            {
                DomainContext = new PrincipalContext(ContextType.Domain, domain, container, username, password);
            }
            catch
            {
            }
        }

        public void Dispose() => DomainContext?.Dispose();

        [Fact]
        public void AddExistingPrincipal()
        {
            if (DomainContext == null)
            {
                return;
            }
            
            // use new GUID for the user name so we be sure this user does not exist yet
            string name = Guid.NewGuid().ToString();
            using (Principal principal = CreatePrincipal(DomainContext, name))
            {
                principal.Save();
            }

            Assert.NotNull(Principal.FindByIdentity(DomainContext, name));

            // this previously caused the user to be deleted. it is still expected to throw an exception, but not delete the user
            bool exceptionThrown = false;
            try
            {
                using (Principal principal = CreatePrincipal(DomainContext, name))
                {
                    principal.Save();
                }
            }
            catch (PrincipalExistsException)
            {
                exceptionThrown = true;
            }

            // validate that we correctly throw an exception when trying to add an existing principal
            Assert.True(exceptionThrown);

            // validate that we did not delete incorrectly delete the first principal
            using (Principal principal2 = Principal.FindByIdentity(DomainContext, name))
            {
                Assert.NotNull(principal2);

                // explicitly delete the user and check it was really deleted
                principal2.Delete();
            }

            // ensure we cleaned up the test principal
            Assert.Null(Principal.FindByIdentity(DomainContext, name));
        }

        [Fact]
        public void TestExtendedPrincipal()
        {
            if (DomainContext == null)
            {
                return;
            }

            string name = Guid.NewGuid().ToString();
            byte[] writtenArray = new byte[] { 10, 20, 30 };
            using (Principal principal = CreateExtendedPrincipal(DomainContext, name))
            {
                IExtendedPrincipalTest extendedPrincipal = (IExtendedPrincipalTest)principal;
                extendedPrincipal.ByteArrayExtension = writtenArray;
                principal.Save();
            }

            RefreshContext();
            using (Principal principal = FindExtendedPrincipal(DomainContext, name))
            {
                IExtendedPrincipalTest extendedPrincipal = (IExtendedPrincipalTest)principal;
                byte[] readArray = extendedPrincipal.ByteArrayExtension;
                principal.Delete();
            }
        }

        public abstract Principal CreatePrincipal(PrincipalContext context, string name);

        public abstract Principal CreateExtendedPrincipal(PrincipalContext context, string name);

        public abstract Principal FindExtendedPrincipal(PrincipalContext context, string name);
    }

    public interface IExtendedPrincipalTest
    {
        object ObjectExtension { get; set; }
        byte[] ByteArrayExtension { get; set; }
        object[] ObjectArrayExtension { get; set; }
    }
}

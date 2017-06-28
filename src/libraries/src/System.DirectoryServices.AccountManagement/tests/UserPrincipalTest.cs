// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public class UserPrincipalTest : PrincipalTest
    {
        public override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            return new UserPrincipal(context) { Name = name };
        }

        public override Principal CreateExtendedPrincipal(PrincipalContext context, string name)
        {
            return new ExtendedUserPrincipal(context) { Name = name };
        }

        public override Principal FindExtendedPrincipal(PrincipalContext context, string name)
        {
            return ExtendedUserPrincipal.FindByIdentity(context, name);
        }
        
        public void UserPrincipalConstructorTest()
        {
            UserPrincipal user = new UserPrincipal(DomainContext);
            user.Dispose();
        }

        public void ComputedUACCheck()
        {
            string username = "Administrator";
            string password = "Adrumble@6";
            //TODO: don't assume it exists, create it if its not
            string OU = "TestNull";
            string baseDomain =WindowsIdentity.GetCurrent().Name.Split(new char[] { '\\' })[1] + "-TEST";
            string domain = $"{baseDomain}.nttest.microsoft.com";
            string container = $"ou={OU},dc={baseDomain},dc=nttest,dc=microsoft,dc=com";

            PrincipalContext context = new PrincipalContext(ContextType.Domain, domain, container, username, password);
            UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, "good");

            // set the wrong password to force account lockout
            // Is there a way of doing it programmatically except for NetUserSetInfo? (managed code)
            context.ValidateCredentials("good", "wrong password");

            //verify that the account is locked out
            Assert.True(user.IsAccountLockedOut(), "trying wrong credentials did not lock the account");

            // if uac is not set correctly, this call might clear the lockout
            user.SmartcardLogonRequired = false;
            user.Save();

            //verify that the account is still locked out
            Assert.True(user.IsAccountLockedOut(), "the account is no longer locked out after writing setting SmartCardLogonRequired");
        }
    }
}

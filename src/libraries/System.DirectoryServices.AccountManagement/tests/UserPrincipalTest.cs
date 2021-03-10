// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}

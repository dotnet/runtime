// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public class ComputerPrincipalTest : PrincipalTest
    {
        [Fact]
        public void ComputerPrincipalConstructorTest()
        {
            ComputerPrincipal computer = new ComputerPrincipal(DomainContext);
            computer.Dispose();
        }

        internal override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            ComputerPrincipal computer = new ComputerPrincipal(context);
            computer.Name = name;
            return computer;
        }

        internal override Principal CreateExtendedPrincipal(PrincipalContext context, string name)
        {
            throw new NotImplementedException();
        }

        internal override Principal FindExtendedPrincipal(PrincipalContext context, string name)
        {
            throw new NotImplementedException();
        }
    }
}

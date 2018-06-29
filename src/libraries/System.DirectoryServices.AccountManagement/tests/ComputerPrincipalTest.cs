// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public class ComputerPrincipalTest : PrincipalTest
    {
        [Fact]
        public void Ctor_Context()
        {
            var context = new PrincipalContext(ContextType.Machine);
            var principal = new ComputerPrincipal(context);
            Assert.Same(context, principal.Context);
            Assert.Empty(principal.ServicePrincipalNames);
        }

        [Fact]
        public void Ctor_NullContext_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ComputerPrincipal(null));
            AssertExtensions.Throws<ArgumentException>(null, () => new ComputerPrincipal(null, "samAccountName", "password", enabled: true));
        }

        [Fact]
        public void Ctor_NullSamAccountName_ThrowsArgumentException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            AssertExtensions.Throws<ArgumentException>(null, () => new ComputerPrincipal(context, null, "password", enabled: true));
        }

        [Fact]
        public void Ctor_EmptySamAccountName_ThrowsArgumentNullException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            AssertExtensions.Throws<ArgumentNullException>("Principal.SamAccountName", null, () => new ComputerPrincipal(context, string.Empty, "password", enabled: true));
        }

        [Fact]
        public void Ctor_NullPassword_ThrowsArgumentException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            AssertExtensions.Throws<ArgumentException>(null, () => new ComputerPrincipal(context, "samAccountName", null, enabled: true));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] 
        public void Ctor_MachineContext_NoException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            var principal = new ComputerPrincipal(context, "samAccountName", "password", enabled: true);
            Assert.Equal(ContextType.Machine, principal.ContextType);
        }

        [Fact]
        public void ComputerPrincipalConstructorTest()
        {
            if (DomainContext == null)
            {
                return;
            }

            ComputerPrincipal computer = new ComputerPrincipal(DomainContext);
            computer.Dispose();
        }

        public override Principal CreatePrincipal(PrincipalContext context, string name)
        {
            return new ComputerPrincipal(context) { Name = name };
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

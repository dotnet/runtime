// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.AccountManagement.Tests
{
    public class PrincipalContextTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_ContextType()
        {
            var context = new PrincipalContext(ContextType.Machine);
            Assert.Equal(ContextType.Machine, context.ContextType);
            Assert.Null(context.Name);
            Assert.Null(context.Container);
            Assert.Null(context.UserName);
            Assert.Equal(ContextOptions.Negotiate, context.Options);
            Assert.NotNull(context.ConnectedServer);
            Assert.Equal(Environment.MachineName, context.ConnectedServer);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(ContextType.Machine, null)]
        [InlineData(ContextType.Machine, "")]
        [InlineData(ContextType.Machine, "\0")]
        [InlineData(ContextType.Machine, "name")]
        public void Ctor_ContextType_Name(ContextType contextType, string name)
        {
            var context = new PrincipalContext(contextType, name);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Null(context.Container);
            Assert.Null(context.UserName);
            Assert.Equal(ContextOptions.Negotiate, context.Options);

            if (name != null)
            {
                Assert.Throws<COMException>(() => context.ConnectedServer);
            }
            else
            {
                Assert.NotNull(context.ConnectedServer);
                Assert.Equal(Environment.MachineName, context.ConnectedServer);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(ContextType.Machine, null, null)]
        [InlineData(ContextType.Machine, "", null)]
        [InlineData(ContextType.Machine, "\0", null)]
        [InlineData(ContextType.Machine, "name", null)]
        public void Ctor_ContextType_Name(ContextType contextType, string name, string container)
        {
            var context = new PrincipalContext(contextType, name, container);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Equal(container, context.Container);
            Assert.Null(context.UserName);
            Assert.Equal(ContextOptions.Negotiate, context.Options);

            if (name != null)
            {
                Assert.Throws<COMException>(() => context.ConnectedServer);
            }
            else
            {
                Assert.NotNull(context.ConnectedServer);
                Assert.Equal(Environment.MachineName, context.ConnectedServer);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(ContextType.Machine, null, null, ContextOptions.Negotiate)]
        [InlineData(ContextType.Machine, "", null, ContextOptions.Negotiate)]
        [InlineData(ContextType.Machine, "\0", null, ContextOptions.Negotiate)]
        [InlineData(ContextType.Machine, "name", null, ContextOptions.Negotiate)]
        public void Ctor_ContextType_Name_Container_Options(ContextType contextType, string name, string container, ContextOptions options)
        {
            var context = new PrincipalContext(contextType, name, container, options);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Equal(container, context.Container);
            Assert.Null(context.UserName);
            Assert.Equal(ContextOptions.Negotiate, context.Options);

            if (name != null)
            {
                Assert.Throws<COMException>(() => context.ConnectedServer);
            }
            else
            {
                Assert.NotNull(context.ConnectedServer);
                Assert.Equal(Environment.MachineName, context.ConnectedServer);
            }
        }

        [ActiveIssue(23800)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(ContextType.Machine, null, "userName", "password")]
        [InlineData(ContextType.Machine, "", "", "")]
        [InlineData(ContextType.Machine, "\0", "userName", "")]
        [InlineData(ContextType.Machine, "name", "\0", "\0")]
        public void Ctor_ContextType_Name_UserName_Password(ContextType contextType, string name, string userName, string password)
        {
            var context = new PrincipalContext(contextType, name, userName, password);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Null(context.Container);
            Assert.Equal(userName, context.UserName);
            Assert.Equal(ContextOptions.Negotiate, context.Options);

            if (name != null)
            {
                Assert.Throws<COMException>(() => context.ConnectedServer);
            }
            else
            {
                Assert.Throws<Exception>(() => context.ConnectedServer);
            }
        }

        [ActiveIssue(23800)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(ContextType.Machine, null, null, "userName", "password")]
        [InlineData(ContextType.Machine, "", null, "", "")]
        [InlineData(ContextType.Machine, "\0", null, "userName", "")]
        [InlineData(ContextType.Machine, "name", null, "\0", "\0")]
        public void Ctor_ContextType_Name_Container_UserName_Password(ContextType contextType, string name, string container, string userName, string password)
        {
            var context = new PrincipalContext(contextType, name, container, userName, password);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Equal(container, context.Container);
            Assert.Equal(userName, context.UserName);
            Assert.Equal(ContextOptions.Negotiate, context.Options);

            if (name != null)
            {
                Assert.Throws<COMException>(() => context.ConnectedServer);
            }
            else
            {
                Assert.Throws<Exception>(() => context.ConnectedServer);
            }
        }

        [Theory]
        [InlineData(ContextType.Machine - 1)]
        [InlineData(ContextType.ApplicationDirectory + 1)]
        public void Ctor_InvalidContexType_ThrowsInvalidEnumArgumentException(ContextType contextType)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType, "name"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType, "name", "container"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType, "name", "container", ContextOptions.Negotiate));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType, "name", "userName", "password"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType, "name", "container", "userName", "password"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new PrincipalContext(contextType, "name", "container", ContextOptions.Negotiate, "userName", "password"));
        }

        [Fact]
        public void Ctor_DomainContextType_ThrowsPrincipalServerDownException()
        {
            if (!PlatformDetection.IsDomainJoinedMachine)
            {
                // The machine is not connected to a domain. we expect PrincipalContext(ContextType.Domain) to throw
                Assert.Throws<PrincipalServerDownException>(() => new PrincipalContext(ContextType.Domain));
            }
        }

        [Fact]
        public void Ctor_ActiveDirectoryContextTypeWithoutNameAndContainer_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory, "name", ""));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory, "name", null));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory, "name"));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory, "name", "userName", "password"));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory, "name", "", "userName", "password"));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.ApplicationDirectory, "name", null, "userName", "password"));
        }

        [Theory]
        [InlineData((ContextOptions)(-1))]
        [InlineData((ContextOptions)int.MaxValue)]
        public void Ctor_InvalidOptions_ThrowsInvalidEnumArgumentException(ContextOptions options)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("options", () => new PrincipalContext(ContextType.Machine, "name", null, options));
            AssertExtensions.Throws<InvalidEnumArgumentException>("options", () => new PrincipalContext(ContextType.Domain, "name", null, options));
            AssertExtensions.Throws<InvalidEnumArgumentException>("options", () => new PrincipalContext(ContextType.ApplicationDirectory, "name", "container", options));
            AssertExtensions.Throws<InvalidEnumArgumentException>("options", () => new PrincipalContext(ContextType.Machine, "name", null, options, "userName", "password"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("options", () => new PrincipalContext(ContextType.Domain, "name", null, options, "userName", "password"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("options", () => new PrincipalContext(ContextType.ApplicationDirectory, "name", "container", options, "userName", "password"));
        }

        [Theory]
        [InlineData(ContextType.Machine, ContextOptions.Sealing)]
        [InlineData(ContextType.Machine, ContextOptions.SecureSocketLayer)]
        [InlineData(ContextType.Machine, ContextOptions.ServerBind)]
        [InlineData(ContextType.Machine, ContextOptions.Signing)]
        [InlineData(ContextType.Machine, ContextOptions.SimpleBind)]
        [InlineData(ContextType.ApplicationDirectory, ContextOptions.Negotiate)]
        [InlineData(ContextType.Domain, ContextOptions.Negotiate | ContextOptions.SimpleBind | ContextOptions.Signing)]
        public void Ctor_MachineAndNonNegotiateContextOptions_ThrowsArgumentException(ContextType contextType, ContextOptions options)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(contextType, "name", null, options));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(contextType, "name", null, options, "userName", "password"));
        }

        [Fact]
        public void Ctor_MachineContextTypeWithContainer_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.Machine, "name", "container"));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.Machine, "name", "container", "userName", "password"));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.Machine, "name", "container", ContextOptions.Negotiate, "userName", "password"));
        }

        [Theory]
        [InlineData(null, "password")]
        [InlineData("userName", null)]
        public void Ctor_InconsistentUserNameAndPassword_ThrowsArgumentException(string userName, string password)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.Machine, "name", userName, password));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.Machine, "name", null, userName, password));
            AssertExtensions.Throws<ArgumentException>(null, () => new PrincipalContext(ContextType.Machine, "name", null, ContextOptions.Negotiate, userName, password));
        }

        [Fact]
        public void ConnectedServer_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.ConnectedServer);
        }

        [Fact]
        public void Container_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.Container);
        }

        [Fact]
        public void ContextType_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.ContextType);
        }

        [Fact]
        public void Name_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.Name);
        }

        [Fact]
        public void Options_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.Options);
        }

        [Fact]
        public void UserName_GetWhenDisposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.UserName);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(null, null, true)]
        [InlineData("", "", false)]
        public void ValidateCredentials_Invoke_ReturnsExpected(string userName, string password, bool expected)
        {
            var context = new PrincipalContext(ContextType.Machine);
            Assert.Equal(expected, context.ValidateCredentials(userName, password));
            Assert.Equal(expected, context.ValidateCredentials(userName, password, ContextOptions.Negotiate));
        }

        [ActiveIssue(23800)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        public void ValidateCredentials_InvalidUserName_ThrowsException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            Assert.Throws<Exception>(() => context.ValidateCredentials("\0", "password"));
        }

        [ActiveIssue(23800)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        public void ValidateCredentials_IncorrectUserNamePassword_ThrowsException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            Assert.Throws<Exception>(() => context.ValidateCredentials("userName", "password"));
        }

        [Theory]
        [InlineData(null, "password")]
        [InlineData("userName", null)]
        public void ValidateCredentials_InvalidUsernamePasswordCombo_ThrowsArgumentException(string userName, string password)
        {
            var context = new PrincipalContext(ContextType.Machine);
            AssertExtensions.Throws<ArgumentException>(null, () => context.ValidateCredentials(userName, password));
            AssertExtensions.Throws<ArgumentException>(null, () => context.ValidateCredentials(userName, password, ContextOptions.Negotiate));
        }

        [Theory]
        [InlineData(ContextOptions.Sealing)]
        [InlineData(ContextOptions.SecureSocketLayer)]
        [InlineData(ContextOptions.ServerBind)]
        [InlineData(ContextOptions.Signing)]
        [InlineData(ContextOptions.SimpleBind)]
        public void ValidateCredentials_InvalidOptions_ThrowsArgumentException(ContextOptions options)
        {
            var context = new PrincipalContext(ContextType.Machine);
            AssertExtensions.Throws<ArgumentException>(null, () => context.ValidateCredentials("userName", "password", options));

        }

        [Fact]
        public void ValidateCredentials_Disposed_ThrowsObjectDisposedException()
        {
            var context = new PrincipalContext(ContextType.Machine);
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.ValidateCredentials(null, null));
            Assert.Throws<ObjectDisposedException>(() => context.ValidateCredentials(null, null, ContextOptions.Negotiate));
        }
    }
}

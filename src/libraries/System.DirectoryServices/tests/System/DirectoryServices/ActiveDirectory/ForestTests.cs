// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.ActiveDirectory.Tests
{
    public class ForestTests
    {
        [Fact]
        public void GetForest_NullContext_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("context", () => Forest.GetForest(null));
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.ConfigurationSet)]
        [InlineData(DirectoryContextType.Domain)]
        public void GetForest_InvalidContextType_ThrowsArgumentException(DirectoryContextType contextType)
        {
            var context = new DirectoryContext(contextType, "Name");
            AssertExtensions.Throws<ArgumentException>("context", () => Forest.GetForest(context));
        }

        [Fact]
        [OuterLoop]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void GetForest_NullNameAndNotRootedDomain_ThrowsActiveDirectoryOperationException()
        {
            var context = new DirectoryContext(DirectoryContextType.Forest);

            if (!PlatformDetection.IsDomainJoinedMachine)
                Assert.Throws<ActiveDirectoryOperationException>(() => Forest.GetForest(context));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(DirectoryContextType.DirectoryServer, "\0")]
        [InlineData(DirectoryContextType.Forest, "server:port")]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/21553", TargetFrameworkMonikers.UapAot)]
        public void GetForest_NonNullNameAndNotRootedDomain_ThrowsActiveDirectoryObjectNotFoundException(DirectoryContextType type, string name)
        {
            var context = new DirectoryContext(type, name);
            Assert.Throws<ActiveDirectoryObjectNotFoundException>(() => Forest.GetForest(context));

            // The result of validation is cached, so repeat this to make sure it's cached properly.
            Assert.Throws<ActiveDirectoryObjectNotFoundException>(() => Forest.GetForest(context));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(DirectoryContextType.Forest, "\0")]
        [InlineData(DirectoryContextType.DirectoryServer, "server:port")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void GetForest_NonNullNameAndNotRootedDomain_ThrowsActiveDirectoryObjectNotFoundException_NonUap(DirectoryContextType type, string name)
        {
            var context = new DirectoryContext(type, name);
            if (!PlatformDetection.IsDomainJoinedMachine)
            {
                Assert.Throws<ActiveDirectoryObjectNotFoundException>(() => Forest.GetForest(context));

                // The result of validation is cached, so repeat this to make sure it's cached properly.
                Assert.Throws<ActiveDirectoryObjectNotFoundException>(() => Forest.GetForest(context));
            }
        }
    }
}

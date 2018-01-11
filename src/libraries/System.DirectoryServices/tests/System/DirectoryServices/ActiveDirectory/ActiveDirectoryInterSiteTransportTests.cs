// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.ActiveDirectory.Tests
{
    public class ActiveDirectoryInterSiteTransportTests
    {
        [Fact]
        public void FindByTransportType_NullContext_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(null, ActiveDirectoryTransportType.Rpc));
        }

        [Fact]
        [OuterLoop]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Getting information about domain is denied inside App")]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/21553", TargetFrameworkMonikers.UapAot)]
        public void FindByTransportType_ForestNoDomainAssociatedWithoutName_ThrowsActiveDirectoryOperationException()
        {
            var context = new DirectoryContext(DirectoryContextType.Forest);
            if (!PlatformDetection.IsDomainJoinedMachine)
            {
                Assert.Throws<ActiveDirectoryOperationException>(() => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
            }
        }

        [Fact]
        public void FindByTransportType_ForestNoDomainAssociatedWithName_ThrowsActiveDirectoryOperationException()
        {
            var context = new DirectoryContext(DirectoryContextType.Forest, "server:port");
            AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void FindByTransportType_ForestNoDomainAssociatedWithName_ThrowsActiveDirectoryOperationException_NoUap()
        {
            // Domain joined machines will not throw on the ActiveDirectoryInterSiteTransport.FindByTransportType call.
            if (!PlatformDetection.IsDomainJoinedMachine)
            {
                var context = new DirectoryContext(DirectoryContextType.Forest, "\0");
                AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
            }
        }

        [Fact]
        public void FindByTransportType_DomainNoDomainAssociatedWithoutName_ThrowsArgumentException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [OuterLoop("Takes too long on domain joined machines")]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.DirectoryServer)]
        [InlineData(DirectoryContextType.Domain)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Access to common path is denied inside App")]
        public void FindByTransportType_InvalidContextTypeWithName_ThrowsArgumentException(DirectoryContextType type)
        {
            var context = new DirectoryContext(type, "Name");
            AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [Fact]
        [OuterLoop]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Getting information about domain is denied inside App")]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/21553", TargetFrameworkMonikers.UapAot)]
        public void FindByTransportType_ConfigurationSetTypeWithName_Throws()
        {
            var context = new DirectoryContext(DirectoryContextType.ConfigurationSet, "Name");
            Assert.Throws<ActiveDirectoryOperationException>(() => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [Theory]
        [InlineData(ActiveDirectoryTransportType.Rpc - 1)]
        [InlineData(ActiveDirectoryTransportType.Smtp + 1)]
        public void FindByTransportType_InvalidTransport_ThrowsInvalidEnumArgumentException(ActiveDirectoryTransportType transport)
        {
            var context = new DirectoryContext(DirectoryContextType.ConfigurationSet, "Name");
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, transport));
        }
    }
}

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
        public void FindByTransportType_ForestNoDomainAssociatedWithoutName_ThrowsActiveDirectoryOperationException()
        {
            var context = new DirectoryContext(DirectoryContextType.Forest);
            Assert.Throws<ActiveDirectoryOperationException>(() => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [Theory]
        [InlineData("\0")]
        [InlineData("server:port")]
        public void FindByTransportType_ForestNoDomainAssociatedWithName_ThrowsActiveDirectoryOperationException(string name)
        {
            var context = new DirectoryContext(DirectoryContextType.Forest, name);
            AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [Fact]
        public void FindByTransportType_DomainNoDomainAssociatedWithoutName_ThrowsArgumentException()
        {
            var context = new DirectoryContext(DirectoryContextType.Domain);
            AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.DirectoryServer)]
        [InlineData(DirectoryContextType.Domain)]
        public void FindByTransportType_InvalidContextTypeWithName_ThrowsArgumentException(DirectoryContextType type)
        {
            var context = new DirectoryContext(type, "Name");
            AssertExtensions.Throws<ArgumentException>("context", () => ActiveDirectoryInterSiteTransport.FindByTransportType(context, ActiveDirectoryTransportType.Rpc));
        }

        [Fact]
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

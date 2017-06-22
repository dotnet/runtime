// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.ActiveDirectory.Tests
{
    public class DirectoryContextTests
    {
        [Theory]
        [InlineData(DirectoryContextType.Domain)]
        [InlineData(DirectoryContextType.Forest)]
        public void Ctor_ContextType(DirectoryContextType contextType)
        {
            var context = new DirectoryContext(contextType);
            Assert.Equal(contextType, context.ContextType);
            Assert.Null(context.Name);
            Assert.Null(context.UserName);
        }

        [Theory]
        [InlineData(DirectoryContextType.Domain, null, null)]
        [InlineData(DirectoryContextType.Forest, "UserName", "Password")]
        public void Ctor_ContextType_UserName_Password(DirectoryContextType contextType, string userName, string password)
        {
            var context = new DirectoryContext(contextType, userName, password);
            Assert.Equal(contextType, context.ContextType);
            Assert.Null(context.Name);
            Assert.Equal(userName, context.UserName);
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition)]
        [InlineData(DirectoryContextType.ConfigurationSet)]
        [InlineData(DirectoryContextType.DirectoryServer)]
        public void Ctor_NotSupportedContextType_ThrowsArgumentException(DirectoryContextType contextType)
        {
            AssertExtensions.Throws<ArgumentException>("contextType", () => new DirectoryContext(contextType));
            AssertExtensions.Throws<ArgumentException>("contextType", () => new DirectoryContext(contextType, "username", "password"));
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition, "Name")]
        [InlineData(DirectoryContextType.ConfigurationSet, "Name")]
        [InlineData(DirectoryContextType.DirectoryServer, "Name")]
        [InlineData(DirectoryContextType.Domain, "Name")]
        [InlineData(DirectoryContextType.Forest, "Name")]
        public void Ctor_ContextType_Name(DirectoryContextType contextType, string name)
        {
            var context = new DirectoryContext(contextType, name);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Null(context.UserName);
        }

        [Theory]
        [InlineData(DirectoryContextType.ApplicationPartition, "Name", null, null)]
        [InlineData(DirectoryContextType.ConfigurationSet, "Name", "", "")]
        [InlineData(DirectoryContextType.DirectoryServer, "Name", "UserName", "Password")]
        [InlineData(DirectoryContextType.Domain, "Name", "UserName", "Password")]
        [InlineData(DirectoryContextType.Forest, "Name", "UserName", "Password")]
        public void Ctor_ContextType_Name_UserName_Password(DirectoryContextType contextType, string name, string userName, string password)
        {
            var context = new DirectoryContext(contextType, name, userName, password);
            Assert.Equal(contextType, context.ContextType);
            Assert.Equal(name, context.Name);
            Assert.Equal(userName, context.UserName);
        }

        [Theory]
        [InlineData(DirectoryContextType.Domain - 1)]
        [InlineData(DirectoryContextType.ApplicationPartition + 1)]
        public void Ctor_InvalidContextType_ThrowsInvalidEnumArgumentException(DirectoryContextType contextType)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new DirectoryContext(contextType, "name"));
            AssertExtensions.Throws<InvalidEnumArgumentException>("contextType", () => new DirectoryContext(contextType, "name", "userName", "password"));
        }

        [Fact]
        public void Ctor_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new DirectoryContext(DirectoryContextType.ConfigurationSet, null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new DirectoryContext(DirectoryContextType.ConfigurationSet, null, "userName", "password"));
        }

        [Fact]
        public void Ctor_EmptyName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("name", () => new DirectoryContext(DirectoryContextType.ConfigurationSet, string.Empty));
            AssertExtensions.Throws<ArgumentException>("name", () => new DirectoryContext(DirectoryContextType.ConfigurationSet, string.Empty, "userName", "password"));
        }
    }
}

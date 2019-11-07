// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ComAliasNameAttributeTests
    {
#pragma warning disable 0169
        [ComAliasName("foo")]
        private int _foo;
#pragma warning restore 0169

        [Fact]
        public void Exists()
        {
            FieldInfo field = typeof(ComAliasNameAttributeTests).GetTypeInfo().DeclaredFields.Single(f => f.Name == "_foo");
            ComAliasNameAttribute attribute = Assert.Single(field.GetCustomAttributes<ComAliasNameAttribute>(inherit: false));
            Assert.Equal("foo", attribute.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void Ctor_Alias(string alias)
        {
            var attribute = new ComAliasNameAttribute(alias);
            Assert.Equal(alias, attribute.Value);
        }
    }
}

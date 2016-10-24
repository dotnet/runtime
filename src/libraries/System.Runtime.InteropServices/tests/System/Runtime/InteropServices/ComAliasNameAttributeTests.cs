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
            var type = typeof(ComAliasNameAttributeTests);
            var field = type.GetTypeInfo().DeclaredFields.Single(f => f.Name == "_foo");
            var attr = field.GetCustomAttributes(typeof(ComAliasNameAttribute), false).OfType<ComAliasNameAttribute>().SingleOrDefault();
            Assert.NotNull(attr);
            Assert.Equal("foo", attr.Value);
        }
    }
}

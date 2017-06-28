// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema.Tests
{
    public class ForeignKeyAttributeTests
    {
        [Theory]
        [InlineData("Old Mother Dismass")]
        public static void Ctor_String(string name)
        {
            ForeignKeyAttribute attribute = new ForeignKeyAttribute(name);
            Assert.Equal(name, attribute.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void Ctor_String_NullOrWhitespaceName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ForeignKeyAttribute(name));
        }
    }
}

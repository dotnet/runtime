// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static void Ctor_String_NullName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentNullException>("name", null, () => new ForeignKeyAttribute(name));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void Ctor_String_WhitespaceName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentException>("name", null, () => new ForeignKeyAttribute(name));
        }
    }
}

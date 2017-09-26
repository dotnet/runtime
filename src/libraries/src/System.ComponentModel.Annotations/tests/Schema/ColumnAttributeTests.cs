// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema.Tests
{
    public class ColumnAttributeTests
    {
        [Fact]
        public static void Ctor_Default()
        {
            ColumnAttribute attribute = new ColumnAttribute();
            Assert.Null(attribute.Name);
            Assert.Equal(-1, attribute.Order);
            Assert.Null(attribute.TypeName);
        }

        [Theory]
        [InlineData("Granny Weatherwax")]
        public static void Ctor_String(string name)
        {
            ColumnAttribute attribute = new ColumnAttribute(name);
            Assert.Equal(name, attribute.Name);
            Assert.Equal(-1, attribute.Order);
            Assert.Null(attribute.TypeName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void Ctor_String_NullOrWhitespaceName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ColumnAttribute(name));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public static void Order_Set_ReturnsExpected(int value)
        {
            ColumnAttribute attribute = new ColumnAttribute() { Order = value };
            Assert.Equal(value, attribute.Order);
        }

        [Fact]
        public static void Order_Set_NegativeValue_ThrowsArgumentOutOfRangeException()
        {
            ColumnAttribute attribute = new ColumnAttribute();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => attribute.Order = -1);
        }

        [Theory]
        [InlineData("Nanny Ogg")]
        public static void TypeName_Set_ReturnsExpected(string value)
        {
            ColumnAttribute attribute = new ColumnAttribute() { TypeName = value };
            Assert.Equal(value, attribute.TypeName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void TypeName_Set_NullOrWhitespaceValue_ThrowsArgumentException(string value)
        {
            ColumnAttribute attribute = new ColumnAttribute();
            AssertExtensions.Throws<ArgumentException>(null, () => attribute.TypeName = value);
        }
    }
}

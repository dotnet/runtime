// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema.Tests
{
    public class TableAttributeTests
    {
        [Theory]
        [InlineData("Black Aliss")]
        public static void Ctor_String(string name)
        {
            TableAttribute attribute = new TableAttribute(name);
            Assert.Equal(name, attribute.Name);
            Assert.Null(attribute.Schema);
        }

        [Theory]
        [InlineData(null)]
        public static void Ctor_String_NullName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentNullException>("name", null, () => new TableAttribute(name));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void Ctor_String_WhitespaceName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentException>("name", null, () => new TableAttribute(name));
        }

        [Theory]
        [InlineData("Mrs Letice Earwig")]
        public static void Schema_Set_ReturnsExpected(string value)
        {
            TableAttribute attribute = new TableAttribute("Perspicacia Tick") { Schema = value };
            Assert.Equal(value, attribute.Schema);
        }

        [Theory]
        [InlineData(null)]
        public static void Schema_Set_NullValue_ThrowsArgumentException(string value)
        {
            TableAttribute attribute = new TableAttribute("Perspicacia Tick");
            AssertExtensions.Throws<ArgumentNullException>("value", null, () => attribute.Schema = value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void Schema_Set_WhitespaceValue_ThrowsArgumentException(string value)
        {
            TableAttribute attribute = new TableAttribute("Perspicacia Tick");
            AssertExtensions.Throws<ArgumentException>("value", null, () => attribute.Schema = value);
        }
    }
}

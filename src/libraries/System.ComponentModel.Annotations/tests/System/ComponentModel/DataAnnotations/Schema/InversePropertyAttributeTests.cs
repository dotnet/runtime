// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema.Tests
{
    public class InversePropertyAttributeTests
    {
        [Theory]
        [InlineData("Gammer Brevis")]
        public static void Ctor_String(string property)
        {
            InversePropertyAttribute attribute = new InversePropertyAttribute(property);
            Assert.Equal(property, attribute.Property);
        }

        [Theory]
        [InlineData(null)]
        public static void Ctor_String_NullProperty_ThrowsArgumentException(string property)
        {
            AssertExtensions.Throws<ArgumentNullException>("property", null, () => new InversePropertyAttribute(property));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public static void Ctor_String_WhitespaceProperty_ThrowsArgumentException(string property)
        {
            AssertExtensions.Throws<ArgumentException>("property", null, () => new InversePropertyAttribute(property));
        }
    }
}

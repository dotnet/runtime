// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public partial class DisplayFormatAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DisplayFormatAttribute();
            Assert.True(attribute.ConvertEmptyStringToNull);
            Assert.True(attribute.HtmlEncode);
            Assert.False(attribute.ApplyFormatInEditMode);

            Assert.Null(attribute.DataFormatString);
            Assert.Null(attribute.NullDisplayText);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{0:C}")]
        [InlineData("{0:d}")]
        public void DataFormatString_Set_GetReturnsExpected(string input)
        {
            var attribute = new DisplayFormatAttribute { DataFormatString = input };
            Assert.Equal(input, attribute.DataFormatString);
        }

        [Fact]
        public void ConvertEmptyStringToNull_Set_GetReturnsExpected()
        {
            var attribute = new DisplayFormatAttribute { ConvertEmptyStringToNull = false };
            Assert.False(attribute.ConvertEmptyStringToNull);
        }

        [Fact]
        public void ApplyFormatInEditMode_Set_GetReturnsExpected()
        {
            var attribute = new DisplayFormatAttribute { ApplyFormatInEditMode = true };
            Assert.True(attribute.ApplyFormatInEditMode);
        }

        [Fact]
        public void HtmlEncode_Set_GetReturnsExpected()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute { HtmlEncode = false };
            Assert.False(attribute.HtmlEncode);
        }

        public static IEnumerable<object[]> NullDisplayText_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { " \r \t \n " };
            yield return new object[] { "abc" };
            yield return new object[] { "NullDisplayText" };
        }

        [Theory]
        [MemberData(nameof(NullDisplayText_TestData))]
        public void NullDisplayText_Get_Set(string input)
        {
            var attribute = new DisplayFormatAttribute { NullDisplayText = input };

            Assert.Equal(input, attribute.NullDisplayText);

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.NullDisplayText = input;
            Assert.Equal(input, attribute.NullDisplayText);
        }
    }
}

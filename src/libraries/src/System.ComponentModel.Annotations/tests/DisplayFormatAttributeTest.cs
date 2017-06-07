// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class DisplayFormatAttributeTest
    {
        [Fact]
        public void Ctor()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            Assert.True(attribute.ConvertEmptyStringToNull);
            Assert.True(attribute.HtmlEncode);
            Assert.False(attribute.ApplyFormatInEditMode);

            Assert.Null(attribute.DataFormatString);
            Assert.Null(attribute.NullDisplayText);
#if netcoreapp
            Assert.Null(attribute.NullDisplayTextResourceType);
#endif
        }

        [Theory]
        [InlineData("{0:C}")]
        [InlineData("{0:d}")]
        public void DataFormatString_Get_Set(string input)
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.DataFormatString = input;

            Assert.Equal(input, attribute.DataFormatString);
        }

        [Fact]
        public void ConvertEmptyStringToNull_Get_Set()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.ConvertEmptyStringToNull = false;

            Assert.False(attribute.ConvertEmptyStringToNull);
        }

        [Fact]
        public void ApplyFormatInEditMode_Get_Set()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.ApplyFormatInEditMode = true;

            Assert.True(attribute.ApplyFormatInEditMode);
        }

        [Fact]
        public void HtmlEncode_Get_Set()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.HtmlEncode = false;

            Assert.False(attribute.HtmlEncode);
        }

        public static IEnumerable<object[]> Strings_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { " \r \t \n " };
            yield return new object[] { "abc" };
            yield return new object[] { "NullDisplayText" };
        }

        [Theory]
        [MemberData(nameof(Strings_TestData))]
        public void NullDisplayText_Get_Set(string input)
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.NullDisplayText = input;

            Assert.Equal(input, attribute.NullDisplayText);
#if netcoreapp
            Assert.NotNull(attribute.GetNullDisplayText());
#endif

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.NullDisplayText = input;
            Assert.Equal(input, attribute.NullDisplayText);
        }

#if netcoreapp
        public class FakeResourceType
        {
            public static string Resource1
            {
                get { return "Resource1Text"; }
            }

            public static string Resource2
            {
                get { return "Resource2Text"; }
            }
        }

        [Fact]
        public void NullDisplayTextResourceType_Get_Set()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.NullDisplayTextResourceType = typeof(FakeResourceType);

            Assert.Equal(typeof(FakeResourceType), attribute.NullDisplayTextResourceType);
        }

        [Fact]
        public void NullDisplayText_WithResource()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.NullDisplayTextResourceType = typeof(FakeResourceType);

            attribute.NullDisplayText = "Resource1";
            Assert.Equal(FakeResourceType.Resource1, attribute.GetNullDisplayText());

            // Changing target resource
            attribute.NullDisplayText = "Resource2";
            Assert.Equal(FakeResourceType.Resource2, attribute.GetNullDisplayText());

            // Not existing resource in the resource type
            attribute.NullDisplayText = "Resource3";
            Assert.Throws<InvalidOperationException>(() => attribute.GetNullDisplayText());
        }

        [Fact]
        public void NullDisplayText_NotAResourceType()
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            // Setting a type that is not a resource type
            attribute.NullDisplayTextResourceType = typeof(string);

            attribute.NullDisplayText = "foo";
            Assert.Throws<InvalidOperationException>(() => attribute.GetNullDisplayText());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(FakeResourceType))]
        public void GetNullDisplayText_WhenNullDisplayTextNotSet(Type input)
        {
            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            attribute.NullDisplayTextResourceType = input;

            Assert.Null(attribute.GetNullDisplayText());
        }
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public partial class DisplayFormatAttributeTests
    {
        [Fact]
        public void NullDisplayTextResourceType_GetDefault_ReturnsNull()
        {
            var attribute = new DisplayFormatAttribute();
            Assert.Null(attribute.NullDisplayTextResourceType);
        }

        [Theory]
        [MemberData(nameof(NullDisplayText_TestData))]
        public void NullDisplayText_Set_GetReturnsExpected(string input)
        {
            var attribute = new DisplayFormatAttribute { NullDisplayText = input };
            Assert.Equal(input, attribute.GetNullDisplayText());

            // Set again, to cover the setter avoiding operations if the value is the same
            attribute.NullDisplayText = input;
            Assert.Equal(input, attribute.NullDisplayText);
        }

        [Fact]
        public void NullDisplayTextResourceType_Set_GetReturnsExpected()
        {
            var attribute = new DisplayFormatAttribute { NullDisplayTextResourceType = typeof(FakeResourceType) };
            Assert.Equal(typeof(FakeResourceType), attribute.NullDisplayTextResourceType);
        }

        [Fact]
        public void GetNullDisplayText_ValidResource_ReturnsExpected()
        {
            var attribute = new DisplayFormatAttribute { NullDisplayTextResourceType = typeof(FakeResourceType) };

            attribute.NullDisplayText = "Resource1";
            Assert.Equal(FakeResourceType.Resource1, attribute.GetNullDisplayText());

            // Changing target resource
            attribute.NullDisplayText = "Resource2";
            Assert.Equal(FakeResourceType.Resource2, attribute.GetNullDisplayText());
        }

        [Theory]
        [InlineData(typeof(FakeResourceType), "Resource3")]
        [InlineData(typeof(FakeResourceType), nameof(FakeResourceType.InstanceProperty))]
        [InlineData(typeof(FakeResourceType), nameof(FakeResourceType.SetOnlyProperty))]
        [InlineData(typeof(FakeResourceType), "NonPublicGetProperty")]
        [InlineData(typeof(string), "foo")]
        public void GetNullDisplayText_InvalidResourceType_ThrowsInvalidOperationException(Type nullDisplayTextResourceType, string nullDisplayText)
        {
            var attribute = new DisplayFormatAttribute { NullDisplayTextResourceType = nullDisplayTextResourceType };
            attribute.NullDisplayText = nullDisplayText;
            Assert.Throws<InvalidOperationException>(() => attribute.GetNullDisplayText());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(FakeResourceType))]
        public void GetNullDisplayText_NullDisplayText_ReturnsNull(Type input)
        {
            var attribute = new DisplayFormatAttribute { NullDisplayTextResourceType = input };
            Assert.Null(attribute.GetNullDisplayText());
        }

        public class FakeResourceType
        {
            public static string Resource1 => "Resource1Text";
            public static string Resource2 => "Resource2Text";

            public string InstanceProperty => "InstanceProperty";
            public static string SetOnlyProperty { set => Assert.NotNull(value); }
            private static string NonPublicGetProperty => "NonPublicGetProperty";
        }
    }
}

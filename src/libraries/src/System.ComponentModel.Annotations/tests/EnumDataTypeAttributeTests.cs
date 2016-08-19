// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class EnumDataTypeAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        [InlineData(typeof(NonFlagsEnumType))]
        [InlineData(typeof(FlagsEnumType))]
        public static void Ctor(Type enumType)
        {
            var attribute = new EnumDataTypeAttribute(enumType);
            Assert.Equal(DataType.Custom, attribute.DataType);
            Assert.Equal("Enumeration", attribute.CustomDataType);

            Assert.Equal(enumType, attribute.EnumType);
        }

        [Theory]
        [InlineData(typeof(NonFlagsEnumType), null)]
        [InlineData(typeof(NonFlagsEnumType), "")]
        [InlineData(typeof(NonFlagsEnumType), NonFlagsEnumType.A)]
        [InlineData(typeof(NonFlagsEnumType), 10)]
        [InlineData(typeof(NonFlagsEnumType), 100)]
        [InlineData(typeof(FlagsEnumType), FlagsEnumType.X)]
        [InlineData(typeof(FlagsEnumType), FlagsEnumType.X | FlagsEnumType.Y)]
        [InlineData(typeof(FlagsEnumType), 5)]
        [InlineData(typeof(FlagsEnumType), 7)]
        [InlineData(typeof(NonFlagsEnumType), "A")]
        [InlineData(typeof(NonFlagsEnumType), "B")]
        [InlineData(typeof(NonFlagsEnumType), "C")]
        [InlineData(typeof(NonFlagsEnumType), "0")]
        [InlineData(typeof(NonFlagsEnumType), "10")]
        [InlineData(typeof(NonFlagsEnumType), "100")]
        [InlineData(typeof(FlagsEnumType), "X")]
        [InlineData(typeof(FlagsEnumType), "X, Y")]
        [InlineData(typeof(FlagsEnumType), "X, Y, Z")]
        [InlineData(typeof(FlagsEnumType), "1")]
        [InlineData(typeof(FlagsEnumType), "5")]
        [InlineData(typeof(FlagsEnumType), "7")]
        public static void Validate_Valid_DoesNotThrow(Type enumType, object value)
        {
            var attribute = new EnumDataTypeAttribute(enumType);
            attribute.Validate(value, s_testValidationContext);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        [InlineData(typeof(NonFlagsEnumType?))]
        [InlineData(typeof(FlagsEnumType?))]
        public static void Validate_InvalidEnumType_ThrowsInvalidOperationException(Type enumType)
        {
            var attribute = new EnumDataTypeAttribute(enumType);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("AnyValue", s_testValidationContext));
        }

        public static IEnumerable<object[]> Validate_Invalid_TestData()
        {
            yield return new object[] { typeof(NonFlagsEnumType), FlagsEnumType.X };
            yield return new object[] { typeof(NonFlagsEnumType), new object() };
            yield return new object[] { typeof(NonFlagsEnumType), true };
            yield return new object[] { typeof(NonFlagsEnumType), 1.1f };
            yield return new object[] { typeof(NonFlagsEnumType), 123.456d };
            yield return new object[] { typeof(NonFlagsEnumType), 123.456m };
            yield return new object[] { typeof(NonFlagsEnumType), '0' };
            yield return new object[] { typeof(NonFlagsEnumType), 42 };
            yield return new object[] { typeof(NonFlagsEnumType), "NoSuchValue" };
            yield return new object[] { typeof(NonFlagsEnumType), "42" };

            yield return new object[] { typeof(FlagsEnumType), 0 };
            yield return new object[] { typeof(FlagsEnumType), 8 };
            yield return new object[] { typeof(FlagsEnumType), "NoSuchValue" };
            yield return new object[] { typeof(FlagsEnumType), "0" };
            yield return new object[] { typeof(FlagsEnumType), "8" };
        }

        [Theory]
        [MemberData(nameof(Validate_Invalid_TestData))]
        public static void Validate_Invalid_ThrowsValidationException(Type enumType, object value)
        {
            var attribute = new EnumDataTypeAttribute(enumType);
            Assert.Throws<ValidationException>(() => attribute.Validate(value, s_testValidationContext));
        }

        private enum NonFlagsEnumType
        {
            A = 0,
            B = 10,
            C = 100
        }

        [Flags]
        private enum FlagsEnumType
        {
            X = 1,
            Y = 2,
            Z = 4
        }
    }
}

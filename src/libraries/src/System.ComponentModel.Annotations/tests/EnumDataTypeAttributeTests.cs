// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class EnumDataTypeAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void EnumDataTypeAttribute_creation_DataType_and_CustomDataType()
        {
            var attribute = new EnumDataTypeAttribute(null);
            Assert.Equal(DataType.Custom, attribute.DataType);
            Assert.Equal("Enumeration", attribute.CustomDataType);
        }

        [Fact]
        public static void Can_get_EnumType()
        {
            var attribute = new EnumDataTypeAttribute(null);
            Assert.Null(attribute.EnumType);

            attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            Assert.Equal(typeof(NonFlagsEnumType), attribute.EnumType);
        }

        [Fact]
        public static void Validate_successful_for_null_value()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // Null is valid
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_for_null_EnumType()
        {
            var attribute = new EnumDataTypeAttribute(null);
            Assert.Null(attribute.EnumType);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Value does not matter - EnumType is null", s_testValidationContext));
        }


        [Fact]
        public static void Validate_throws_InvalidOperationException_for_non_enum_EnumType()
        {
            var attribute = new EnumDataTypeAttribute(typeof(string));
            Assert.Equal(typeof(string), attribute.EnumType);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Value does not matter - EnumType is not an enum", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_for_Nullable_EnumType()
        {
            var attribute = new EnumDataTypeAttribute(typeof(Nullable<NonFlagsEnumType>));
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Value does not matter - EnumType is Nullable", s_testValidationContext));

            attribute = new EnumDataTypeAttribute(typeof(Nullable<FlagsEnumType>));
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Value does not matter - EnumType is Nullable", s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_null_or_empty_value()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_non_matching_EnumType()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate(FlagsEnumType.X, s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_non_ValueType_value()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate(new object(), s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_non_integral_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate(true, s_testValidationContext)); // bool
            Assert.Throws<ValidationException>(() => attribute.Validate(1.1f, s_testValidationContext)); // float
            Assert.Throws<ValidationException>(() => attribute.Validate(123.456d, s_testValidationContext)); // double
            Assert.Throws<ValidationException>(() => attribute.Validate(123.456m, s_testValidationContext)); // decimal
            Assert.Throws<ValidationException>(() => attribute.Validate('0', s_testValidationContext)); // char
        }

        [Fact]
        public static void Validate_successful_for_matching_non_flags_enums_and_matching_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            AssertEx.DoesNotThrow(() => attribute.Validate(NonFlagsEnumType.A, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(10, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(100, s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_matching_flags_enums_and_matching_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(FlagsEnumType));
            AssertEx.DoesNotThrow(() => attribute.Validate(FlagsEnumType.X, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(FlagsEnumType.X | FlagsEnumType.Y, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(5, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(7, s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_matching_non_flags_enums_and_non_matching_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate(42, s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_matching_flags_enums_and_non_matching_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(FlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate(0, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(8, s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_string_values_which_can_be_converted_to_enum_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            AssertEx.DoesNotThrow(() => attribute.Validate("A", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("B", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("C", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("0", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("10", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("100", s_testValidationContext));

            attribute = new EnumDataTypeAttribute(typeof(FlagsEnumType));
            AssertEx.DoesNotThrow(() => attribute.Validate("X", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("X, Y", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("X, Y, Z", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("1", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("5", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("7", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_string_values_which_cannot_be_converted_to_enum_values()
        {
            var attribute = new EnumDataTypeAttribute(typeof(NonFlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate("NonExist", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("42", s_testValidationContext));

            attribute = new EnumDataTypeAttribute(typeof(FlagsEnumType));
            Assert.Throws<ValidationException>(() => attribute.Validate("NonExist", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("0", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("8", s_testValidationContext));
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

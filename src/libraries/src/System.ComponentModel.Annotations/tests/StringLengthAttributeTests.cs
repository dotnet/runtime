// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class StringLengthAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Can_construct_and_get_MaximumLength_and_default_MinimumLength()
        {
            var attribute = new StringLengthAttribute(42);
            Assert.Equal(42, attribute.MaximumLength);
            Assert.Equal(0, attribute.MinimumLength);
        }

        [Fact]
        public static void Can_set_and_get_MinimumLength()
        {
            var attribute = new StringLengthAttribute(42);
            attribute.MinimumLength = 29;
            Assert.Equal(29, attribute.MinimumLength);
        }

        [Fact]
        public static void Validation_throws_InvalidOperationException_for_maximum_less_than_zero()
        {
            var attribute = new StringLengthAttribute(-1);
            Assert.Equal(-1, attribute.MaximumLength);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - MaximumLength < 0", s_testValidationContext));
        }

        [Fact]
        public static void ValidationThrowsIf_minimum_is_greater_than_maximum()
        {
            var attribute = new StringLengthAttribute(42);
            attribute.MinimumLength = 43;
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - MinimumLength > MaximumLength", s_testValidationContext));
        }

        [Fact]
        public static void ValidationThrowsIf_value_passed_is_non_null_non_string()
        {
            var attribute = new StringLengthAttribute(42);
            Assert.Throws<InvalidCastException>(() => attribute.Validate(new object(), s_testValidationContext));
        }

        [Fact]
        public static void Validation_successful_for_valid_strings()
        {
            var attribute = new StringLengthAttribute(12);
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("Valid string", s_testValidationContext));

            attribute.MinimumLength = 5;
            AssertEx.DoesNotThrow(() => attribute.Validate("Valid", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("Valid string", s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_ValidationException_for_invalid_strings()
        {
            var attribute = new StringLengthAttribute(12);
            Assert.Throws<ValidationException>(() => attribute.Validate("Invalid string", s_testValidationContext)); // string too long
            attribute.MinimumLength = 8;
            Assert.Throws<ValidationException>(() => attribute.Validate("Invalid", s_testValidationContext)); // string too short
        }
    }
}

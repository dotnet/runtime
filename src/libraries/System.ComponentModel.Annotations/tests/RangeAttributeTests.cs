// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class RangeAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Can_construct_and_get_minimum_and_maximum_for_int_constructor()
        {
            var attribute = new RangeAttribute(1, 3);
            Assert.Equal(1, attribute.Minimum);
            Assert.Equal(3, attribute.Maximum);
            Assert.Equal(typeof(int), attribute.OperandType);
        }

        [Fact]
        public static void Can_construct_and_get_minimum_and_maximum_for_double_constructor()
        {
            var attribute = new RangeAttribute(1.0, 3.0);
            Assert.Equal(1.0, attribute.Minimum);
            Assert.Equal(3.0, attribute.Maximum);
            Assert.Equal(typeof(double), attribute.OperandType);
        }

        [Fact]
        public static void Can_construct_and_get_minimum_and_maximum_for_type_with_strings_constructor()
        {
            var attribute = new RangeAttribute(null, "SomeMinimum", "SomeMaximum");
            Assert.Equal("SomeMinimum", attribute.Minimum);
            Assert.Equal("SomeMaximum", attribute.Maximum);
            Assert.Equal(null, attribute.OperandType);
        }

        [Fact]
        public static void Can_validate_valid_values_for_int_constructor()
        {
            var attribute = new RangeAttribute(1, 3);
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext)); // empty string is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(1, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(2, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(3, s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_invalid_values_for_int_constructor()
        {
            var attribute = new RangeAttribute(1, 3);
            Assert.Throws<ValidationException>(() => attribute.Validate(0, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(4, s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_valid_values_for_double_constructor()
        {
            var attribute = new RangeAttribute(1.0, 3.0);
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext)); // empty string is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(1.0, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(2.0, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(3.0, s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_invalid_values_for_double_constructor()
        {
            var attribute = new RangeAttribute(1.0, 3.0);
            Assert.Throws<ValidationException>(() => attribute.Validate(0.9999999, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(3.0000001, s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_valid_values_for_integers_using_type_and_strings_constructor()
        {
            var attribute = new RangeAttribute(typeof(int), "1", "3");
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext)); // empty string is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(1, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("1", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(2, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("2", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(3, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("3", s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_invalid_values_for_integers_using_type_and_strings_constructor()
        {
            var attribute = new RangeAttribute(typeof(int), "1", "3");
            Assert.Throws<ValidationException>(() => attribute.Validate(0, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("0", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(4, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("4", s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_valid_values_for_doubles_using_type_and_strings_constructor()
        {
            var attribute = new RangeAttribute(typeof(double), (1.0).ToString("F1"), (3.0).ToString("F1"));
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext)); // empty string is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(1.0, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate((1.0).ToString("F1"), s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(2.0, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate((2.0).ToString("F1"), s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(3.0, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate((3.0).ToString("F1"), s_testValidationContext));
        }

        [Fact]
        public static void Can_validate_invalid_values_for_doubles_using_type_and_strings_constructor()
        {
            var attribute = new RangeAttribute(typeof(double), (1.0).ToString("F1"), (3.0).ToString("F1"));
            Assert.Throws<ValidationException>(() => attribute.Validate(0.9999999, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate((0.9999999).ToString(), s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(3.0000001, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate((3.0000001).ToString(), s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_InvalidOperationException_for_null_OperandType()
        {
            var attribute = new RangeAttribute(null, "someMinimum", "someMaximum");
            Assert.Null(attribute.OperandType);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - OperandType is null", s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_InvalidOperationException_for_OperandType_which_is_not_assignable_from_IComparable()
        {
            var attribute = new RangeAttribute(typeof(InvalidOperandType), "someMinimum", "someMaximum");
            Assert.Equal(typeof(InvalidOperandType), attribute.OperandType);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - OperandType is not assignable from IComparable", s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_InvalidOperationException_if_minimum_is_greater_than_maximum()
        {
            var attribute = new RangeAttribute(3, 1);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - minimum > maximum", s_testValidationContext));

            attribute = new RangeAttribute(3.0, 1.0);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - minimum > maximum", s_testValidationContext));

            attribute = new RangeAttribute(typeof(int), "3", "1");
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - minimum > maximum", s_testValidationContext));

            attribute = new RangeAttribute(typeof(double), (3.0).ToString("F1"), (1.0).ToString("F1"));
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - minimum > maximum", s_testValidationContext));

            attribute = new RangeAttribute(typeof(string), "z", "a");
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - minimum > maximum", s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_FormatException_if_min_and_max_values_cannot_be_converted_to_DateTime_OperandType()
        {
            var attribute = new RangeAttribute(typeof(DateTime), "Cannot Convert", "2014-03-19");
            Assert.Throws<FormatException>(
                () => attribute.Validate("Does not matter - cannot convert minimum to DateTime", s_testValidationContext));

            attribute = new RangeAttribute(typeof(DateTime), "2014-03-19", "Cannot Convert");
            Assert.Throws<FormatException>(
                () => attribute.Validate("Does not matter - cannot convert maximum to DateTime", s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_Exception_if_min_and_max_values_cannot_be_converted_to_int_OperandType()
        {
            var attribute = new RangeAttribute(typeof(int), "Cannot Convert", "3");
            Assert.Throws<FormatException>(
                () => attribute.Validate("Does not matter - cannot convert minimum to int", s_testValidationContext));

            attribute = new RangeAttribute(typeof(int), "1", "Cannot Convert");
            Assert.Throws<FormatException>(
                () => attribute.Validate("Does not matter - cannot convert maximum to int", s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_Exception_if_min_and_max_values_cannot_be_converted_to_double_OperandType()
        {
            var attribute = new RangeAttribute(typeof(double), "Cannot Convert", (3.0).ToString("F1"));
            Assert.Throws<FormatException>(
                () => attribute.Validate("Does not matter - cannot convert minimum to double", s_testValidationContext));

            attribute = new RangeAttribute(typeof(double), (1.0).ToString("F1"), "Cannot Convert");
            Assert.Throws<FormatException>(
                () => attribute.Validate("Does not matter - cannot convert maximum to double", s_testValidationContext));
        }

        public class InvalidOperandType // does not implement IComparable
        {
            public InvalidOperandType(string message) { }
        }
    }
}

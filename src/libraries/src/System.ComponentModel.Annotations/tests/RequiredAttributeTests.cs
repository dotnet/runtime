// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class RequiredAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Can_get_and_set_AllowEmptyStrings()
        {
            var attribute = new RequiredAttribute();
            Assert.False(attribute.AllowEmptyStrings);
            attribute.AllowEmptyStrings = true;
            Assert.True(attribute.AllowEmptyStrings);
            attribute.AllowEmptyStrings = false;
            Assert.False(attribute.AllowEmptyStrings);
        }

        [Fact]
        public static void Validation_throws_ValidationException_for_null_value()
        {
            var attribute = new RequiredAttribute();
            Assert.Throws<ValidationException>(() => attribute.Validate(null, s_testValidationContext));
        }

        [Fact]
        public static void Validation_throws_ValidationException_for_empty_string_if_AllowEmptyStrings_is_false()
        {
            var attribute = new RequiredAttribute();
            attribute.AllowEmptyStrings = false;
            Assert.Throws<ValidationException>(() => attribute.Validate(string.Empty, s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_non_empty_string_if_AllowEmptyStrings_is_false()
        {
            var attribute = new RequiredAttribute();
            AssertEx.DoesNotThrow(() => attribute.Validate("SomeString", s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_empty_string_if_AllowEmptyStrings_is_true()
        {
            var attribute = new RequiredAttribute();
            attribute.AllowEmptyStrings = true;
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_non_string_object()
        {
            var attribute = new RequiredAttribute();
            AssertEx.DoesNotThrow(() => attribute.Validate(new object(), s_testValidationContext));
        }
    }
}

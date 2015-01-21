// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class RegularExpressionAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Can_construct_and_get_pattern()
        {
            var attribute = new RegularExpressionAttribute("SomePattern");
            Assert.Equal("SomePattern", attribute.Pattern);
        }

        [Fact]
        public static void Validation_throws_InvalidOperationException_for_null_or_empty_pattern()
        {
            var attribute = new RegularExpressionAttribute(null);
            Assert.Null(attribute.Pattern);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - Pattern is null", s_testValidationContext));

            attribute = new RegularExpressionAttribute(string.Empty);
            AssertEx.Empty(attribute.Pattern);
            Assert.Throws<InvalidOperationException>(
                () => attribute.Validate("Does not matter - Pattern is empty", s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_null_or_empty_value()
        {
            var attribute = new RegularExpressionAttribute("SomePattern");
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // Null is valid
            AssertEx.DoesNotThrow(() => attribute.Validate(string.Empty, s_testValidationContext)); // Empty string is valid
        }

        [Fact]
        public static void Validate_successful_for_value_matching_pattern()
        {
            var attribute = new RegularExpressionAttribute("defghi");
            AssertEx.DoesNotThrow(() => attribute.Validate("defghi", s_testValidationContext));

            attribute = new RegularExpressionAttribute("[^a]+\\.[^z]+");
            AssertEx.DoesNotThrow(() => attribute.Validate("bcdefghijklmnopqrstuvwxyz.abcdefghijklmnopqrstuvwxy", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_values_which_do_not_match_pattern()
        {
            var attribute = new RegularExpressionAttribute("defghi");
            Assert.Throws<ValidationException>(() => attribute.Validate("zyxwvu", s_testValidationContext)); // pattern does not match
            Assert.Throws<ValidationException>(() => attribute.Validate("defghijkl", s_testValidationContext)); // pattern only matches beginning of value
            Assert.Throws<ValidationException>(() => attribute.Validate("abcdefghi", s_testValidationContext)); // pattern only matches end of value
            Assert.Throws<ValidationException>(() => attribute.Validate("abcdefghijkl", s_testValidationContext)); // pattern only matches part of value

            attribute = new RegularExpressionAttribute("[^a]+\\.[^z]+");
            Assert.Throws<ValidationException>(() => attribute.Validate("aaaaa", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("zzzzz", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("b.z", s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("a.y", s_testValidationContext));
        }
    }
}

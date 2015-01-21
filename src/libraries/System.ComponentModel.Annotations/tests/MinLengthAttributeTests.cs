// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class MinLengthAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Length_returns_set_length()
        {
            Assert.Equal(10, new MinLengthAttribute(10).Length);
            Assert.Equal(0, new MinLengthAttribute(0).Length);

            // This only throws when GetValidationResult is called
            Assert.Equal(-1, new MinLengthAttribute(-1).Length);
        }

        [Fact]
        public static void GetValidationResult_throws_for_negative_lengths()
        {
            var attribute = new MinLengthAttribute(-1);
            Assert.Throws<InvalidOperationException>(
                () => attribute.GetValidationResult("Rincewind", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_throws_for_object_that_is_not_string_or_array()
        {
            Assert.Throws<InvalidCastException>(
                () => new MinLengthAttribute(0).GetValidationResult(new Random(), s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_success_for_null_target()
        {
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(10).GetValidationResult(null, s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_validates_string_length()
        {
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(0).GetValidationResult(string.Empty, s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(12).GetValidationResult("OverMinLength", s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(16).GetValidationResult("EqualToMinLength", s_testValidationContext));
            Assert.NotNull((new MinLengthAttribute(15).GetValidationResult("UnderMinLength", s_testValidationContext)).ErrorMessage);
        }

        [Fact]
        public static void GetValidationResult_validates_array_length()
        {
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(0).GetValidationResult(new int[0], s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(12).GetValidationResult(new int[13], s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MinLengthAttribute(16).GetValidationResult(new string[16], s_testValidationContext));
            Assert.NotNull((new MinLengthAttribute(15).GetValidationResult(new byte[14], s_testValidationContext)).ErrorMessage);
        }
    }
}

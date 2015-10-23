// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class MaxLengthAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Length_returns_set_length()
        {
            Assert.Equal(-1, new MaxLengthAttribute().Length);
            Assert.Equal(-1, new MaxLengthAttribute(-1).Length);
            Assert.Equal(10, new MaxLengthAttribute(10).Length);

            // These only throw when GetValidationResult is called
            Assert.Equal(0, new MaxLengthAttribute(0).Length);
            Assert.Equal(-10, new MaxLengthAttribute(-10).Length);
        }

        [Fact]
        public static void GetValidationResult_throws_for_negative_or_zero_lengths_other_than_negative_one()
        {
            var attribute1 = new MaxLengthAttribute(0);
            Assert.Throws<InvalidOperationException>(
                () => attribute1.GetValidationResult("Twoflower", s_testValidationContext));

            var attribute2 = new MaxLengthAttribute(-10);
            Assert.Throws<InvalidOperationException>(
                () => attribute2.GetValidationResult("Rincewind", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_throws_for_object_that_is_not_string_or_array()
        {
            Assert.Throws<InvalidCastException>(
                () => new MaxLengthAttribute().GetValidationResult(new Random(), s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_success_for_null_target()
        {
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(10).GetValidationResult(null, s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_validates_string_length()
        {
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute().GetValidationResult("UnspecifiedMaxLength", s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(15).GetValidationResult("UnderMaxLength", s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(16).GetValidationResult("EqualToMaxLength", s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(-1).GetValidationResult("SpecifiedMaximumMaxLength", s_testValidationContext));
            Assert.NotNull((new MaxLengthAttribute(12).GetValidationResult("OverMaxLength", s_testValidationContext)).ErrorMessage);
        }

        [Fact]
        public static void GetValidationResult_validates_array_length()
        {
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute().GetValidationResult(new int[500], s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(15).GetValidationResult(new string[14], s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(16).GetValidationResult(new string[16], s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(-1).GetValidationResult(new object[500], s_testValidationContext));
            Assert.NotNull((new MaxLengthAttribute(12).GetValidationResult(new byte[13], s_testValidationContext)).ErrorMessage);
        }

        [Fact]
        public static void GetValidationResult_validates_collection_length()
        {
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute().GetValidationResult(new Collection<int>(new int[500]), s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(15).GetValidationResult(new Collection<string>(new string[14]), s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(16).GetValidationResult(new Collection<string>(new string[16]), s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(-1).GetValidationResult(new Collection<object>(new object[500]), s_testValidationContext));
            Assert.NotNull((new MaxLengthAttribute(12).GetValidationResult(new Collection<byte>(new byte[13]), s_testValidationContext)).ErrorMessage);
        }

        [Fact]
        public static void GetValidationResult_validates_list_length()
        {
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute().GetValidationResult(new List<int>(new int[500]), s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(15).GetValidationResult(new List<string>(new string[14]), s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(16).GetValidationResult(new List<string>(new string[16]), s_testValidationContext));
            Assert.Equal(ValidationResult.Success, new MaxLengthAttribute(-1).GetValidationResult(new List<object>(new object[500]), s_testValidationContext));
            Assert.NotNull((new MaxLengthAttribute(12).GetValidationResult(new List<byte>(new byte[13]), s_testValidationContext)).ErrorMessage);
        }
    }
}

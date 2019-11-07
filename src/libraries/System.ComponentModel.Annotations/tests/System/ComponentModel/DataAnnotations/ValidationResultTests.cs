// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class ValidationResultTests
    {
        [Fact]
        public static void ValidationResult_Success_is_null()
        {
            Assert.Null(ValidationResult.Success);
        }

        [Fact]
        public static void Can_construct_get_and_set_ErrorMessage()
        {
            var validationResult = new ValidationResult("SomeErrorMessage");
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
            validationResult.ErrorMessage = "SomeOtherErrorMessage";
            Assert.Equal("SomeOtherErrorMessage", validationResult.ErrorMessage);
        }

        [Fact]
        public static void MemberNames_are_empty_for_one_arg_constructor()
        {
            var validationResult = new ValidationResult("SomeErrorMessage");
            Assert.Empty(validationResult.MemberNames);
        }

        [Fact]
        public static void MemberNames_can_be_set_through_two_args_constructor()
        {
            var validationResult = new ValidationResult("SomeErrorMessage", null);
            Assert.Empty(validationResult.MemberNames);

            var memberNames = new List<string>() { "firstMember", "secondMember" };
            validationResult = new ValidationResult("SomeErrorMessage", memberNames);
            Assert.True(memberNames.SequenceEqual(validationResult.MemberNames));
        }

        [Theory]
        [InlineData(null, "System.ComponentModel.DataAnnotations.ValidationResult")]
        [InlineData("", "")]
        [InlineData("ErrorMessage", "ErrorMessage")]
        public void ToString_ReturnsExpected(string errorMessage, string expected)
        {
            ValidationResult validationResult = new ValidationResult(errorMessage);
            Assert.Equal(expected, validationResult.ToString());
        }

        [Fact]
        public void Ctor_ValidationResult_ReturnsClone()
        {
            ValidationResult validationResult = new ValidationResult("ErrorMessage", new string[] { "Member1", "Member2" });
            ValidationResultSubClass createdValidationResult = new ValidationResultSubClass(validationResult);
            Assert.Equal(validationResult.ErrorMessage, createdValidationResult.ErrorMessage);
            Assert.Equal(validationResult.MemberNames, createdValidationResult.MemberNames);
        }

        [Fact]
        public void Ctor_ValidationResult_NullValidationResult_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("validationResult", () => new ValidationResultSubClass(null));
        }

        public class ValidationResultSubClass : ValidationResult
        {
            public ValidationResultSubClass(ValidationResult validationResult) : base(validationResult) { }
        }
    }
}

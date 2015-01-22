// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations
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
            AssertEx.Empty(validationResult.MemberNames);
        }

        [Fact]
        public static void MemberNames_can_be_set_through_two_args_constructor()
        {
            var validationResult = new ValidationResult("SomeErrorMessage", null);
            AssertEx.Empty(validationResult.MemberNames);

            var memberNames = new List<string>() { "firstMember", "secondMember" };
            validationResult = new ValidationResult("SomeErrorMessage", memberNames);
            Assert.True(memberNames.SequenceEqual(validationResult.MemberNames));
        }
    }
}

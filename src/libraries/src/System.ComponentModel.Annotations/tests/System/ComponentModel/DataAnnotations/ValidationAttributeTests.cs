// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class ValidationAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Default_value_of_RequiresValidationContext_is_false()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.False(attribute.RequiresValidationContext);
        }

        [Fact]
        public static void Can_get_and_set_ErrorMessage()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.Null(attribute.ErrorMessage);
            attribute.ErrorMessage = "SomeErrorMessage";
            Assert.Equal("SomeErrorMessage", attribute.ErrorMessage);
        }

        [Fact]
        public static void Can_get_and_set_ErrorMessageResourceName()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.Null(attribute.ErrorMessageResourceName);
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            Assert.Equal("SomeErrorMessageResourceName", attribute.ErrorMessageResourceName);
        }

        [Fact]
        public static void Can_get_and_set_ErrorMessageResourceType()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.Null(attribute.ErrorMessageResourceType);
            attribute.ErrorMessageResourceType = typeof(string);
            Assert.Equal(typeof(string), attribute.ErrorMessageResourceType);
        }

        // Public_IsValid_throws_NotImplementedException_if_derived_ValidationAttribute_does_not_override_either_IsValid_method
        [Fact]
        public static void TestThrowIfNoOverrideIsValid()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.Throws<NotImplementedException>(
                () => attribute.IsValid("Does not matter - no override of IsValid"));
        }

        // Validate_object_string_throws_NotImplementedException_if_derived_ValidationAttribute_does_not_override_either_IsValid_method
        [Fact]
        public static void TestThrowIfNoOverrideIsValid01()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.Throws<NotImplementedException>(
                () => attribute.Validate(
                    "Object to validate does not matter - no override of IsValid",
                    "Name to put in error message does not matter either"));
        }

        // Validate_object_ValidationContext_throws_NotImplementedException_if_derived_ValidationAttribute_does_not_override_either_IsValid_method
        [Fact]
        public static void TestThrowIfNoOverrideIsValid02()
        {
            var attribute = new ValidationAttributeNoOverrides();
            Assert.Throws<NotImplementedException>(
                () => attribute.Validate("Object to validate does not matter - no override of IsValid", s_testValidationContext));
        }

        // Validate_object_string_successful_if_derived_ValidationAttribute_overrides_One_Arg_IsValid_method
        [Fact]
        public static void TestNoThrowIfOverrideIsValid()
        {
            var attribute = new ValidationAttributeOverrideOneArgIsValid();
            attribute.Validate("Valid Value", "Name to put in error message does not matter - no error");
        }

        // Validate_object_string_successful_if_derived_ValidationAttribute_overrides_Two_Args_IsValid_method
        [Fact]
        public static void TestNoThrowIfOverrideIsValid01()
        {
            var attribute = new ValidationAttributeOverrideTwoArgsIsValid();
            attribute.Validate("Valid Value", "Name to put in error message does not matter - no error");
        }

        // Validate_object_ValidationContext_successful_if_derived_ValidationAttribute_overrides_One_Arg_IsValid_method
        [Fact]
        public static void TestNoThrowIfOverrideIsValid02()
        {
            var attribute = new ValidationAttributeOverrideOneArgIsValid();
            attribute.Validate("Valid Value", s_testValidationContext);
        }

        // Validate_object_ValidationContext_successful_if_derived_ValidationAttribute_overrides_Two_Args_IsValid_method()
        [Fact]
        public static void TestNoThrowIfOverrideIsValid03()
        {
            var attribute = new ValidationAttributeOverrideTwoArgsIsValid();
            attribute.Validate("Valid Value", s_testValidationContext);
        }

        // Validate_object_string_preferentially_uses_One_Arg_IsValid_method_to_validate
        [Fact]
        public static void TestNoThrowIfOverrideIsValid04()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.Validate("Valid 1-Arg Value", "Name to put in error message does not matter - no error");
            Assert.Throws<ValidationException>(
                () => attribute.Validate("Valid 2-Args Value", "Name to put in error message does not matter - no error"));
        }

        // Validate_object_ValidationContext_preferentially_uses_Two_Args_IsValid_method_to_validate
        [Fact]
        public static void TestNoThrowIfOverrideIsValid05()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            Assert.Throws<ValidationException>(() => attribute.Validate("Valid 1-Arg Value", s_testValidationContext));
            attribute.Validate("Valid 2-Args Value", s_testValidationContext);
        }

        // FormatErrorMessage_throws_InvalidOperationException_if_ErrorMessage_and_ErrorMessageResourceName_are_both_null_or_empty
        [Fact]
        public static void TestThrowIfNullOrEmptyErrorMessage()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = null;
            attribute.ErrorMessageResourceName = null;
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("Name to put in error message does not matter"));

            attribute.ErrorMessage = string.Empty;
            attribute.ErrorMessageResourceName = string.Empty;
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("Name to put in error message does not matter"));
        }

        // FormatErrorMessage_throws_InvalidOperationException_if_ErrorMessage_and_ErrorMessageResourceName_are_both_set
        [Fact]
        public static void TestThrowIfErrorMessageAndNameBothSet()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = "SomeErrorMessage";
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("Name to put in error message does not matter"));
        }

        // FormatErrorMessage_throws_InvalidOperationException_if_ErrorMessageResourceName_set_but_ErrorMessageResourceType_is_not
        [Fact]
        public static void TestFormatErrorMessageThrow()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            attribute.ErrorMessageResourceType = null;
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("Name to put in error message does not matter"));
        }

        // FormatErrorMessage_throws_InvalidOperationException_if_ErrorMessageResourceType_set_but_ErrorMessageResourceName_is_not(
        [Fact]
        public static void TestFormatErrorMessageThrow01()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessageResourceName = string.Empty;
            attribute.ErrorMessageResourceType = typeof(string);
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("Name to put in error message does not matter"));
        }

        // FormatErrorMessage_returns_ErrorMessage_if_only_ErrorMessage_is_set
        [Fact]
        public static void TestFormatErrorMessage()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = "SomeErrorMessage";
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = null;
            Assert.Equal("SomeErrorMessage", attribute.FormatErrorMessage("Name to put in error message does not matter - no placeholder"));
        }

        // FormatErrorMessage_returns_ErrorMessage_with_name_if_only_ErrorMessage_is_set
        [Fact]
        public static void TestFormatErrorMessage01()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = "SomeErrorMessage with name <{0}> here";
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = null;
            Assert.Equal(
                string.Format("SomeErrorMessage with name <{0}> here", "Error Message Name"),
                attribute.FormatErrorMessage("Error Message Name"));
        }

        // FormatErrorMessage_throws_InvalidOperationException_if_ErrorMessageResourceType_and_ErrorMessageResourceName_point_to_non_existent_resource
        [Fact]
        public static void TestFormatErrorMessage02()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessageResourceName = "NonExistentErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ValidationAttributeOverrideBothIsValids);
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("Name to put in error message does not matter"));
        }

        // FormatErrorMessage_returns_ErrorMessage_from_resource_if_only_ErrorMessageResourceName_and_ErrorMessageResourceType_are_set
        [Fact]
        public static void TestFormatErrorMessage03()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = string.Empty;
            attribute.ErrorMessageResourceName = "PublicErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ValidationAttributeOverrideBothIsValids);
            Assert.Equal(ValidationAttributeOverrideBothIsValids.PublicErrorMessageTestProperty,
                attribute.FormatErrorMessage("Name to put in error message does not matter - no placeholder"));
        }

        // FormatErrorMessage_returns_ErrorMessage_from_resource_with_name_if_only_ErrorMessageResourceName_and_ErrorMessageResourceType_are_set
        [Fact]
        public static void TestFormatErrorMessage04()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = string.Empty;
            attribute.ErrorMessageResourceName = "PublicErrorMessageTestPropertyWithName";
            attribute.ErrorMessageResourceType = typeof(ValidationAttributeOverrideBothIsValids);
            Assert.Equal(
                string.Format(
                    ValidationAttributeOverrideBothIsValids.PublicErrorMessageTestPropertyWithName,
                    "Error Message Name"),
                attribute.FormatErrorMessage("Error Message Name"));
        }

        // Validate_object_string_throws_exception_with_ValidationResult_ErrorMessage_matching_ErrorMessage_passed_in
        [Fact]
        public static void TestThrowWithValidationResultErrorMessage()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = "SomeErrorMessage with name <{0}> here";
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = null;
            var exception = Assert.Throws<ValidationException>(() => attribute.Validate("Invalid Value", "Error Message Name"));
            Assert.Equal("Invalid Value", exception.Value);
            Assert.Equal(
                string.Format(
                    "SomeErrorMessage with name <{0}> here",
                    "Error Message Name"),
                exception.ValidationResult.ErrorMessage);
        }

        // Validate_object_string_throws_exception_with_ValidationResult_ErrorMessage_from_ErrorMessageResourceName_and_ErrorMessageResourceType
        [Fact]
        public static void TestThrowWithValidationResultErrorMessage01()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            attribute.ErrorMessage = string.Empty;
            attribute.ErrorMessageResourceName = "PublicErrorMessageTestPropertyWithName";
            attribute.ErrorMessageResourceType = typeof(ValidationAttributeOverrideBothIsValids);
            var exception = Assert.Throws<ValidationException>(() => attribute.Validate("Invalid Value", "Error Message Name"));
            Assert.Equal("Invalid Value", exception.Value);
            Assert.Equal(
                string.Format(
                    ValidationAttributeOverrideBothIsValids.PublicErrorMessageTestPropertyWithName,
                    "Error Message Name"),
                exception.ValidationResult.ErrorMessage);
        }

        // FormatErrorMessage_is_successful_when_ErrorMessage_from_ErrorMessageResourceName_and_ErrorMessageResourceType_point_to_internal_property
        [Fact]
        public static void TestFormatErrorMessage05()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids()
            {
                ErrorMessageResourceName = "InternalErrorMessageTestProperty",
                ErrorMessageResourceType = typeof(ErrorMessageResources)
            };

            Assert.Equal(
                ErrorMessageResources.InternalErrorMessageTestProperty,
                attribute.FormatErrorMessage("Ignored by this error message"));
        }

        // GetValidationResult_throws_ArgumentNullException_if_ValidationContext_is_null(
        [Fact]
        public static void TestThrowIfNullValidationContext()
        {
            var attribute = new ValidationAttributeOverrideBothIsValids();
            Assert.Throws<ArgumentNullException>(() => attribute.GetValidationResult("Does not matter", validationContext: null));
        }

        [Fact]
        public static void Validate_NullValidationContext_ThrowsArgumentNullException()
        {
            ValidationAttributeOverrideBothIsValids attribute = new ValidationAttributeOverrideBothIsValids();
            AssertExtensions.Throws<ArgumentNullException>("validationContext", () => attribute.Validate("Any", validationContext: null));
        }

        // GetValidationResult_successful_if_One_Arg_IsValid_validates_successfully
        [Fact]
        public static void TestOneArgsIsValidValidatesSuccessfully()
        {
            var attribute = new ValidationAttributeOverrideOneArgIsValid();
            attribute.GetValidationResult("Valid Value", s_testValidationContext);
        }

        // GetValidationResult_successful_if_Two_Args_IsValid_validates_successfully
        [Fact]
        public static void TestTwoArgsIsValidValidatesSuccessfully()
        {
            var attribute = new ValidationAttributeOverrideTwoArgsIsValid();
            attribute.GetValidationResult("Valid Value", s_testValidationContext);
        }

        // GetValidationResult_returns_ValidationResult_with_preset_error_message_if_One_Arg_IsValid_fails_to_validate
        [Fact]
        public static void TestReturnPresetErrorMessage()
        {
            var attribute = new ValidationAttributeOverrideOneArgIsValid();
            var toBeTested = new ToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "PropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.NotNull(validationResult); // validationResult == null would be success
                                              // cannot check error message - not defined on ret builds
        }

        // GetValidationResult_returns_ValidationResult_with_error_message_defined_in_IsValid_if_Two_Args_IsValid_fails_to_validate
        [Fact]
        public static void TestReturnErrorMessageInIsValid()
        {
            var attribute = new ValidationAttributeOverrideTwoArgsIsValid();
            var toBeTested = new ToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "PropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.NotNull(validationResult); // validationResult == null would be success
                                              // cannot check error message - not defined on ret builds
        }

        [Fact]
        public void GetValidationResult_NullErrorMessage_ProvidesErrorMessage()
        {
            ValidationAttributeAlwaysInvalidNullErrorMessage attribute = new ValidationAttributeAlwaysInvalidNullErrorMessage();
            ValidationResult validationResult = attribute.GetValidationResult("abc", new ValidationContext(new object()));
            Assert.NotEmpty(validationResult.ErrorMessage);
        }

        [Fact]
        public void GetValidationResult_EmptyErrorMessage_ProvidesErrorMessage()
        {
            ValidationAttributeAlwaysInvalidEmptyErrorMessage attribute = new ValidationAttributeAlwaysInvalidEmptyErrorMessage();
            ValidationResult validationResult = attribute.GetValidationResult("abc", new ValidationContext(new object()));
            Assert.NotEmpty(validationResult.ErrorMessage);
        }

        public class ValidationAttributeNoOverrides : ValidationAttribute
        {
        }

        public class ValidationAttributeOverrideOneArgIsValid : ValidationAttribute
        {
            public override bool IsValid(object value)
            {
                var valueAsString = value as string;
                if ("Valid Value".Equals(valueAsString)) { return true; }
                return false;
            }
        }

        public class ValidationAttributeOverrideTwoArgsIsValid : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                var valueAsString = value as string;
                if ("Valid Value".Equals(valueAsString)) { return ValidationResult.Success; }
                return new ValidationResult("TestValidationAttributeOverrideTwoArgsIsValid IsValid failed");
            }
        }

        public class ValidationAttributeOverrideBothIsValids : ValidationAttribute
        {
            public static string PublicErrorMessageTestProperty
            {
                get { return "Error Message from PublicErrorMessageTestProperty"; }
            }

            public static string PublicErrorMessageTestPropertyWithName
            {
                get { return "Error Message from PublicErrorMessageTestProperty With Name <{0}>"; }
            }

            public override bool IsValid(object value)
            {
                var valueAsString = value as string;
                if ("Valid 1-Arg Value".Equals(valueAsString)) { return true; }
                return false;
            }

            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                var valueAsString = value as string;
                if ("Valid 2-Args Value".Equals(valueAsString)) { return ValidationResult.Success; }
                return new ValidationResult("TestValidationAttributeOverrideTwoArgsIsValid IsValid failed");
            }
        }

        public class ValidationAttributeAlwaysInvalidNullErrorMessage : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext) => new ValidationResult(null);
        }

        public class ValidationAttributeAlwaysInvalidEmptyErrorMessage : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext) => new ValidationResult(string.Empty);
        }

        public class ToBeTested
        {
            public string PropertyToBeTested
            {
                get { return "PropertyToBeTested"; }
            }
        }
    }

    internal class ErrorMessageResources
    {
        internal static string InternalErrorMessageTestProperty
        {
            get { return "Error Message from ErrorMessageResources.InternalErrorMessageTestProperty"; }
        }

        internal string InstanceProperty => "";
        private static string PrivateProperty => "";
        internal string SetOnlyProperty { set { } }
        internal static bool BoolProperty => false;
    }
}

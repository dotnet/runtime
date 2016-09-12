// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class ValidatorTests
    {
        public static readonly ValidationContext s_estValidationContext = new ValidationContext(new object());

        #region TryValidateObject

        [Fact]
        public static void TryValidateObjectThrowsIf_ValidationContext_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateObject(new object(), validationContext: null, validationResults: null));

            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateObject(new object(), validationContext: null, validationResults: null, validateAllProperties: false));
        }

        [Fact]
        public static void TryValidateObjectThrowsIf_instance_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateObject(null, s_estValidationContext, validationResults: null));

            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateObject(null, s_estValidationContext, validationResults: null, validateAllProperties: false));
        }

        // TryValidateObjectThrowsIf_instance_does_not_match_ValidationContext_ObjectInstance
        [Fact]
        public static void TestTryValidateObjectThrowsIfInstanceNotMatch()
        {
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateObject(new object(), s_estValidationContext, validationResults: null));

            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateObject(new object(), s_estValidationContext, validationResults: null, validateAllProperties: true));
        }

        [Fact]
        public static void TryValidateObject_returns_true_if_no_errors()
        {
            var objectToBeValidated = "ToBeValidated";
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults: null));
            Assert.True(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults: null, validateAllProperties: true));
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_errors()
        {
            var objectToBeValidated = new ToBeValidated()
            {
                PropertyToBeTested = "Invalid Value",
                PropertyWithRequiredAttribute = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.False(
                Validator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        // TryValidateObject_returns_true_if_validateAllProperties_is_false_and_Required_test_passes_even_if_there_are_other_errors()
        [Fact]
        public static void TestTryValidateObjectSuccessEvenWithOtherErrors()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Invalid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(
                Validator.TryValidateObject(objectToBeValidated, validationContext, null, false));

            var validationResults = new List<ValidationResult>();
            Assert.True(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, false));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_validateAllProperties_is_true_and_Required_test_fails()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = null };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.False(
                Validator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static void TryValidateObject_returns_true_if_validateAllProperties_is_true_and_all_attributes_are_valid()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(
                Validator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.True(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_all_properties_are_valid_but_class_is_invalid()
        {
            var objectToBeValidated = new InvalidToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.False(
                Validator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidClassAttribute.IsValid failed for class of type " + typeof(InvalidToBeValidated).FullName, validationResults[0].ErrorMessage);
        }

        #endregion TryValidateObject

        #region ValidateObject

        [Fact]
        public static void ValidateObjectThrowsIf_ValidationContext_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateObject(new object(), validationContext: null));

            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateObject(new object(), validationContext: null, validateAllProperties: false));
        }

        [Fact]
        public static void ValidateObjectThrowsIf_instance_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateObject(null, s_estValidationContext));

            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateObject(null, s_estValidationContext, false));
        }

        [Fact]
        public static void ValidateObjectThrowsIf_instance_does_not_match_ValidationContext_ObjectInstance()
        {
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateObject(new object(), s_estValidationContext));

            Assert.Throws<ArgumentException>(
                () => Validator.ValidateObject(new object(), s_estValidationContext, true));
        }

        [Fact]
        public static void ValidateObject_succeeds_if_no_errors()
        {
            var objectToBeValidated = "ToBeValidated";
            var validationContext = new ValidationContext(objectToBeValidated);
            Validator.ValidateObject(objectToBeValidated, validationContext);
            Validator.ValidateObject(objectToBeValidated, validationContext, true);
        }

        [Fact]
        public static void ValidateObject_throws_ValidationException_if_errors()
        {
            var objectToBeValidated = new ToBeValidated()
            {
                PropertyToBeTested = "Invalid Value",
                PropertyWithRequiredAttribute = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        // ValidateObject_returns_true_if_validateAllProperties_is_false_and_Required_test_passes_even_if_there_are_other_errors
        [Fact]
        public static void TestValidateObjectNotThrowIfvalidateAllPropertiesFalse()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Invalid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Validator.ValidateObject(objectToBeValidated, validationContext, false);
        }

        // ValidateObject_throws_ValidationException_if_validateAllProperties_is_true_and_Required_test_fails
        [Fact]
        public static void TestValidateObjectThrowsIfRequiredTestFails()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = null };
            var validationContext = new ValidationContext(objectToBeValidated);
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            // cannot check error message - not defined on ret builds
            Assert.Null(exception.Value);
        }

        [Fact]
        public static void ValidateObject_succeeds_if_validateAllProperties_is_true_and_all_attributes_are_valid()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Validator.ValidateObject(objectToBeValidated, validationContext, true);
        }

        [Fact]
        public static void ValidateObject_throws_ValidationException_if_all_properties_are_valid_but_class_is_invalid()
        {
            var objectToBeValidated = new InvalidToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.IsType<ValidClassAttribute>(exception.ValidationAttribute);
            Assert.Equal(
                "ValidClassAttribute.IsValid failed for class of type " + typeof(InvalidToBeValidated).FullName,
                exception.ValidationResult.ErrorMessage);
            Assert.Equal(objectToBeValidated, exception.Value);
        }

        #endregion ValidateObject

        #region TryValidateProperty

        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateProperty(new object(), validationContext: null, validationResults: null));
        }

        [Fact]
        public static void TryValidatePropertyThrowsIf_value_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateProperty(null, s_estValidationContext, validationResults: null));
        }

        // TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_null_or_empty()
        [Fact]
        public static void TestTryValidatePropertyThrowsIfNullOrEmptyValidationContextMemberName()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = string.Empty;
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateProperty(null, validationContext, null));
        }

        // TryValidatePropertyThrowsIf_ValidationContext_MemberName_does_not_exist_on_object()
        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_MemberName_does_not_exist_on_object()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NonExist";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, null));
        }

        // TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "InternalProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = "ProtectedProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = "PrivateProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, null));
        }

        // TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "Item";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        // TryValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        [Fact]
        public static void TryValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            validationContext.MemberName = "NoAttributesProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(123, validationContext, validationResults: null));
        }

        [Fact]
        public static void TryValidatePropertyThrowsIf_null_passed_to_non_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            // cannot assign null to a non-value-type property
            validationContext.MemberName = "EnumProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, validationResults: null));

            // cannot assign null to a non-nullable property
            validationContext.MemberName = "NonNullableProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        [Fact]
        public static void TryValidateProperty_returns_true_if_null_passed_to_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NullableProperty";
            Assert.True(Validator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        [Fact]
        public static void TryValidateProperty_returns_true_if_no_attributes_to_validate()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            Assert.True(
                Validator.TryValidateProperty("Any Value", validationContext, validationResults: null));
        }

        [Fact]
        public static void TryValidateProperty_returns_false_if_errors()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            Assert.False(
                Validator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateProperty_returns_false_if_Required_attribute_test_fails()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            Assert.False(
                Validator.TryValidateProperty(null, validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                Validator.TryValidateProperty(null, validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static void TryValidateProperty_returns_true_if_all_attributes_are_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            Assert.True(
                Validator.TryValidateProperty("Valid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.True(
                Validator.TryValidateProperty("Valid Value", validationContext, validationResults));
            Assert.Equal(0, validationResults.Count);
        }

        #endregion TryValidateProperty

        #region ValidateProperty

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateProperty(new object(), validationContext: null));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_value_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateProperty(null, s_estValidationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_MemberName_is_null_or_empty()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateProperty(null, validationContext));

            validationContext.MemberName = string.Empty;
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_MemberName_does_not_exist_on_object()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NonExist";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "InternalProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));

            validationContext.MemberName = "ProtectedProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));

            validationContext.MemberName = "PrivateProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "Item";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            validationContext.MemberName = "NoAttributesProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(123, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_null_passed_to_non_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            // cannot assign null to a non-value-type property
            validationContext.MemberName = "EnumProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));

            // cannot assign null to a non-nullable property
            validationContext.MemberName = "NonNullableProperty";
            Assert.Throws<ArgumentException>(
                () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidateProperty_succeeds_if_null_passed_to_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NullableProperty";
            Validator.ValidateProperty(null, validationContext);
        }

        [Fact]
        public static void ValidateProperty_succeeds_if_no_attributes_to_validate()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            Validator.ValidateProperty("Any Value", validationContext);
        }

        [Fact]
        public static void ValidateProperty_throws_ValidationException_if_errors()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static void ValidateProperty_throws_ValidationException_if_Required_attribute_test_fails()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty(null, validationContext));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            // cannot check error message - not defined on ret builds
            Assert.Null(exception.Value);
        }

        [Fact]
        public static void ValidateProperty_succeeds_if_all_attributes_are_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            Validator.ValidateProperty("Valid Value", validationContext);
        }

        #endregion ValidateProperty

        #region TryValidateValue

        [Fact]
        public static void TryValidateValueThrowsIf_ValidationContext_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateValue(new object(),
                    validationContext: null, validationResults: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static void TryValidateValueThrowsIf_ValidationAttributeEnumerable_is_null()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            Assert.Throws<ArgumentNullException>(
                () => Validator.TryValidateValue(new object(), validationContext, validationResults: null, validationAttributes: null));
        }

        [Fact]
        public static void TryValidateValue_returns_true_if_no_attributes_to_validate_regardless_of_value()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            Assert.True(Validator.TryValidateValue(null, validationContext,
                validationResults: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
            Assert.True(Validator.TryValidateValue(new object(), validationContext,
                validationResults: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static void TryValidateValue_returns_false_if_Property_has_RequiredAttribute_and_value_is_null()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.False(Validator.TryValidateValue(null, validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateValue(null, validationContext, validationResults, attributesToValidate));
            Assert.Equal(1, validationResults.Count);
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static void TryValidateValue_returns_false_if_Property_has_RequiredAttribute_and_value_is_invalid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.False(Validator.TryValidateValue("Invalid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateValue("Invalid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateValue_returns_true_if_Property_has_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.True(Validator.TryValidateValue("Valid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.True(Validator.TryValidateValue("Valid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static void TryValidateValue_returns_false_if_Property_has_no_RequiredAttribute_and_value_is_invalid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Assert.False(Validator.TryValidateValue("Invalid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateValue("Invalid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateValue_returns_true_if_Property_has_no_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Assert.True(Validator.TryValidateValue("Valid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.True(Validator.TryValidateValue("Valid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(0, validationResults.Count);
        }

        #endregion TryValidateValue

        #region ValidateValue

        [Fact]
        public static void ValidateValueThrowsIf_ValidationContext_is_null()
        {
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateValue(new object(),
                    validationContext: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static void ValidateValueThrowsIf_ValidationAttributeEnumerable_is_null()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            Assert.Throws<ArgumentNullException>(
                () => Validator.ValidateValue(new object(), validationContext, validationAttributes: null));
        }

        [Fact]
        public static void ValidateValue_succeeds_if_no_attributes_to_validate_regardless_of_value()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            Validator.ValidateValue(null, validationContext, Enumerable.Empty<ValidationAttribute>());
            Validator.ValidateValue(new object(), validationContext, Enumerable.Empty<ValidationAttribute>());
        }

        // ValidateValue_throws_ValidationException_if_Property_has_RequiredAttribute_and_value_is_null()
        [Fact]
        public static void TestValidateValueThrowsIfNullRequiredAttribute()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateValue(null, validationContext, attributesToValidate));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            // cannot check error message - not defined on ret builds
            Assert.Null(exception.Value);
        }

        // ValidateValue_throws_ValidationException_if_Property_has_RequiredAttribute_and_value_is_invalid()
        [Fact]
        public static void TestValidateValueThrowsIfRequiredAttributeInvalid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateValue("Invalid Value", validationContext, attributesToValidate));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static void ValidateValue_succeeds_if_Property_has_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Validator.ValidateValue("Valid Value", validationContext, attributesToValidate);
        }

        // ValidateValue_throws_ValidationException_if_Property_has_no_RequiredAttribute_and_value_is_invalid()
        [Fact]
        public static void TestValidateValueThrowsIfNoRequiredAttribute()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateValue("Invalid Value", validationContext, attributesToValidate));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static void ValidateValue_succeeds_if_Property_has_no_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Validator.ValidateValue("Valid Value", validationContext, attributesToValidate);
        }

        #endregion ValidateValue

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class ValidValueStringPropertyAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                if (value == null) { return ValidationResult.Success; }
                var valueAsString = value as string;
                if ("Valid Value".Equals(valueAsString)) { return ValidationResult.Success; }
                return new ValidationResult("ValidValueStringPropertyAttribute.IsValid failed for value " + value);
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class ValidClassAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                if (value == null) { return ValidationResult.Success; }
                if (value.GetType().Name.ToLowerInvariant().Contains("invalid"))
                {
                    return new ValidationResult("ValidClassAttribute.IsValid failed for class of type " + value.GetType().FullName);
                }
                return ValidationResult.Success;
            }
        }

        [ValidClass]
        public class ToBeValidated
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string NoAttributesProperty { get; set; }

            [Required]
            [ValidValueStringProperty]
            public string PropertyWithRequiredAttribute { get; set; }

            internal string InternalProperty { get; set; }
            protected string ProtectedProperty { get; set; }
            private string PrivateProperty { get; set; }

            public string this[int index]
            {
                get { return null; }
                set { }
            }

            public TestEnum EnumProperty { get; set; }

            public int NonNullableProperty { get; set; }
            public int? NullableProperty { get; set; }
        }

        public enum TestEnum
        {
            A = 0
        }

        [ValidClass]
        public class InvalidToBeValidated
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string NoAttributesProperty { get; set; }

            [Required]
            [ValidValueStringProperty]
            public string PropertyWithRequiredAttribute { get; set; }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            AssertExtensions.Throws<ArgumentException>("instance", () => Validator.TryValidateObject(new object(), s_estValidationContext, validationResults: null));
            AssertExtensions.Throws<ArgumentException>("instance", () => Validator.TryValidateObject(new object(), s_estValidationContext, validationResults: null, validateAllProperties: true));
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

        [Fact]
        public static void TryValidateObject_collection_can_have_multiple_results()
        {
            HasDoubleFailureProperty objectToBeValidated = new HasDoubleFailureProperty();
            ValidationContext validationContext = new ValidationContext(objectToBeValidated);
            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, results, true));
            Assert.Equal(2, results.Count);
        }


        [Fact]
        public static void TryValidateObject_collection_can_have_multiple_results_from_type_attributes()
        {
            DoublyInvalid objectToBeValidated = new DoublyInvalid();
            ValidationContext validationContext = new ValidationContext(objectToBeValidated);
            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, results, true));
            Assert.Equal(2, results.Count);
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

        [Fact]
        public void TryValidateObject_IValidatableObject_Success()
        {
            var instance = new ValidatableSuccess();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(instance, context, results));
            Assert.Empty(results);
        }

        public class ValidatableSuccess : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new ValidationResult[] { ValidationResult.Success };
            }
        }

        [Fact]
        public void TryValidateObject_IValidatableObject_Error()
        {
            var instance = new ValidatableError();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(instance, context, results));
            Assert.Equal("error", Assert.Single(results).ErrorMessage);
        }

        public class ValidatableError : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new ValidationResult[] { new ValidationResult("error") };
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Null check not present in .NET Framework. See https://github.com/dotnet/runtime/issues/24237")]
        public void TryValidateObject_IValidatableObject_Null()
        {
            var instance = new ValidatableNull();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(instance, context, results));
            Assert.Equal(0, results.Count);
        }

        public class ValidatableNull : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return null;
            }
        }

        [Fact]
        public void TryValidateObject_RequiredNonNull_Success()
        {
            var instance = new RequiredFailure { Required = "Text" };
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(instance, context, results));
            Assert.Empty(results);
        }

        [Fact]
        public void TryValidateObject_RequiredNull_Error()
        {
            var instance = new RequiredFailure();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(instance, context, results));
            Assert.Contains("Required", Assert.Single(results).ErrorMessage);
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The SecondPropertyToBeTested field is required.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value",
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_has_unmatched_property_name()
        {
            var objectToBeValidated = new HasMetadataTypeWithUnmatchedProperties()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithUnmatchedProperties), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithUnmatchedProperties));

            var validationResults = new List<ValidationResult>();
            var exception = Assert.Throws<InvalidOperationException>(
                () => Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal("The associated metadata type for type 'System.ComponentModel.DataAnnotations.Tests.ValidatorTests+HasMetadataTypeWithUnmatchedProperties' contains the following unknown properties or fields: SecondPropertyToBeTested. Please make sure that the names of these members match the names of the properties on the main type.",
                exception.Message);
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is required.");
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var objectToBeValidated = new HasMetadataTypeWithComplementaryRequirements()
            {
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.");
        }

        [Fact]
        public static void TryValidateObject_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var objectToBeValidated = new SelfMetadataType()
            {
                PropertyToBeTested = "Invalid Value",
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
        }

        public class RequiredFailure
        {
            [Required]
            public string Required { get; set; }
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
            AssertExtensions.Throws<ArgumentException>("instance", () => Validator.ValidateObject(new object(), s_estValidationContext));
            AssertExtensions.Throws<ArgumentException>("instance", () => Validator.ValidateObject(new object(), s_estValidationContext, true));
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

        [Fact]
        public void ValidateObject_IValidatableObject_Success()
        {
            var instance = new ValidatableSuccess();
            var context = new ValidationContext(instance);

            Validator.ValidateObject(instance, context);
        }

        [Fact]
        public void ValidateObject_IValidatableObject_Error()
        {
            var instance = new ValidatableError();
            var context = new ValidationContext(instance);
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(instance, context));
            Assert.Equal("error", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Null check not present in .NET Framework. See https://github.com/dotnet/runtime/issues/24237")]
        public void ValidateObject_IValidatableObject_Null()
        {
            var instance = new ValidatableNull();
            var context = new ValidationContext(instance);

            Validator.ValidateObject(instance, context);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field is required.", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value",
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_type_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value",
                SecondPropertyToBeTested = "TypeInvalid"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field mustn't be \"TypeInvalid\".", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_has_unmatched_property_name()
        {
            var objectToBeValidated = new HasMetadataTypeWithUnmatchedProperties()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithUnmatchedProperties), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithUnmatchedProperties));

            var exception = Assert.Throws<InvalidOperationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The associated metadata type for type 'System.ComponentModel.DataAnnotations.Tests.ValidatorTests+HasMetadataTypeWithUnmatchedProperties' contains the following unknown properties or fields: SecondPropertyToBeTested. Please make sure that the names of these members match the names of the properties on the main type.",
                exception.Message);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value",
                exception.Message);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var objectToBeValidated = new HasMetadataTypeWithComplementaryRequirements()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));

            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value",
                exception.Message);

            objectToBeValidated.PropertyToBeTested = null;
            objectToBeValidated.SecondPropertyToBeTested = "Not Phone #";

            exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.",
                exception.Message);

            objectToBeValidated.SecondPropertyToBeTested = "0800123456789";

            exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.",
                exception.Message);
        }

        [Fact]
        public static void ValidateObject_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var objectToBeValidated = new SelfMetadataType()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));

            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value",
                exception.Message);

            objectToBeValidated.PropertyToBeTested = null;
            objectToBeValidated.SecondPropertyToBeTested = "Not Phone #";

            exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.",
                exception.Message);
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

        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_MemberName_does_not_exist_on_object()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NonExist";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.TryValidateProperty(null, validationContext, null));
        }

        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "InternalProperty";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = "ProtectedProperty";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = "PrivateProperty";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.TryValidateProperty(null, validationContext, null));
        }

        [Fact]
        public static void TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "Item";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        [Fact]
        public static void TryValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            validationContext.MemberName = "NoAttributesProperty";
            AssertExtensions.Throws<ArgumentException>("value", () => Validator.TryValidateProperty(123, validationContext, validationResults: null));
        }

        [Fact]
        public static void TryValidatePropertyThrowsIf_null_passed_to_non_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            // cannot assign null to a non-value-type property
            validationContext.MemberName = "EnumProperty";
            AssertExtensions.Throws<ArgumentException>("value", () => Validator.TryValidateProperty(null, validationContext, validationResults: null));

            // cannot assign null to a non-nullable property
            validationContext.MemberName = "NonNullableProperty";
            AssertExtensions.Throws<ArgumentException>("value", () => Validator.TryValidateProperty(null, validationContext, validationResults: null));
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
        public static void TryValidateProperty_collection_can_have_multiple_results()
        {
            ValidationContext validationContext = new ValidationContext(new HasDoubleFailureProperty());
            validationContext.MemberName = nameof(HasDoubleFailureProperty.WillAlwaysFailTwice);
            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateProperty("Nope", validationContext, results));
            Assert.Equal(2, results.Count);
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

        [Fact]
        public static void TryValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            Assert.False(Validator.TryValidateProperty(null, validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateProperty(null, validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The SecondPropertyToBeTested field is required.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateProperty_returns_true_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static void TryValidateProperty_returns_true_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeWithComplementaryRequirements());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.");
        }

        [Fact]
        public static void TryValidateProperty_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var validationContext = new ValidationContext(new SelfMetadataType());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value");

            validationContext.MemberName = "SecondPropertyToBeTested";
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, null));

            validationResults.Clear();
            Assert.False(Validator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            //Assert.Equal(1, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
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
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "InternalProperty";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.ValidateProperty(null, validationContext));

            validationContext.MemberName = "ProtectedProperty";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.ValidateProperty(null, validationContext));

            validationContext.MemberName = "PrivateProperty";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "Item";
            AssertExtensions.Throws<ArgumentException>("propertyName", () => Validator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            validationContext.MemberName = "NoAttributesProperty";
            AssertExtensions.Throws<ArgumentException>("value", () => Validator.ValidateProperty(123, validationContext));
        }

        [Fact]
        public static void ValidatePropertyThrowsIf_null_passed_to_non_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            // cannot assign null to a non-value-type property
            validationContext.MemberName = "EnumProperty";
            AssertExtensions.Throws<ArgumentException>("value", () => Validator.ValidateProperty(null, validationContext));

            // cannot assign null to a non-nullable property
            validationContext.MemberName = "NonNullableProperty";
            AssertExtensions.Throws<ArgumentException>("value", () => Validator.ValidateProperty(null, validationContext));
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

        [Fact]
        public static void ValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty(null, validationContext));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            Assert.Null(exception.Value);
        }

        [Fact]
        public static void ValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<MaxLengthAttribute>(exception.ValidationAttribute);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static void ValidateProperty_returns_false_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static void ValidateProperty_returns_false_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeWithComplementaryRequirements());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);

            validationContext.MemberName = "SecondPropertyToBeTested";
            exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Not Phone #", validationContext));
            Assert.IsType<PhoneAttribute>(exception.ValidationAttribute);
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Not Phone #", exception.Value);

            exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("0800123456789", validationContext));
            Assert.IsType<MaxLengthAttribute>(exception.ValidationAttribute);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", exception.ValidationResult.ErrorMessage);
            Assert.Equal("0800123456789", exception.Value);
        }

        [Fact]
        public static void ValidateProperty_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var validationContext = new ValidationContext(new SelfMetadataType());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));
            var exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);

            validationContext.MemberName = "SecondPropertyToBeTested";
            exception = Assert.Throws<ValidationException>(
                () => Validator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<PhoneAttribute>(exception.ValidationAttribute);
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
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
        public static void TryValidateValue_collection_can_have_multiple_results()
        {
            ValidationContext validationContext = new ValidationContext(new HasDoubleFailureProperty());
            validationContext.MemberName = nameof(HasDoubleFailureProperty.WillAlwaysFailTwice);
            ValidationAttribute[] attributesToValidate =
                {new ValidValueStringPropertyAttribute(), new ValidValueStringPropertyDuplicateAttribute()};

            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateValue("Not Valid", validationContext, results, attributesToValidate));
            Assert.Equal(2, results.Count);
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

        // Allows easy testing that multiple failures can be reported
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class ValidValueStringPropertyDuplicateAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                if (value == null)
                { return ValidationResult.Success; }
                var valueAsString = value as string;
                if ("Valid Value".Equals(valueAsString))
                { return ValidationResult.Success; }
                return new ValidationResult("ValidValueStringPropertyAttribute.IsValid failed for value " + value);
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class ValidClassAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                if (value == null)
                { return ValidationResult.Success; }
                if (value.GetType().Name.ToLowerInvariant().Contains("invalid"))
                {
                    return new ValidationResult("ValidClassAttribute.IsValid failed for class of type " + value.GetType().FullName);
                }
                return ValidationResult.Success;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class ValidClassDuplicateAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext _)
            {
                if (value == null)
                { return ValidationResult.Success; }
                if (value.GetType().Name.ToLowerInvariant().Contains("invalid"))
                {
                    return new ValidationResult("ValidClassAttribute.IsValid failed for class of type " + value.GetType().FullName);
                }
                return ValidationResult.Success;
            }
        }

        public class HasDoubleFailureProperty
        {
            [ValidValueStringProperty, ValidValueStringPropertyDuplicate]
            public string WillAlwaysFailTwice => "This is never valid.";
        }

        [ValidClass, ValidClassDuplicate]
        public class DoublyInvalid
        {
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

            // Private properties should not be validated.

            [Required]
            private string PrivateSetOnlyProperty { set { } }

            [Required]
            protected string ProtectedSetOnlyProperty { set { } }

            [Required]
            internal string InternalSetOnlyProperty { set { } }

            [Required]
            protected internal string ProtectedInternalSetOnlyProperty { set { } }

            [Required]
            private string PrivateGetOnlyProperty { get; }

            [Required]
            protected string ProtectedGetOnlyProperty { get; }

            [Required]
            internal string InternalGetOnlyProperty { get; }

            [Required]
            protected internal string ProtectedInternalGetOnlyProperty { get; }
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

        public class HasMetadataTypeToBeValidated
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string SecondPropertyToBeTested { get; set; }
        }

        public class HasMetadataTypeWithUnmatchedProperties
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string MismatchedNameProperty { get; set; }
        }

        public class HasMetadataTypeWithComplementaryRequirements
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            [Phone]
            public string SecondPropertyToBeTested { get; set; }
        }

        public class SelfMetadataType
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            [Phone]
            public string SecondPropertyToBeTested { get; set; }
        }

        [CustomValidation(typeof(MetadataTypeToAddValidationAttributes), nameof(Validate))]
        public class MetadataTypeToAddValidationAttributes
        {
            [Required]
            [MaxLength(11)]
            public string SecondPropertyToBeTested { get; set; }

            public static ValidationResult Validate(HasMetadataTypeToBeValidated value)
                => value.SecondPropertyToBeTested == "TypeInvalid"
                    ? new ValidationResult("The SecondPropertyToBeTested field mustn't be \"TypeInvalid\".")
                    : ValidationResult.Success;
        }
    }
}

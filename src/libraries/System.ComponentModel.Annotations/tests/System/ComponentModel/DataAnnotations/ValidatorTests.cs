// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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

        [Fact]
        public static void TryValidateObject_for_JObject_does_not_throw()
        {
            var objectToBeValidated = JObject.Parse("{\"Enabled\":true}");
            var results = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(objectToBeValidated, new ValidationContext(objectToBeValidated), results, true));
            Assert.Empty(results);
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

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
        public class AsyncAlwaysFailsAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                return Task.FromResult<ValidationResult?>(new ValidationResult("Async validation always fails"));
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
        public class AsyncAlwaysSucceedsAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                return Task.FromResult<ValidationResult?>(null);
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
        public class AsyncDelayedAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override async Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                await Task.Yield();
                if (value is string s && s == "Valid Value")
                    return ValidationResult.Success;

                return new ValidationResult("Async delayed validation failed");
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
        public class AsyncCancellableAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override async Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();

                return ValidationResult.Success;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class AsyncClassAlwaysFailsAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                return Task.FromResult<ValidationResult?>(new ValidationResult("Async class validation failed"));
            }
        }

        public class HasAsyncProperty
        {
            [AsyncAlwaysFails]
            public string AsyncProp { get; set; }
        }

        public class HasAsyncSucceedingProperty
        {
            [AsyncAlwaysSucceeds]
            public string AsyncProp { get; set; }
        }

        public class HasTrulyAsyncProperty
        {
            [AsyncDelayed]
            public string AsyncProp { get; set; }
        }

        public class HasAsyncCancellableProperty
        {
            [AsyncCancellable]
            public string CancellableProp { get; set; }
        }

        public class HasMixedValidation
        {
            [ValidValueStringProperty]
            [AsyncAlwaysFails]
            public string MixedProp { get; set; }
        }

        public class HasMixedPassingValidation
        {
            [ValidValueStringProperty]
            [AsyncAlwaysSucceeds]
            public string MixedProp { get; set; }
        }

        public class HasRequiredAndAsyncProperty
        {
            [Required]
            [AsyncAlwaysFails]
            public string Prop { get; set; }
        }

        [AsyncClassAlwaysFails]
        public class HasAsyncClassLevelAttr
        {
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
        public class AsyncDelayedSucceedsAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override async Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();

                return ValidationResult.Success;
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
        public class AsyncDelayedFailsAttribute : AsyncValidationAttribute
        {
            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override async Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();

                return new ValidationResult("Async delayed validation failed");
            }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
        public class AsyncConcurrencyProbeAttribute : AsyncValidationAttribute
        {
            public static int ConcurrentCount;
            public static TaskCompletionSource<bool> AllRunningGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public static int ExpectedCount;

            public static void Reset(int expectedCount)
            {
                ConcurrentCount = 0;
                AllRunningGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                ExpectedCount = expectedCount;
            }

            protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            protected override async Task<ValidationResult?> IsValidAsync(
                object? value, ValidationContext validationContext, CancellationToken cancellationToken)
            {
                int current = Interlocked.Increment(ref ConcurrentCount);
                if (current >= ExpectedCount)
                    AllRunningGate.TrySetResult(true);

                await AllRunningGate.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                Interlocked.Decrement(ref ConcurrentCount);

                return ValidationResult.Success;
            }
        }

        public class HasMultipleAsyncProperties
        {
            [AsyncDelayedSucceeds]
            public string Prop1 { get; set; } = "value";

            [AsyncDelayedSucceeds]
            public string Prop2 { get; set; } = "value";
        }

        public class HasMultipleConcurrencyProbeProperties
        {
            [AsyncConcurrencyProbe]
            public string Prop1 { get; set; } = "value";

            [AsyncConcurrencyProbe]
            public string Prop2 { get; set; } = "value";
        }

        public class HasMultipleFailingAsyncProperties
        {
            [AsyncDelayedFails]
            public string Prop1 { get; set; } = "value";

            [AsyncDelayedFails]
            public string Prop2 { get; set; } = "value";
        }

        public class AsyncValidatableSuccess : IAsyncValidatableObject
        {
            IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            public async IAsyncEnumerable<ValidationResult> ValidateAsync(
                ValidationContext validationContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield return ValidationResult.Success;
            }
        }

        public class AsyncValidatableError : IAsyncValidatableObject
        {
            IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            public async IAsyncEnumerable<ValidationResult> ValidateAsync(
                ValidationContext validationContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield return new ValidationResult("async object error");
            }
        }

        public class AsyncValidatableNull : IAsyncValidatableObject
        {
            IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

#pragma warning disable CS1998
            public async IAsyncEnumerable<ValidationResult> ValidateAsync(
                ValidationContext validationContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield break;
            }
#pragma warning restore CS1998
        }

        public class DualValidatableModel : IAsyncValidatableObject
        {
            IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
            {
                return new ValidationResult[] { new ValidationResult("sync error from dual model") };
            }

            public async IAsyncEnumerable<ValidationResult> ValidateAsync(
                ValidationContext validationContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield return new ValidationResult("async error from dual model");
            }
        }

        public class AsyncValidatableWithRequired : IAsyncValidatableObject
        {
            [Required]
            public string RequiredProp { get; set; }

            IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
                => throw new InvalidOperationException("Use async validation");

            public async IAsyncEnumerable<ValidationResult> ValidateAsync(
                ValidationContext validationContext, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield return new ValidationResult("async object error");
            }
        }

        [Fact]
        public static async Task TryValidateObjectAsyncThrowsIf_instance_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidateObjectAsync(null, s_estValidationContext, null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidateObjectAsync(null, s_estValidationContext, null, false));
        }

        [Fact]
        public static async Task TryValidateObjectAsyncThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidateObjectAsync(new object(), null, null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidateObjectAsync(new object(), null, null, false));
        }

        [Fact]
        public static async Task TryValidateObjectAsync_ThrowsIf_instance_does_not_match_ValidationContext()
        {
            await AssertExtensions.ThrowsAsync<ArgumentException>("instance",
                async () => await Validator.TryValidateObjectAsync(new object(), s_estValidationContext, null));
        }

        [Fact]
        public static async Task TryValidateObjectAsync_returns_true_if_no_errors()
        {
            var objectToBeValidated = "ToBeValidated";
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(await Validator.TryValidateObjectAsync(objectToBeValidated, validationContext, null));
            Assert.True(await Validator.TryValidateObjectAsync(objectToBeValidated, validationContext, null, true));
        }

        [Fact]
        public static async Task TryValidateObjectAsync_returns_false_with_sync_errors()
        {
            var objectToBeValidated = new ToBeValidated()
            {
                PropertyToBeTested = "Invalid Value",
                PropertyWithRequiredAttribute = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            var validationResults = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_returns_false_with_async_attr_failure()
        {
            var obj = new HasAsyncProperty { AsyncProp = "anything" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
            Assert.Equal("Async validation always fails", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_returns_true_with_async_attr_success()
        {
            var obj = new HasAsyncSucceedingProperty { AsyncProp = "anything" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_validateAllProperties_false_only_checks_Required()
        {
            var obj = new HasAsyncProperty { AsyncProp = "anything" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, results, false));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_Required_fails_before_async()
        {
            var obj = new HasRequiredAndAsyncProperty { Prop = null };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_collection_can_have_multiple_results()
        {
            HasDoubleFailureProperty obj = new HasDoubleFailureProperty();
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_returns_false_if_class_level_attribute_fails()
        {
            var obj = new InvalidToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var ctx = new ValidationContext(obj);
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, null, true));

            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
            Assert.Equal("ValidClassAttribute.IsValid failed for class of type " + typeof(InvalidToBeValidated).FullName, results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IValidatableObject_Null()
        {
            var instance = new ValidatableNull();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(instance, ctx, results));
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public static async Task ValidateObjectAsyncThrowsIf_instance_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidateObjectAsync(null, s_estValidationContext));

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidateObjectAsync(null, s_estValidationContext, false));
        }

        [Fact]
        public static async Task ValidateObjectAsyncThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidateObjectAsync(new object(), null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidateObjectAsync(new object(), null, false));
        }

        [Fact]
        public static async Task ValidateObjectAsync_ThrowsIf_instance_does_not_match()
        {
            await AssertExtensions.ThrowsAsync<ArgumentException>("instance",
                async () => await Validator.ValidateObjectAsync(new object(), s_estValidationContext));
        }

        [Fact]
        public static async Task ValidateObjectAsync_succeeds_if_no_errors()
        {
            var obj = "ToBeValidated";
            var ctx = new ValidationContext(obj);
            await Validator.ValidateObjectAsync(obj, ctx);
            await Validator.ValidateObjectAsync(obj, ctx, true);
        }

        [Fact]
        public static async Task ValidateObjectAsync_throws_ValidationException_if_sync_errors()
        {
            var obj = new ToBeValidated()
            {
                PropertyToBeTested = "Invalid Value",
                PropertyWithRequiredAttribute = "Valid Value"
            };
            var ctx = new ValidationContext(obj);
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateObjectAsync(obj, ctx, true));
            Assert.IsType<ValidValueStringPropertyAttribute>(ex.ValidationAttribute);
        }

        [Fact]
        public static async Task ValidateObjectAsync_throws_ValidationException_if_async_attr_fails()
        {
            var obj = new HasAsyncProperty { AsyncProp = "anything" };
            var ctx = new ValidationContext(obj);
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateObjectAsync(obj, ctx, true));
            Assert.Equal("Async validation always fails", ex.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidateObjectAsync_succeeds_validateAllProperties_false()
        {
            var obj = new HasAsyncProperty { AsyncProp = "anything" };
            var ctx = new ValidationContext(obj);
            await Validator.ValidateObjectAsync(obj, ctx, false);
        }

        [Fact]
        public static async Task ValidateObjectAsync_throws_if_class_level_sync_attribute_fails()
        {
            var obj = new InvalidToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var ctx = new ValidationContext(obj);
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateObjectAsync(obj, ctx, true));
            Assert.IsType<ValidClassAttribute>(ex.ValidationAttribute);
            Assert.Equal("ValidClassAttribute.IsValid failed for class of type " + typeof(InvalidToBeValidated).FullName, ex.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidatePropertyAsync(new object(), null, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_true_if_no_errors()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NoAttributesProperty";
            Assert.True(await Validator.TryValidatePropertyAsync("Any Value", ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_false_with_sync_attr_failure()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyToBeTested";
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidatePropertyAsync("Invalid Value", ctx, results));
            Assert.Equal(1, results.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_false_with_async_attr_failure()
        {
            var obj = new HasAsyncProperty();
            var ctx = new ValidationContext(obj);
            ctx.MemberName = nameof(HasAsyncProperty.AsyncProp);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidatePropertyAsync("anything", ctx, results));
            Assert.Equal(1, results.Count);
            Assert.Equal("Async validation always fails", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_true_with_async_attr_success()
        {
            var obj = new HasAsyncSucceedingProperty();
            var ctx = new ValidationContext(obj);
            ctx.MemberName = nameof(HasAsyncSucceedingProperty.AsyncProp);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidatePropertyAsync("anything", ctx, results));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_value_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidatePropertyAsync(null, s_estValidationContext, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_MemberName_is_null_or_empty()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = null;
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));

            ctx.MemberName = string.Empty;
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_MemberName_does_not_exist()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NonExist";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_MemberName_is_not_public()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "InternalProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));

            ctx.MemberName = "ProtectedProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));

            ctx.MemberName = "PrivateProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_MemberName_is_indexer()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "Item";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_value_is_wrong_type()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NoAttributesProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value",
                async () => await Validator.TryValidatePropertyAsync(123, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_ThrowsIf_null_passed_to_non_nullable_property()
        {
            var ctx = new ValidationContext(new ToBeValidated());

            ctx.MemberName = "EnumProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));

            ctx.MemberName = "NonNullableProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value",
                async () => await Validator.TryValidatePropertyAsync(null, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_true_if_null_passed_to_nullable_property()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NullableProperty";
            Assert.True(await Validator.TryValidatePropertyAsync(null, ctx, null));
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_false_if_Required_fails()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            Assert.False(await Validator.TryValidatePropertyAsync(null, ctx, null));

            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidatePropertyAsync(null, ctx, results));
            Assert.Equal(1, results.Count);
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_collection_can_have_multiple_results()
        {
            var ctx = new ValidationContext(new HasDoubleFailureProperty());
            ctx.MemberName = nameof(HasDoubleFailureProperty.WillAlwaysFailTwice);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidatePropertyAsync("Nope", ctx, results));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static async Task TryValidatePropertyAsync_returns_true_if_all_attributes_are_valid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            Assert.True(await Validator.TryValidatePropertyAsync("Valid Value", ctx, null));

            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidatePropertyAsync("Valid Value", ctx, results));
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidatePropertyAsync(new object(), null));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_succeeds_if_no_errors()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NoAttributesProperty";
            await Validator.ValidatePropertyAsync("Any Value", ctx);
        }

        [Fact]
        public static async Task ValidatePropertyAsync_throws_ValidationException_with_sync_attr_failure()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyToBeTested";
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidatePropertyAsync("Invalid Value", ctx));
            Assert.IsType<ValidValueStringPropertyAttribute>(ex.ValidationAttribute);
        }

        [Fact]
        public static async Task ValidatePropertyAsync_throws_ValidationException_with_async_attr_failure()
        {
            var obj = new HasAsyncProperty();
            var ctx = new ValidationContext(obj);
            ctx.MemberName = nameof(HasAsyncProperty.AsyncProp);
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidatePropertyAsync("anything", ctx));
            Assert.Equal("Async validation always fails", ex.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_value_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidatePropertyAsync(null, s_estValidationContext));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_MemberName_is_null_or_empty()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = null;
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidatePropertyAsync(null, ctx));

            ctx.MemberName = string.Empty;
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidatePropertyAsync(null, ctx));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_MemberName_does_not_exist()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NonExist";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.ValidatePropertyAsync(null, ctx));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_MemberName_is_not_public()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "InternalProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.ValidatePropertyAsync(null, ctx));

            ctx.MemberName = "ProtectedProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.ValidatePropertyAsync(null, ctx));

            ctx.MemberName = "PrivateProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.ValidatePropertyAsync(null, ctx));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_MemberName_is_indexer()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "Item";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName",
                async () => await Validator.ValidatePropertyAsync(null, ctx));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_value_is_wrong_type()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NoAttributesProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value",
                async () => await Validator.ValidatePropertyAsync(123, ctx));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_ThrowsIf_null_passed_to_non_nullable()
        {
            var ctx = new ValidationContext(new ToBeValidated());

            ctx.MemberName = "EnumProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value",
                async () => await Validator.ValidatePropertyAsync(null, ctx));

            ctx.MemberName = "NonNullableProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value",
                async () => await Validator.ValidatePropertyAsync(null, ctx));
        }

        [Fact]
        public static async Task ValidatePropertyAsync_succeeds_if_null_passed_to_nullable()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "NullableProperty";
            await Validator.ValidatePropertyAsync(null, ctx);
        }

        [Fact]
        public static async Task ValidatePropertyAsync_throws_if_Required_fails()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidatePropertyAsync(null, ctx));
            Assert.IsType<RequiredAttribute>(ex.ValidationAttribute);
            Assert.Null(ex.Value);
        }

        [Fact]
        public static async Task ValidatePropertyAsync_succeeds_if_all_attributes_valid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            await Validator.ValidatePropertyAsync("Valid Value", ctx);
        }

        [Fact]
        public static async Task TryValidateValueAsync_ThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidateValueAsync(
                    new object(), null, null, Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static async Task TryValidateValueAsync_ThrowsIf_attributes_is_null()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.TryValidateValueAsync(new object(), ctx, null, null));
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_true_if_no_attributes()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            Assert.True(await Validator.TryValidateValueAsync(
                "any", ctx, null, Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_false_with_async_attr_failure()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new AsyncAlwaysFailsAttribute() };
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync("anything", ctx, results, attrs));
            Assert.Equal(1, results.Count);
            Assert.Equal("Async validation always fails", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_true_with_async_attr_success()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new AsyncAlwaysSucceedsAttribute() };
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateValueAsync("anything", ctx, results, attrs));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateValueAsync_sync_Required_failure_blocks_async()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new RequiredAttribute(), new AsyncAlwaysFailsAttribute() };
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync(null, ctx, results, attrs));
            Assert.Equal(1, results.Count);
        }

        [Fact]
        public static async Task TryValidateValueAsync_sync_attr_failure_blocks_async()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute(), new AsyncAlwaysFailsAttribute() };
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync("Invalid Value", ctx, results, attrs));
            Assert.Equal(1, results.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValueAsync_sync_passes_then_async_runs()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute(), new AsyncAlwaysFailsAttribute() };
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync("Valid Value", ctx, results, attrs));
            Assert.Equal(1, results.Count);
            Assert.Equal("Async validation always fails", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_true_if_Required_and_valid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var attrs = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.True(await Validator.TryValidateValueAsync("Valid Value", ctx, null, attrs));

            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateValueAsync("Valid Value", ctx, results, attrs));
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_false_if_Required_and_invalid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var attrs = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.False(await Validator.TryValidateValueAsync("Invalid Value", ctx, null, attrs));

            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync("Invalid Value", ctx, results, attrs));
            Assert.Equal(1, results.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_true_if_no_Required_and_valid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyToBeTested";
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Assert.True(await Validator.TryValidateValueAsync("Valid Value", ctx, null, attrs));

            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateValueAsync("Valid Value", ctx, results, attrs));
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public static async Task TryValidateValueAsync_returns_false_if_no_Required_and_invalid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyToBeTested";
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Assert.False(await Validator.TryValidateValueAsync("Invalid Value", ctx, null, attrs));

            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync("Invalid Value", ctx, results, attrs));
            Assert.Equal(1, results.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValueAsync_collection_can_have_multiple_results()
        {
            var ctx = new ValidationContext(new HasDoubleFailureProperty());
            ctx.MemberName = nameof(HasDoubleFailureProperty.WillAlwaysFailTwice);
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute(), new ValidValueStringPropertyDuplicateAttribute() };
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateValueAsync("Not Valid", ctx, results, attrs));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static async Task ValidateValueAsync_ThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidateValueAsync(
                    new object(), null, Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static async Task ValidateValueAsync_ThrowsIf_attributes_is_null()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Validator.ValidateValueAsync(new object(), ctx, null));
        }

        [Fact]
        public static async Task ValidateValueAsync_succeeds_if_no_attributes()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            await Validator.ValidateValueAsync("any", ctx, Enumerable.Empty<ValidationAttribute>());
        }

        [Fact]
        public static async Task ValidateValueAsync_throws_ValidationException_with_async_attr_failure()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new AsyncAlwaysFailsAttribute() };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateValueAsync("anything", ctx, attrs));
            Assert.Equal("Async validation always fails", ex.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidateValueAsync_succeeds_with_async_attr_success()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new AsyncAlwaysSucceedsAttribute() };
            await Validator.ValidateValueAsync("anything", ctx, attrs);
        }

        [Fact]
        public static async Task ValidateValueAsync_throws_if_Required_and_null()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var attrs = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateValueAsync(null, ctx, attrs));
            Assert.IsType<RequiredAttribute>(ex.ValidationAttribute);
            Assert.Null(ex.Value);
        }

        [Fact]
        public static async Task ValidateValueAsync_throws_if_Required_and_invalid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var attrs = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateValueAsync("Invalid Value", ctx, attrs));
            Assert.IsType<ValidValueStringPropertyAttribute>(ex.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", ex.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", ex.Value);
        }

        [Fact]
        public static async Task ValidateValueAsync_succeeds_if_Required_and_valid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var attrs = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            await Validator.ValidateValueAsync("Valid Value", ctx, attrs);
        }

        [Fact]
        public static async Task ValidateValueAsync_throws_if_no_Required_and_invalid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyWithRequiredAttribute";
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateValueAsync("Invalid Value", ctx, attrs));
            Assert.IsType<ValidValueStringPropertyAttribute>(ex.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", ex.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", ex.Value);
        }

        [Fact]
        public static async Task ValidateValueAsync_succeeds_if_no_Required_and_valid()
        {
            var ctx = new ValidationContext(new ToBeValidated());
            ctx.MemberName = "PropertyToBeTested";
            var attrs = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            await Validator.ValidateValueAsync("Valid Value", ctx, attrs);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IAsyncValidatableObject_Success()
        {
            var instance = new AsyncValidatableSuccess();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(instance, ctx, results));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IAsyncValidatableObject_Error()
        {
            var instance = new AsyncValidatableError();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(instance, ctx, results));
            Assert.Equal("async object error", Assert.Single(results).ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IAsyncValidatableObject_Null_Result()
        {
            var instance = new AsyncValidatableNull();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(instance, ctx, results));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IAsyncValidatableObject_Preferred_Over_IValidatableObject()
        {
            var instance = new DualValidatableModel();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(instance, ctx, results));
            Assert.Equal("async error from dual model", Assert.Single(results).ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IValidatableObject_Fallback()
        {
            var instance = new ValidatableError();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(instance, ctx, results));
            Assert.Equal("error", Assert.Single(results).ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_IAsyncValidatableObject_SkippedIfPropertyErrors()
        {
            var instance = new AsyncValidatableWithRequired { RequiredProp = null };
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(instance, ctx, results, true));
            Assert.Equal(1, results.Count);
            Assert.DoesNotContain(results, r => r.ErrorMessage == "async object error");
        }

        [Fact]
        public static async Task TryValidateObjectAsync_CancellationToken_Propagated()
        {
            var obj = new HasAsyncCancellableProperty { CancellableProp = "value" };
            var ctx = new ValidationContext(obj);
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await Validator.TryValidateObjectAsync(obj, ctx, null, true, cts.Token));
        }

        [Fact]
        public static async Task TryValidateValueAsync_CancellationToken_Propagated()
        {
            var ctx = new ValidationContext(new object());
            var attrs = new ValidationAttribute[] { new AsyncCancellableAttribute() };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await Validator.TryValidateValueAsync("value", ctx, null, attrs, cts.Token));
        }

        [Fact]
        public static async Task ValidateObjectAsync_CancellationToken_Propagated()
        {
            var obj = new HasAsyncCancellableProperty { CancellableProp = "value" };
            var ctx = new ValidationContext(obj);
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await Validator.ValidateObjectAsync(obj, ctx, true, cts.Token));
        }

        [Fact]
        public static async Task TwoPhase_SyncFailure_BlocksAsyncExecution()
        {
            var obj = new HasMixedValidation { MixedProp = "Invalid Value" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TwoPhase_SyncPasses_AsyncRuns()
        {
            var obj = new HasMixedValidation { MixedProp = "Valid Value" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
            Assert.Equal("Async validation always fails", results[0].ErrorMessage);
        }

        [Fact]
        public static async Task TwoPhase_AllPass()
        {
            var obj = new HasMixedPassingValidation { MixedProp = "Valid Value" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TwoPhase_Required_Fails_AsyncSkipped()
        {
            var obj = new HasRequiredAndAsyncProperty { Prop = null };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
            Assert.DoesNotContain(results, r => r.ErrorMessage == "Async validation always fails");
        }

        [Fact]
        public static async Task TryValidateObjectAsync_EmptyObject_ReturnsTrue()
        {
            var obj = new object();
            var ctx = new ValidationContext(obj);
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, null));
        }

        [Fact]
        public static async Task TryValidateObjectAsync_OnlyAsyncAttrs_Success()
        {
            var obj = new HasAsyncSucceedingProperty { AsyncProp = "anything" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_TrulyAsyncAttr_Success()
        {
            var obj = new HasTrulyAsyncProperty { AsyncProp = "Valid Value" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_TrulyAsyncAttr_Failure()
        {
            var obj = new HasTrulyAsyncProperty { AsyncProp = "Invalid" };
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(1, results.Count);
        }

        [Fact]
        public static async Task ValidateObjectAsync_ClassLevel_AsyncAttr()
        {
            var obj = new HasAsyncClassLevelAttr();
            var ctx = new ValidationContext(obj);
            var ex = await Assert.ThrowsAsync<ValidationException>(
                async () => await Validator.ValidateObjectAsync(obj, ctx, true));
            Assert.Equal("Async class validation failed", ex.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_MultipleAsyncProperties_RunInParallel()
        {
            AsyncConcurrencyProbeAttribute.Reset(expectedCount: 2);
            var obj = new HasMultipleConcurrencyProbeProperties();
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.True(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateObjectAsync_MultipleAsyncProperties_CollectsAllFailures()
        {
            var obj = new HasMultipleFailingAsyncProperties();
            var ctx = new ValidationContext(obj);
            var results = new List<ValidationResult>();
            Assert.False(await Validator.TryValidateObjectAsync(obj, ctx, results, true));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static async Task TryValidateValueAsync_MultipleAsyncAttrs_RunInParallel()
        {
            AsyncConcurrencyProbeAttribute.Reset(expectedCount: 2);
            var ctx = new ValidationContext(new object()) { MemberName = "TestProp" };
            var results = new List<ValidationResult>();
            var attrs = new ValidationAttribute[] { new AsyncConcurrencyProbeAttribute(), new AsyncConcurrencyProbeAttribute() };
            Assert.True(await Validator.TryValidateValueAsync("Valid Value", ctx, results, attrs));
            Assert.Empty(results);
        }

        [Fact]
        public static async Task TryValidateValueAsync_MultipleAsyncAttrs_CollectsAllFailures()
        {
            var ctx = new ValidationContext(new object()) { MemberName = "TestProp" };
            var results = new List<ValidationResult>();
            var attrs = new ValidationAttribute[] { new AsyncAlwaysFailsAttribute(), new AsyncAlwaysFailsAttribute() };
            Assert.False(await Validator.TryValidateValueAsync("anything", ctx, results, attrs));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static void TryValidateObject_IAsyncValidatableObject_SyncPath_ThrowsInvalidOperation()
        {
            var instance = new AsyncValidatableError();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.Throws<InvalidOperationException>(
                () => Validator.TryValidateObject(instance, ctx, results));
        }

        [Fact]
        public static void TryValidateObject_DualModel_SyncPath_UsesExplicitValidate()
        {
            var instance = new DualValidatableModel();
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(instance, ctx, results));
            Assert.Equal("sync error from dual model", Assert.Single(results).ErrorMessage);
        }

        [Fact]
        public static void IAsyncValidatableObject_InheritsIValidatableObject()
        {
            var instance = new AsyncValidatableSuccess();
            Assert.IsAssignableFrom<IValidatableObject>(instance);
        }
    }
}

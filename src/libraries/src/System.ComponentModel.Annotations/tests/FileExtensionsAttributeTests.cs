// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class FileExtensionsAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void FileExtensionsAttribute_creation_DataType_and_CustomDataType()
        {
            var attribute = new FileExtensionsAttribute();
            Assert.Equal(DataType.Upload, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }

        [Fact]
        public static void Can_get_and_set_Extensions()
        {
            var attribute = new FileExtensionsAttribute();
            Assert.Equal("png,jpg,jpeg,gif", attribute.Extensions); // default value
            attribute.Extensions = "test1, test2, test3";
            Assert.Equal("test1, test2, test3", attribute.Extensions);

            // reverts to default value if set to empty
            attribute.Extensions = string.Empty;
            Assert.Equal("png,jpg,jpeg,gif", attribute.Extensions);

            // reverts to default value if set to whitespace
            attribute.Extensions = " \t\r\n";
            Assert.Equal("png,jpg,jpeg,gif", attribute.Extensions);

            // reverts to default value if set to null
            attribute.Extensions = null;
            Assert.Equal("png,jpg,jpeg,gif", attribute.Extensions);
        }

        [Fact]
        public static void Validate_successful_for_null_value()
        {
            var attribute = new FileExtensionsAttribute();
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext)); // Null is valid
        }

        [Fact]
        public static void Validate_successful_for_contained_extensions()
        {
            var attribute = new FileExtensionsAttribute();
            AssertEx.DoesNotThrow(() => attribute.Validate(".jpeg", s_testValidationContext));

            // mixture of dotted and non-dotted extensions, separated by spaces as well as commas
            attribute.Extensions = "myExt, .otherExt, UPPERCASE_extension";
            AssertEx.DoesNotThrow(() => attribute.Validate("myfile.myExt", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("some.Other.File.otherext", s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate("Case.Does.Not.matter.uppercase_EXTENSION", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_for_non_contained_extensions()
        {
            var attribute = new FileExtensionsAttribute();
            Assert.Throws<ValidationException>(() => attribute.Validate(string.Empty, s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate("someFile.nonContainedExtension", s_testValidationContext));

            // mixture of dotted and non-dotted extensions, separated by spaces as well as commas
            attribute.Extensions = "myExt, .otherExt, UPPERCASE_extension";
            Assert.Throws<ValidationException>(() => attribute.Validate("someFile.nonContainedExtension", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_is_null()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.ErrorMessage = null; // note: this overrides the default value
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("someFile.nonContainedExtension", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessage_and_ErrorMessageResourceName_are_set()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("someFile.nonContainedExtension", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceName_set_but_ErrorMessageResourceType_not_set()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.ErrorMessageResourceName = "SomeErrorMessageResourceName";
            attribute.ErrorMessageResourceType = null;
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("someFile.nonContainedExtension", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_if_ErrorMessageResourceType_set_but_ErrorMessageResourceName_not_set()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.ErrorMessageResourceName = null;
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("someFile.nonContainedExtension", s_testValidationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_if_ErrorMessage_overrides_default()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.ErrorMessage = "SomeErrorMessage";
            var toBeTested = new FileExtensionsClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "FileExtensionsPropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }


        [Fact]
        public static void GetValidationResult_returns_DefaultErrorMessage_if_ErrorMessage_is_not_set()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.Extensions = "";
            var toBeTested = new FileExtensionsClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "FileExtensionsPropertyToBeTested";
            AssertEx.DoesNotThrow(() => attribute.GetValidationResult(toBeTested, validationContext));
        }

        [Fact]
        public static void GetValidationResult_returns_ErrorMessage_from_resource_if_ErrorMessageResourceName_and_ErrorMessageResourceType_both_set()
        {
            var attribute = new FileExtensionsAttribute();
            attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            var toBeTested = new FileExtensionsClassToBeTested();
            var validationContext = new ValidationContext(toBeTested);
            validationContext.MemberName = "FileExtensionsPropertyToBeTested";
            var validationResult = attribute.GetValidationResult(toBeTested, validationContext);
            Assert.Equal(
                "Error Message from ErrorMessageResources.InternalErrorMessageTestProperty",
                validationResult.ErrorMessage);
        }
    }

    public class FileExtensionsClassToBeTested
    {
        public string FileExtensionsPropertyToBeTested
        {
            get { return "someFile.nonContainedExtension"; }
        }
    }
}

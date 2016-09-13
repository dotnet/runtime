// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public abstract class ValidationAttributeTestBase
    {
        protected abstract IEnumerable<TestCase> ValidValues();
        protected abstract IEnumerable<TestCase> InvalidValues();

        protected virtual bool RespectsErrorMessage => true;
        private Type InvalidErrorMessage_Type => RespectsErrorMessage ? typeof(InvalidOperationException) : typeof(ValidationException);

        [Fact]
        public void Validate_Valid()
        {
            Assert.All(ValidValues(), test => Validate(test.Attribute, test.Value, test.ValidationContext, isValid: true));
        }

        [Fact]
        public void Validate_Invalid()
        {
            Assert.All(InvalidValues(), test => Validate(test.Attribute, test.Value, test.ValidationContext, isValid: false));
        }

        public void Validate(ValidationAttribute attribute, object value, ValidationContext validationContext, bool isValid)
        {
            if (isValid)
            {
                attribute.Validate(value, validationContext);
                Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(value, validationContext));
            }
            else
            {
                Assert.Throws<ValidationException>(() => attribute.Validate(value, validationContext));
                Assert.NotNull(attribute.GetValidationResult(value, validationContext));
            }
            if (!attribute.RequiresValidationContext)
            {
                Assert.Equal(isValid, attribute.IsValid(value));
            }
        }

        [Fact]
        public void ErrorMessage_Invalid_Throws()
        {
            if (InvalidValues().Count() == 0)
            {
                return;
            }
            ErrorMessageNotSet(InvalidValues().First());
            ErrorMessageSet_ErrorMessageResourceNameSet(InvalidValues().First());
            ErrorMessageResourceNameSet_ErrorMessageResourceTypeNotSet(InvalidValues().First());
            ErrorMessageResourceNameNotSet_ErrorMessageResourceTypeSet(InvalidValues().First());
        }

        [Fact]
        public void ErrorMessage_Valid()
        {
            if (!RespectsErrorMessage || InvalidValues().Count() == 0)
            {
                return;
            }
            ErrorMessageSet_ReturnsOverridenValue(InvalidValues().First());
            ErrorMessageNotSet_ReturnsDefaultValue(InvalidValues().First());
            ErrorMessageSetFromResource_ReturnsExpectedValue(InvalidValues().First());
        }
        
        private void ErrorMessageNotSet(TestCase test)
        {
            test.Attribute.ErrorMessage = null;
            Assert.Throws(InvalidErrorMessage_Type, () => test.Attribute.Validate(test.Value, test.ValidationContext));
        }
        
        private void ErrorMessageSet_ErrorMessageResourceNameSet(TestCase test)
        {
            test.Attribute.ErrorMessage = "Some";
            test.Attribute.ErrorMessageResourceName = "Some";
            Assert.Throws(InvalidErrorMessage_Type, () => test.Attribute.Validate(test.Value, test.ValidationContext));
        }
        
        private void ErrorMessageResourceNameSet_ErrorMessageResourceTypeNotSet(TestCase test)
        {
            test.Attribute.ErrorMessageResourceName = "Some";
            test.Attribute.ErrorMessageResourceType = null;
            Assert.Throws(InvalidErrorMessage_Type, () => test.Attribute.Validate(test.Value, test.ValidationContext));
        }
        
        private void ErrorMessageResourceNameNotSet_ErrorMessageResourceTypeSet(TestCase test)
        {
            test.Attribute.ErrorMessageResourceName = null;
            test.Attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);
            Assert.Throws(InvalidErrorMessage_Type, () => test.Attribute.Validate(test.Value, test.ValidationContext));
        }
        
        private void ErrorMessageSet_ReturnsOverridenValue(TestCase test)
        {
            test.Attribute.ErrorMessage = "SomeErrorMessage";

            var validationResult = test.Attribute.GetValidationResult(test.Value, test.ValidationContext);
            Assert.Equal("SomeErrorMessage", validationResult.ErrorMessage);
        }
        
        private void ErrorMessageNotSet_ReturnsDefaultValue(TestCase test)
        {
            test.Attribute.GetValidationResult(test.Value, test.ValidationContext);
        }
        
        private void ErrorMessageSetFromResource_ReturnsExpectedValue(TestCase test)
        {
            test.Attribute.ErrorMessageResourceName = "InternalErrorMessageTestProperty";
            test.Attribute.ErrorMessageResourceType = typeof(ErrorMessageResources);

            var validationResult = test.Attribute.GetValidationResult(test.Value, test.ValidationContext);
            Assert.Equal("Error Message from ErrorMessageResources.InternalErrorMessageTestProperty", validationResult.ErrorMessage);
        }

        protected class TestCase
        {
            public ValidationAttribute Attribute { get; }
            public object Value { get; }
            public ValidationContext ValidationContext { get; }

            public TestCase(ValidationAttribute attribute, object value, ValidationContext validationContext = null)
            {
                Attribute = attribute;
                Value = value;
                ValidationContext = validationContext ?? new ValidationContext(new object());
            }
        }
    }
}

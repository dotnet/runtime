// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class CompareAttributeTests
    {
        [Fact]
        public static void Constructor_Null_OtherProperty()
        {
            Assert.Throws<ArgumentNullException>(() => new CompareAttribute(otherProperty: null));
        }

        [Fact]
        public static void Constructor_NonNull_OtherProperty()
        {
            AssertEx.DoesNotThrow(() => new CompareAttribute("OtherProperty"));
        }

        [Fact]
        public static void Validate_does_not_throw_when_compared_objects_are_equal()
        {
            var otherObject = new CompareObject("test");
            var currentObject = new CompareObject("test");
            var testContext = new ValidationContext(otherObject, null, null);

            var attribute = new CompareAttribute("CompareProperty");
            AssertEx.DoesNotThrow(() => attribute.Validate(currentObject.CompareProperty, testContext));
        }

        [Fact]
        public static void Validate_throws_when_compared_objects_are_not_equal()
        {
            var currentObject = new CompareObject("a");
            var otherObject = new CompareObject("b");

            var testContext = new ValidationContext(otherObject, null, null);
            testContext.DisplayName = "CurrentProperty";

            var attribute = new CompareAttribute("CompareProperty");
            Assert.Throws<ValidationException>(
                () => attribute.Validate(currentObject.CompareProperty, testContext));
        }

        [Fact]
        public static void Validate_throws_with_OtherProperty_DisplayName()
        {
            var currentObject = new CompareObject("a");
            var otherObject = new CompareObject("b");

            var testContext = new ValidationContext(otherObject, null, null);
            testContext.DisplayName = "CurrentProperty";

            var attribute = new CompareAttribute("ComparePropertyWithDisplayName");
            Assert.Throws<ValidationException>(
                () => attribute.Validate(currentObject.CompareProperty, testContext));
        }

        [Fact]
        public static void Validate_throws_when_PropertyName_is_unknown()
        {
            var currentObject = new CompareObject("a");
            var otherObject = new CompareObject("b");

            var testContext = new ValidationContext(otherObject, null, null);
            testContext.DisplayName = "CurrentProperty";

            var attribute = new CompareAttribute("UnknownPropertyName");
            Assert.Throws<ValidationException>(
                () => attribute.Validate(currentObject.CompareProperty, testContext));
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static void CompareAttribute_can_be_derived_from_and_override_is_valid()
        {
            var otherObject = new CompareObject("a");
            var currentObject = new CompareObject("b");
            var testContext = new ValidationContext(otherObject, null, null);

            var attribute = new DerivedCompareAttribute("CompareProperty");
            AssertEx.DoesNotThrow(() => attribute.Validate(currentObject.CompareProperty, testContext));
        }

        private class DerivedCompareAttribute : CompareAttribute
        {
            public DerivedCompareAttribute(string otherProperty)
                : base(otherProperty)
            {
            }

            protected override ValidationResult IsValid(object value, ValidationContext context)
            {
                return ValidationResult.Success;
            }
        }

        private class CompareObject
        {
            public string CompareProperty { get; set; }

            [Display(Name = "DisplayName")]
            public string ComparePropertyWithDisplayName { get; set; }

            public CompareObject(string otherValue)
            {
                CompareProperty = otherValue;
                ComparePropertyWithDisplayName = otherValue;
            }
        }
    }
}

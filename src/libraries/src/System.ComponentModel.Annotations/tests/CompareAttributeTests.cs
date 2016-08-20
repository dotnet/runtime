// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class CompareAttributeTests
    {
        [Fact]
        public static void Constructor_NullOtherProperty_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("otherProperty", () => new CompareAttribute(null));
        }

        [Theory]
        [InlineData("OtherProperty")]
        [InlineData("")]
        public static void Constructor(string otherProperty)
        {
            CompareAttribute attribute = new CompareAttribute(otherProperty);
            Assert.Equal(otherProperty, attribute.OtherProperty);

            Assert.True(attribute.RequiresValidationContext);
        }

        [Fact]
        public static void Validate_EqualObjects_DoesNotThrow()
        {
            var otherObject = new CompareObject("test");
            var currentObject = new CompareObject("test");
            var testContext = new ValidationContext(otherObject, null, null);

            var attribute = new CompareAttribute("CompareProperty");
            attribute.Validate(currentObject.CompareProperty, testContext);
        }
        
        public static IEnumerable<object[]> Invalid_TestData()
        {
            ValidationContext context = new ValidationContext(new CompareObject("a")) { DisplayName = "CurrentProperty" };

            yield return new object[] { nameof(CompareObject.CompareProperty), context, nameof(CompareObject.CompareProperty), typeof(ValidationException) };
            yield return new object[] { nameof(CompareObject.ComparePropertyWithDisplayName), context, "CustomDisplayName", typeof(ValidationException) };
            yield return new object[] { "UnknownPropertyName", context, null, typeof(ValidationException) };

            ValidationContext subClassContext = new ValidationContext(new CompareObjectSubClass("a"));
            yield return new object[] { nameof(CompareObject.CompareProperty), subClassContext, "CompareProperty", typeof(ValidationException) };

            yield return new object[] { "Item", context, null, typeof(TargetParameterCountException) };
            yield return new object[] { nameof(CompareObject.SetOnlyProperty), context, null, typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(Invalid_TestData))]
        public static void Validate_Invalid_Throws(string otherProperty, ValidationContext context, string otherPropertyDisplayName, Type exceptionType)
        {
            var attribute = new CompareAttribute(otherProperty);
            
            Assert.Throws(exceptionType, () => attribute.Validate("b", context));
            Assert.Equal(otherPropertyDisplayName, attribute.OtherPropertyDisplayName);

            // Make sure that we can run Validate twice
            Assert.Throws(exceptionType, () => attribute.Validate("b", context));
            Assert.Equal(otherPropertyDisplayName, attribute.OtherPropertyDisplayName);
        }

        [Fact]
        public static void Validate_PropertyHasDisplayName_UpdatesFormatErrorMessageToContainDisplayName()
        {
            CompareAttribute attribute = new CompareAttribute(nameof(CompareObject.ComparePropertyWithDisplayName));

            string oldErrorMessage = attribute.FormatErrorMessage("name");
            Assert.False(oldErrorMessage.Contains("CustomDisplayName"));

            Assert.Throws<ValidationException>(() => attribute.Validate("test1", new ValidationContext(new CompareObject("test"))));

            string newErrorMessage = attribute.FormatErrorMessage("name");
            Assert.NotEqual(oldErrorMessage, newErrorMessage);
            Assert.True(newErrorMessage.Contains("CustomDisplayName"));
        }

        [Fact]
        public static void Validate_CustomDerivedClass_DoesNotThrow()
        {
            var otherObject = new CompareObject("a");
            var currentObject = new CompareObject("b");
            var testContext = new ValidationContext(otherObject, null, null);

            var attribute = new DerivedCompareAttribute("CompareProperty");
            attribute.Validate(currentObject.CompareProperty, testContext);
        }
        
        private class DerivedCompareAttribute : CompareAttribute
        {
            public DerivedCompareAttribute(string otherProperty) : base(otherProperty) { }

            protected override ValidationResult IsValid(object value, ValidationContext context) => ValidationResult.Success;
        }

        private class CompareObject
        {
            public string CompareProperty { get; set; }

            [Display(Name = "CustomDisplayName")]
            public string ComparePropertyWithDisplayName { get; set; }

            public string this[int index] { get { return "abc"; } set { } }
            public string SetOnlyProperty { set { } }

            public CompareObject(string otherValue)
            {
                CompareProperty = otherValue;
                ComparePropertyWithDisplayName = otherValue;
            }
        }

        private class CompareObjectSubClass : CompareObject
        {
            public CompareObjectSubClass(string otherValue) : base(otherValue) { }
        }
    }
}

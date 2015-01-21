// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class CustomValidationAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void Can_construct_attribute_and_get_values()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "SomeMethod");
            Assert.Equal(typeof(CustomValidator), attribute.ValidatorType);
            Assert.Equal("SomeMethod", attribute.Method);
        }

        [Fact]
        public static void Can_construct_attribute_and_get_invalid_values()
        {
            var attribute = new CustomValidationAttribute(null, null);
            Assert.Equal(null, attribute.ValidatorType);
            Assert.Equal(null, attribute.Method);

            attribute = new CustomValidationAttribute(typeof(string), string.Empty);
            Assert.Equal(typeof(string), attribute.ValidatorType);
            Assert.Equal(string.Empty, attribute.Method);

            attribute = new CustomValidationAttribute(typeof(int), " \t\r\n");
            Assert.Equal(typeof(int), attribute.ValidatorType);
            Assert.Equal(" \t\r\n", attribute.Method);
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_for_invalid_ValidatorType()
        {
            var attribute = new CustomValidationAttribute(null, "Does not matter");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(NonPublicCustomValidator), "Does not matter");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_InvalidOperationException_for_invalid_validation_method()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), null);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), string.Empty);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "NonExistentMethod");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "NonPublicValidationMethod");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "NonStaticValidationMethod");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "ValidationMethodDoesNotReturnValidationResult");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "ValidationMethodWithNoArgs");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "ValidationMethodWithByRefArg");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "ValidationMethodTwoArgsButSecondIsNotValidationContext");
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_valid_validation_type_and_method()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodOneArg");
            AssertEx.DoesNotThrow(() => attribute.Validate("Validation returns success for any string", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodTwoArgs");
            AssertEx.DoesNotThrow(() => attribute.Validate(new TestClass("Validation returns success for any TestClass"), s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_valid_validation_type_and_method_with_strongly_typed_first_arg()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodOneArgStronglyTyped");
            AssertEx.DoesNotThrow(() => attribute.Validate("Validation returns success for any string", s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodTwoArgsStronglyTyped");
            AssertEx.DoesNotThrow(() => attribute.Validate(new TestClass("Validation returns success for any TestClass"), s_testValidationContext));
        }

        [Fact]
        public static void Validate_throws_ValidationException_for_invalid_values()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodOneArg");
            Assert.Throws<ValidationException>(
                () => attribute.Validate(new TestClass("Value is not a string - so validation fails"), s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodTwoArgs");
            Assert.Throws<ValidationException>(
                () => attribute.Validate("Value is not a TestClass - so validation fails", s_testValidationContext));

            // This Assert produces different results on Core CLR versus .Net Native. In CustomValidationAttribute.TryConvertValue()
            // we call Convert.ChangeType(instanceOfAClass, typeof(string), ...). On K this throws InvalidCastException because
            // the class does not implement IConvertible. On N this just returns the result of ToString() on the class and does not throw.
            // As of 7/9/14 no plans to change this.
            //attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodOneArgStronglyTyped");
            //Assert.Throws<ValidationException>(
            //    () => attribute.Validate(new TestClass("Validation method expects a string but is given a TestClass and so fails"), TestValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodTwoArgsStronglyTyped");
            Assert.Throws<ValidationException>(
                () => attribute.Validate("Validation method expects a TestClass but is given a string and so fails", s_testValidationContext));
        }

        [Fact]
        public static void Validation_works_for_null_and_non_null_values_and_validation_method_taking_nullable_value_type()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodOneArgNullable");
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext));
            AssertEx.DoesNotThrow(
                () => attribute.Validate(new TestStruct() { Value = "Valid Value" }, s_testValidationContext));
            Assert.Throws<ValidationException>(
                () => attribute.Validate(new TestStruct() { Value = "Some non-valid value" }, s_testValidationContext));

            attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodTwoArgsWithFirstNullable");
            AssertEx.DoesNotThrow(() => attribute.Validate(null, s_testValidationContext));
            AssertEx.DoesNotThrow(
                () => attribute.Validate(new TestStruct() { Value = "Valid Value" }, s_testValidationContext));
            Assert.Throws<ValidationException>(
                () => attribute.Validate(new TestStruct() { Value = "Some non-valid value" }, s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_validation_method_with_strongly_typed_first_arg_and_value_type_assignable_from_expected_type()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodTwoArgsStronglyTyped");
            AssertEx.DoesNotThrow(
                () => attribute.Validate(new DerivedTestClass("Validation returns success for DerivedTestClass too"), s_testValidationContext));
        }

        [Fact]
        public static void Validate_successful_for_validation_method_with_strongly_typed_first_arg_and_value_type_convertible_to_expected_type()
        {
            var attribute = new CustomValidationAttribute(typeof(CustomValidator), "CorrectValidationMethodIntegerArg");

            // validation works for integer value as it is declared with integer arg
            AssertEx.DoesNotThrow(() => attribute.Validate(123, s_testValidationContext));

            // also works with bool, long, float & double as can convert them to int
            AssertEx.DoesNotThrow(() => attribute.Validate(false, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(123456L, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(123.456F, s_testValidationContext));
            AssertEx.DoesNotThrow(() => attribute.Validate(123.456D, s_testValidationContext));

            // does not work with TestClass or DateTime as cannot convert them
            Assert.Throws<ValidationException>(() => attribute.Validate(new TestClass("Does not convert to int"), s_testValidationContext));
            Assert.Throws<ValidationException>(() => attribute.Validate(new DateTime(2014, 3, 19), s_testValidationContext));
        }

        internal class NonPublicCustomValidator
        {
            public static ValidationResult ValidationMethodOneArg(object o)
            {
                return ValidationResult.Success;
            }
        }

        public class CustomValidator
        {
            internal static ValidationResult NonPublicValidationMethod(object o)
            {
                return ValidationResult.Success;
            }

            public ValidationResult NonStaticValidationMethod(object o)
            {
                return ValidationResult.Success;
            }

            public static string ValidationMethodDoesNotReturnValidationResult(object o)
            {
                return null;
            }

            public static ValidationResult ValidationMethodWithNoArgs()
            {
                return ValidationResult.Success;
            }

            public static ValidationResult ValidationMethodWithByRefArg(ref object o)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult ValidationMethodTwoArgsButSecondIsNotValidationContext(object o, object someOtherObject)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult ValidationMethodThreeArgs(object o, ValidationContext context, object someOtherObject)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult CorrectValidationMethodOneArg(object o)
            {
                if (o is string) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - not a string");
            }

            public static ValidationResult CorrectValidationMethodOneArgStronglyTyped(string s)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult CorrectValidationMethodTwoArgs(object o, ValidationContext context)
            {
                if (o is TestClass) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - not a TestClass");
            }

            public static ValidationResult CorrectValidationMethodTwoArgsStronglyTyped(TestClass tc, ValidationContext context)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult CorrectValidationMethodIntegerArg(int i)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult CorrectValidationMethodOneArgNullable(Nullable<TestStruct> testStruct)
            {
                if (testStruct == null) { return ValidationResult.Success; }
                var ts = (TestStruct)testStruct;
                if ("Valid Value".Equals(ts.Value)) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - neither null nor Value=\"Valid Value\"");
            }

            public static ValidationResult CorrectValidationMethodTwoArgsWithFirstNullable(Nullable<TestStruct> testStruct, ValidationContext context)
            {
                if (testStruct == null) { return ValidationResult.Success; }
                var ts = (TestStruct)testStruct;
                if ("Valid Value".Equals(ts.Value)) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - neither null nor Value=\"Valid Value\"");
            }
        }

        public class TestClass
        {
            public TestClass(string message) { }
        }

        public class DerivedTestClass : TestClass
        {
            public DerivedTestClass(string message) : base(message) { }
        }

        public struct TestStruct
        {
            public string Value { get; set; }
        }
    }
}

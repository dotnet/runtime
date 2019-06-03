// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class CustomValidationAttributeTests : ValidationAttributeTestBase
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArg)), "AnyString");
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgs)), new TestClass("AnyString"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgStronglyTyped)), "AnyString");
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsStronglyTyped)), new TestClass("AnyString"));

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgNullable)), null);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgNullable)), new TestStruct() { Value = "Valid Value" });

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsWithFirstNullable)), null);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsWithFirstNullable)), new TestStruct() { Value = "Valid Value" });

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsStronglyTyped)), new DerivedTestClass("AnyString"));

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), 123);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), false);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), 123456L);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), 123.456F);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), 123.456D);
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArg)), null);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArg)), new TestClass("AnyString"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgs)), "AnyString");

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgStronglyTyped)), new TestClass("AnyString"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsStronglyTyped)), "AnyString");

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgGenericStruct)), null);

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgNullable)), new TestStruct());
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsWithFirstNullable)), new TestStruct() { Value = "Invalid Value" });

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), null);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), new TestClass("NotInt"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), new DateTime(2014, 3, 19));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgDateTime)), null);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgDateTime)), "abcdef");

            // Implements IConvertible (throws NotSupportedException - is caught)
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgInt)), new IConvertibleImplementor() { IntThrow = new NotSupportedException() });
        }

        protected override bool RespectsErrorMessage => false;

        private static CustomValidationAttribute GetAttribute(string name) => new CustomValidationAttribute(typeof(CustomValidator), name);

        [Theory]
        [InlineData(typeof(CustomValidator), "SomeMethod")]
        [InlineData(null, null)]
        [InlineData(typeof(string), "")]
        [InlineData(typeof(int), " \t\r\n")]
        public static void Ctor_Type_String(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.Equal(validatorType, attribute.ValidatorType);
            Assert.Equal(method, attribute.Method);
        }

        [Theory]
        [InlineData(typeof(CustomValidator), nameof(CustomValidator.ValidationMethodDerivedReturnTypeReturnsSomeError))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full .NET Frameworks had a restriction, that prevented to use custom ValidationResult. .NET Core allows to return class derived from ValidatioResult")]
        public static void Ctor_Type_String_IgnoreNetFramework(Type validatorType, string method)
        {
            Ctor_Type_String(validatorType, method);
        }

        [Fact]
        public void FormatErrorMessage_NotPerformedValidation_ContainsName()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArg));
            string errorMessage = attribute.FormatErrorMessage("name");
            Assert.Contains("name", errorMessage);
            Assert.Equal(errorMessage, attribute.FormatErrorMessage("name"));
        }

        [Fact]
        public void FormatErrorMessage_PerformedValidation_DoesNotContainName()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArg));
            Assert.False(attribute.IsValid(new TestClass("AnyString")));

            string errorMessage = attribute.FormatErrorMessage("name");
            Assert.DoesNotContain("name", errorMessage);
            Assert.Equal(errorMessage, attribute.FormatErrorMessage("name"));
        }

        [Theory]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodOneArg), false)]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodOneArgStronglyTyped), false)]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodTwoArgs), true)]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodTwoArgsStronglyTyped), true)]
        public static void RequiresValidationContext_Get_ReturnsExpected(string method, bool expected)
        {
            CustomValidationAttribute attribute = GetAttribute(method);

            // The full .NET Framework has a bug where CustomValidationAttribute doesn't
            // validate the context. See https://github.com/dotnet/corefx/issues/18360.
            if (PlatformDetection.IsFullFramework)
            {
                Assert.False(attribute.RequiresValidationContext);
            }
            else
            {
                Assert.Equal(expected, attribute.RequiresValidationContext);
            }
        }

        public static IEnumerable<object[]> BadlyFormed_TestData()
        {
            yield return new object[] { null, "Does not matter" };
            yield return new object[] { typeof(NonPublicCustomValidator), "Does not matter" };
            yield return new object[] { typeof(CustomValidator), null };
            yield return new object[] { typeof(CustomValidator), "" };
            yield return new object[] { typeof(CustomValidator), "NonExistentMethod" };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.NonPublicValidationMethod) };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.NonStaticValidationMethod) };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.ValidationMethodDoesNotReturnValidationResult) };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.ValidationMethodWithNoArgs) };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.ValidationMethodWithByRefArg) };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.ValidationMethodTwoArgsButSecondIsNotValidationContext) };
            yield return new object[] { typeof(CustomValidator), nameof(CustomValidator.ValidationMethodThreeArgs) };
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core fixes a bug where CustomValidationAttribute doesn't validate the context. See https://github.com/dotnet/corefx/issues/18360")]
        [MemberData(nameof(BadlyFormed_TestData))]
        public static void RequiresValidationContext_BadlyFormed_NetCore_ThrowsInvalidOperationException(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.Throws<InvalidOperationException>(() => attribute.RequiresValidationContext);
        }

        [Theory]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "The full .NET Framework has a bug where CustomValidationAttribute doesn't validate the context. See https://github.com/dotnet/corefx/issues/18360")]
        [MemberData(nameof(BadlyFormed_TestData))]
        public static void RequiresValidationContext_BadlyFormed_NetFx_DoesNotThrow(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.False(attribute.RequiresValidationContext);
        }

        [Theory]
        [MemberData(nameof(BadlyFormed_TestData))]
        public static void Validate_BadlyFormed_ThrowsInvalidOperationException(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Does not matter", s_testValidationContext));
        }

        [Theory]
        [MemberData(nameof(BadlyFormed_TestData))]
        public static void FormatErrorMessage_BadlyFormed_ThrowsInvalidOperationException(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.Throws<InvalidOperationException>(() => attribute.FormatErrorMessage("name"));
        }

        [Fact]
        public static void Validate_IConvertibleThrowsCustomException_IsNotCaught()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgInt));
            Assert.Throws<ArithmeticException>(() => attribute.Validate(new IConvertibleImplementor() { IntThrow = new ArithmeticException() }, s_testValidationContext));
        }

        [Fact]
        public static void Validate_MethodThrowsCustomException_IsNotCaught()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.ValidationMethodThrowsException));
            AssertExtensions.Throws<ArgumentException>(null, () => attribute.Validate(new IConvertibleImplementor(), s_testValidationContext));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full .NET Frameworks had a restriction, that prevented to use custom ValidationResult. .NET Core allows to return class derived from ValidatioResult")]
        public static void GetValidationResult_MethodReturnDerivedValidationResult_ReturnsExpected()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.ValidationMethodDerivedReturnTypeReturnsSomeError));
            ValidationResult validationResult = attribute.GetValidationResult(new object(), s_testValidationContext);
            Assert.Equal(DerivedValidationResult.SomeError, validationResult);
        }

        internal class NonPublicCustomValidator
        {
            public static ValidationResult ValidationMethodOneArg(object o) => ValidationResult.Success;
        }

        public class CustomValidator
        {
            internal static ValidationResult NonPublicValidationMethod(object o) => ValidationResult.Success;

            public ValidationResult NonStaticValidationMethod(object o) => ValidationResult.Success;

            public static string ValidationMethodDoesNotReturnValidationResult(object o) => null;

            public static ValidationResult ValidationMethodWithNoArgs() => ValidationResult.Success;

            public static ValidationResult ValidationMethodWithByRefArg(ref object o) => ValidationResult.Success;

            public static ValidationResult ValidationMethodTwoArgsButSecondIsNotValidationContext(object o, object someOtherObject)
            {
                return ValidationResult.Success;
            }
            
            public static ValidationResult ValidationMethodThrowsException(object o)
            {
                throw new ArgumentException();
            }

            public static ValidationResult ValidationMethodThreeArgs(object o, ValidationContext context, object someOtherObject)
            {
                return ValidationResult.Success;
            }

            public static DerivedValidationResult ValidationMethodDerivedReturnTypeReturnsSomeError(object o) =>
                DerivedValidationResult.SomeError;

            public static ValidationResult CorrectValidationMethodOneArg(object o)
            {
                if (o is string) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - not a string");
            }

            public static ValidationResult CorrectValidationMethodOneArgStronglyTyped(string s) => ValidationResult.Success;

            public static ValidationResult CorrectValidationMethodTwoArgs(object o, ValidationContext context)
            {
                if (o is TestClass) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - not a TestClass");
            }

            public static ValidationResult CorrectValidationMethodTwoArgsStronglyTyped(TestClass tc, ValidationContext context)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult CorrectValidationMethodIntegerArg(int i) => ValidationResult.Success;

            public static ValidationResult CorrectValidationMethodOneArgNullable(TestStruct? testStruct)
            {
                if (testStruct == null) { return ValidationResult.Success; }
                var ts = (TestStruct)testStruct;
                if ("Valid Value".Equals(ts.Value)) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - neither null nor Value=\"Valid Value\"");
            }

            public static ValidationResult CorrectValidationMethodOneArgGenericStruct(GenericStruct<int> testStruct)
            {
                return ValidationResult.Success;
            }

            public static ValidationResult CorrectValidationMethodTwoArgsWithFirstNullable(TestStruct? testStruct, ValidationContext context)
            {
                if (testStruct == null) { return ValidationResult.Success; }
                var ts = (TestStruct)testStruct;
                if ("Valid Value".Equals(ts.Value)) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - neither null nor Value=\"Valid Value\"");
            }

            public static ValidationResult CorrectValidationMethodOneArgDateTime(DateTime dateTime) => ValidationResult.Success;
            public static ValidationResult CorrectValidationMethodOneArgInt(int i) => ValidationResult.Success;
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

        public struct GenericStruct<T> { }

        public class DerivedValidationResult : ValidationResult
        {
            public DerivedValidationResult(string errorMessage): base(errorMessage)
            {
            }

            public static readonly DerivedValidationResult SomeError =
                new DerivedValidationResult("Some Error") { AdditionalData = "Additional Data" }; 

            public string AdditionalData { get; set; }
        }

        [Fact]
        public static void AllowMultiple()
        {
            Assert.Equal(3, TypeDescriptor.GetAttributes(typeof(AllowMultipleClass)).Count);
        }

        [CustomValidation(typeof(AllowMultipleClass), nameof(AllowMultipleClass.Method1))]
        [CustomValidation(typeof(AllowMultipleClass), nameof(AllowMultipleClass.Method2))]
        [CustomValidation(typeof(AllowMultipleClass), nameof(AllowMultipleClass.Method3))]
        [CustomValidation(typeof(AllowMultipleClass), nameof(AllowMultipleClass.Method3))]
        public class AllowMultipleClass 
        {
            public void Method1() { }
            public void Method2() { }
            public void Method3() { }
        }
    }
}

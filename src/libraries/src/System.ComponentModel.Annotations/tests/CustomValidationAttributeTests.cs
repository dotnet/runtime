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
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArg)), new TestClass("AnyString"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgs)), "AnyString");

            // This Assert produces different results on Core CLR versus .Net Native. In CustomValidationAttribute.TryConvertValue()
            // we call Convert.ChangeType(instanceOfAClass, typeof(string), ...). On K this throws InvalidCastException because
            // the class does not implement IConvertible. On N this just returns the result of ToString() on the class and does not throw.
            // As of 7/9/14 no plans to change this.
            // yield return new Test(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgStronglyTyped)), new TestClass("AnyString"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsStronglyTyped)), "AnyString");

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgNullable)), new TestStruct());
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodTwoArgsWithFirstNullable)), new TestStruct() { Value = "Invalid Value" });

            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), null);
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), new TestClass("NotInt"));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodIntegerArg)), new DateTime(2014, 3, 19));
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgDateTime)), "abcdef");

            // Implements IConvertible (throws NotSupportedException - is caught)
            yield return new TestCase(GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgDateTime)), new IConvertibleImplementor());
        }

        public override bool RespectsErrorMessage => false;

        private static CustomValidationAttribute GetAttribute(string name) => new CustomValidationAttribute(typeof(CustomValidator), name);

        [Theory]
        [InlineData(typeof(CustomValidator), "SomeMethod")]
        [InlineData(null, null)]
        [InlineData(typeof(string), "")]
        [InlineData(typeof(int), " \t\r\n")]
        public static void Constructor(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.Equal(validatorType, attribute.ValidatorType);
            Assert.Equal(method, attribute.Method);
        }

        [Theory]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodOneArg), false)]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodOneArgStronglyTyped), false)]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodTwoArgs), true)]
        [InlineData(nameof(CustomValidator.CorrectValidationMethodTwoArgsStronglyTyped), true)]
        public static void RequiresValidationContext_Get_ReturnsExpected(string method, bool expected)
        {
            CustomValidationAttribute attribute = GetAttribute(method);
            Assert.Equal(expected, attribute.RequiresValidationContext);
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
        }

        [Theory]
        [MemberData(nameof(BadlyFormed_TestData))]
        public static void RequiresValidationContext_BadlyFormed_ThrowsInvalidOperationException(Type validatorType, string method)
        {
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);
            Assert.Throws<InvalidOperationException>(() => attribute.RequiresValidationContext);
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
        
        // Implements IConvertible (throws custom ArithmeticException - is not caught)
        [Fact]
        public static void Validate_IConvertibleThrowsCustomException_IsNotCaught()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.CorrectValidationMethodOneArgDecimal));
            Assert.Throws<ArithmeticException>(() => attribute.Validate(new IConvertibleImplementor(), s_testValidationContext));
        }

        [Fact]
        public static void Validate_MethodThrowsCustomException_IsNotCaught()
        {
            CustomValidationAttribute attribute = GetAttribute(nameof(CustomValidator.ValidationMethodThrowsException));
            Assert.Throws<ArgumentException>(() => attribute.Validate(new IConvertibleImplementor(), s_testValidationContext));
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

            public static ValidationResult CorrectValidationMethodTwoArgsWithFirstNullable(TestStruct? testStruct, ValidationContext context)
            {
                if (testStruct == null) { return ValidationResult.Success; }
                var ts = (TestStruct)testStruct;
                if ("Valid Value".Equals(ts.Value)) { return ValidationResult.Success; }
                return new ValidationResult("Validation failed - neither null nor Value=\"Valid Value\"");
            }

            public static ValidationResult CorrectValidationMethodOneArgDateTime(DateTime dateTime) => ValidationResult.Success;
            public static ValidationResult CorrectValidationMethodOneArgDecimal(decimal d) => ValidationResult.Success;
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

        public class IConvertibleImplementor : IConvertible
        {
            public TypeCode GetTypeCode() => TypeCode.Empty;

            public bool ToBoolean(IFormatProvider provider) => true;
            public byte ToByte(IFormatProvider provider) => 0;
            public char ToChar(IFormatProvider provider) => '\0';
            public DateTime ToDateTime(IFormatProvider provider)
            {
                throw new NotSupportedException();
            }

            public decimal ToDecimal(IFormatProvider provider)
            {
                throw new ArithmeticException();
            }

            public double ToDouble(IFormatProvider provider) => 0;
            public short ToInt16(IFormatProvider provider) => 0;
            public int ToInt32(IFormatProvider provider) => 0;
            public long ToInt64(IFormatProvider provider) => 0;
            public sbyte ToSByte(IFormatProvider provider) => 0;
            public float ToSingle(IFormatProvider provider) => 0;

            public string ToString(IFormatProvider provider) => "";
            public object ToType(Type conversionType, IFormatProvider provider) => null;

            public ushort ToUInt16(IFormatProvider provider) => 0;
            public uint ToUInt32(IFormatProvider provider) => 0;
            public ulong ToUInt64(IFormatProvider provider) => 0;
        }
        }
}

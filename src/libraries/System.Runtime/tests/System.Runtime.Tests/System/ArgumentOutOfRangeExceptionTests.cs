// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace System.Tests
{
    public static class ArgumentOutOfRangeExceptionTests
    {
        private const int COR_E_ARGUMENTOUTOFRANGE = unchecked((int)0x80131502);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new ArgumentOutOfRangeException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Null(exception.ParamName);
            Assert.Null(exception.ActualValue);
        }

        [Fact]
        public static void Ctor_String()
        {
            string argumentName = "theArgument";
            var exception = new ArgumentOutOfRangeException(argumentName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Null(exception.ActualValue);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "the argument is out of range";
            var innerException = new Exception("Inner exception");
            var exception = new ArgumentOutOfRangeException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, innerException: innerException, message: message);
            Assert.Null(exception.ParamName);
            Assert.Null(exception.ActualValue);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "the argument is out of range";
            string argumentName = "theArgument";
            var exception = new ArgumentOutOfRangeException(argumentName, message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Null(exception.ActualValue);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
        }

        [Fact]
        public static void Ctor_String_Object_String()
        {
            string message = "the argument is out of range";
            string argumentName = "theArgument";
            int argumentValue = Int32.MaxValue;
            var exception = new ArgumentOutOfRangeException(argumentName, argumentValue, message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_ARGUMENTOUTOFRANGE, validateMessage: false);
            Assert.Equal(argumentName, exception.ParamName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(argumentName, exception.Message);
            Assert.Contains(argumentValue.ToString(), exception.Message);
        }

        private const string HelpersParamName = "value";
        private static Action ZeroHelper<T>(T value) where T : INumberBase<T> => () => ArgumentOutOfRangeException.ThrowIfZero(value);
        private static Action NegativeOrZeroHelper<T>(T value) where T : INumberBase<T> => () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        private static Action GreaterThanHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfGreaterThan(value, other);
        private static Action GreaterThanOrEqualHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, other);
        private static Action LessThanHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfLessThan(value, other);
        private static Action LessThanOrEqualHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, other);
        private static Action EqualHelper<T>(T value, T other) where T : IEquatable<T> => () => ArgumentOutOfRangeException.ThrowIfEqual(value, other);
        private static Action NotEqualHelper<T>(T value, T other) where T : IEquatable<T> => () => ArgumentOutOfRangeException.ThrowIfNotEqual(value, other);

        [Fact]
        public static void GenericHelpers_ThrowIfZero_Throws()
        {
            Assert.Equal(0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<int>(0)).ActualValue);
            Assert.Equal(0u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<uint>(0)).ActualValue);

            Assert.Equal(0.0f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<float>(0.0f)).ActualValue);
            Assert.Equal(-0.0f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<float>(-0.0f)).ActualValue);
            Assert.Equal((double)0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<double>(0)).ActualValue);
            Assert.Equal(+0.0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<double>(+0.0)).ActualValue);
            Assert.Equal(-0.0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, ZeroHelper<double>(-0.0)).ActualValue);

            ZeroHelper(1)();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfNegativeZero_Throws()
        {
            Assert.Equal(-1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NegativeOrZeroHelper<int>(-1)).ActualValue);
            Assert.Equal(-0.0f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NegativeOrZeroHelper<float>(-0.0f)).ActualValue);
            Assert.Equal(-0.0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NegativeOrZeroHelper<double>(-0.0)).ActualValue);

            NegativeOrZeroHelper(1)();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfGreaterThan_Throws()
        {
            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<int>(1, 0)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<uint>(1, 0)).ActualValue);
            Assert.Equal(1.000000001, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<double>(1.000000001, 1)).ActualValue);
            Assert.Equal(1.00001f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<float>(1.00001f, 1)).ActualValue);

            GreaterThanHelper(1, 2)();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfGreaterThanOrEqual_Throws()
        {
            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<int>(1, 1)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<uint>(1, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<double>(1, 1)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<float>(1, 1)).ActualValue);

            Assert.Equal(3, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<int>(3, 1)).ActualValue);
            Assert.Equal(4u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<uint>(4, 1)).ActualValue);
            Assert.Equal((double)1.1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<double>(1.1, 1)).ActualValue);
            Assert.Equal(2.1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<float>(2.1f, 1)).ActualValue);

            GreaterThanOrEqualHelper(1, 2)();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfLessThan_Throws()
        {
            Assert.Equal(0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<int>(0, 1)).ActualValue);
            Assert.Equal(0u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<uint>(0, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<double>(1, 1.000000001)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<float>(1, 1.00001f)).ActualValue);

            LessThanHelper(2, 1)();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfLessThanOrEqual_Throws()
        {
            Assert.Equal(-1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<int>(-1, 1)).ActualValue);
            Assert.Equal(0u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<uint>(0, 1)).ActualValue);
            Assert.Equal((double)0.9, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<double>(0.9, 1)).ActualValue);
            Assert.Equal(-0.1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<float>(-0.1f, 1)).ActualValue);

            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<int>(1, 1)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<uint>(1, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<double>(1, 1)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<float>(1, 1)).ActualValue);

            LessThanHelper(2, 1)();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfEqual_Throws()
        {
            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, EqualHelper<int>(1, 1)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, EqualHelper<uint>(1, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, EqualHelper<double>(1, 1)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, EqualHelper<float>(1, 1)).ActualValue);
            Assert.Null(AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, EqualHelper<string>(null, null)).ActualValue);

            EqualHelper(1, 2)();
            EqualHelper("test1", "test2")();
            EqualHelper("test1", null)();
            EqualHelper(null, "test2")();
        }

        [Fact]
        public static void GenericHelpers_ThrowIfNotEqual_Throws()
        {
            Assert.Equal(-1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NotEqualHelper<int>(-1, 1)).ActualValue);
            Assert.Equal(2u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NotEqualHelper<uint>(2, 1)).ActualValue);
            Assert.Equal((double)2, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NotEqualHelper<double>(2, 1)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NotEqualHelper<float>(1, 2)).ActualValue);
            Assert.Equal("test", AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NotEqualHelper<string>("test", null)).ActualValue);
            Assert.Null(AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NotEqualHelper<string>(null, "test")).ActualValue);

            NotEqualHelper(2, 2)();
            NotEqualHelper("test", "test")();
        }
    }
}

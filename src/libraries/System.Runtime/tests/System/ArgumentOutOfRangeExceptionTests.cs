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
        }

        [Fact]
        public static void GenericHelpers_ThrowIfNegativeZero_Throws()
        {
            Assert.Equal(-1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NegativeOrZeroHelper<int>(-1)).ActualValue);
            Assert.Equal(-0.0f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NegativeOrZeroHelper<float>(-0.0f)).ActualValue);
            Assert.Equal(-0.0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, NegativeOrZeroHelper<double>(-0.0)).ActualValue);
        }

        [Fact]
        public static void GenericHelpers_ThrowIfGreaterThan_Throws()
        {
            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<int>(1, 0)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<uint>(1, 0)).ActualValue);
            Assert.Equal(1.000000001, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<double>(1.000000001, 1)).ActualValue);
            Assert.Equal(1.00001f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanHelper<float>(1.00001f, 1)).ActualValue);
        }

        [Fact]
        public static void GenericHelpers_ThrowIfGreaterThanOrEqual_Throws()
        {
            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<int>(1, 1)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<uint>(1, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<double>(1, 1)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, GreaterThanOrEqualHelper<float>(1, 1)).ActualValue);
        }

        [Fact]
        public static void GenericHelpers_ThrowIfLessThan_Throws()
        {
            Assert.Equal(0, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<int>(0, 1)).ActualValue);
            Assert.Equal(0u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<uint>(0, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<double>(1, 1.000000001)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanHelper<float>(1, 1.00001f)).ActualValue);
        }

        [Fact]
        public static void GenericHelpers_ThrowIfLessThanOrEqual_Throws()
        {
            Assert.Equal(1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<int>(1, 1)).ActualValue);
            Assert.Equal(1u, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<uint>(1, 1)).ActualValue);
            Assert.Equal((double)1, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<double>(1, 1)).ActualValue);
            Assert.Equal(1f, AssertExtensions.Throws<ArgumentOutOfRangeException>(HelpersParamName, LessThanOrEqualHelper<float>(1, 1)).ActualValue);
        }
    }
}

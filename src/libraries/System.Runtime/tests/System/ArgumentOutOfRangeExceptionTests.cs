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

        private static Action ZeroHelper<T>(T value) where T : INumberBase<T> => () => ArgumentOutOfRangeException.ThrowIfZero(value);
        private static Action NegativeOrZeroHelper<T>(T value) where T : INumberBase<T> => () => ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        private static Action GreaterThanHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfGreaterThan(value, other);
        private static Action GreaterThanOrEqualHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, other);
        private static Action LessThanHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfLessThan(value, other);
        private static Action LessThanOrEqualHelper<T>(T value, T other) where T : IComparable<T> => () => ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, other);

        [Fact]
        public static void GenericHelpers_ThrowIfZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<uint>(0));

            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<float>(0.0f));
            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<float>(-0.0f));
            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<double>(0));
            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<double>(+0.0));
            Assert.Throws<ArgumentOutOfRangeException>(ZeroHelper<double>(-0.0));
        }

        [Fact]
        public static void GenericHelpers_ThrowIfNegativeZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(NegativeOrZeroHelper<int>(-1));

            Assert.Throws<ArgumentOutOfRangeException>(NegativeOrZeroHelper<float>(-0.0f));
            Assert.Throws<ArgumentOutOfRangeException>(NegativeOrZeroHelper<double>(-0.0));
        }

        [Fact]
        public static void GenericHelpers_ThrowIfGreaterThan_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanHelper<int>(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanHelper<uint>(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanHelper<double>(1.000000001, 1));
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanHelper<float>(1.00001f, 1));
        }

        [Fact]
        public static void GenericHelpers_ThrowIfGreaterThanOrEqual_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanOrEqualHelper<int>(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanOrEqualHelper<uint>(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanOrEqualHelper<double>(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(GreaterThanOrEqualHelper<float>(1, 1));
        }

        [Fact]
        public static void GenericHelpers_ThrowIfLessThan_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(LessThanHelper<int>(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(LessThanHelper<uint>(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(LessThanHelper<double>(1, 1.000000001));
            Assert.Throws<ArgumentOutOfRangeException>(LessThanHelper<float>(1, 1.00001f));
        }

        [Fact]
        public static void GenericHelpers_ThrowIfLessThanOrEqual_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(LessThanOrEqualHelper<int>(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(LessThanOrEqualHelper<uint>(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(LessThanOrEqualHelper<double>(1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(LessThanOrEqualHelper<float>(1, 1));
        }
    }
}

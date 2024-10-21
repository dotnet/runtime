// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class NotFiniteNumberExceptionTests
    {
        private const int COR_E_NOTFINITENUMBER = -2146233048;

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new NotFiniteNumberException();
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
            Assert.Equal(COR_E_NOTFINITENUMBER, exception.HResult);
        }

        [Fact]
        public static void Ctor_Double()
        {
            double offendingNumber = double.PositiveInfinity;
            var exception = new NotFiniteNumberException(offendingNumber);
            Assert.Equal(offendingNumber, exception.OffendingNumber);
            Assert.Equal(COR_E_NOTFINITENUMBER, exception.HResult);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "Created NotFiniteNumberException";
            var exception = new NotFiniteNumberException(message);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_NOTFINITENUMBER, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Double()
        {
            string message = "Created NotFiniteNumberException";
            double offendingNumber = double.NegativeInfinity;
            var exception = new NotFiniteNumberException(message, offendingNumber);
            Assert.Equal(message, exception.Message);
            Assert.Equal(offendingNumber, exception.OffendingNumber);
            Assert.Equal(COR_E_NOTFINITENUMBER, exception.HResult);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "Created NotFiniteNumberException";
            var innerException = new Exception("Created inner exception");
            var exception = new NotFiniteNumberException(message, innerException);
            Assert.Equal(message, exception.Message);
            Assert.Equal(COR_E_NOTFINITENUMBER, exception.HResult);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.HResult, exception.InnerException.HResult);
        }

        [Fact]
        public static void Ctor_String_Double_Exception()
        {
            string message = "Created NotFiniteNumberException";
            double offendingNumber = double.NaN;
            var innerException = new Exception("Created inner exception");
            var exception = new NotFiniteNumberException(message, offendingNumber, innerException);
            Assert.Equal(offendingNumber, exception.OffendingNumber);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(COR_E_NOTFINITENUMBER, exception.HResult);
        }
    }
}

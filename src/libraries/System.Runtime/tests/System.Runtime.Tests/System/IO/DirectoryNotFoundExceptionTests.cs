// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class DirectoryNotFoundExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new DirectoryNotFoundException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, validateMessage: false);
            Assert.Null(exception.DirectoryPath);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "That page was missing from the directory.";
            var exception = new DirectoryNotFoundException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, message: message);
            Assert.Null(exception.DirectoryPath);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "That page was missing from the directory.";
            var innerException = new Exception("Inner exception");
            var exception = new DirectoryNotFoundException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, innerException: innerException, message: message);
            Assert.Null(exception.DirectoryPath);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "That directory is gone.";
            string directoryPath = @"C:\missing\dir";
            var exception = new DirectoryNotFoundException(message, directoryPath);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, message: message);
            Assert.Equal(directoryPath, exception.DirectoryPath);
        }

        [Fact]
        public static void Ctor_String_String_Exception()
        {
            string message = "That directory is gone.";
            string directoryPath = @"C:\missing\dir";
            var innerException = new Exception("Inner exception");
            var exception = new DirectoryNotFoundException(message, directoryPath, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, innerException: innerException, message: message);
            Assert.Equal(directoryPath, exception.DirectoryPath);
        }

        [Fact]
        public static void Message_AutoConstructed_WhenNullMessage_WithDirectoryPath()
        {
            string directoryPath = @"/data/reports";
            var exception = new DirectoryNotFoundException(null, directoryPath);
            Assert.NotNull(exception.Message);
            Assert.Contains(directoryPath, exception.Message);
        }

        [Fact]
        public static void ToStringTest()
        {
            string message = "That directory is gone.";
            string directoryPath = @"C:\missing\dir";
            var innerException = new Exception("Inner exception");
            var exception = new DirectoryNotFoundException(message, directoryPath, innerException);

            string toString = exception.ToString();
            Assert.Contains(": " + message, toString);
            Assert.Contains(": '" + directoryPath + "'", toString);
            Assert.Contains("---> " + innerException.ToString(), toString);

            try { throw exception; }
            catch
            {
                Assert.False(string.IsNullOrEmpty(exception.StackTrace));
                Assert.Contains(exception.StackTrace, exception.ToString());
            }
        }
    }
}

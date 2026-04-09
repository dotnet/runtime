// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

        [Theory]
        [InlineData("That directory is gone.", @"C:\missing\dir")]
        [InlineData("", @"/data/reports")]
        [InlineData("Custom message", "")]
        public static void Ctor_String_String(string message, string directoryPath)
        {
            var exception = new DirectoryNotFoundException(message, directoryPath);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, message: message);
            Assert.Equal(directoryPath, exception.DirectoryPath);
        }

        [Theory]
        [InlineData("That directory is gone.", @"C:\missing\dir")]
        [InlineData("", @"/data/reports")]
        [InlineData("Custom message", "")]
        public static void Ctor_String_String_Exception(string message, string directoryPath)
        {
            var innerException = new Exception("Inner exception");
            var exception = new DirectoryNotFoundException(message, directoryPath, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, innerException: innerException, message: message);
            Assert.Equal(directoryPath, exception.DirectoryPath);
        }

        [Fact]
        public static void Ctor_NullDirectoryPath()
        {
            string message = "msg";
            var exception = new DirectoryNotFoundException(message, (string?)null);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, message: message);
            Assert.Null(exception.DirectoryPath);

            var innerException = new Exception();
            var exception2 = new DirectoryNotFoundException(message, (string?)null, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception2, hResult: HResults.COR_E_DIRECTORYNOTFOUND, innerException: innerException, message: message);
            Assert.Null(exception2.DirectoryPath);
        }

        [Fact]
        public static void Ctor_NullMessageAndNullDirectoryPath()
        {
            var exception = new DirectoryNotFoundException(null, (string?)null);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, validateMessage: false);
            Assert.Null(exception.DirectoryPath);
            Assert.NotNull(exception.Message);
            Assert.NotEmpty(exception.Message);
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
        public static void ToString_WithDirectoryPath()
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public static void ToString_WithoutDirectoryPath_OmitsDirectoryLine(string? directoryPath)
        {
            var exception = new DirectoryNotFoundException("message", directoryPath);
            string toString = exception.ToString();
            Assert.DoesNotContain("Directory path:", toString);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public static void GetObjectData_Roundtrip()
        {
            string message = "dir not found";
            string directoryPath = @"C:\missing\dir";
            var original = new DirectoryNotFoundException(message, directoryPath);

#pragma warning disable SYSLIB0011
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, original);
            stream.Position = 0;
            var deserialized = (DirectoryNotFoundException)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011

            Assert.Equal(message, deserialized.Message);
            Assert.Equal(directoryPath, deserialized.DirectoryPath);
            Assert.Equal(original.HResult, deserialized.HResult);
        }
    }
}

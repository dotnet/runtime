// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class FileNotFoundExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new FileNotFoundException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILENOTFOUND, validateMessage: false);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "this is not the file you're looking for";
            var exception = new FileNotFoundException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILENOTFOUND, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "this is not the file you're looking for";
            var innerException = new Exception("Inner exception");
            var exception = new FileNotFoundException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILENOTFOUND, innerException: innerException, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var exception = new FileNotFoundException(message, fileName);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILENOTFOUND, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String_Exception()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new FileNotFoundException(message, fileName, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILENOTFOUND, innerException: innerException, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Exception strings not guaranteed on UapAot.")]
        public static void ToStringTest()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new FileNotFoundException(message, fileName, innerException);

            var toString = exception.ToString();
            Assert.Contains(": " + message, toString);
            Assert.Contains(": '" + fileName + "'", toString);
            Assert.Contains("---> " + innerException.ToString(), toString);

            // set the stack trace
            try { throw exception; }
            catch
            {
                Assert.False(string.IsNullOrEmpty(exception.StackTrace));
                Assert.Contains(exception.StackTrace, exception.ToString());
            }
        }

        [Fact]
        public static void FusionLogTest()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new FileNotFoundException(message, fileName, innerException);

            Assert.Null(exception.FusionLog);
        }
    }
}

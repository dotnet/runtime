// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class BadImageFormatExceptionTests
    {
        private const int COR_E_BADIMAGEFORMAT = unchecked((int)0x8007000B);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new BadImageFormatException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, validateMessage: false);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "this is not the file you're looking for";
            var exception = new BadImageFormatException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "this is not the file you're looking for";
            var innerException = new Exception("Inner exception");
            var exception = new BadImageFormatException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, innerException: innerException, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var exception = new BadImageFormatException(message, fileName);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String_Exception()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new BadImageFormatException(message, fileName, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, innerException: innerException, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Exception strings not guaranteed on UapAot.")]
        public static void ToStringTest()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new BadImageFormatException(message, fileName, innerException);

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
            var exception = new BadImageFormatException(message, fileName, innerException);

            Assert.Null(exception.FusionLog);
        }
    }
}

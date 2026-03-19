// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, validateMessage: false);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "this is not the file you're looking for";
            var exception = new BadImageFormatException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "this is not the file you're looking for";
            var innerException = new Exception("Inner exception");
            var exception = new BadImageFormatException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, innerException: innerException, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var exception = new BadImageFormatException(message, fileName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String_Exception()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new BadImageFormatException(message, fileName, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_BADIMAGEFORMAT, innerException: innerException, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public static void RequestingAssemblyChain_IncludedInMessage()
        {
            string fileName = "Bad.Assembly";
            string requestingAssembly = "Parent.Assembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            int hResult = unchecked((int)0x8007000B); // COR_E_BADIMAGEFORMAT

            ConstructorInfo? ctor = typeof(BadImageFormatException).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(string), typeof(string), typeof(int) },
                modifiers: null);

            Assert.NotNull(ctor);
            var exception = (BadImageFormatException)ctor.Invoke(new object[] { fileName, requestingAssembly, hResult });

            Assert.Contains(requestingAssembly, exception.Message);
            Assert.Null(exception.FusionLog);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public static void RequestingAssemblyChain_MultipleAssemblies_IncludedInMessage()
        {
            string fileName = "Bad.Assembly";
            string assemblyA = "A, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            string assemblyB = "B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null";
            string requestingChain = assemblyA + "\n" + assemblyB;
            int hResult = unchecked((int)0x8007000B); // COR_E_BADIMAGEFORMAT

            ConstructorInfo? ctor = typeof(BadImageFormatException).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(string), typeof(string), typeof(int) },
                modifiers: null);

            Assert.NotNull(ctor);
            var exception = (BadImageFormatException)ctor.Invoke(new object[] { fileName, requestingChain, hResult });

            Assert.Contains(assemblyA, exception.Message);
            Assert.Contains(assemblyB, exception.Message);
            Assert.Null(exception.FusionLog);
        }
    }
}

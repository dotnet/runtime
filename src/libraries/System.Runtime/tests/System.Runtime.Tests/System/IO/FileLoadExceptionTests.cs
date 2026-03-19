// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class FileLoadExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new FileLoadException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILELOAD, validateMessage: false);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "this is not the file you're looking for";
            var exception = new FileLoadException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILELOAD, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "this is not the file you're looking for";
            var innerException = new Exception("Inner exception");
            var exception = new FileLoadException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILELOAD, innerException: innerException, message: message);
            Assert.Null(exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var exception = new FileLoadException(message, fileName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILELOAD, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        public static void Ctor_String_String_Exception()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new FileLoadException(message, fileName, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_FILELOAD, innerException: innerException, message: message);
            Assert.Equal(fileName, exception.FileName);
        }

        [Fact]
        public static void ToStringTest()
        {
            string message = "this is not the file you're looking for";
            string fileName = "file.txt";
            var innerException = new Exception("Inner exception");
            var exception = new FileLoadException(message, fileName, innerException);

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
            var exception = new FileLoadException(message, fileName, innerException);

            Assert.Null(exception.FusionLog);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public static void RequestingAssemblyChain_IncludedInMessage()
        {
            string fileName = "Missing.Assembly";
            string requestingAssembly = "Parent.Assembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            int hResult = unchecked((int)0x80131621); // COR_E_FILELOAD

            ConstructorInfo? ctor = typeof(FileLoadException).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(string), typeof(string), typeof(int) },
                modifiers: null);

            Assert.NotNull(ctor);
            var exception = (FileLoadException)ctor.Invoke(new object[] { fileName, requestingAssembly, hResult });

            Assert.Contains(requestingAssembly, exception.Message);
            Assert.Null(exception.FusionLog);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public static void RequestingAssemblyChain_MultipleAssemblies_IncludedInMessage()
        {
            string fileName = "Missing.Assembly";
            string assemblyA = "A, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            string assemblyB = "B, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null";
            string requestingChain = assemblyA + "\n" + assemblyB;
            int hResult = unchecked((int)0x80131621); // COR_E_FILELOAD

            ConstructorInfo? ctor = typeof(FileLoadException).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(string), typeof(string), typeof(int) },
                modifiers: null);

            Assert.NotNull(ctor);
            var exception = (FileLoadException)ctor.Invoke(new object[] { fileName, requestingChain, hResult });

            Assert.Contains(assemblyA, exception.Message);
            Assert.Contains(assemblyB, exception.Message);
            Assert.Null(exception.FusionLog);
        }
    }
}

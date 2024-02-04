// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class ObjectDisposedExceptionTests
    {
        private const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);

        [Fact]
        public static void Ctor_String()
        {
            string objectName = "theObject";
            var exception = new ObjectDisposedException(objectName);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_OBJECTDISPOSED, validateMessage: false);
            Assert.Contains(objectName, exception.Message);

            var exceptionNullObjectName = new ObjectDisposedException(null);
            Assert.Equal("", exceptionNullObjectName.ObjectName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "object disposed";
            var innerException = new Exception("Inner exception");
            var exception = new ObjectDisposedException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_OBJECTDISPOSED, innerException: innerException, message: message);
            Assert.Equal("", exception.ObjectName);
        }

        [Fact]
        public static void Ctor_String_String()
        {
            string message = "object disposed";
            string objectName = "theObject";
            var exception = new ObjectDisposedException(objectName, message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_OBJECTDISPOSED, validateMessage: false);
            Assert.Equal(objectName, exception.ObjectName);
            Assert.Contains(message, exception.Message);
            Assert.Contains(objectName, exception.Message);
        }

        [Fact]
        public static void Throw_Object()
        {
            var obj = new object();
            ObjectDisposedException ex = AssertExtensions.Throws<ObjectDisposedException>(
                () => ObjectDisposedException.ThrowIf(true, obj));
            
            Assert.Equal("System.Object", ex.ObjectName);
        }

        [Fact]
        public static void Throw_Type()
        {
            Type type = new object().GetType();
            ObjectDisposedException ex = AssertExtensions.Throws<ObjectDisposedException>(
                () => ObjectDisposedException.ThrowIf(true, type));
            
            Assert.Equal("System.Object", ex.ObjectName);
        }
    }
}

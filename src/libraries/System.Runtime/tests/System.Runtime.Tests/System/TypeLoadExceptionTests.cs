// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public static class TypeLoadExceptionTests
    {
        private const int COR_E_TYPELOAD = unchecked((int)0x80131522);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new TypeLoadException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_TYPELOAD, validateMessage: false);
            Assert.Equal("", exception.TypeName);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "type failed to load";
            var exception = new TypeLoadException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_TYPELOAD, message: message);
            Assert.Equal("", exception.TypeName);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "type failed to load";
            var innerException = new Exception("Inner exception");
            var exception = new TypeLoadException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_TYPELOAD, innerException: innerException, message: message);
            Assert.Equal("", exception.TypeName);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
        public static void TypeLoadExceptionMessageContainsMethodNameWhenInternalCallOnlyMethodIsCalled()
        {
            var ex = Assert.Throws<TypeLoadException>(() => new F1());
            Assert.Contains("Internal call method 'F2.Foo' with non_NULL RVA.", ex.Message);
        }

        class F1
        {
            public F1()
            {
                var f2 = new F2();
            }
        }

        class F2
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            public void Foo()
            {

            }
        }
    }
}

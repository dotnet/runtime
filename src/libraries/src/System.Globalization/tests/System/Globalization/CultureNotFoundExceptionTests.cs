// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureNotFoundExceptionTests
    {
        [Fact]
        public void Ctor_String()
        {
            string message = "this is a test string";
            CultureNotFoundException cultureNotFoundException = new CultureNotFoundException(message);

            Assert.Equal(message, cultureNotFoundException.Message);
        }

        [Fact]
        public void Ctor_String_Exception()
        {
            string message = "this is a test string";
            Exception innerException = new Exception("inner exception string");
            CultureNotFoundException cultureNotFoundException = new CultureNotFoundException(message, innerException);

            Assert.Equal(message, cultureNotFoundException.Message);
            Assert.Same(innerException, cultureNotFoundException.InnerException);
        }

        [Fact]
        public void Ctor_String_String()
        {
            string paramName = "nameOfParam";
            string message = "this is a test string";
            CultureNotFoundException cultureNotFoundException = new CultureNotFoundException(paramName, message);

            Assert.Equal(paramName, cultureNotFoundException.ParamName);
            Assert.NotEmpty(cultureNotFoundException.Message);
        }

        [Fact]
        public void Ctor_String_String_Exception()
        {
            string message = "this is a test string";
            string invalidCultureName = "abcd";
            Exception innerException = new Exception("inner exception string");
            CultureNotFoundException cultureNotFoundException = new CultureNotFoundException(message, invalidCultureName, innerException);

            Assert.NotEmpty(cultureNotFoundException.Message);
            Assert.Equal(invalidCultureName, cultureNotFoundException.InvalidCultureName);
            Assert.Same(innerException, cultureNotFoundException.InnerException);
        }

        [Fact]
        public void Ctor_String_String_String()
        {
            string paramName = "nameOfParam";
            string invalidCultureName = "abcd";
            string message = "this is a test string";
            CultureNotFoundException cultureNotFoundException = new CultureNotFoundException(paramName, invalidCultureName, message);

            Assert.Equal(paramName, cultureNotFoundException.ParamName);
            Assert.Equal(invalidCultureName, cultureNotFoundException.InvalidCultureName);
            Assert.NotEmpty(cultureNotFoundException.Message);
        }
    }
}

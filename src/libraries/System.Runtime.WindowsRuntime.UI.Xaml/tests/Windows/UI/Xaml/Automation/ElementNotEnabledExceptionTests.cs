// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Windows.UI.Xaml.Automation.Tests
{
    public class ElementNotEnabledExceptionTests
    {
        private const int E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);

        [Fact]
        public void Ctor_Default()
        {
            var exception = new ElementNotEnabledException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(E_ELEMENTNOTENABLED, exception.HResult);
        }

        [Fact]
        public void Ctor_Message()
        {
            var exception = new ElementNotEnabledException("Message");
            Assert.Equal("Message", exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(E_ELEMENTNOTENABLED, exception.HResult);
        }

        [Fact]
        public void Ctor_Message_InnerException()
        {
            var innerException = new Exception();
            var exception = new ElementNotEnabledException("Message", innerException);
            Assert.Equal("Message", exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(E_ELEMENTNOTENABLED, exception.HResult);
        }
    }
}
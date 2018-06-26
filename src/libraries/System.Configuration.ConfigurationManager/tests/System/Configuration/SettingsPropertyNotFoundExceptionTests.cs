// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Configuration
{
    public class SettingsPropertyNotFoundExceptionTests
    {
        [Fact]
        public void SingleParameterExceptionReturnsExpected()
        {
            var exception =  new SettingsPropertyNotFoundException("ThisIsATest");
            Assert.Equal("ThisIsATest", exception.Message);
        }

        [Fact]
        public void ExceptionWithInnerExceptionExceptionReturnsExpected()
        {
            var exception = new SettingsPropertyNotFoundException("ThisIsATest", new AggregateException("AlsoATest"));
            Assert.Equal("ThisIsATest", exception.Message);
            Assert.Equal("AlsoATest", exception.InnerException.Message);
            Assert.IsType<AggregateException>(exception.InnerException);
        }

        [Fact]
        public void ExceptionEmptyConstructorReturnsExpected()
        {
            var exception = new SettingsPropertyNotFoundException();
            Assert.IsType<SettingsPropertyNotFoundException>(exception);
        }
    }
}

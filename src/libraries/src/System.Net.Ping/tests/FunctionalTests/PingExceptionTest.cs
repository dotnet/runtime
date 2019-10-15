// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class PingExceptionTest
    {
        [Fact]
        public void CtorValuesPassedToProperties()
        {
            string message = "hello";
            Exception inner = new Exception();

            Assert.Same(message, new PingException(message).Message);
            Assert.Same(message, new PingException(message, inner).Message);
            Assert.Same(inner, new PingException(message, inner).InnerException);
        }
    }
}

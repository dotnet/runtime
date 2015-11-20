// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

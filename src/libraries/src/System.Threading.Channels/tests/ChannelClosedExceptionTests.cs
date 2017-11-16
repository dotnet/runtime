// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Threading.Channels.Tests
{
    public class ChannelClosedExceptionTests
    {
        [Fact]
        public void Ctors()
        {
            var e = new ChannelClosedException();
            Assert.NotEmpty(e.Message);
            Assert.Null(e.InnerException);

            e = new ChannelClosedException("hello");
            Assert.Equal("hello", e.Message);
            Assert.Null(e.InnerException);

            var inner = new FormatException();
            e = new ChannelClosedException("hello", inner);
            Assert.Equal("hello", e.Message);
            Assert.Same(inner, e.InnerException);
        }
    }
}

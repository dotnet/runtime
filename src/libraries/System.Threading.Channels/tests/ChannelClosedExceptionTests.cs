// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Channels.Tests
{
    public partial class ChannelClosedExceptionTests
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

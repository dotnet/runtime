// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class HandledEventArgsTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var args = new HandledEventArgs();
            Assert.False(args.Handled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool(bool handled)
        {
            var args = new HandledEventArgs(handled);
            Assert.Equal(handled, handled);
        }
    }
}

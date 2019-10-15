// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class CancelEventArgsTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var eventArgs = new CancelEventArgs();
            Assert.False(eventArgs.Cancel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Cancel(bool cancel)
        {
            var eventArgs = new CancelEventArgs(cancel);
            Assert.Equal(cancel, eventArgs.Cancel);
        }

        [Fact]
        public void Cancel_Set_GetReturnsExpected()
        {
            var eventArgs = new CancelEventArgs();
            for (int i = 0; i < 2; i++)
            {
                eventArgs.Cancel = false;
                Assert.False(eventArgs.Cancel);

                eventArgs.Cancel = true;
                Assert.True(eventArgs.Cancel);
            }
        }
    }
}
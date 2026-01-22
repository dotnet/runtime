// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class ConsoleLifetimeOptionsTests
    {
        [Fact]
        public void SuppressStatusMessages_CanBeToggled()
        {
            var options = new ConsoleLifetimeOptions();
            Assert.False(options.SuppressStatusMessages);

            options.SuppressStatusMessages = true;
            Assert.True(options.SuppressStatusMessages);

            options.SuppressStatusMessages = false;
            Assert.False(options.SuppressStatusMessages);
        }
    }
}

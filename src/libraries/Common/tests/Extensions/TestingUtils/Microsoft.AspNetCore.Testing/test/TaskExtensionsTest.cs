// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TaskExtensionsTest
    {
        [Fact]
        public async Task TimeoutAfterTest()
        {
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(1000).TimeoutAfter(TimeSpan.FromMilliseconds(50)));
        }
    }
}

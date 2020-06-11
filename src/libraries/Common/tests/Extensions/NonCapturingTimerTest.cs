﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class NonCapturingTimerTest
    {
        [Fact]
        public async Task NonCapturingTimer_DoesntCaptureExecutionContext()
        {
            // Arrange
            var message = new AsyncLocal<string>();
            message.Value = "Hey, this is a value stored in the execuion context";

            var tcs = new TaskCompletionSource<string>();

            // Act
            var timer = NonCapturingTimer.Create((_) =>
            {
                // Observe the value based on the current execution context
                tcs.SetResult(message.Value);
            }, state: null, dueTime: TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);

            // Assert
            var messageFromTimer = await tcs.Task;
            timer.Dispose();

            // ExecutionContext didn't flow to timer callback
            Assert.Null(messageFromTimer);

            // ExecutionContext was restored
            Assert.NotNull(await Task.Run(() => message.Value));
        }
    }
}

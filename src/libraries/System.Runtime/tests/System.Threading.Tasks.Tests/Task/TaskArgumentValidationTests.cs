// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public sealed class TaskArgumentValidationTests
    {
        [Theory]
        [InlineData(-2)]
        [InlineData((long)int.MaxValue + 1)]
        public void Task_Wait_ArgumentOutOfRange(long milliseconds)
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(milliseconds);
            Task task = Task.Run(static () => {});
            Assert.Throws<ArgumentOutOfRangeException>("timeout", () => task.Wait(timeout));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class FlushResultTests
    {
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [Theory]
        public void FlushResultCanBeConstructed(bool cancelled, bool completed)
        {
            var result = new FlushResult(cancelled, completed);

            Assert.Equal(cancelled, result.IsCanceled);
            Assert.Equal(completed, result.IsCompleted);
        }
    }
}

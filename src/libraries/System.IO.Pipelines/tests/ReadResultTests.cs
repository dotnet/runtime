// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class ReadResultTests
    {
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [Theory]
        public void ReadResultCanBeConstructed(bool cancelled, bool completed)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 });
            var result = new ReadResult(buffer, cancelled, completed);

            Assert.Equal(new byte[] { 1, 2, 3 }, result.Buffer.ToArray());
            Assert.Equal(cancelled, result.IsCanceled);
            Assert.Equal(completed, result.IsCompleted);
        }
    }
}

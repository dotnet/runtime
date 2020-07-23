// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public sealed class UmsDisposeAsync
    {
        [Fact]
        public void DisposeAsync_ClosesStream()
        {
            using (var manager = new UmsManager(FileAccess.Write, 1000))
            {
                UnmanagedMemoryStream stream = manager.Stream;
                Assert.True(stream.CanWrite);
                Assert.True(stream.DisposeAsync().IsCompletedSuccessfully);
                Assert.False(stream.CanWrite);
            }
        }
    }
}

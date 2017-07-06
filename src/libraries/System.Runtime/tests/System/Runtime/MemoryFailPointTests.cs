// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.Tests
{
    public class MemoryFailPointTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Ctor_Dispose_Success(int sizeInMegabytes)
        {
            var memoryFailPoint = new MemoryFailPoint(sizeInMegabytes);
            memoryFailPoint.Dispose();
            memoryFailPoint.Dispose();
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        public void Ctor_Negative_ThrowsArgumentOutOfRangeException(int sizeInMegabytes)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sizeInMegabytes", () => new MemoryFailPoint(sizeInMegabytes));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] //https://github.com/dotnet/coreclr/issues/7807
        public void Ctor_LargeSizeInMegabytes_ThrowsInsufficientMemoryException()
        {
            Assert.Throws<InsufficientMemoryException>(() => new MemoryFailPoint(int.MaxValue));
        }
    }
}
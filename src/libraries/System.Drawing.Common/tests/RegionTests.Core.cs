// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public partial class RegionTests
    {
        [Fact]
        public void FromHandle()
        {
            using var region = new Region();

            var expectedHandle = region.Handle;
            var actualRegion = Region.FromHandle(expectedHandle);

            IntPtr actualHandle = actualRegion.Handle;
            Assert.Equal(expectedHandle, actualHandle);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public partial class ImageTests
    {
        [Fact]
        public void FromHandle()
        {
            using var image = new Bitmap(1, 1);

            var expectedHandle = image.Handle;
            var actualImage = Image.FromHandle(expectedHandle);

            IntPtr actualHandle = actualImage.Handle;
            Assert.Equal(expectedHandle, actualHandle);
        }
    }
}

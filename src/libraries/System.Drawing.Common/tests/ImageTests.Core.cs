// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public partial class ImageTests
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [Fact]
        public void FromHandle()
        {
            var image = new Bitmap(1, 1);

            var expectedHandle = image.Handle;
            var actualImage = Image.FromHandle(expectedHandle);

            IntPtr actualHandle = actualImage.Handle;
            Assert.Equal(expectedHandle, actualHandle);

            // Do not dispose `image` because it is the same handle
            actualImage.Dispose();
        }
    }
}

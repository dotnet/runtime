// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public partial class BrushTests
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [Fact]
        public void FromHandle()
        {
            using var brush = new SolidBrush(Color.White);

            var expectedHandle = brush.Handle;
            var actualBrush = Brush.FromHandle(expectedHandle);

            IntPtr actualHandle = actualBrush.Handle;
            Assert.Equal(expectedHandle, actualHandle);
        }
    }
}

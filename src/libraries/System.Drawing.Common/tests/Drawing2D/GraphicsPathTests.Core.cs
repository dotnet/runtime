// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public partial class GraphicsPathTests
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [Fact]
        public void FromHandle()
        {
            using var graphicsPath = new GraphicsPath();

            var expectedHandle = graphicsPath.Handle;
            var actualGraphicsPath = Matrix.FromHandle(expectedHandle);

            IntPtr actualHandle = actualGraphicsPath.Handle;
            Assert.Equal(expectedHandle, actualHandle);
        }
    }
}

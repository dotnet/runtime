// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public partial class BrushTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not implemented in .NET Framework.")]
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

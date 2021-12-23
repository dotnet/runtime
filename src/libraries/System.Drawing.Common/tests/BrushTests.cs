// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public class BrushTests
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetNativeBrush_Brush_Success()
        {
            using (var brush = new SubBrush())
            {
                brush.PublicSetNativeBrush((IntPtr)10);
                brush.PublicSetNativeBrush(IntPtr.Zero);

                brush.PublicSetNativeBrush((IntPtr)10);
                brush.PublicSetNativeBrush(IntPtr.Zero);
            }
        }

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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/30157")]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dispose_NoSuchEntryPoint_SilentyCatchesException()
        {
            var brush = new SubBrush();
            brush.PublicSetNativeBrush((IntPtr)10);

            // No EntryPointNotFoundException will be thrown.
            brush.Dispose();
        }

        private class SubBrush : Brush
        {
            public override object Clone() => this;
            public void PublicSetNativeBrush(IntPtr brush) => SetNativeBrush(brush);
        }
    }
}

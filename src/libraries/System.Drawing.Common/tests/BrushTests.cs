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
            var handle = IntPtr.Zero;
            var pen = Brush.FromHandle(handle);

            IntPtr actualHandle = pen.Handle;
            Assert.Equal(handle, actualHandle);
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

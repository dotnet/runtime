// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Drawing.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public static class GdiPlusHandlesTests
    {
        public static bool IsDrawingAndRemoteExecutorSupported => Helpers.GetIsDrawingSupported() && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(IsDrawingAndRemoteExecutorSupported))]
        public static void GraphicsDrawIconDoesNotLeakHandles()
        {
            RemoteExecutor.Invoke(() =>
            {
                const int handleTreshold = 1;
                Bitmap bmp = new(100, 100);
                Icon ico = new(Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico"));
                IntPtr currentProcessHandle = Process.GetCurrentProcess().Handle;
                IntPtr hdc = Helpers.GetDC(Helpers.GetForegroundWindow());
                using Graphics graphicsFromHdc = Graphics.FromHdc(hdc);

                int initialHandles = Helpers.GetGuiResources(currentProcessHandle, 0);

                for (int i = 0; i < 5000; i++)
                {
                    graphicsFromHdc.DrawIcon(ico, 100, 100);
                }

                int finalHandles = Helpers.GetGuiResources(currentProcessHandle, 0);

                Assert.InRange(finalHandles, initialHandles, initialHandles + handleTreshold);
            }).Dispose();

        }

    }
}

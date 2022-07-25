// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.Drawing.Tests
{
    public static class GdiPlusHandlesTests
    {
        public static bool IsDrawingAndRemoteExecutorSupported => Helpers.GetIsDrawingSupported() && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(IsDrawingAndRemoteExecutorSupported))]
        public static void GraphicsDrawIconDoesNotLeakHandles()
        {
            RemoteExecutor.Invoke(() =>
            {
                const int handleTreshold = 1;
                using Bitmap bmp = new(100, 100);
                using Icon ico = new(Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico"));

                IntPtr hdc = Helpers.GetDC(Helpers.GetForegroundWindow());
                using Graphics graphicsFromHdc = Graphics.FromHdc(hdc);

                using Process currentProcess = Process.GetCurrentProcess();
                IntPtr processHandle = currentProcess.Handle;

                int initialHandles = Helpers.GetGuiResources(processHandle, 0);
                ValidateNoWin32Error(initialHandles);

                for (int i = 0; i < 5000; i++)
                {
                    graphicsFromHdc.DrawIcon(ico, 100, 100);
                }

                int finalHandles = Helpers.GetGuiResources(processHandle, 0);
                ValidateNoWin32Error(finalHandles);

                Assert.InRange(finalHandles, initialHandles - handleTreshold, initialHandles + handleTreshold);
            }).Dispose();
        }

        private static void ValidateNoWin32Error(int handleCount)
        {
            if (handleCount == 0)
            {
                int error = Marshal.GetLastWin32Error();

                if (error != 0)
                    throw new XunitException($"GetGuiResources failed with win32 error: {error}");
            }
        }

    }
}

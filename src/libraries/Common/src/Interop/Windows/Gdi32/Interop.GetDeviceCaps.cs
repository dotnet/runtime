// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        public enum DeviceCapability : int
        {
            TECHNOLOGY = 2,
            VERTRES = 10,
            HORZRES = 8,
            BITSPIXEL = 12,
            PLANES = 14,
            LOGPIXELSX = 88,
            LOGPIXELSY = 90,
            PHYSICALWIDTH = 110,
            PHYSICALHEIGHT = 111,
            PHYSICALOFFSETX = 112,
            PHYSICALOFFSETY = 113
        }

        public static class DeviceTechnology
        {
            public const int DT_PLOTTER = 0;
            public const int DT_RASPRINTER = 2;
        }

        [DllImport(Libraries.Gdi32, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, DeviceCapability index);

        public static int GetDeviceCaps(HandleRef hdc, DeviceCapability index)
        {
            int caps = GetDeviceCaps(hdc.Handle, index);
            GC.KeepAlive(hdc.Wrapper);
            return caps;
        }
    }
}

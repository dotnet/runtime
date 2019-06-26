// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the attributes of a bitmap image.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class BitmapData
    {
        private int _width;
        private int _height;
        private int _stride;
        private PixelFormat _pixelFormat;
        private IntPtr _scan0;
        private int _reserved;
    }
}

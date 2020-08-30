// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{
    public partial class CustomLineCap
    {
        internal static CustomLineCap CreateCustomLineCapObject(IntPtr cap)
        {
            // libgdiplus does not implement GdipGetCustomLineCapType, so it will not correctly handle
            // AdjustableArrowCap objects.
            return new CustomLineCap(cap);
        }
    }
}

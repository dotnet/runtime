// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{

    [StructLayout(LayoutKind.Sequential)]
    internal struct GPRECTF
    {
        internal float X;
        internal float Y;
        internal float Width;
        internal float Height;

        internal GPRECTF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal GPRECTF(RectangleF rect)
        {
            X = rect.X;
            Y = rect.Y;
            Width = rect.Width;
            Height = rect.Height;
        }

        internal SizeF SizeF => new SizeF(Width, Height);

        internal RectangleF ToRectangleF() => new RectangleF(X, Y, Width, Height);
    }
}

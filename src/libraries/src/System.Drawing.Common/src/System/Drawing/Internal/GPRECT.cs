// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPRECT
    {
        internal int X;
        internal int Y;
        internal int Width;
        internal int Height;

        internal GPRECT(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal GPRECT(Rectangle rect)
        {
            X = rect.X;
            Y = rect.Y;
            Width = rect.Width;
            Height = rect.Height;
        }
    }
}

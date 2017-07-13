// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GPPOINTF
    {
        internal float X;
        internal float Y;

        internal GPPOINTF()
        {
        }

        internal GPPOINTF(PointF pt)
        {
            X = pt.X;
            Y = pt.Y;
        }

        internal PointF ToPoint()
        {
            return new PointF(X, Y);
        }
    }
}

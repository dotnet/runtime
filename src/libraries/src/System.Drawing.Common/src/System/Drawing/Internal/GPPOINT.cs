// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GPPOINT
    {
        internal int X;
        internal int Y;

        internal GPPOINT()
        {
        }

        internal GPPOINT(Point pt)
        {
            X = pt.X;
            Y = pt.Y;
        }
    }
}

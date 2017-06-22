// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    public enum PathPointType
    {
        Start = 0, // move
        Line = 1, // line
        Bezier = 3, // default Beizer (= cubic Bezier)
        PathTypeMask = 0x07, // type mask (lowest 3 bits).
        DashMode = 0x10, // currently in dash mode.
        PathMarker = 0x20, // a marker for the path.
        CloseSubpath = 0x80, // closed flag

        // Path types used for advanced path.
        Bezier3 = 3,    // cubic Bezier
    }
}

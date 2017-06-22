// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    public enum LineCap
    {
        Flat = 0,
        Square = 1,
        Round = 2,
        Triangle = 3,
        NoAnchor = 0x10, // corresponds to flat cap
        SquareAnchor = 0x11, // corresponds to square cap
        RoundAnchor = 0x12, // corresponds to round cap
        DiamondAnchor = 0x13, // corresponds to triangle cap
        ArrowAnchor = 0x14, // no correspondence

        Custom = 0xff, // custom cap
        AnchorMask = 0xf0  // mask to check for anchor or not.
    }
}

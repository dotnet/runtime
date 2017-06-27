// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies the different patterns available 'RotateFlipType' objects.
    /// </summary>
    public enum RotateFlipType
    {
        RotateNoneFlipNone = 0,
        Rotate90FlipNone = 1,
        Rotate180FlipNone = 2,
        Rotate270FlipNone = 3,
        RotateNoneFlipX = 4,
        Rotate90FlipX = 5,
        Rotate180FlipX = 6,
        Rotate270FlipX = 7,
        RotateNoneFlipY = Rotate180FlipX,
        Rotate90FlipY = Rotate270FlipX,
        Rotate180FlipY = RotateNoneFlipX,
        Rotate270FlipY = Rotate90FlipX,
        RotateNoneFlipXY = Rotate180FlipNone,
        Rotate90FlipXY = Rotate270FlipNone,
        Rotate180FlipXY = RotateNoneFlipNone,
        Rotate270FlipXY = Rotate90FlipNone
    }
}


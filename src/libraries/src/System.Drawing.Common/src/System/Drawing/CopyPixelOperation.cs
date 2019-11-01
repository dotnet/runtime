// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies the Copy Pixel (ROP) operation.
    /// </summary>
    public enum CopyPixelOperation
    {
        /// <summary>
        /// Fills the Destination Rectangle using the color associated with the index 0 in the physical palette.
        /// </summary>
        Blackness = Interop.Gdi32.RasterOp.BLACKNESS,
        /// <summary>
        /// Includes any windows that are Layered on Top.
        /// </summary>
        CaptureBlt = Interop.Gdi32.RasterOp.CAPTUREBLT,
        DestinationInvert = Interop.Gdi32.RasterOp.DSTINVERT,
        MergeCopy = Interop.Gdi32.RasterOp.MERGECOPY,
        MergePaint = Interop.Gdi32.RasterOp.MERGEPAINT,
        NoMirrorBitmap = Interop.Gdi32.RasterOp.NOMIRRORBITMAP,
        NotSourceCopy = Interop.Gdi32.RasterOp.NOTSRCCOPY,
        NotSourceErase = Interop.Gdi32.RasterOp.NOTSRCERASE,
        PatCopy = Interop.Gdi32.RasterOp.PATCOPY,
        PatInvert = Interop.Gdi32.RasterOp.PATINVERT,
        PatPaint = Interop.Gdi32.RasterOp.PATPAINT,
        SourceAnd = Interop.Gdi32.RasterOp.SRCAND,
        SourceCopy = Interop.Gdi32.RasterOp.SRCCOPY,
        SourceErase = Interop.Gdi32.RasterOp.SRCERASE,
        SourceInvert = Interop.Gdi32.RasterOp.SRCINVERT,
        SourcePaint = Interop.Gdi32.RasterOp.SRCPAINT,
        Whiteness = Interop.Gdi32.RasterOp.WHITENESS,
    }
}

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
        Blackness = SafeNativeMethods.BLACKNESS,
        /// <summary>
        /// Includes any windows that are Layered on Top.
        /// </summary>
        CaptureBlt = SafeNativeMethods.CAPTUREBLT,
        DestinationInvert = SafeNativeMethods.DSTINVERT,
        MergeCopy = SafeNativeMethods.MERGECOPY,
        MergePaint = SafeNativeMethods.MERGEPAINT,
        NoMirrorBitmap = SafeNativeMethods.NOMIRRORBITMAP,
        NotSourceCopy = SafeNativeMethods.NOTSRCCOPY,
        NotSourceErase = SafeNativeMethods.NOTSRCERASE,
        PatCopy = SafeNativeMethods.PATCOPY,
        PatInvert = SafeNativeMethods.PATINVERT,
        PatPaint = SafeNativeMethods.PATPAINT,
        SourceAnd = SafeNativeMethods.SRCAND,
        SourceCopy = SafeNativeMethods.SRCCOPY,
        SourceErase = SafeNativeMethods.SRCERASE,
        SourceInvert = SafeNativeMethods.SRCINVERT,
        SourcePaint = SafeNativeMethods.SRCPAINT,
        Whiteness = SafeNativeMethods.WHITENESS,
    }
}

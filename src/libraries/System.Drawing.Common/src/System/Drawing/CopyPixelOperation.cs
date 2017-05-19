// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the
    ///       Copy Pixel (ROP) operation.
    ///    </para>
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum CopyPixelOperation
    {
        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.Blackness"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Fills the Destination Rectangle using the color associated with the index 0 in the physical palette.
        ///    </para>
        /// </devdoc>
        Blackness = SafeNativeMethods.BLACKNESS,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.CaptureBlt"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Includes any windows that are Layered on Top.
        ///    </para>
        /// </devdoc>
        CaptureBlt = SafeNativeMethods.CAPTUREBLT,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.DestinationInvert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       DestinationInvert.
        ///    </para>
        /// </devdoc>
        DestinationInvert = SafeNativeMethods.DSTINVERT,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.MergeCopy"]/*' />
        /// <devdoc>
        ///    <para>
        ///       MergeCopy.
        ///    </para>
        /// </devdoc>
        MergeCopy = SafeNativeMethods.MERGECOPY,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.MergePaint"]/*' />
        /// <devdoc>
        ///    <para>
        ///       MergePaint.
        ///    </para>
        /// </devdoc>
        MergePaint = SafeNativeMethods.MERGEPAINT,


        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.NoMirrorBitmap"]/*' />
        /// <devdoc>
        ///    <para>
        ///       NoMirrorBitmap.
        ///    </para>
        /// </devdoc>
        NoMirrorBitmap = SafeNativeMethods.NOMIRRORBITMAP,


        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.NotSourceCopy"]/*' />
        /// <devdoc>
        ///    <para>
        ///       NotSourceCopy.
        ///    </para>
        /// </devdoc>
        NotSourceCopy = SafeNativeMethods.NOTSRCCOPY,


        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.NotSourceErase"]/*' />
        /// <devdoc>
        ///    <para>
        ///       NotSourceErase.
        ///    </para>
        /// </devdoc>
        NotSourceErase = SafeNativeMethods.NOTSRCERASE,



        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.PatCopy"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PatCopy.
        ///    </para>
        /// </devdoc>
        PatCopy = SafeNativeMethods.PATCOPY,



        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.PatInvert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PatInvert.
        ///    </para>
        /// </devdoc>
        PatInvert = SafeNativeMethods.PATINVERT,


        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.PatPaint"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PatPaint.
        ///    </para>
        /// </devdoc>
        PatPaint = SafeNativeMethods.PATPAINT,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.SourceAnd"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SourceAnd.
        ///    </para>
        /// </devdoc>
        SourceAnd = SafeNativeMethods.SRCAND,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.SourceCopy"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SourceCopy.
        ///    </para>
        /// </devdoc>
        SourceCopy = SafeNativeMethods.SRCCOPY,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.SourceErase"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SourceErase.
        ///    </para>
        /// </devdoc>
        SourceErase = SafeNativeMethods.SRCERASE,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.SourceInvert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SourceInvert.
        ///    </para>
        /// </devdoc>
        SourceInvert = SafeNativeMethods.SRCINVERT,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.SourcePaint"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SourcePaint.
        ///    </para>
        /// </devdoc>
        SourcePaint = SafeNativeMethods.SRCPAINT,

        /// <include file='doc\CopyPixelOperation.uex' path='docs/doc[@for="CopyPixelOperation.Whiteness"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Whiteness.
        ///    </para>
        /// </devdoc>
        Whiteness = SafeNativeMethods.WHITENESS,
    }
}

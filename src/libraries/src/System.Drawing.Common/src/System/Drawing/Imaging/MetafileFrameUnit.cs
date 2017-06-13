// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * Page unit constants
     */
    /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the unit of measurement for the
    ///       rectangle used to size and position a metafile. This is specified during the
    ///       creation of the <see cref='System.Drawing.Imaging.Metafile'/>.
    ///    </para>
    /// </devdoc>    
    public enum MetafileFrameUnit
    {
        /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit.Pixel"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies a pixel as the unit of measure.
        ///    </para>
        /// </devdoc>
        Pixel = GraphicsUnit.Pixel,
        /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit.Point"]/*' />
        /// <devdoc>
        ///    Specifies a printer's point as
        ///    the unit of measure.
        /// </devdoc>
        Point = GraphicsUnit.Point,
        /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit.Inch"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies an inch as the unit of measure.
        ///    </para>
        /// </devdoc>
        Inch = GraphicsUnit.Inch,
        /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit.Document"]/*' />
        /// <devdoc>
        ///    Specifies 1/300 of an inch as the unit of
        ///    measure.
        /// </devdoc>
        Document = GraphicsUnit.Document,
        /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit.Millimeter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies a millimeter as the unit of
        ///       measure.
        ///    </para>
        /// </devdoc>
        Millimeter = GraphicsUnit.Millimeter,
        /// <include file='doc\MetafileFrameUnit.uex' path='docs/doc[@for="MetafileFrameUnit.GdiCompatible"]/*' />
        /// <devdoc>
        ///    Specifies .01 millimeter as the unit of
        ///    measure. Provided for compatibility with GDI.
        /// </devdoc>
        GdiCompatible
    }
}

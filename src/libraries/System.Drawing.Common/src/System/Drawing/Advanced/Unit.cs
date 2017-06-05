// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit"]/*' />
    /// <devdoc>
    ///    Specifies the unit of measure for the given
    ///    data.
    /// </devdoc>
    public enum GraphicsUnit
    {
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.World"]/*' />
        /// <devdoc>
        ///    Specifies the world unit as the unit of
        ///    measure.
        /// </devdoc>
        World = 0,     // 0 -- World coordinate (non-physical unit)
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.Display"]/*' />
        /// <devdoc>
        ///    Specifies 1/75 inch as the unit of measure.
        /// </devdoc>
        Display = 1,    // 1 -- Variable - for PageTransform only
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.Pixel"]/*' />
        /// <devdoc>
        ///    Specifies a device pixel as the unit of
        ///    measure.
        /// </devdoc>
        Pixel = 2,      // 2 -- Each unit is one device pixel.
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.Point"]/*' />
        /// <devdoc>
        ///    Specifies a printer's point (1/72 inch) as
        ///    the unit of measure.
        /// </devdoc>
        Point = 3,      // 3 -- Each unit is a printer's point, or 1/72 inch.
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.Inch"]/*' />
        /// <devdoc>
        ///    Specifies the inch as the unit of measure.
        /// </devdoc>
        Inch = 4,       // 4 -- Each unit is 1 inch.
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.Document"]/*' />
        /// <devdoc>
        ///    Specifes the document unit (1/300 inch) as
        ///    the unit of measure.
        /// </devdoc>
        Document = 5,   // 5 -- Each unit is 1/300 inch.
        /// <include file='doc\Unit.uex' path='docs/doc[@for="GraphicsUnit.Millimeter"]/*' />
        /// <devdoc>
        ///    Specifies the millimeter as the unit of
        ///    measure.
        /// </devdoc>
        Millimeter = 6  // 6 -- Each unit is 1 millimeter.
    }
}


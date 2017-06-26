// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the unit of measurement for the rectangle used to size and position a metafile.
    /// This is specified during the creation of the <see cref='Metafile'/>.
    /// </summary>    
    public enum MetafileFrameUnit
    {
        /// <summary>
        /// Specifies a pixel as the unit of measure.
        /// </summary>
        Pixel = GraphicsUnit.Pixel,
        /// <summary>
        /// Specifies a printer's point as the unit of measure.
        /// </summary>
        Point = GraphicsUnit.Point,
        /// <summary>
        /// Specifies an inch as the unit of measure.
        /// </summary>
        Inch = GraphicsUnit.Inch,
        /// <summary>
        /// Specifies 1/300 of an inch as the unit of measure.
        /// </summary>
        Document = GraphicsUnit.Document,
        /// <summary>
        /// Specifies a millimeter as the unit of measure.
        /// </summary>
        Millimeter = GraphicsUnit.Millimeter,
        /// <summary>
        /// Specifies .01 millimeter as the unit of measure. Provided for compatibility with GDI.
        /// </summary>
        GdiCompatible
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies the units of measure for a text string.
    /// </summary>
    public enum StringUnit
    {
        /// <summary>
        /// Specifies world units as the unit of measure.
        /// </summary>
        World = GraphicsUnit.World,
        /// <summary>
        /// Specifies the device unit as the unit of measure.
        /// </summary>
        Display = GraphicsUnit.Display,
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
        /// Specifies a millimeter as the unit of measure
        /// </summary>
        Millimeter = GraphicsUnit.Millimeter,
        /// <summary>
        /// Specifies a printer's em size of 32 as the unit of measure.
        /// </summary>
        Em = 32
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies several of the units of measure Microsoft Win32 uses for printing.
    /// </summary>
    public enum PrinterUnit
    {
        /// <summary>
        /// The default unit (0.01 in.).
        /// </summary>
        Display = 0,

        /// <summary>
        /// One thousandth of an inch (0.001 in.).
        /// </summary>
        // Used by PAGESETUPDLG.rtMargin and rtMinMargin
        ThousandthsOfAnInch = 1,

        /// <summary>
        /// One hundredth of a millimeter (0.01 mm).
        /// </summary>
        // Used by PAGESETUPDLG.rtMargin and rtMinMargin
        HundredthsOfAMillimeter = 2,

        /// <summary>
        /// One tenth of a millimeter (0.1 mm).
        /// </summary>
        // DeviceCapabilities(DC_PAPERSIZE)
        TenthsOfAMillimeter = 3,
    }
}

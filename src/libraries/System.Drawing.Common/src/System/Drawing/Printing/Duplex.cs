// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies the printer's duplex setting.
    /// </summary>
    public enum Duplex
    {
        /// <summary>
        /// The printer's default duplex setting.
        /// </summary>
        Default = -1,

        /// <summary>
        /// Single-sided printing.
        /// </summary>
        Simplex = SafeNativeMethods.DMDUP_SIMPLEX,

        /// <summary>
        /// Double-sided, horizontal printing.
        /// </summary>
        Horizontal = SafeNativeMethods.DMDUP_HORIZONTAL,

        /// <summary>
        /// Double-sided, vertical printing.
        /// </summary>
        Vertical = SafeNativeMethods.DMDUP_VERTICAL,
    }
}

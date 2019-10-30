// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies a printer resolution.
    /// </summary>
    public enum PrinterResolutionKind
    {
        /// <summary>
        /// High resolution.
        /// </summary>
        High = SafeNativeMethods.DMRES_HIGH,
        /// <summary>
        /// Medium resolution.
        /// </summary>
        Medium = SafeNativeMethods.DMRES_MEDIUM,
        /// <summary>
        /// Low resolution.
        /// </summary>
        Low = SafeNativeMethods.DMRES_LOW,
        /// <summary>
        /// Draft-quality resolution.
        /// </summary>
        Draft = SafeNativeMethods.DMRES_DRAFT,
        /// <summary>
        /// Custom resolution.
        /// </summary>
        Custom = 0,
    }
}

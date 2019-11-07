// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies available options for color-adjusting. GDI+ can adjust color data only, grayscale data only, or both.
    /// </summary>
    public enum ColorMatrixFlag
    {
        /// <summary>
        /// Both colors and grayscale are color-adjusted.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Grascale values are not color-adjusted.
        /// </summary>
        SkipGrays = 1,
        /// <summary>
        /// Only grascale values are color-adjusted.
        /// </summary>
        AltGrays = 2
    }
}

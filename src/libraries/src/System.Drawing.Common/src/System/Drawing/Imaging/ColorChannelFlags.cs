// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies a range of CMYK channels.
    /// </summary>
    public enum ColorChannelFlag
    {
        /// <summary>
        /// Specifies the Cyan color channel.
        /// </summary>
        ColorChannelC = 0,
        /// <summary>
        /// Specifies the Magenta color channel.
        /// </summary>
        ColorChannelM,
        /// <summary>
        /// Specifies the Yellow color channel.
        /// </summary>
        ColorChannelY,
        /// <summary>
        /// Specifies the Black color channel.
        /// </summary>
        ColorChannelK,
        /// <summary>
        /// This element specifies to leave the color channel unchanged from the last selected channel.
        /// </summary>
        ColorChannelLast
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * Color channel flag constants
     */
    /// <include file='doc\ColorChannelFlags.uex' path='docs/doc[@for="ColorChannelFlag"]/*' />
    /// <devdoc>
    ///    Specifies a range of CMYK channels.
    /// </devdoc>
    public enum ColorChannelFlag
    {
        /// <include file='doc\ColorChannelFlags.uex' path='docs/doc[@for="ColorChannelFlag.ColorChannelC"]/*' />
        /// <devdoc>
        ///    Specifies the Cyan color channel.
        /// </devdoc>
        ColorChannelC = 0,
        /// <include file='doc\ColorChannelFlags.uex' path='docs/doc[@for="ColorChannelFlag.ColorChannelM"]/*' />
        /// <devdoc>
        ///    Specifies the Magenta color channel.
        /// </devdoc>
        ColorChannelM,
        /// <include file='doc\ColorChannelFlags.uex' path='docs/doc[@for="ColorChannelFlag.ColorChannelY"]/*' />
        /// <devdoc>
        ///    Specifies the Yellow color channel.
        /// </devdoc>
        ColorChannelY,
        /// <include file='doc\ColorChannelFlags.uex' path='docs/doc[@for="ColorChannelFlag.ColorChannelK"]/*' />
        /// <devdoc>
        ///    Specifies the Black color channel.
        /// </devdoc>
        ColorChannelK,
        /// <include file='doc\ColorChannelFlags.uex' path='docs/doc[@for="ColorChannelFlag.ColorChannelLast"]/*' />
        /// <devdoc>
        ///    <para>
        ///       This element specifies to leave the color
        ///       channel unchanged from the last selected channel.
        ///    </para>
        /// </devdoc>
        ColorChannelLast
    }
}

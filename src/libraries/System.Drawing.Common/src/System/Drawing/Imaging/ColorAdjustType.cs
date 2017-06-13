// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * Color adjust type constants
     */
    /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType"]/*' />
    /// <devdoc>
    ///    Specifies which GDI+ objects use color
    ///    adjustment information.
    /// </devdoc>
    public enum ColorAdjustType
    {
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Default"]/*' />
        /// <devdoc>
        ///    Defines color adjustment information that is
        ///    used by all GDI+ objects that do not have their own color adjustment
        ///    information.
        /// </devdoc>
        Default = 0,
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Bitmap"]/*' />
        /// <devdoc>
        ///    Defines color adjustment information for
        /// <see cref='System.Drawing.Bitmap'/> 
        /// objects.
        /// </devdoc>
        Bitmap,
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Brush"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Defines color adjustment information for <see cref='System.Drawing.Brush'/> objects.
        ///    </para>
        /// </devdoc>
        Brush,
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Pen"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Defines color adjustment information for <see cref='System.Drawing.Pen'/> objects.
        ///    </para>
        /// </devdoc>
        Pen,
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Text"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Defines color adjustment information for text.
        ///    </para>
        /// </devdoc>
        Text,
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Count"]/*' />
        /// <devdoc>
        ///    Specifies the number of types specified.
        /// </devdoc>
        Count,
        /// <include file='doc\ColorAdjustType.uex' path='docs/doc[@for="ColorAdjustType.Any"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies the number of types specified.
        ///    </para>
        /// </devdoc>
        Any
    }
}


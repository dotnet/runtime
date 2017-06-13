// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <include file='doc\ColorMap.uex' path='docs/doc[@for="ColorMap"]/*' />
    /// <devdoc>
    ///    Defines a map for converting colors.
    /// </devdoc>
    public sealed class ColorMap
    {
        private Color _oldColor;
        private Color _newColor;

        /// <include file='doc\ColorMap.uex' path='docs/doc[@for="ColorMap.ColorMap"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.ColorMap'/> class.
        ///    </para>
        /// </devdoc>
        public ColorMap()
        {
            _oldColor = new Color();
            _newColor = new Color();
        }

        /// <include file='doc\ColorMap.uex' path='docs/doc[@for="ColorMap.OldColor"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies the existing <see cref='System.Drawing.Color'/> to be
        ///       converted.
        ///    </para>
        /// </devdoc>
        public Color OldColor
        {
            get { return _oldColor; }
            set { _oldColor = value; }
        }
        /// <include file='doc\ColorMap.uex' path='docs/doc[@for="ColorMap.NewColor"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifes the new <see cref='System.Drawing.Color'/> to which to convert.
        ///    </para>
        /// </devdoc>
        public Color NewColor
        {
            get { return _newColor; }
            set { _newColor = value; }
        }
    }
}

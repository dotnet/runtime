// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\ColorBlend.uex' path='docs/doc[@for="ColorBlend"]/*' />
    /// <devdoc>
    ///    Defines arrays of colors and positions used
    ///    for interpolating color blending in a gradient.
    /// </devdoc>
    public sealed class ColorBlend
    {
        private Color[] _colors;
        private float[] _positions;

        /// <include file='doc\ColorBlend.uex' path='docs/doc[@for="ColorBlend.ColorBlend"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.ColorBlend'/> class.
        /// </devdoc>
        public ColorBlend()
        {
            _colors = new Color[1];
            _positions = new float[1];
        }

        /// <include file='doc\ColorBlend.uex' path='docs/doc[@for="ColorBlend.ColorBlend1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.ColorBlend'/> class with the specified number of
        ///       colors and positions.
        ///    </para>
        /// </devdoc>
        public ColorBlend(int count)
        {
            _colors = new Color[count];
            _positions = new float[count];
        }

        /// <include file='doc\ColorBlend.uex' path='docs/doc[@for="ColorBlend.Colors"]/*' />
        /// <devdoc>
        ///    Represents an array of colors.
        /// </devdoc>
        public Color[] Colors
        {
            get
            {
                return _colors;
            }
            set
            {
                _colors = value;
            }
        }

        /// <include file='doc\ColorBlend.uex' path='docs/doc[@for="ColorBlend.Positions"]/*' />
        /// <devdoc>
        ///    Represents the positions along a gradient
        ///    line.
        /// </devdoc>
        public float[] Positions
        {
            get
            {
                return _positions;
            }
            set
            {
                _positions = value;
            }
        }
    }
}

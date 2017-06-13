// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\Blend.uex' path='docs/doc[@for="Blend"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Defines a blend pattern for a <see cref='System.Drawing.Drawing2D.LinearGradientBrush'/>
    ///       .
    ///    </para>
    /// </devdoc>
    public sealed class Blend
    {
        private float[] _factors;
        private float[] _positions;

        /// <include file='doc\Blend.uex' path='docs/doc[@for="Blend.Blend"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.Blend'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public Blend()
        {
            _factors = new float[1];
            _positions = new float[1];
        }

        /// <include file='doc\Blend.uex' path='docs/doc[@for="Blend.Blend1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.Blend'/>
        ///       class with the specified number of factors and positions.
        ///    </para>
        /// </devdoc>
        public Blend(int count)
        {
            _factors = new float[count];
            _positions = new float[count];
        }
        /// <include file='doc\Blend.uex' path='docs/doc[@for="Blend.Factors"]/*' />
        /// <devdoc>
        ///    Specifies an array of blend factors for the
        ///    gradient.
        /// </devdoc>
        public float[] Factors
        {
            get
            {
                return _factors;
            }
            set
            {
                _factors = value;
            }
        }

        /// <include file='doc\Blend.uex' path='docs/doc[@for="Blend.Positions"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies an array of blend positions for the gradient.
        ///    </para>
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

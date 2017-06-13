// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Represent the internal data of a path object
     */
    /// <include file='doc\PathData.uex' path='docs/doc[@for="PathData"]/*' />
    /// <devdoc>
    ///    Contains the graphical data that makes up a
    /// <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
    /// </devdoc>
    public sealed class PathData
    {
        private PointF[] _points;
        private byte[] _types;

        /// <include file='doc\PathData.uex' path='docs/doc[@for="PathData.PathData"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.PathData'/> class.
        /// </devdoc>
        public PathData()
        {
        }

        /// <include file='doc\PathData.uex' path='docs/doc[@for="PathData.Points"]/*' />
        /// <devdoc>
        ///    Contains an array of <see cref='System.Drawing.PointF'/> objects
        ///    that represent the points through which the path is constructed.
        /// </devdoc>
        public PointF[] Points
        {
            get
            {
                return _points;
            }
            set
            {
                _points = value;
            }
        }

        /// <include file='doc\PathData.uex' path='docs/doc[@for="PathData.Types"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Contains an array of <see cref='System.Drawing.Drawing2D.PathPointType'/> objects that represent the types of
        ///       data in the corresponding elements of the <see cref='System.Drawing.Drawing2D.PathData. _points'/> array.
        ///    </para>
        /// </devdoc>
        public byte[] Types
        {
            get
            {
                return _types;
            }
            set
            {
                _types = value;
            }
        }
    }
}

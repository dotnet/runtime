// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\RegionData.uex' path='docs/doc[@for="RegionData"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Encapsulates the data that makes up a <see cref='System.Drawing.Region'/>.
    ///    </para>
    /// </devdoc>
    public sealed class RegionData
    {
        private byte[] _data;

        internal RegionData(byte[] data)
        {
            _data = data;
        }

        /// <include file='doc\RegionData.uex' path='docs/doc[@for="RegionData.Data"]/*' />
        /// <devdoc>
        ///    <para>
        ///       An array of characters that contain the data that makes up a <see cref='System.Drawing.Region'/>.
        ///    </para>
        /// </devdoc>
        public byte[] Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }
    }
}

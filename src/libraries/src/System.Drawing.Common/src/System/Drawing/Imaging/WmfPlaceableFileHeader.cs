// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader"]/*' />
    /// <devdoc>
    ///    Defines an Placeable Metafile.
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class WmfPlaceableFileHeader
    {
        private int _key = unchecked((int)0x9aC6CDD7);
        private short _hmf;
        private short _bboxLeft;
        private short _bboxTop;
        private short _bboxRight;
        private short _bboxBottom;
        private short _inch;
        private int _reserved;
        private short _checksum;

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.Key"]/*' />
        /// <devdoc>
        ///    Indicates the presence of a placeable
        ///    metafile header.
        /// </devdoc>
        public int Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.Hmf"]/*' />
        /// <devdoc>
        ///    Stores the handle of the metafile in
        ///    memory.
        /// </devdoc>
        public short Hmf
        {
            get { return _hmf; }
            set { _hmf = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.BboxLeft"]/*' />
        /// <devdoc>
        ///    The x-coordinate of the upper-left corner
        ///    of the bounding rectangle of the metafile image on the output device.
        /// </devdoc>
        public short BboxLeft
        {
            get { return _bboxLeft; }
            set { _bboxLeft = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.BboxTop"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The y-coordinate of the upper-left corner of the bounding rectangle of the
        ///       metafile image on the output device.
        ///    </para>
        /// </devdoc>
        public short BboxTop
        {
            get { return _bboxTop; }
            set { _bboxTop = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.BboxRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The x-coordinate of the lower-right corner of the bounding rectangle of the
        ///       metafile image on the output device.
        ///    </para>
        /// </devdoc>
        public short BboxRight
        {
            get { return _bboxRight; }
            set { _bboxRight = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.BboxBottom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The y-coordinate of the lower-right corner of the bounding rectangle of the
        ///       metafile image on the output device.
        ///    </para>
        /// </devdoc>
        public short BboxBottom
        {
            get { return _bboxBottom; }
            set { _bboxBottom = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.Inch"]/*' />
        /// <devdoc>
        ///    Indicates the number of twips per inch.
        /// </devdoc>
        public short Inch
        {
            get { return _inch; }
            set { _inch = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.Reserved"]/*' />
        /// <devdoc>
        ///    Reserved. Do not use.
        /// </devdoc>
        public int Reserved
        {
            get { return _reserved; }
            set { _reserved = value; }
        }

        /// <include file='doc\WmfPlaceableFileHeader.uex' path='docs/doc[@for="WmfPlaceableFileHeader.Checksum"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Indicates the checksum value for the
        ///       previous ten WORDs in the header.
        ///    </para>
        /// </devdoc>
        public short Checksum
        {
            get { return _checksum; }
            set { _checksum = value; }
        }
    }
}

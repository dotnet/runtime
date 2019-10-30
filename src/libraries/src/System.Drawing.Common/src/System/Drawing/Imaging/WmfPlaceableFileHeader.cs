// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Defines an Placeable Metafile.
    /// </summary>
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

        /// <summary>
        /// Indicates the presence of a placeable metafile header.
        /// </summary>
        public int Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// Stores the handle of the metafile in memory.
        /// </summary>
        public short Hmf
        {
            get { return _hmf; }
            set { _hmf = value; }
        }

        /// <summary>
        /// The x-coordinate of the upper-left corner of the bounding rectangle of the metafile image on the output device.
        /// </summary>
        public short BboxLeft
        {
            get { return _bboxLeft; }
            set { _bboxLeft = value; }
        }

        /// <summary>
        /// The y-coordinate of the upper-left corner of the bounding rectangle of the metafile image on the output device.
        /// </summary>
        public short BboxTop
        {
            get { return _bboxTop; }
            set { _bboxTop = value; }
        }

        /// <summary>
        /// The x-coordinate of the lower-right corner of the bounding rectangle of the metafile image on the output device.
        /// </summary>
        public short BboxRight
        {
            get { return _bboxRight; }
            set { _bboxRight = value; }
        }

        /// <summary>
        /// The y-coordinate of the lower-right corner of the bounding rectangle of the metafile image on the output device.
        /// </summary>
        public short BboxBottom
        {
            get { return _bboxBottom; }
            set { _bboxBottom = value; }
        }

        /// <summary>
        /// Indicates the number of twips per inch.
        /// </summary>
        public short Inch
        {
            get { return _inch; }
            set { _inch = value; }
        }

        /// <summary>
        ///  Reserved. Do not use.
        /// </summary>
        public int Reserved
        {
            get { return _reserved; }
            set { _reserved = value; }
        }

        /// <summary>
        /// Indicates the checksum value for the previous ten WORDs in the header.
        /// </summary>
        public short Checksum
        {
            get { return _checksum; }
            set { _checksum = value; }
        }
    }
}

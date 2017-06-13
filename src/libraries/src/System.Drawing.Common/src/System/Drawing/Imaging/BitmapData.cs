// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData"]/*' />
    /// <devdoc>
    ///    Specifies the attributes of a bitmap image.
    /// </devdoc>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class BitmapData
    {
        private int _width;
        private int _height;
        private int _stride;
        private int _pixelFormat;
        private IntPtr _scan0;
        private int _reserved;

        /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData.Width"]/*' />
        /// <devdoc>
        ///    Specifies the pixel width of the <see cref='System.Drawing.Bitmap'/>.
        /// </devdoc>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData.Height"]/*' />
        /// <devdoc>
        ///    Specifies the pixel height of the <see cref='System.Drawing.Bitmap'/>.
        /// </devdoc>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData.Stride"]/*' />
        /// <devdoc>
        ///    Specifies the stride width of the <see cref='System.Drawing.Bitmap'/>.
        /// </devdoc>
        public int Stride
        {
            get { return _stride; }
            set { _stride = value; }
        }

        /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData.PixelFormat"]/*' />
        /// <devdoc>
        ///    Specifies the format of the pixel
        ///    information in this <see cref='System.Drawing.Bitmap'/>.
        /// </devdoc>
        public PixelFormat PixelFormat
        {
            get { return (PixelFormat)_pixelFormat; }
            set
            {
                switch (value)
                {
                    case PixelFormat.DontCare:
                    // case PixelFormat.Undefined: same as DontCare
                    case PixelFormat.Max:
                    case PixelFormat.Indexed:
                    case PixelFormat.Gdi:
                    case PixelFormat.Format16bppRgb555:
                    case PixelFormat.Format16bppRgb565:
                    case PixelFormat.Format24bppRgb:
                    case PixelFormat.Format32bppRgb:
                    case PixelFormat.Format1bppIndexed:
                    case PixelFormat.Format4bppIndexed:
                    case PixelFormat.Format8bppIndexed:
                    case PixelFormat.Alpha:
                    case PixelFormat.Format16bppArgb1555:
                    case PixelFormat.PAlpha:
                    case PixelFormat.Format32bppPArgb:
                    case PixelFormat.Extended:
                    case PixelFormat.Format16bppGrayScale:
                    case PixelFormat.Format48bppRgb:
                    case PixelFormat.Format64bppPArgb:
                    case PixelFormat.Canonical:
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format64bppArgb:
                        break;
                    default:
                        throw new System.ComponentModel.InvalidEnumArgumentException("value", unchecked((int)value), typeof(PixelFormat));
                }


                _pixelFormat = (int)value;
            }
        }

        /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData.Scan0"]/*' />
        /// <devdoc>
        ///    Specifies the address of the pixel data.
        /// </devdoc>
        public IntPtr Scan0
        {
            get { return _scan0; }
            set { _scan0 = value; }
        }

        /// <include file='doc\BitmapData.uex' path='docs/doc[@for="BitmapData.Reserved"]/*' />
        /// <devdoc>
        ///    Reserved. Do not use.
        /// </devdoc>
        public int Reserved
        {
            // why make public??
            //
            get { return _reserved; }
            set { _reserved = value; }
        }
    }
}

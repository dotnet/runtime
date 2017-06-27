// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the attributes of a bitmap image.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public sealed class BitmapData
    {
        private int _width;
        private int _height;
        private int _stride;
        private int _pixelFormat;
        private IntPtr _scan0;
        private int _reserved;

        /// <summary>
        /// Specifies the pixel width of the <see cref='Bitmap'/>.
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Specifies the pixel height of the <see cref='Bitmap'/>.
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// Specifies the stride width of the <see cref='Bitmap'/>.
        /// </summary>
        public int Stride
        {
            get { return _stride; }
            set { _stride = value; }
        }

        /// <summary>
        /// Specifies the format of the pixel information in this <see cref='Bitmap'/>.
        /// </summary>
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

        /// <summary>
        /// Specifies the address of the pixel data.
        /// </summary>
        public IntPtr Scan0
        {
            get { return _scan0; }
            set { _scan0 = value; }
        }

        /// <summary>
        /// Reserved. Do not use.
        /// </summary>
        public int Reserved
        {
            get { return _reserved; }
            set { _reserved = value; }
        }
    }
}

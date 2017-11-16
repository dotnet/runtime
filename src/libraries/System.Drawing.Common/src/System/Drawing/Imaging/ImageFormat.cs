// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the format of the image.
    /// </summary>
    public sealed class ImageFormat
    {
        // Format IDs
        // private static ImageFormat undefined = new ImageFormat(new Guid("{b96b3ca9-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_memoryBMP = new ImageFormat(new Guid("{b96b3caa-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_bmp = new ImageFormat(new Guid("{b96b3cab-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_emf = new ImageFormat(new Guid("{b96b3cac-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_wmf = new ImageFormat(new Guid("{b96b3cad-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_jpeg = new ImageFormat(new Guid("{b96b3cae-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_png = new ImageFormat(new Guid("{b96b3caf-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_gif = new ImageFormat(new Guid("{b96b3cb0-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_tiff = new ImageFormat(new Guid("{b96b3cb1-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_exif = new ImageFormat(new Guid("{b96b3cb2-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_photoCD = new ImageFormat(new Guid("{b96b3cb3-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_flashPIX = new ImageFormat(new Guid("{b96b3cb4-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat s_icon = new ImageFormat(new Guid("{b96b3cb5-0728-11d3-9d7b-0000f81ef32e}"));

        private Guid _guid;

        /// <summary>
        /// Initializes a new instance of the <see cref='ImageFormat'/> class with the specified GUID.
        /// </summary>
        public ImageFormat(Guid guid)
        {
            _guid = guid;
        }

        /// <summary>
        /// Specifies a global unique identifier (GUID) that represents this <see cref='ImageFormat'/>.
        /// </summary>
        public Guid Guid
        {
            get { return _guid; }
        }

        /// <summary>
        /// Specifies a memory bitmap image format.
        /// </summary>
        public static ImageFormat MemoryBmp
        {
            get { return s_memoryBMP; }
        }

        /// <summary>
        /// Specifies the bitmap image format.
        /// </summary>
        public static ImageFormat Bmp
        {
            get { return s_bmp; }
        }

        /// <summary>
        /// Specifies the enhanced Windows metafile image format.
        /// </summary>
        public static ImageFormat Emf
        {
            get { return s_emf; }
        }

        /// <summary>
        /// Specifies the Windows metafile image format.
        /// </summary>
        public static ImageFormat Wmf
        {
            get { return s_wmf; }
        }

        /// <summary>
        /// Specifies the GIF image format.
        /// </summary>
        public static ImageFormat Gif
        {
            get { return s_gif; }
        }

        /// <summary>
        /// Specifies the JPEG image format.
        /// </summary>
        public static ImageFormat Jpeg
        {
            get { return s_jpeg; }
        }

        /// <summary>
        /// Specifies the W3C PNG image format.
        /// </summary>
        public static ImageFormat Png
        {
            get { return s_png; }
        }

        /// <summary>
        /// Specifies the Tag Image File Format (TIFF) image format.
        /// </summary>
        public static ImageFormat Tiff
        {
            get { return s_tiff; }
        }

        /// <summary>
        /// Specifies the Exchangeable Image Format (EXIF).
        /// </summary>
        public static ImageFormat Exif
        {
            get { return s_exif; }
        }

        /// <summary>
        /// Specifies the Windows icon image format.
        /// </summary>
        public static ImageFormat Icon
        {
            get { return s_icon; }
        }

        /// <summary>
        /// Returns a value indicating whether the specified object is an <see cref='ImageFormat'/> equivalent to this
        /// <see cref='ImageFormat'/>.
        /// </summary>
        public override bool Equals(object o)
        {
            ImageFormat format = o as ImageFormat;
            if (format == null)
                return false;
            return _guid == format._guid;
        }

        /// <summary>
        /// Returns a hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

#if !FEATURE_PAL        
        // Find any random encoder which supports this format
        internal ImageCodecInfo FindEncoder()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID.Equals(_guid))
                    return codec;
            }
            return null;
        }
#endif

        /// <summary>
        /// Converts this <see cref='System.Drawing.Imaging.ImageFormat'/> to a human-readable string.
        /// </summary>
        public override string ToString()
        {
            if (this == s_memoryBMP) return "MemoryBMP";
            if (this == s_bmp) return "Bmp";
            if (this == s_emf) return "Emf";
            if (this == s_wmf) return "Wmf";
            if (this == s_gif) return "Gif";
            if (this == s_jpeg) return "Jpeg";
            if (this == s_png) return "Png";
            if (this == s_tiff) return "Tiff";
            if (this == s_exif) return "Exif";
            if (this == s_icon) return "Icon";
            return "[ImageFormat: " + _guid + "]";
        }
    }
}

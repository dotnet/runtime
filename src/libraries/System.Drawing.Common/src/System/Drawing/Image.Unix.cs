// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Image.cs
//
// Authors:     Christian Meyer (Christian.Meyer@cs.tum.edu)
//         Alexandre Pigolkine (pigolkine@gmx.de)
//        Jordi Mas i Hernandez (jordi@ximian.com)
//        Sanjay Gupta (gsanjay@novell.com)
//        Ravindra (rkumar@novell.com)
//        Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2002 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004, 2007 Novell, Inc (http://www.novell.com)
// Copyright (C) 2013 Kristof Ralovich, changes are available under the terms of the MIT X11 license
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace System.Drawing
{
    [Serializable]
#if !NETCORE
[Editor ("System.Drawing.Design.ImageEditor, " + Consts.AssemblySystem_Drawing_Design, typeof (System.Drawing.Design.UITypeEditor))]
[TypeConverter (typeof(ImageConverter))]
#endif
    public abstract partial class Image
    {
        public delegate bool GetThumbnailImageAbort();
        private object tag;

        internal IntPtr nativeImage = IntPtr.Zero;

        // constructor
        internal Image()
        {
        }

#if NETCORE
        protected Image(SerializationInfo info, StreamingContext context)
#else
    internal Image (SerializationInfo info, StreamingContext context)
#endif
        {
            foreach (SerializationEntry serEnum in info)
            {
                if (String.Compare(serEnum.Name, "Data", true) == 0)
                {
                    byte[] bytes = (byte[])serEnum.Value;

                    if (bytes != null)
                    {
                        MemoryStream ms = new MemoryStream(bytes);
                        nativeImage = InitFromStream(ms);
                    }
                }
            }
        }

        // FIXME - find out how metafiles (another decoder-only codec) are handled
        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Icon is a decoder-only codec
                if (RawFormat.Equals(ImageFormat.Icon))
                {
                    Save(ms, ImageFormat.Png);
                }
                else
                {
                    Save(ms, RawFormat);
                }
                si.AddValue("Data", ms.ToArray());
            }
        }

        // public methods
        // static
        public static Image FromFile(string filename)
        {
            return FromFile(filename, false);
        }

        public static Image FromFile(string filename, bool useEmbeddedColorManagement)
        {
            IntPtr imagePtr;
            int st;

            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);

            if (useEmbeddedColorManagement)
                st = SafeNativeMethods.Gdip.GdipLoadImageFromFileICM(filename, out imagePtr);
            else
                st = SafeNativeMethods.Gdip.GdipLoadImageFromFile(filename, out imagePtr);
            SafeNativeMethods.Gdip.CheckStatus(st);

            return CreateFromHandle(imagePtr);
        }

        public static Bitmap FromHbitmap(IntPtr hbitmap)
        {
            return FromHbitmap(hbitmap, IntPtr.Zero);
        }

        public static Bitmap FromHbitmap(IntPtr hbitmap, IntPtr hpalette)
        {
            IntPtr imagePtr;
            int st;

            st = SafeNativeMethods.Gdip.GdipCreateBitmapFromHBITMAP(new HandleRef(null, hbitmap), new HandleRef(null, hpalette), out imagePtr);

            SafeNativeMethods.Gdip.CheckStatus(st);
            return new Bitmap(imagePtr);
        }

        // note: FromStream can return either a Bitmap or Metafile instance

        public static Image FromStream(Stream stream)
        {
            return LoadFromStream(stream, false);
        }

        [MonoLimitation("useEmbeddedColorManagement  isn't supported.")]
        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement)
        {
            return LoadFromStream(stream, false);
        }

        // See http://support.microsoft.com/default.aspx?scid=kb;en-us;831419 for performance discussion    
        [MonoLimitation("useEmbeddedColorManagement  and validateImageData aren't supported.")]
        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement, bool validateImageData)
        {
            return LoadFromStream(stream, false);
        }

        internal static Image LoadFromStream(Stream stream, bool keepAlive)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            Image img = CreateFromHandle(InitFromStream(stream));
            return img;
        }

        internal static Image CreateImageObject(IntPtr nativeImage)
        {
            return CreateFromHandle(nativeImage);
        }

        internal static Image CreateFromHandle(IntPtr handle)
        {
            ImageType type;
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipGetImageType(handle, out type));
            switch (type)
            {
                case ImageType.Bitmap:
                    return new Bitmap(handle);
                case ImageType.Metafile:
                    return new Metafile(handle);
                default:
                    throw new NotSupportedException("Unknown image type.");
            }
        }

        public static int GetPixelFormatSize(PixelFormat pixfmt)
        {
            int result = 0;
            switch (pixfmt)
            {
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                    result = 16;
                    break;
                case PixelFormat.Format1bppIndexed:
                    result = 1;
                    break;
                case PixelFormat.Format24bppRgb:
                    result = 24;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    result = 32;
                    break;
                case PixelFormat.Format48bppRgb:
                    result = 48;
                    break;
                case PixelFormat.Format4bppIndexed:
                    result = 4;
                    break;
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    result = 64;
                    break;
                case PixelFormat.Format8bppIndexed:
                    result = 8;
                    break;
            }
            return result;
        }

        public static bool IsAlphaPixelFormat(PixelFormat pixfmt)
        {
            bool result = false;
            switch (pixfmt)
            {
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    result = true;
                    break;
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format1bppIndexed:
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format48bppRgb:
                case PixelFormat.Format4bppIndexed:
                case PixelFormat.Format8bppIndexed:
                    result = false;
                    break;
            }
            return result;
        }

        public static bool IsCanonicalPixelFormat(PixelFormat pixfmt)
        {
            return ((pixfmt & PixelFormat.Canonical) != 0);
        }

        public static bool IsExtendedPixelFormat(PixelFormat pixfmt)
        {
            return ((pixfmt & PixelFormat.Extended) != 0);
        }

        internal static IntPtr InitFromStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            IntPtr imagePtr;
            int st;

            // Seeking required
            if (!stream.CanSeek)
            {
                byte[] buffer = new byte[256];
                int index = 0;
                int count;

                do
                {
                    if (buffer.Length < index + 256)
                    {
                        byte[] newBuffer = new byte[buffer.Length * 2];
                        Array.Copy(buffer, newBuffer, buffer.Length);
                        buffer = newBuffer;
                    }
                    count = stream.Read(buffer, index, 256);
                    index += count;
                }
                while (count != 0);

                stream = new MemoryStream(buffer, 0, index);
            }

            // Unix, with libgdiplus
            // We use a custom API for this, because there's no easy way
            // to get the Stream down to libgdiplus.  So, we wrap the stream
            // with a set of delegates.
            GdiPlusStreamHelper sh = new GdiPlusStreamHelper(stream, true);

            st = SafeNativeMethods.Gdip.GdipLoadImageFromDelegate_linux(sh.GetHeaderDelegate, sh.GetBytesDelegate,
                sh.PutBytesDelegate, sh.SeekDelegate, sh.CloseDelegate, sh.SizeDelegate, out imagePtr);

            return st == SafeNativeMethods.Gdip.Ok ? imagePtr : IntPtr.Zero;
        }

        // non-static    
        public RectangleF GetBounds(ref GraphicsUnit pageUnit)
        {
            RectangleF source;

            int status = SafeNativeMethods.Gdip.GdipGetImageBounds(nativeImage, out source, ref pageUnit);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return source;
        }

        public EncoderParameters GetEncoderParameterList(Guid encoder)
        {
            int status;
            uint sz;

            status = SafeNativeMethods.Gdip.GdipGetEncoderParameterListSize(nativeImage, ref encoder, out sz);
            SafeNativeMethods.Gdip.CheckStatus(status);

            IntPtr rawEPList = Marshal.AllocHGlobal((int)sz);
            EncoderParameters eps;

            try
            {
                status = SafeNativeMethods.Gdip.GdipGetEncoderParameterList(nativeImage, ref encoder, sz, rawEPList);
                eps = EncoderParameters.ConvertFromMemory(rawEPList);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
            finally
            {
                Marshal.FreeHGlobal(rawEPList);
            }

            return eps;
        }

        public int GetFrameCount(FrameDimension dimension)
        {
            uint count;
            Guid guid = dimension.Guid;

            int status = SafeNativeMethods.Gdip.GdipImageGetFrameCount(nativeImage, ref guid, out count);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return (int)count;
        }

        public PropertyItem GetPropertyItem(int propid)
        {
            int propSize;
            IntPtr property;
            PropertyItem item = new PropertyItem();
            GdipPropertyItem gdipProperty = new GdipPropertyItem();
            int status;

            status = SafeNativeMethods.Gdip.GdipGetPropertyItemSize(nativeImage, propid,
                                        out propSize);
            SafeNativeMethods.Gdip.CheckStatus(status);

            /* Get PropertyItem */
            property = Marshal.AllocHGlobal(propSize);
            try
            {
                status = SafeNativeMethods.Gdip.GdipGetPropertyItem(nativeImage, propid, propSize, property);
                SafeNativeMethods.Gdip.CheckStatus(status);
                gdipProperty = (GdipPropertyItem)Marshal.PtrToStructure(property,
                                    typeof(GdipPropertyItem));
                GdipPropertyItem.MarshalTo(gdipProperty, item);
            }
            finally
            {
                Marshal.FreeHGlobal(property);
            }
            return item;
        }

        public Image GetThumbnailImage(int thumbWidth, int thumbHeight, Image.GetThumbnailImageAbort callback, IntPtr callbackData)
        {
            if ((thumbWidth <= 0) || (thumbHeight <= 0))
                throw new OutOfMemoryException("Invalid thumbnail size");

            Image ThumbNail = new Bitmap(thumbWidth, thumbHeight);

            using (Graphics g = Graphics.FromImage(ThumbNail))
            {
                int status = SafeNativeMethods.Gdip.GdipDrawImageRectRectI(g.nativeObject, nativeImage,
                    0, 0, thumbWidth, thumbHeight,
                    0, 0, this.Width, this.Height,
                    GraphicsUnit.Pixel, IntPtr.Zero, null, IntPtr.Zero);

                SafeNativeMethods.Gdip.CheckStatus(status);
            }

            return ThumbNail;
        }


        public void RemovePropertyItem(int propid)
        {
            int status = SafeNativeMethods.Gdip.GdipRemovePropertyItem(nativeImage, propid);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            int status = SafeNativeMethods.Gdip.GdipImageRotateFlip(nativeImage, rotateFlipType);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        internal ImageCodecInfo findEncoderForFormat(ImageFormat format)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo encoder = null;

            if (format.Guid.Equals(ImageFormat.MemoryBmp.Guid))
                format = ImageFormat.Png;

            /* Look for the right encoder for our format*/
            for (int i = 0; i < encoders.Length; i++)
            {
                if (encoders[i].FormatID.Equals(format.Guid))
                {
                    encoder = encoders[i];
                    break;
                }
            }

            return encoder;
        }

        public void Save(string filename)
        {
            Save(filename, RawFormat);
        }

        public void Save(string filename, ImageFormat format)
        {
            ImageCodecInfo encoder = findEncoderForFormat(format);
            if (encoder == null)
            {
                // second chance
                encoder = findEncoderForFormat(RawFormat);
                if (encoder == null)
                {
                    string msg = string.Format("No codec available for saving format '{0}'.", format.Guid);
                    throw new ArgumentException(msg, "format");
                }
            }
            Save(filename, encoder, null);
        }

        public void Save(string filename, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            int st;
            Guid guid = encoder.Clsid;

            if (encoderParams == null)
            {
                st = SafeNativeMethods.Gdip.GdipSaveImageToFile(nativeImage, filename, ref guid, IntPtr.Zero);
            }
            else
            {
                IntPtr nativeEncoderParams = encoderParams.ConvertToMemory();
                st = SafeNativeMethods.Gdip.GdipSaveImageToFile(nativeImage, filename, ref guid, nativeEncoderParams);
                Marshal.FreeHGlobal(nativeEncoderParams);
            }

            SafeNativeMethods.Gdip.CheckStatus(st);
        }

        public void Save(Stream stream, ImageFormat format)
        {
            ImageCodecInfo encoder = findEncoderForFormat(format);

            if (encoder == null)
                throw new ArgumentException("No codec available for format:" + format.Guid);

            Save(stream, encoder, null);
        }

        public void Save(Stream stream, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            int st;
            IntPtr nativeEncoderParams;
            Guid guid = encoder.Clsid;

            if (encoderParams == null)
                nativeEncoderParams = IntPtr.Zero;
            else
                nativeEncoderParams = encoderParams.ConvertToMemory();

            try
            {
                GdiPlusStreamHelper sh = new GdiPlusStreamHelper(stream, false);
                st = SafeNativeMethods.Gdip.GdipSaveImageToDelegate_linux(nativeImage, sh.GetBytesDelegate, sh.PutBytesDelegate,
                    sh.SeekDelegate, sh.CloseDelegate, sh.SizeDelegate, ref guid, nativeEncoderParams);
            }
            finally
            {
                if (nativeEncoderParams != IntPtr.Zero)
                    Marshal.FreeHGlobal(nativeEncoderParams);
            }

            SafeNativeMethods.Gdip.CheckStatus(st);
        }

        public void SaveAdd(EncoderParameters encoderParams)
        {
            int st;

            IntPtr nativeEncoderParams = encoderParams.ConvertToMemory();
            st = SafeNativeMethods.Gdip.GdipSaveAdd(nativeImage, nativeEncoderParams);
            Marshal.FreeHGlobal(nativeEncoderParams);
            SafeNativeMethods.Gdip.CheckStatus(st);
        }

        public void SaveAdd(Image image, EncoderParameters encoderParams)
        {
            int st;

            IntPtr nativeEncoderParams = encoderParams.ConvertToMemory();
            st = SafeNativeMethods.Gdip.GdipSaveAddImage(nativeImage, image.nativeImage, nativeEncoderParams);
            Marshal.FreeHGlobal(nativeEncoderParams);
            SafeNativeMethods.Gdip.CheckStatus(st);
        }

        public int SelectActiveFrame(FrameDimension dimension, int frameIndex)
        {
            Guid guid = dimension.Guid;
            int st = SafeNativeMethods.Gdip.GdipImageSelectActiveFrame(nativeImage, ref guid, frameIndex);

            SafeNativeMethods.Gdip.CheckStatus(st);

            return frameIndex;
        }

        public void SetPropertyItem(PropertyItem propitem)
        {
            if (propitem == null)
                throw new ArgumentNullException("propitem");

            int nItemSize = Marshal.SizeOf(propitem.Value[0]);
            int size = nItemSize * propitem.Value.Length;
            IntPtr dest = Marshal.AllocHGlobal(size);
            try
            {
                GdipPropertyItem pi = new GdipPropertyItem();
                pi.id = propitem.Id;
                pi.len = propitem.Len;
                pi.type = propitem.Type;

                Marshal.Copy(propitem.Value, 0, dest, size);
                pi.value = dest;

                unsafe
                {
                    int status = SafeNativeMethods.Gdip.GdipSetPropertyItem(nativeImage, &pi);

                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(dest);
            }
        }

        // properties    
        [Browsable(false)]
        public int Flags
        {
            get
            {
                int flags;

                int status = SafeNativeMethods.Gdip.GdipGetImageFlags(nativeImage, out flags);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return flags;
            }
        }

        [Browsable(false)]
        public Guid[] FrameDimensionsList
        {
            get
            {
                uint found;
                int status = SafeNativeMethods.Gdip.GdipImageGetFrameDimensionsCount(nativeImage, out found);
                SafeNativeMethods.Gdip.CheckStatus(status);
                Guid[] guid = new Guid[found];
                status = SafeNativeMethods.Gdip.GdipImageGetFrameDimensionsList(nativeImage, guid, found);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return guid;
            }
        }

        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Height
        {
            get
            {
                uint height;
                int status = SafeNativeMethods.Gdip.GdipGetImageHeight(nativeImage, out height);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (int)height;
            }
        }

        public float HorizontalResolution
        {
            get
            {
                float resolution;

                int status = SafeNativeMethods.Gdip.GdipGetImageHorizontalResolution(nativeImage, out resolution);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return resolution;
            }
        }

        [Browsable(false)]
        public ColorPalette Palette
        {
            get
            {
                return retrieveGDIPalette();
            }
            set
            {
                storeGDIPalette(value);
            }
        }

        internal ColorPalette retrieveGDIPalette()
        {
            int bytes;
            ColorPalette ret = new ColorPalette();

            int st = SafeNativeMethods.Gdip.GdipGetImagePaletteSize(nativeImage, out bytes);
            SafeNativeMethods.Gdip.CheckStatus(st);
            IntPtr palette_data = Marshal.AllocHGlobal(bytes);
            try
            {
                st = SafeNativeMethods.Gdip.GdipGetImagePalette(nativeImage, palette_data, bytes);
                SafeNativeMethods.Gdip.CheckStatus(st);
                ret.ConvertFromMemory(palette_data);
                return ret;
            }

            finally
            {
                Marshal.FreeHGlobal(palette_data);
            }
        }

        internal void storeGDIPalette(ColorPalette palette)
        {
            if (palette == null)
            {
                throw new ArgumentNullException("palette");
            }
            IntPtr palette_data = palette.ConvertToMemory();
            if (palette_data == IntPtr.Zero)
            {
                return;
            }

            try
            {
                int st = SafeNativeMethods.Gdip.GdipSetImagePalette(nativeImage, palette_data);
                SafeNativeMethods.Gdip.CheckStatus(st);
            }

            finally
            {
                Marshal.FreeHGlobal(palette_data);
            }
        }


        public SizeF PhysicalDimension
        {
            get
            {
                float width, height;
                int status = SafeNativeMethods.Gdip.GdipGetImageDimension(nativeImage, out width, out height);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return new SizeF(width, height);
            }
        }

        public PixelFormat PixelFormat
        {
            get
            {
                PixelFormat pixFormat;
                int status = SafeNativeMethods.Gdip.GdipGetImagePixelFormat(nativeImage, out pixFormat);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return pixFormat;
            }
        }

        [Browsable(false)]
        public int[] PropertyIdList
        {
            get
            {
                uint propNumbers;

                int status = SafeNativeMethods.Gdip.GdipGetPropertyCount(nativeImage,
                                        out propNumbers);
                SafeNativeMethods.Gdip.CheckStatus(status);

                int[] idList = new int[propNumbers];
                status = SafeNativeMethods.Gdip.GdipGetPropertyIdList(nativeImage,
                                    propNumbers, idList);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return idList;
            }
        }

        [Browsable(false)]
        public PropertyItem[] PropertyItems
        {
            get
            {
                int propNums, propsSize, propSize;
                IntPtr properties, propPtr;
                PropertyItem[] items;
                GdipPropertyItem gdipProperty = new GdipPropertyItem();
                int status;

                status = SafeNativeMethods.Gdip.GdipGetPropertySize(nativeImage, out propsSize, out propNums);
                SafeNativeMethods.Gdip.CheckStatus(status);

                items = new PropertyItem[propNums];

                if (propNums == 0)
                    return items;

                /* Get PropertyItem list*/
                properties = Marshal.AllocHGlobal(propsSize * propNums);
                try
                {
                    status = SafeNativeMethods.Gdip.GdipGetAllPropertyItems(nativeImage, propsSize,
                                    propNums, properties);
                    SafeNativeMethods.Gdip.CheckStatus(status);

                    propSize = Marshal.SizeOf(gdipProperty);
                    propPtr = properties;

                    for (int i = 0; i < propNums; i++, propPtr = new IntPtr(propPtr.ToInt64() + propSize))
                    {
                        gdipProperty = (GdipPropertyItem)Marshal.PtrToStructure
                            (propPtr, typeof(GdipPropertyItem));
                        items[i] = new PropertyItem();
                        GdipPropertyItem.MarshalTo(gdipProperty, items[i]);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(properties);
                }
                return items;
            }
        }

        public ImageFormat RawFormat
        {
            get
            {
                Guid guid;
                int st = SafeNativeMethods.Gdip.GdipGetImageRawFormat(nativeImage, out guid);

                SafeNativeMethods.Gdip.CheckStatus(st);
                return new ImageFormat(guid);
            }
        }

        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
        }

        [DefaultValue(null)]
        [LocalizableAttribute(false)]
#if !NETCORE
    [BindableAttribute(true)]     
    [TypeConverter (typeof (StringConverter))]
#endif
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }
        public float VerticalResolution
        {
            get
            {
                float resolution;

                int status = SafeNativeMethods.Gdip.GdipGetImageVerticalResolution(nativeImage, out resolution);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return resolution;
            }
        }

        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Width
        {
            get
            {
                uint width;
                int status = SafeNativeMethods.Gdip.GdipGetImageWidth(nativeImage, out width);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (int)width;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Image()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (nativeImage != IntPtr.Zero)
            {
                int status = SafeNativeMethods.Gdip.GdipDisposeImage(nativeImage);
                // ... set nativeImage to null before (possibly) throwing an exception
                nativeImage = IntPtr.Zero;
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public object Clone()
        {
            IntPtr newimage = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCloneImage(nativeImage, out newimage);
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (this is Bitmap)
                return new Bitmap(newimage);
            else
                return new Metafile(newimage);
        }
    }

}

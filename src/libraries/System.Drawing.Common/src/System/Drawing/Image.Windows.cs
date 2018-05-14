// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Drawing
{
    public abstract partial class Image
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        public static Image FromFile(string filename, bool useEmbeddedColorManagement)
        {
            if (!File.Exists(filename))
            {
                // Throw a more specific exception for invalid paths that are null or empty,
                // contain invalid characters or are too long.
                filename = Path.GetFullPath(filename);
                throw new FileNotFoundException(filename);
            }

            // GDI+ will read this file multiple times. Get the fully qualified path
            // so if our app changes default directory we won't get an error
            filename = Path.GetFullPath(filename);

            IntPtr image = IntPtr.Zero;
            int status;

            if (useEmbeddedColorManagement)
            {
                status = SafeNativeMethods.Gdip.GdipLoadImageFromFileICM(filename, out image);
            }
            else
            {
                status = SafeNativeMethods.Gdip.GdipLoadImageFromFile(filename, out image);
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
            ValidateImage(image);

            Image img = CreateImageObject(image);
            EnsureSave(img, filename, null);
            return img;
        }

        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement, bool validateImageData)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            IntPtr image = IntPtr.Zero;
            int status;

            if (useEmbeddedColorManagement)
            {
                status = SafeNativeMethods.Gdip.GdipLoadImageFromStreamICM(new GPStream(stream), out image);
            }
            else
            {
                status = SafeNativeMethods.Gdip.GdipLoadImageFromStream(new GPStream(stream), out image);
            }
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (validateImageData)
            {
                ValidateImage(image);
            }

            Image img = CreateImageObject(image);
            EnsureSave(img, null, stream);
            return img;
        }

        // Used for serialization
        private void InitializeFromStream(Stream stream)
        {
            IntPtr image = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipLoadImageFromStream(new GPStream(stream), out image);
            SafeNativeMethods.Gdip.CheckStatus(status);
            ValidateImage(image);

            nativeImage = image;

            int type = -1;

            status = SafeNativeMethods.Gdip.GdipGetImageType(new HandleRef(this, nativeImage), out type);
            SafeNativeMethods.Gdip.CheckStatus(status);
            EnsureSave(this, null, stream);
        }

        internal Image(IntPtr nativeImage) => SetNativeImage(nativeImage);

        /// <summary>
        /// Creates an exact copy of this <see cref='Image'/>.
        /// </summary>
        public object Clone()
        {
            IntPtr cloneImage = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneImage(new HandleRef(this, nativeImage), out cloneImage);
            SafeNativeMethods.Gdip.CheckStatus(status);
            ValidateImage(cloneImage);

            return CreateImageObject(cloneImage);
        }

        protected virtual void Dispose(bool disposing)
        {
#if FINALIZATION_WATCH
            if (!disposing && nativeImage != IntPtr.Zero)
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif
            if (nativeImage != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(this, nativeImage));
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif
                }
                catch (Exception ex)
                {
                    if (ClientUtils.IsSecurityOrCriticalException(ex))
                    {
                        throw;
                    }

                    Debug.Fail("Exception thrown during Dispose: " + ex.ToString());
                }
                finally
                {
                    nativeImage = IntPtr.Zero;
                }
            }
        }

        internal static Image CreateImageObject(IntPtr nativeImage)
        {
            Image image;

            int type = -1;

            int status = SafeNativeMethods.Gdip.GdipGetImageType(new HandleRef(null, nativeImage), out type);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            switch ((ImageType)type)
            {
                case ImageType.Bitmap:
                    image = new Bitmap(nativeImage);
                    break;

                case ImageType.Metafile:
                    image = Metafile.FromGDIplus(nativeImage);
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.InvalidImage));
            }

            return image;
        }

        /// <summary>
        /// Returns information about the codecs used for this <see cref='Image'/>.
        /// </summary>
        public EncoderParameters GetEncoderParameterList(Guid encoder)
        {
            EncoderParameters p;
            int size;

            int status = SafeNativeMethods.Gdip.GdipGetEncoderParameterListSize(new HandleRef(this, nativeImage),
                                                                 ref encoder,
                                                                 out size);
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (size <= 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                status = SafeNativeMethods.Gdip.GdipGetEncoderParameterList(new HandleRef(this, nativeImage),
                                                             ref encoder,
                                                             size,
                                                             buffer);
                SafeNativeMethods.Gdip.CheckStatus(status);

                p = EncoderParameters.ConvertFromMemory(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return p;
        }

        /// <summary>
        /// Saves this <see cref='Image'/> to the specified file in the specified format.
        /// </summary>
        public void Save(string filename, ImageFormat format)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            ImageCodecInfo codec = format.FindEncoder();

            if (codec == null)
                codec = ImageFormat.Png.FindEncoder();

            Save(filename, codec, null);
        }

        /// <summary>
        /// Saves this <see cref='Image'/> to the specified file in the specified format and with the specified encoder parameters.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void Save(string filename, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (encoder == null)
                throw new ArgumentNullException("encoder");

            IntPtr encoderParamsMemory = IntPtr.Zero;

            if (encoderParams != null)
            {
                _rawData = null;
                encoderParamsMemory = encoderParams.ConvertToMemory();
            }
            int status = SafeNativeMethods.Gdip.Ok;

            try
            {
                Guid g = encoder.Clsid;
                bool saved = false;

                if (_rawData != null)
                {
                    ImageCodecInfo rawEncoder = RawFormat.FindEncoder();
                    if (rawEncoder != null && rawEncoder.Clsid == g)
                    {
                        using (FileStream fs = File.OpenWrite(filename))
                        {
                            fs.Write(_rawData, 0, _rawData.Length);
                            saved = true;
                        }
                    }
                }

                if (!saved)
                {
                    status = SafeNativeMethods.Gdip.GdipSaveImageToFile(new HandleRef(this, nativeImage),
                                                             filename,
                                                             ref g,
                                                             new HandleRef(encoderParams, encoderParamsMemory));
                }
            }
            finally
            {
                if (encoderParamsMemory != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(encoderParamsMemory);
                }
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        internal void Save(MemoryStream stream)
        {
            // Jpeg loses data, so we don't want to use it to serialize...
            ImageFormat dest = RawFormat;
            if (dest == ImageFormat.Jpeg)
            {
                dest = ImageFormat.Png;
            }
            ImageCodecInfo codec = dest.FindEncoder();

            // If we don't find an Encoder (for things like Icon), we
            // just switch back to PNG...
            if (codec == null)
            {
                codec = ImageFormat.Png.FindEncoder();
            }
            Save(stream, codec, null);
        }

        /// <summary>
        /// Saves this <see cref='Image'/> to the specified stream in the specified format.
        /// </summary>
        public void Save(Stream stream, ImageFormat format)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            ImageCodecInfo codec = format.FindEncoder();
            Save(stream, codec, null);
        }

        /// <summary>
        /// Saves this <see cref='Image'/> to the specified stream in the specified format.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void Save(Stream stream, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoder == null)
            {
                throw new ArgumentNullException("encoder");
            }

            IntPtr encoderParamsMemory = IntPtr.Zero;

            if (encoderParams != null)
            {
                _rawData = null;
                encoderParamsMemory = encoderParams.ConvertToMemory();
            }

            int status = SafeNativeMethods.Gdip.Ok;

            try
            {
                Guid g = encoder.Clsid;
                bool saved = false;

                if (_rawData != null)
                {
                    ImageCodecInfo rawEncoder = RawFormat.FindEncoder();
                    if (rawEncoder != null && rawEncoder.Clsid == g)
                    {
                        stream.Write(_rawData, 0, _rawData.Length);
                        saved = true;
                    }
                }

                if (!saved)
                {
                    status = SafeNativeMethods.Gdip.GdipSaveImageToStream(new HandleRef(this, nativeImage),
                                                                     new UnsafeNativeMethods.ComStreamFromDataStream(stream),
                                                                     ref g,
                                                                     new HandleRef(encoderParams, encoderParamsMemory));
                }
            }
            finally
            {
                if (encoderParamsMemory != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(encoderParamsMemory);
                }
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Adds an <see cref='EncoderParameters'/> to this <see cref='Image'/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void SaveAdd(EncoderParameters encoderParams)
        {
            IntPtr encoder = IntPtr.Zero;
            if (encoderParams != null)
            {
                encoder = encoderParams.ConvertToMemory();
            }

            _rawData = null;
            int status = SafeNativeMethods.Gdip.GdipSaveAdd(new HandleRef(this, nativeImage), new HandleRef(encoderParams, encoder));

            if (encoder != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(encoder);
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Adds an <see cref='EncoderParameters'/> to the specified <see cref='Image'/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void SaveAdd(Image image, EncoderParameters encoderParams)
        {
            IntPtr encoder = IntPtr.Zero;

            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            if (encoderParams != null)
            {
                encoder = encoderParams.ConvertToMemory();
            }

            _rawData = null;
            int status = SafeNativeMethods.Gdip.GdipSaveAddImage(new HandleRef(this, nativeImage), new HandleRef(image, image.nativeImage), new HandleRef(encoderParams, encoder));

            if (encoder != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(encoder);
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Gets a bounding rectangle in the specified units for this <see cref='Image'/>.
        /// </summary>        
        public RectangleF GetBounds(ref GraphicsUnit pageUnit)
        {
            GPRECTF gprectf = new GPRECTF();

            int status = SafeNativeMethods.Gdip.GdipGetImageBounds(new HandleRef(this, nativeImage), ref gprectf, out pageUnit);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return gprectf.ToRectangleF();
        }

        /// <summary>
        /// Gets or sets the color palette used for this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public ColorPalette Palette
        {
            get
            {
                int size = -1;

                int status = SafeNativeMethods.Gdip.GdipGetImagePaletteSize(new HandleRef(this, nativeImage), out size);
                SafeNativeMethods.Gdip.CheckStatus(status);
                // "size" is total byte size:
                // sizeof(ColorPalette) + (pal->Count-1)*sizeof(ARGB)

                ColorPalette palette = new ColorPalette(size);

                // Memory layout is:
                //    UINT Flags
                //    UINT Count
                //    ARGB Entries[size]

                IntPtr memory = Marshal.AllocHGlobal(size);
                try
                {
                    status = SafeNativeMethods.Gdip.GdipGetImagePalette(new HandleRef(this, nativeImage), memory, size);
                    SafeNativeMethods.Gdip.CheckStatus(status);

                    palette.ConvertFromMemory(memory);
                }
                finally
                {
                    Marshal.FreeHGlobal(memory);
                }

                return palette;
            }
            set
            {
                IntPtr memory = value.ConvertToMemory();
                int status = SafeNativeMethods.Gdip.GdipSetImagePalette(new HandleRef(this, nativeImage), memory);

                if (memory != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(memory);
                }
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        // Thumbnail support

        /// <summary>
        /// Returns the thumbnail for this <see cref='Image'/>.
        /// </summary>
        public Image GetThumbnailImage(int thumbWidth, int thumbHeight,
                                       GetThumbnailImageAbort callback, IntPtr callbackData)
        {
            IntPtr thumbImage = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipGetImageThumbnail(new HandleRef(this, nativeImage), thumbWidth, thumbHeight, out thumbImage,
                                                       callback, callbackData);
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return CreateImageObject(thumbImage);
        }

        // Multi-frame support

        /// <summary>
        /// Gets an array of GUIDs that represent the dimensions of frames within this <see cref='Image'/>.
        /// </summary>        
        [Browsable(false)]
        public Guid[] FrameDimensionsList
        {
            get
            {
                int count;

                int status = SafeNativeMethods.Gdip.GdipImageGetFrameDimensionsCount(new HandleRef(this, nativeImage), out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                Debug.Assert(count >= 0, "FrameDimensionsList returns bad count");
                if (count <= 0)
                {
                    return Array.Empty<Guid>();
                }

                int size = (int)Marshal.SizeOf(typeof(Guid));

                IntPtr buffer = Marshal.AllocHGlobal(checked(size * count));
                if (buffer == IntPtr.Zero)
                {
                    throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.OutOfMemory);
                }

                status = SafeNativeMethods.Gdip.GdipImageGetFrameDimensionsList(new HandleRef(this, nativeImage), buffer, count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    Marshal.FreeHGlobal(buffer);
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                Guid[] guids = new Guid[count];

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        guids[i] = (Guid)Marshal.PtrToStructure((IntPtr)((long)buffer + size * i), typeof(Guid));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                return guids;
            }
        }

        /// <summary>
        /// Gets an array of the property IDs stored in this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public int[] PropertyIdList
        {
            get
            {
                int count;

                int status = SafeNativeMethods.Gdip.GdipGetPropertyCount(new HandleRef(this, nativeImage), out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                int[] propid = new int[count];

                //if we have a 0 count, just return our empty array
                if (count == 0)
                    return propid;

                status = SafeNativeMethods.Gdip.GdipGetPropertyIdList(new HandleRef(this, nativeImage), count, propid);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return propid;
            }
        }

        /// <summary>
        /// Gets the specified property item from this <see cref='Image'/>.
        /// </summary>
        public PropertyItem GetPropertyItem(int propid)
        {
            PropertyItem propitem;
            int size;

            int status = SafeNativeMethods.Gdip.GdipGetPropertyItemSize(new HandleRef(this, nativeImage), propid, out size);
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (size == 0)
                return null;

            IntPtr propdata = Marshal.AllocHGlobal(size);

            if (propdata == IntPtr.Zero)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.OutOfMemory);

            try
            {
                status = SafeNativeMethods.Gdip.GdipGetPropertyItem(new HandleRef(this, nativeImage), propid, size, propdata);
                SafeNativeMethods.Gdip.CheckStatus(status);

                propitem = PropertyItemInternal.ConvertFromMemory(propdata, 1)[0];
            }
            finally
            {
                Marshal.FreeHGlobal(propdata);
            }

            return propitem;
        }

        /// <summary>
        /// Sets the specified property item to the specified value.
        /// </summary>
        public void SetPropertyItem(PropertyItem propitem)
        {
            PropertyItemInternal propItemInternal = PropertyItemInternal.ConvertFromPropertyItem(propitem);

            using (propItemInternal)
            {
                int status = SafeNativeMethods.Gdip.GdipSetPropertyItem(new HandleRef(this, nativeImage), propItemInternal);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        /// <summary>
        /// Gets an array of <see cref='PropertyItem'/> objects that describe this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public PropertyItem[] PropertyItems
        {
            get
            {
                int size;
                int count;

                int status = SafeNativeMethods.Gdip.GdipGetPropertyCount(new HandleRef(this, nativeImage), out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                status = SafeNativeMethods.Gdip.GdipGetPropertySize(new HandleRef(this, nativeImage), out size, ref count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                if (size == 0 || count == 0)
                    return Array.Empty<PropertyItem>();

                IntPtr propdata = Marshal.AllocHGlobal(size);
                try
                {
                    status = SafeNativeMethods.Gdip.GdipGetAllPropertyItems(new HandleRef(this, nativeImage), size, count, propdata);
                    SafeNativeMethods.Gdip.CheckStatus(status);

                    return PropertyItemInternal.ConvertFromMemory(propdata, count);
                }
                finally
                {
                    Marshal.FreeHGlobal(propdata);
                }
            }
        }

        /// <summary>
        /// Returns the size of the specified pixel format.
        /// </summary>
        public static int GetPixelFormatSize(PixelFormat pixfmt)
        {
            return (unchecked((int)pixfmt) >> 8) & 0xFF;
        }

        /// <summary>
        /// Returns a value indicating whether the pixel format contains alpha information.
        /// </summary>
        public static bool IsAlphaPixelFormat(PixelFormat pixfmt)
        {
            return (pixfmt & PixelFormat.Alpha) != 0;
        }

        internal static void ValidateImage(IntPtr image)
        {
            int status = SafeNativeMethods.Gdip.GdipImageForceValidation(new HandleRef(null, image));
            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(null, image));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }
    }
}


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

        // The signature of this delegate is incorrect. The signature of the corresponding 
        // native callback function is:
        // extern "C" {
        //     typedef BOOL (CALLBACK * ImageAbort)(VOID *);
        //     typedef ImageAbort DrawImageAbort;
        //     typedef ImageAbort GetThumbnailImageAbort;
        // }
        // However, as this delegate is not used in both GDI 1.0 and 1.1, we choose not
        // to modify it, in order to preserve compatibility.
        public delegate bool GetThumbnailImageAbort();

        internal IntPtr nativeImage;

        //userData : so that user can use TAGS with IMAGES..
        private object _userData;

        internal Image()
        {
        }

        [
        Localizable(false),
        DefaultValue(null),
        ]
        public object Tag
        {
            get
            {
                return _userData;
            }
            set
            {
                _userData = value;
            }
        }

        /// <summary>
        /// Creates an <see cref='Image'/> from the specified file.
        /// </summary>
        public static Image FromFile(string filename) => FromFile(filename, false);

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

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            status = SafeNativeMethods.Gdip.GdipImageForceValidation(new HandleRef(null, image));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(null, image));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            Image img = CreateImageObject(image);

            EnsureSave(img, filename, null);

            return img;
        }


        /// <summary>
        /// Creates an <see cref='Image'/> from the specified data stream.
        /// </summary>
        public static Image FromStream(Stream stream)
        {
            return Image.FromStream(stream, false);
        }

        public static Image FromStream(Stream stream,
                                       bool useEmbeddedColorManagement)
        {
            return FromStream(stream, useEmbeddedColorManagement, true);
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

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            if (validateImageData)
            {
                status = SafeNativeMethods.Gdip.GdipImageForceValidation(new HandleRef(null, image));

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(null, image));
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }
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

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            status = SafeNativeMethods.Gdip.GdipImageForceValidation(new HandleRef(null, image));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(null, image));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            nativeImage = image;

            int type = -1;

            status = SafeNativeMethods.Gdip.GdipGetImageType(new HandleRef(this, nativeImage), out type);

            EnsureSave(this, null, stream);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        internal Image(IntPtr nativeImage)
        {
            SetNativeImage(nativeImage);
        }

        /// <summary>
        /// Creates an exact copy of this <see cref='Image'/>.
        /// </summary>
        public object Clone()
        {
            IntPtr cloneImage = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneImage(new HandleRef(this, nativeImage), out cloneImage);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            status = SafeNativeMethods.Gdip.GdipImageForceValidation(new HandleRef(null, cloneImage));

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDisposeImage(new HandleRef(null, cloneImage));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            return CreateImageObject(cloneImage);
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='Image'/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        /// <summary>
        /// Cleans up Windows resources for this <see cref='Image'/>.
        /// </summary>
        ~Image()
        {
            Dispose(false);
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
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            if (size <= 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                status = SafeNativeMethods.Gdip.GdipGetEncoderParameterList(new HandleRef(this, nativeImage),
                                                             ref encoder,
                                                             size,
                                                             buffer);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                p = EncoderParameters.ConvertFromMemory(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return p;
        }

        /// <summary>
        /// Saves this <see cref='Image'/> to the specified file.
        /// </summary>
        public void Save(string filename)
        {
            Save(filename, RawFormat);
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

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
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

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
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
            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
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
            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        private SizeF _GetPhysicalDimension()
        {
            float width;
            float height;

            int status = SafeNativeMethods.Gdip.GdipGetImageDimension(new HandleRef(this, nativeImage), out width, out height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new SizeF(width, height);
        }

        /// <summary>
        /// Gets the width and height of this <see cref='Image'/>.
        /// </summary>
        public SizeF PhysicalDimension
        {
            get { return _GetPhysicalDimension(); }
        }

        /// <summary>
        /// Gets the width and height of this <see cref='Image'/>.
        /// </summary>
        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
        }

        /// <summary>
        /// Gets the width of this <see cref='Image'/>.
        /// </summary>
        [
        DefaultValue(false),
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)
        ]
        public int Width
        {
            get
            {
                int width;

                int status = SafeNativeMethods.Gdip.GdipGetImageWidth(new HandleRef(this, nativeImage), out width);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return width;
            }
        }

        /// <summary>
        /// Gets the height of this <see cref='Image'/>.
        /// </summary>
        [
        DefaultValue(false),
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)
        ]
        public int Height
        {
            get
            {
                int height;

                int status = SafeNativeMethods.Gdip.GdipGetImageHeight(new HandleRef(this, nativeImage), out height);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return height;
            }
        }

        /// <summary>
        /// Gets the horizontal resolution, in pixels-per-inch, of this <see cref='Image'/>.
        /// </summary>
        public float HorizontalResolution
        {
            get
            {
                float horzRes;

                int status = SafeNativeMethods.Gdip.GdipGetImageHorizontalResolution(new HandleRef(this, nativeImage), out horzRes);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return horzRes;
            }
        }

        /// <summary>
        /// Gets the vertical resolution, in pixels-per-inch, of this <see cref='Image'/>.
        /// </summary>
        public float VerticalResolution
        {
            get
            {
                float vertRes;

                int status = SafeNativeMethods.Gdip.GdipGetImageVerticalResolution(new HandleRef(this, nativeImage), out vertRes);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return vertRes;
            }
        }

        /// <summary>
        /// Gets attribute flags for this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public int Flags
        {
            get
            {
                int flags;

                int status = SafeNativeMethods.Gdip.GdipGetImageFlags(new HandleRef(this, nativeImage), out flags);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return flags;
            }
        }

        /// <summary>
        /// Gets the format of this <see cref='Image'/>.
        /// </summary>
        public ImageFormat RawFormat
        {
            get
            {
                Guid guid = new Guid();

                int status = SafeNativeMethods.Gdip.GdipGetImageRawFormat(new HandleRef(this, nativeImage), ref guid);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);


                return new ImageFormat(guid);
            }
        }

        /// <summary>
        /// Gets the pixel format for this <see cref='Image'/>.
        /// </summary>
        public PixelFormat PixelFormat
        {
            get
            {
                int format;

                int status = SafeNativeMethods.Gdip.GdipGetImagePixelFormat(new HandleRef(this, nativeImage), out format);

                if (status != SafeNativeMethods.Gdip.Ok)
                    return PixelFormat.Undefined;
                else
                    return (PixelFormat)format;
            }
        }

        /// <summary>
        /// Gets a bounding rectangle in the specified units for this <see cref='Image'/>.
        /// </summary>        
        public RectangleF GetBounds(ref GraphicsUnit pageUnit)
        {
            GPRECTF gprectf = new GPRECTF();

            int status = SafeNativeMethods.Gdip.GdipGetImageBounds(new HandleRef(this, nativeImage), ref gprectf, out pageUnit);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return gprectf.ToRectangleF();
        }

        private ColorPalette _GetColorPalette()
        {
            int size = -1;

            int status = SafeNativeMethods.Gdip.GdipGetImagePaletteSize(new HandleRef(this, nativeImage), out size);
            // "size" is total byte size:
            // sizeof(ColorPalette) + (pal->Count-1)*sizeof(ARGB)

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            ColorPalette palette = new ColorPalette(size);

            // Memory layout is:
            //    UINT Flags
            //    UINT Count
            //    ARGB Entries[size]

            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                status = SafeNativeMethods.Gdip.GdipGetImagePalette(new HandleRef(this, nativeImage), memory, size);
                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                palette.ConvertFromMemory(memory);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return palette;
        }

        private void _SetColorPalette(ColorPalette palette)
        {
            IntPtr memory = palette.ConvertToMemory();

            int status = SafeNativeMethods.Gdip.GdipSetImagePalette(new HandleRef(this, nativeImage), memory);

            if (memory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(memory);
            }
            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <summary>
        /// Gets or sets the color palette used for this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public ColorPalette Palette
        {
            get
            {
                return _GetColorPalette();
            }
            set
            {
                _SetColorPalette(value);
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

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                Debug.Assert(count >= 0, "FrameDimensionsList returns bad count");
                if (count <= 0)
                {
                    return new Guid[0];
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
        /// Returns the number of frames of the given dimension.
        /// </summary>
        public int GetFrameCount(FrameDimension dimension)
        {
            int[] count = new int[] { 0 };

            Guid dimensionID = dimension.Guid;
            int status = SafeNativeMethods.Gdip.GdipImageGetFrameCount(new HandleRef(this, nativeImage), ref dimensionID, count);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return count[0];
        }

        /// <summary>
        /// Selects the frame specified by the given dimension and index.
        /// </summary>
        public int SelectActiveFrame(FrameDimension dimension, int frameIndex)
        {
            int[] count = new int[] { 0 };

            Guid dimensionID = dimension.Guid;
            int status = SafeNativeMethods.Gdip.GdipImageSelectActiveFrame(new HandleRef(this, nativeImage), ref dimensionID, frameIndex);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return count[0];
        }

        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            int status = SafeNativeMethods.Gdip.GdipImageRotateFlip(new HandleRef(this, nativeImage), unchecked((int)rotateFlipType));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
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

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                int[] propid = new int[count];

                //if we have a 0 count, just return our empty array
                if (count == 0)
                    return propid;

                status = SafeNativeMethods.Gdip.GdipGetPropertyIdList(new HandleRef(this, nativeImage), count, propid);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

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

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            if (size == 0)
                return null;

            IntPtr propdata = Marshal.AllocHGlobal(size);

            if (propdata == IntPtr.Zero)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.OutOfMemory);

            try
            {
                status = SafeNativeMethods.Gdip.GdipGetPropertyItem(new HandleRef(this, nativeImage), propid, size, propdata);
                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                propitem = PropertyItemInternal.ConvertFromMemory(propdata, 1)[0];
            }
            finally
            {
                Marshal.FreeHGlobal(propdata);
            }

            return propitem;
        }

        /// <summary>
        /// Removes the specified property item from this <see cref='Image'/>.
        /// </summary>
        public void RemovePropertyItem(int propid)
        {
            int status = SafeNativeMethods.Gdip.GdipRemovePropertyItem(new HandleRef(this, nativeImage), propid);
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
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
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
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

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                status = SafeNativeMethods.Gdip.GdipGetPropertySize(new HandleRef(this, nativeImage), out size, ref count);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                if (size == 0 || count == 0)
                    return new PropertyItem[0];

                IntPtr propdata = Marshal.AllocHGlobal(size);
                try
                {
                    status = SafeNativeMethods.Gdip.GdipGetAllPropertyItems(new HandleRef(this, nativeImage), size, count, propdata);
                    if (status != SafeNativeMethods.Gdip.Ok)
                    {
                        throw SafeNativeMethods.Gdip.StatusException(status);
                    }

                    return PropertyItemInternal.ConvertFromMemory(propdata, count);
                }
                finally
                {
                    Marshal.FreeHGlobal(propdata);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref='Bitmap'/> from a Windows handle.
        /// </summary>
        public static Bitmap FromHbitmap(IntPtr hbitmap)
        {
            return FromHbitmap(hbitmap, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a <see cref='Bitmap'/> from the specified Windows handle with the specified color palette.
        /// </summary>
        public static Bitmap FromHbitmap(IntPtr hbitmap, IntPtr hpalette)
        {
            IntPtr bitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromHBITMAP(new HandleRef(null, hbitmap), new HandleRef(null, hpalette), out bitmap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new Bitmap(bitmap);
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

        /// <summary>
        /// Returns a value indicating whether the pixel format is extended.
        /// </summary>
        public static bool IsExtendedPixelFormat(PixelFormat pixfmt)
        {
            return (pixfmt & PixelFormat.Extended) != 0;
        }

        /*
         * Determine if the pixel format is canonical format:
         *   PixelFormat32bppARGB
         *   PixelFormat32bppPARGB
         *   PixelFormat64bppARGB
         *   PixelFormat64bppPARGB
         */
        /// <summary>
        /// Returns a value indicating whether the pixel format is canonical.
        /// </summary>
        public static bool IsCanonicalPixelFormat(PixelFormat pixfmt)
        {
            return (pixfmt & PixelFormat.Canonical) != 0;
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Drawing
{
    /// <summary>
    /// An abstract base class that provides functionality for 'Bitmap', 'Icon', 'Cursor', and 'Metafile' descended classes.
    /// </summary>
    [ImmutableObject(true)]
    public abstract partial class Image : MarshalByRefObject, IDisposable, ICloneable, ISerializable
    {
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

        private object _userData;

        // used to work around lack of animated gif encoder... rarely set...
        private byte[] _rawData;

        [Localizable(false)]
        [DefaultValue(null)]
#if !NETCORE
        [BindableAttribute(true)]     
        [TypeConverter(typeof(StringConverter))]
#endif
        public object Tag
        {
            get => _userData;
            set => _userData = value;
        }

        internal Image() { }

        /// <summary>
        /// Creates an <see cref='Image'/> from the specified file.
        /// </summary>
        public static Image FromFile(string filename) => FromFile(filename, false);

        /// <summary>
        /// Creates an <see cref='Image'/> from the specified data stream.
        /// </summary>
        public static Image FromStream(Stream stream) => Image.FromStream(stream, false);

        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement) => FromStream(stream, useEmbeddedColorManagement, true);

        /// <summary>
        /// Cleans up Windows resources for this <see cref='Image'/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='Image'/>.
        /// </summary>
        ~Image() => Dispose(false);

        /// <summary>
        /// Saves this <see cref='Image'/> to the specified file.
        /// </summary>
        public void Save(string filename) => Save(filename, RawFormat);

        /// <summary>
        /// Gets the width and height of this <see cref='Image'/>.
        /// </summary>
        public SizeF PhysicalDimension
        {
            get
            {
                float width;
                float height;

                int status = SafeNativeMethods.Gdip.GdipGetImageDimension(new HandleRef(this, nativeImage), out width, out height);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return new SizeF(width, height);
            }
        }

        /// <summary>
        /// Gets the width and height of this <see cref='Image'/>.
        /// </summary>
        public Size Size => new Size(Width, Height);

        /// <summary>
        /// Gets the width of this <see cref='Image'/>.
        /// </summary>
        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Width
        {
            get
            {
                int width;

                int status = SafeNativeMethods.Gdip.GdipGetImageWidth(new HandleRef(this, nativeImage), out width);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return width;
            }
        }

        /// <summary>
        /// Gets the height of this <see cref='Image'/>.
        /// </summary>
        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Height
        {
            get
            {
                int height;

                int status = SafeNativeMethods.Gdip.GdipGetImageHeight(new HandleRef(this, nativeImage), out height);
                SafeNativeMethods.Gdip.CheckStatus(status);

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
                SafeNativeMethods.Gdip.CheckStatus(status);

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
                SafeNativeMethods.Gdip.CheckStatus(status);

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
                SafeNativeMethods.Gdip.CheckStatus(status);

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
                SafeNativeMethods.Gdip.CheckStatus(status);

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
        /// Returns the number of frames of the given dimension.
        /// </summary>
        public int GetFrameCount(FrameDimension dimension)
        {
            int[] count = new int[] { 0 };

            Guid dimensionID = dimension.Guid;
            int status = SafeNativeMethods.Gdip.GdipImageGetFrameCount(new HandleRef(this, nativeImage), ref dimensionID, count);
            SafeNativeMethods.Gdip.CheckStatus(status);

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
            SafeNativeMethods.Gdip.CheckStatus(status);

            return count[0];
        }

        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            int status = SafeNativeMethods.Gdip.GdipImageRotateFlip(new HandleRef(this, nativeImage), unchecked((int)rotateFlipType));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Removes the specified property item from this <see cref='Image'/>.
        /// </summary>
        public void RemovePropertyItem(int propid)
        {
            int status = SafeNativeMethods.Gdip.GdipRemovePropertyItem(new HandleRef(this, nativeImage), propid);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Creates a <see cref='Bitmap'/> from a Windows handle.
        /// </summary>
        public static Bitmap FromHbitmap(IntPtr hbitmap) => FromHbitmap(hbitmap, IntPtr.Zero);

        /// <summary>
        /// Creates a <see cref='Bitmap'/> from the specified Windows handle with the specified color palette.
        /// </summary>
        public static Bitmap FromHbitmap(IntPtr hbitmap, IntPtr hpalette)
        {
            IntPtr bitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromHBITMAP(new HandleRef(null, hbitmap), new HandleRef(null, hpalette), out bitmap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Bitmap(bitmap);
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

        internal void SetNativeImage(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Format(SR.NativeHandle0), nameof(handle));

            nativeImage = handle;
        }

        internal static void EnsureSave(Image image, string filename, Stream dataStream)
        {
            if (image.RawFormat.Equals(ImageFormat.Gif))
            {
                bool animatedGif = false;

                Guid[] dimensions = image.FrameDimensionsList;
                foreach (Guid guid in dimensions)
                {
                    FrameDimension dimension = new FrameDimension(guid);
                    if (dimension.Equals(FrameDimension.Time))
                    {
                        animatedGif = image.GetFrameCount(FrameDimension.Time) > 1;
                        break;
                    }
                }


                if (animatedGif)
                {
                    try
                    {
                        Stream created = null;
                        long lastPos = 0;
                        if (dataStream != null)
                        {
                            lastPos = dataStream.Position;
                            dataStream.Position = 0;
                        }

                        try
                        {
                            if (dataStream == null)
                            {
                                created = dataStream = File.OpenRead(filename);
                            }

                            image._rawData = new byte[(int)dataStream.Length];
                            dataStream.Read(image._rawData, 0, (int)dataStream.Length);
                        }
                        finally
                        {
                            if (created != null)
                            {
                                created.Close();
                            }
                            else
                            {
                                dataStream.Position = lastPos;
                            }
                        }
                    }
                    // possible exceptions for reading the filename
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                    // possible exceptions for setting/getting the position inside dataStream
                    catch (NotSupportedException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    // possible exception when reading stuff into dataStream
                    catch (ArgumentException)
                    {
                    }
                }
            }
        }
    }
}


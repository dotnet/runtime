// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    /// <summary>
    /// An abstract base class that provides functionality for 'Bitmap', 'Icon', 'Cursor', and 'Metafile' descended classes.
    /// </summary>
    [Editor("System.Drawing.Design.ImageEditor, System.Drawing.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ImmutableObject(true)]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [TypeConverter(typeof(ImageConverter))]
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

        private object? _userData;

        // used to work around lack of animated gif encoder... rarely set...
        private byte[]? _rawData;

        [Localizable(false)]
        [DefaultValue(null)]
        public object? Tag
        {
            get => _userData;
            set => _userData = value;
        }

        private protected Image() { }

#pragma warning disable CA2229 // Implement Serialization constructor
        private protected Image(SerializationInfo info, StreamingContext context)
#pragma warning restore CA2229
        {
            byte[] dat = (byte[])info.GetValue("Data", typeof(byte[]))!; // Do not rename (binary serialization)

            try
            {
                SetNativeImage(InitializeFromStream(new MemoryStream(dat)));
            }
            catch (ExternalException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (OutOfMemoryException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (NotImplementedException)
            {
            }
            catch (FileNotFoundException)
            {
            }
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Save(stream);
                si.AddValue("Data", stream.ToArray(), typeof(byte[])); // Do not rename (binary serialization)
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

            if (useEmbeddedColorManagement)
            {
                Gdip.CheckStatus(Gdip.GdipLoadImageFromFileICM(filename, out image));
            }
            else
            {
                Gdip.CheckStatus(Gdip.GdipLoadImageFromFile(filename, out image));
            }

            ValidateImage(image);

            Image img = CreateImageObject(image);
            EnsureSave(img, filename, null);
            return img;
        }

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

        private static void ThrowIfDirectoryDoesntExist(string filename)
        {
            var directoryPart = System.IO.Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directoryPart) && !System.IO.Directory.Exists(directoryPart))
            {
                throw new DirectoryNotFoundException(SR.Format(SR.TargetDirectoryDoesNotExist, directoryPart, filename));
            }
        }

        /// <summary>
        /// Gets the width and height of this <see cref='Image'/>.
        /// </summary>
        public SizeF PhysicalDimension
        {
            get
            {
                float width;
                float height;

                int status = Gdip.GdipGetImageDimension(new HandleRef(this, nativeImage), out width, out height);
                Gdip.CheckStatus(status);

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

                int status = Gdip.GdipGetImageWidth(new HandleRef(this, nativeImage), out width);
                Gdip.CheckStatus(status);

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

                int status = Gdip.GdipGetImageHeight(new HandleRef(this, nativeImage), out height);
                Gdip.CheckStatus(status);

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

                int status = Gdip.GdipGetImageHorizontalResolution(new HandleRef(this, nativeImage), out horzRes);
                Gdip.CheckStatus(status);

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

                int status = Gdip.GdipGetImageVerticalResolution(new HandleRef(this, nativeImage), out vertRes);
                Gdip.CheckStatus(status);

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

                int status = Gdip.GdipGetImageFlags(new HandleRef(this, nativeImage), out flags);
                Gdip.CheckStatus(status);

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
                Guid guid = default;

                int status = Gdip.GdipGetImageRawFormat(new HandleRef(this, nativeImage), ref guid);
                Gdip.CheckStatus(status);

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
                int status = Gdip.GdipGetImagePixelFormat(new HandleRef(this, nativeImage), out PixelFormat format);
                return (status != Gdip.Ok) ? PixelFormat.Undefined : format;
            }
        }

        /// <summary>
        /// Gets an array of the property IDs stored in this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public unsafe int[] PropertyIdList
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPropertyCount(new HandleRef(this, nativeImage), out uint count));
                if (count == 0)
                    return Array.Empty<int>();

                var propid = new int[count];
                fixed (int* pPropid = propid)
                {
                    Gdip.CheckStatus(Gdip.GdipGetPropertyIdList(new HandleRef(this, nativeImage), count, pPropid));
                }

                return propid;
            }
        }

        /// <summary>
        /// Gets an array of <see cref='PropertyItem'/> objects that describe this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public unsafe PropertyItem[] PropertyItems
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPropertySize(new HandleRef(this, nativeImage), out uint size, out uint count));

                if (size == 0 || count == 0)
                    return Array.Empty<PropertyItem>();

                var result = new PropertyItem[(int)count];
                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)size);
                fixed (byte *pBuffer = buffer)
                {
                    PropertyItemInternal* pPropData = (PropertyItemInternal*)pBuffer;
                    Gdip.CheckStatus(Gdip.GdipGetAllPropertyItems(new HandleRef(this, nativeImage), size, count, pPropData));

                    for (int i = 0; i < count; i++)
                    {
                        result[i] = new PropertyItem
                        {
                            Id = pPropData[i].id,
                            Len = pPropData[i].len,
                            Type = pPropData[i].type,
                            Value = pPropData[i].Value.ToArray()
                        };
                    }
                }

                ArrayPool<byte>.Shared.Return(buffer);
                return result;
            }
        }

        /// <summary>
        /// Returns the number of frames of the given dimension.
        /// </summary>
        public int GetFrameCount(FrameDimension dimension)
        {
            Guid dimensionID = dimension.Guid;
            Gdip.CheckStatus(Gdip.GdipImageGetFrameCount(new HandleRef(this, nativeImage), ref dimensionID, out int count));
            return count;
        }

        /// <summary>
        /// Gets the specified property item from this <see cref='Image'/>.
        /// </summary>
        public unsafe PropertyItem? GetPropertyItem(int propid)
        {
            Gdip.CheckStatus(Gdip.GdipGetPropertyItemSize(new HandleRef(this, nativeImage), propid, out uint size));

            if (size == 0)
                return null;

            PropertyItem result;
            byte[] buffer = ArrayPool<byte>.Shared.Rent((int)size);
            fixed (byte *pBuffer = buffer)
            {
                PropertyItemInternal* pPropData = (PropertyItemInternal*)pBuffer;
                Gdip.CheckStatus(Gdip.GdipGetPropertyItem(new HandleRef(this, nativeImage), propid, size, pPropData));

                result = new PropertyItem
                {
                    Id = pPropData->id,
                    Len = pPropData->len,
                    Type = pPropData->type,
                    Value = pPropData->Value.ToArray()
                };
            }

            ArrayPool<byte>.Shared.Return(buffer);
            return result;
        }

        /// <summary>
        /// Selects the frame specified by the given dimension and index.
        /// </summary>
        public int SelectActiveFrame(FrameDimension dimension, int frameIndex)
        {
            Guid dimensionID = dimension.Guid;
            Gdip.CheckStatus(Gdip.GdipImageSelectActiveFrame(new HandleRef(this, nativeImage), ref dimensionID, frameIndex));
            return 0;
        }

        /// <summary>
        /// Sets the specified property item to the specified value.
        /// </summary>
        public unsafe void SetPropertyItem(PropertyItem propitem)
        {
            fixed (byte *propItemValue = propitem.Value)
            {
                var propItemInternal = new PropertyItemInternal
                {
                    id = propitem.Id,
                    len = propitem.Len,
                    type = propitem.Type,
                    value = propItemValue
                };
                Gdip.CheckStatus(Gdip.GdipSetPropertyItem(new HandleRef(this, nativeImage), &propItemInternal));
            }
        }

        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            int status = Gdip.GdipImageRotateFlip(new HandleRef(this, nativeImage), unchecked((int)rotateFlipType));
            Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Removes the specified property item from this <see cref='Image'/>.
        /// </summary>
        public void RemovePropertyItem(int propid)
        {
            int status = Gdip.GdipRemovePropertyItem(new HandleRef(this, nativeImage), propid);
            Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Returns information about the codecs used for this <see cref='Image'/>.
        /// </summary>
        public EncoderParameters? GetEncoderParameterList(Guid encoder)
        {
            EncoderParameters p;

            Gdip.CheckStatus(Gdip.GdipGetEncoderParameterListSize(
                new HandleRef(this, nativeImage),
                ref encoder,
                out int size));

            if (size <= 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Gdip.CheckStatus(Gdip.GdipGetEncoderParameterList(
                    new HandleRef(this, nativeImage),
                    ref encoder,
                    size,
                    buffer));

                p = EncoderParameters.ConvertFromMemory(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return p;
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
            Gdip.CheckStatus(Gdip.GdipCreateBitmapFromHBITMAP(hbitmap, hpalette, out IntPtr bitmap));
            return new Bitmap(bitmap);
        }

        /// <summary>
        /// Returns a value indicating whether the pixel format is extended.
        /// </summary>
        public static bool IsExtendedPixelFormat(PixelFormat pixfmt)
        {
            return (pixfmt & PixelFormat.Extended) != 0;
        }

        /// <summary>
        /// Returns a value indicating whether the pixel format is canonical.
        /// </summary>
        public static bool IsCanonicalPixelFormat(PixelFormat pixfmt)
        {
            // Canonical formats:
            //
            //  PixelFormat32bppARGB
            //  PixelFormat32bppPARGB
            //  PixelFormat64bppARGB
            //  PixelFormat64bppPARGB

            return (pixfmt & PixelFormat.Canonical) != 0;
        }

        internal void SetNativeImage(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.NativeHandle0, nameof(handle));

            nativeImage = handle;
        }

        // Multi-frame support

        /// <summary>
        /// Gets an array of GUIDs that represent the dimensions of frames within this <see cref='Image'/>.
        /// </summary>
        [Browsable(false)]
        public unsafe Guid[] FrameDimensionsList
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipImageGetFrameDimensionsCount(new HandleRef(this, nativeImage), out int count));

                Debug.Assert(count >= 0, "FrameDimensionsList returns bad count");
                if (count <= 0)
                    return Array.Empty<Guid>();

                Guid[] guids = new Guid[count];
                fixed (Guid* g = guids)
                {
                    Gdip.CheckStatus(Gdip.GdipImageGetFrameDimensionsList(new HandleRef(this, nativeImage), g, count));
                }

                return guids;
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

        internal static Image CreateImageObject(IntPtr nativeImage)
        {
            Gdip.CheckStatus(Gdip.GdipGetImageType(nativeImage, out int type));
            switch ((ImageType)type)
            {
                case ImageType.Bitmap:
                    return new Bitmap(nativeImage);
                case ImageType.Metafile:
                    return new Metafile(nativeImage);
                default:
                    throw new ArgumentException(SR.InvalidImage);
            }
        }

        internal static unsafe void EnsureSave(Image image, string? filename, Stream? dataStream)
        {
            if (image.RawFormat.Equals(ImageFormat.Gif))
            {
                bool animatedGif = false;

                Gdip.CheckStatus(Gdip.GdipImageGetFrameDimensionsCount(new HandleRef(image, image.nativeImage), out int dimensions));
                if (dimensions <= 0)
                {
                    return;
                }

                Span<Guid> guids = dimensions < 16 ?
                    stackalloc Guid[dimensions] :
                    new Guid[dimensions];

                fixed (Guid* g = &MemoryMarshal.GetReference(guids))
                {
                    Gdip.CheckStatus(Gdip.GdipImageGetFrameDimensionsList(new HandleRef(image, image.nativeImage), g, dimensions));
                }

                Guid timeGuid = FrameDimension.Time.Guid;
                for (int i = 0; i < dimensions; i++)
                {
                    if (timeGuid == guids[i])
                    {
                        animatedGif = image.GetFrameCount(FrameDimension.Time) > 1;
                        break;
                    }
                }

                if (animatedGif)
                {
                    try
                    {
                        Stream? created = null;
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
                                created = dataStream = File.OpenRead(filename!);
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
                                dataStream!.Position = lastPos;
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

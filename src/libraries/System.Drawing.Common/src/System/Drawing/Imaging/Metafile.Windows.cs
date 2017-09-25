// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Internal;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Defines a graphic metafile. A metafile contains records that describe a sequence of graphics operations that
    /// can be recorded and played back.
    /// </summary>
    public sealed partial class Metafile : Image
    {
        // GDI+ doesn't handle filenames over MAX_PATH very well
        private const int MaxPath = 260;

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle and
        /// <see cref='WmfPlaceableFileHeader'/>.
        /// </summary>
        public Metafile(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader) :
        this(hmetafile, wmfHeader, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle and
        /// <see cref='WmfPlaceableFileHeader'/>.
        /// </summary>
        public Metafile(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader, bool deleteWmf)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromWmf(new HandleRef(null, hmetafile), deleteWmf, wmfHeader, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle and
        /// <see cref='WmfPlaceableFileHeader'/>.
        /// </summary>
        public Metafile(IntPtr henhmetafile, bool deleteEmf)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromEmf(new HandleRef(null, henhmetafile), deleteEmf, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified filename.
        /// </summary>
        public Metafile(string filename)
        {
            // Called in order to emulate exception behavior from netfx related to invalid file paths.
            Path.GetFullPath(filename);

            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromFile(filename, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified stream.
        /// </summary>
        public Metafile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromStream(new GPStream(stream), out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle to a device context.
        /// </summary>
        public Metafile(IntPtr referenceHdc, EmfType emfType) :
        this(referenceHdc, emfType, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle to a device context.
        /// </summary>
        public Metafile(IntPtr referenceHdc, EmfType emfType, String description)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipRecordMetafile(new HandleRef(null, referenceHdc),
                                                    unchecked((int)emfType),
                                                    NativeMethods.NullHandleRef,
                                                    unchecked((int)MetafileFrameUnit.GdiCompatible),
                                                    description,
                                                    out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect) :
        this(referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit) :
        this(referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, EmfType type) :
        this(referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, EmfType type, String description)
        {
            IntPtr metafile = IntPtr.Zero;

            GPRECTF rectf = new GPRECTF(frameRect);
            int status = SafeNativeMethods.Gdip.GdipRecordMetafile(new HandleRef(null, referenceHdc),
                                                    unchecked((int)type),
                                                    ref rectf,
                                                    unchecked((int)frameUnit),
                                                    description, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect) :
        this(referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit) :
        this(referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type) :
        this(referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string desc)
        {
            IntPtr metafile = IntPtr.Zero;

            int status;

            if (frameRect.IsEmpty)
            {
                status = SafeNativeMethods.Gdip.GdipRecordMetafile(new HandleRef(null, referenceHdc),
                                                    unchecked((int)type),
                                                    NativeMethods.NullHandleRef,
                                                    unchecked((int)MetafileFrameUnit.GdiCompatible),
                                                    desc,
                                                    out metafile);
            }
            else
            {
                GPRECT gprect = new GPRECT(frameRect);
                status = SafeNativeMethods.Gdip.GdipRecordMetafileI(new HandleRef(null, referenceHdc),
                                                     unchecked((int)type),
                                                     ref gprect,
                                                     unchecked((int)frameUnit),
                                                     desc,
                                                     out metafile);
            }

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc) :
        this(fileName, referenceHdc, EmfType.EmfPlusDual, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, EmfType type) :
        this(fileName, referenceHdc, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, EmfType type, String description)
        {
            // Called in order to emulate exception behavior from netfx related to invalid file paths.
            Path.GetFullPath(fileName);

            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipRecordMetafileFileName(fileName, new HandleRef(null, referenceHdc),
                                                            unchecked((int)type),
                                                            NativeMethods.NullHandleRef,
                                                            unchecked((int)MetafileFrameUnit.GdiCompatible),
                                                            description,
                                                            out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect) :
        this(fileName, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(fileName, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, string desc) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual, desc)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type, String description)
        {
            // Called in order to emulate exception behavior from netfx related to invalid file paths.
            Path.GetFullPath(fileName);

            if (fileName.Length > MaxPath)
            {
                throw new PathTooLongException();
            }

            IntPtr metafile = IntPtr.Zero;

            GPRECTF rectf = new GPRECTF(frameRect);
            int status = SafeNativeMethods.Gdip.GdipRecordMetafileFileName(fileName,
                                                            new HandleRef(null, referenceHdc),
                                                            unchecked((int)type),
                                                            ref rectf,
                                                            unchecked((int)frameUnit),
                                                            description,
                                                            out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect) :
        this(fileName, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(fileName, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, string description) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual, description)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string description)
        {
            // Called in order to emulate exception behavior from netfx related to invalid file paths.
            Path.GetFullPath(fileName);

            IntPtr metafile = IntPtr.Zero;

            int status;

            if (frameRect.IsEmpty)
            {
                status = SafeNativeMethods.Gdip.GdipRecordMetafileFileName(fileName,
                                                            new HandleRef(null, referenceHdc),
                                                            unchecked((int)type),
                                                            NativeMethods.NullHandleRef,
                                                            unchecked((int)frameUnit),
                                                            description,
                                                            out metafile);
            }
            else
            {
                GPRECT gprect = new GPRECT(frameRect);
                status = SafeNativeMethods.Gdip.GdipRecordMetafileFileNameI(fileName,
                                                             new HandleRef(null, referenceHdc),
                                                             unchecked((int)type),
                                                             ref gprect,
                                                             unchecked((int)frameUnit),
                                                             description,
                                                             out metafile);
            }

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified data stream.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc) :
        this(stream, referenceHdc, EmfType.EmfPlusDual, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified data stream.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, EmfType type) :
        this(stream, referenceHdc, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified data stream.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, EmfType type, string description)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipRecordMetafileStream(new GPStream(stream),
                                                          new HandleRef(null, referenceHdc),
                                                          unchecked((int)type),
                                                          NativeMethods.NullHandleRef,
                                                          unchecked((int)MetafileFrameUnit.GdiCompatible),
                                                          description,
                                                          out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified data stream.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect) :
        this(stream, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit) :
        this(stream, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(stream, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type, string description)
        {
            IntPtr metafile = IntPtr.Zero;

            GPRECTF rectf = new GPRECTF(frameRect);
            int status = SafeNativeMethods.Gdip.GdipRecordMetafileStream(new GPStream(stream),
                                                          new HandleRef(null, referenceHdc),
                                                          unchecked((int)type),
                                                          ref rectf,
                                                          unchecked((int)frameUnit),
                                                          description,
                                                          out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified data stream.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect) :
        this(stream, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit) :
        this(stream, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(stream, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit,
                        EmfType type, string description)
        {
            IntPtr metafile = IntPtr.Zero;

            int status;

            if (frameRect.IsEmpty)
            {
                status = SafeNativeMethods.Gdip.GdipRecordMetafileStream(new GPStream(stream),
                                                          new HandleRef(null, referenceHdc),
                                                          unchecked((int)type),
                                                          NativeMethods.NullHandleRef,
                                                          unchecked((int)frameUnit),
                                                          description,
                                                          out metafile);
            }
            else
            {
                GPRECT gprect = new GPRECT(frameRect);
                status = SafeNativeMethods.Gdip.GdipRecordMetafileStreamI(new GPStream(stream),
                                                           new HandleRef(null, referenceHdc),
                                                           unchecked((int)type),
                                                           ref gprect,
                                                           unchecked((int)frameUnit),
                                                           description,
                                                           out metafile);
            }

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader)
        {
            MetafileHeader header = new MetafileHeader();

            header.wmf = new MetafileHeaderWmf();

            int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromWmf(new HandleRef(null, hmetafile), wmfHeader, header.wmf);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return header;
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(IntPtr henhmetafile)
        {
            MetafileHeader header = new MetafileHeader();
            header.emf = new MetafileHeaderEmf();

            int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromEmf(new HandleRef(null, henhmetafile), header.emf);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return header;
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(string fileName)
        {
            // Called in order to emulate exception behavior from netfx related to invalid file paths.
            Path.GetFullPath(fileName);

            MetafileHeader header = new MetafileHeader();

            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MetafileHeaderEmf)));

            try
            {
                int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromFile(fileName, memory);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                int[] type = new int[] { 0 };

                Marshal.Copy(memory, type, 0, 1);

                MetafileType metafileType = (MetafileType)type[0];

                if (metafileType == MetafileType.Wmf ||
                    metafileType == MetafileType.WmfPlaceable)
                {
                    // WMF header
                    header.wmf = (MetafileHeaderWmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderWmf));
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderEmf));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return header;
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(Stream stream)
        {
            MetafileHeader header;

            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MetafileHeaderEmf)));

            try
            {
                int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromStream(new GPStream(stream), memory);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                int[] type = new int[] { 0 };

                Marshal.Copy(memory, type, 0, 1);

                MetafileType metafileType = (MetafileType)type[0];

                header = new MetafileHeader();

                if (metafileType == MetafileType.Wmf ||
                    metafileType == MetafileType.WmfPlaceable)
                {
                    // WMF header
                    header.wmf = (MetafileHeaderWmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderWmf));
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderEmf));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return header;
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with this <see cref='Metafile'/>.
        /// </summary>
        public MetafileHeader GetMetafileHeader()
        {
            MetafileHeader header;

            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MetafileHeaderEmf)));

            try
            {
                int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromMetafile(new HandleRef(this, nativeImage), memory);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                int[] type = new int[] { 0 };

                Marshal.Copy(memory, type, 0, 1);

                MetafileType metafileType = (MetafileType)type[0];

                header = new MetafileHeader();

                if (metafileType == MetafileType.Wmf ||
                    metafileType == MetafileType.WmfPlaceable)
                {
                    // WMF header
                    header.wmf = (MetafileHeaderWmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderWmf));
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderEmf));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return header;
        }

        /// <summary>
        /// Returns a Windows handle to an enhanced <see cref='Metafile'/>.
        /// </summary>
        public IntPtr GetHenhmetafile()
        {
            IntPtr hEmf = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipGetHemfFromMetafile(new HandleRef(this, nativeImage), out hEmf);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return hEmf;
        }

        /// <summary>
        /// Plays an EMF+ file.
        /// </summary>
        public void PlayRecord(EmfPlusRecordType recordType,
                               int flags,
                               int dataSize,
                               byte[] data)
        {
            // Used in conjunction with Graphics.EnumerateMetafile to play an EMF+
            // The data must be DWORD aligned if it's an EMF or EMF+.  It must be
            // WORD aligned if it's a WMF.

            int status = SafeNativeMethods.Gdip.GdipPlayMetafileRecord(new HandleRef(this, nativeImage),
                                                        recordType,
                                                        flags,
                                                        dataSize,
                                                        data);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /*
         * Create a new metafile object from a native metafile handle.
         * This is only for internal purpose.
         */
        internal static Metafile FromGDIplus(IntPtr nativeImage)
        {
            Metafile metafile = new Metafile();
            metafile.SetNativeImage(nativeImage);
            return metafile;
        }

        private Metafile()
        {
        }
    }
}

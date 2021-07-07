// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.IO;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Defines a graphic metafile. A metafile contains records that describe a sequence of graphics operations that
    /// can be recorded and played back.
    /// </summary>
    public sealed partial class Metafile : Image
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle and
        /// <see cref='WmfPlaceableFileHeader'/>.
        /// </summary>
        public Metafile(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader) :
            this(hmetafile, wmfHeader, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified stream.
        /// </summary>
        public unsafe Metafile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using DrawingCom.IStreamWrapper streamWrapper = DrawingCom.GetComWrapper(new GPStream(stream));

            IntPtr metafile = IntPtr.Zero;
            Gdip.CheckStatus(Gdip.GdipCreateMetafileFromStream(streamWrapper.Ptr, &metafile));

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified handle to a device context.
        /// </summary>
        public Metafile(IntPtr referenceHdc, EmfType emfType, string? description)
        {
            Gdip.CheckStatus(Gdip.GdipRecordMetafile(
                referenceHdc,
                emfType,
                IntPtr.Zero,
                MetafileFrameUnit.GdiCompatible,
                description,
                out IntPtr metafile));

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified device context, bounded
        /// by the specified rectangle.
        /// </summary>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string? desc)
        {
            IntPtr metafile = IntPtr.Zero;

            if (frameRect.IsEmpty)
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafile(
                    referenceHdc,
                    type,
                    IntPtr.Zero,
                    MetafileFrameUnit.GdiCompatible,
                    desc,
                    out metafile));
            }
            else
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileI(
                    referenceHdc,
                    type,
                    ref frameRect,
                    frameUnit,
                    desc,
                    out metafile));
            }

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, EmfType type, string? description)
        {
            // Called in order to emulate exception behavior from .NET Framework related to invalid file paths.
            Path.GetFullPath(fileName);

            Gdip.CheckStatus(Gdip.GdipRecordMetafileFileName(
                fileName,
                referenceHdc,
                type,
                IntPtr.Zero,
                MetafileFrameUnit.GdiCompatible,
                description,
                out IntPtr metafile));

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            // Called in order to emulate exception behavior from .NET Framework related to invalid file paths.
            Path.GetFullPath(fileName);

            IntPtr metafile = IntPtr.Zero;

            if (frameRect.IsEmpty)
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileFileName(
                    fileName,
                    referenceHdc,
                    type,
                    IntPtr.Zero,
                    frameUnit,
                    description,
                    out metafile));
            }
            else
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileFileNameI(
                    fileName,
                    referenceHdc,
                    type,
                    ref frameRect,
                    frameUnit,
                    description,
                    out metafile));
            }

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class from the specified data stream.
        /// </summary>
        public unsafe Metafile(Stream stream, IntPtr referenceHdc, EmfType type, string? description)
        {
            using DrawingCom.IStreamWrapper streamWrapper = DrawingCom.GetComWrapper(new GPStream(stream));

            IntPtr metafile = IntPtr.Zero;
            Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                streamWrapper.Ptr,
                referenceHdc,
                type,
                IntPtr.Zero,
                MetafileFrameUnit.GdiCompatible,
                description,
                &metafile));

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public unsafe Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            using DrawingCom.IStreamWrapper streamWrapper = DrawingCom.GetComWrapper(new GPStream(stream));

            IntPtr metafile = IntPtr.Zero;
            Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                streamWrapper.Ptr,
                referenceHdc,
                type,
                &frameRect,
                frameUnit,
                description,
                &metafile));

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='Metafile'/> class with the specified filename.
        /// </summary>
        public unsafe Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string? description)
        {
            using DrawingCom.IStreamWrapper streamWrapper = DrawingCom.GetComWrapper(new GPStream(stream));

            IntPtr metafile = IntPtr.Zero;
            if (frameRect.IsEmpty)
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileStream(
                    streamWrapper.Ptr,
                    referenceHdc,
                    type,
                    IntPtr.Zero,
                    frameUnit,
                    description,
                    &metafile));
            }
            else
            {
                Gdip.CheckStatus(Gdip.GdipRecordMetafileStreamI(
                    streamWrapper.Ptr,
                    referenceHdc,
                    type,
                    &frameRect,
                    frameUnit,
                    description,
                    &metafile));
            }

            SetNativeImage(metafile);
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader)
        {
            MetafileHeader header = new MetafileHeader
            {
                wmf = new MetafileHeaderWmf()
            };

            Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromWmf(hmetafile, wmfHeader, header.wmf));
            return header;
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(IntPtr henhmetafile)
        {
            MetafileHeader header = new MetafileHeader
            {
                emf = new MetafileHeaderEmf()
            };

            Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromEmf(henhmetafile, header.emf));
            return header;
        }

        /// <summary>
        /// Returns the <see cref='MetafileHeader'/> associated with the specified <see cref='Metafile'/>.
        /// </summary>
        public static MetafileHeader GetMetafileHeader(string fileName)
        {
            // Called in order to emulate exception behavior from .NET Framework related to invalid file paths.
            Path.GetFullPath(fileName);

            MetafileHeader header = new MetafileHeader();

            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MetafileHeaderEmf)));

            try
            {
                Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromFile(fileName, memory));

                int[] type = new int[] { 0 };

                Marshal.Copy(memory, type, 0, 1);

                MetafileType metafileType = (MetafileType)type[0];

                if (metafileType == MetafileType.Wmf ||
                    metafileType == MetafileType.WmfPlaceable)
                {
                    // WMF header
                    header.wmf = (MetafileHeaderWmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderWmf))!;
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderEmf))!;
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
                using DrawingCom.IStreamWrapper streamWrapper = DrawingCom.GetComWrapper(new GPStream(stream));
                Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromStream(streamWrapper.Ptr, memory));

                int[] type = new int[] { 0 };

                Marshal.Copy(memory, type, 0, 1);

                MetafileType metafileType = (MetafileType)type[0];

                header = new MetafileHeader();

                if (metafileType == MetafileType.Wmf ||
                    metafileType == MetafileType.WmfPlaceable)
                {
                    // WMF header
                    header.wmf = (MetafileHeaderWmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderWmf))!;
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderEmf))!;
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
                Gdip.CheckStatus(Gdip.GdipGetMetafileHeaderFromMetafile(new HandleRef(this, nativeImage), memory));

                int[] type = new int[] { 0 };

                Marshal.Copy(memory, type, 0, 1);

                MetafileType metafileType = (MetafileType)type[0];

                header = new MetafileHeader();

                if (metafileType == MetafileType.Wmf ||
                    metafileType == MetafileType.WmfPlaceable)
                {
                    // WMF header
                    header.wmf = (MetafileHeaderWmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderWmf))!;
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)Marshal.PtrToStructure(memory, typeof(MetafileHeaderEmf))!;
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
            Gdip.CheckStatus(Gdip.GdipGetHemfFromMetafile(new HandleRef(this, nativeImage), out IntPtr hEmf));
            return hEmf;
        }
    }
}

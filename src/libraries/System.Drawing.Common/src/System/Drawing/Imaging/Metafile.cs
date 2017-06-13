// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;
    using System.IO;
    using System.Drawing.Internal;
    using System.Runtime.Serialization;

    /**
     * Represent a metafile image
     */
    /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile"]/*' />
    /// <devdoc>
    ///    Defines a graphic metafile. A metafile
    ///    contains records that describe a sequence of graphics operations that can be
    ///    recorded and played back.
    /// </devdoc>
    [Serializable]
    public sealed class Metafile : Image
    {
        /*
         * Create a new metafile object from a metafile handle (WMF)
         */

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified handle and
        ///    <see cref='System.Drawing.Imaging.WmfPlaceableFileHeader'/>.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader) :
        this(hmetafile, wmfHeader, false)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified handle and
        ///    <see cref='System.Drawing.Imaging.WmfPlaceableFileHeader'/>.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader, bool deleteWmf)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromWmf(new HandleRef(null, hmetafile), deleteWmf, wmfHeader, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /*
         * Create a new metafile object from an enhanced metafile handle
         */

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified handle and <see cref='System.Drawing.Imaging.WmfPlaceableFileHeader'/>.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr henhmetafile, bool deleteEmf)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromEmf(new HandleRef(null, henhmetafile), deleteEmf, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /**
         * Create a new metafile object from a file
         */
        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile3"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified filename.
        /// </devdoc>
        public Metafile(string filename)
        {
            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromFile(filename, out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /**
         * Create a new metafile object from a stream
         */
        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile4"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified stream.
        /// </devdoc>
        public Metafile(Stream stream)
        {
            if (stream == null)
                throw new ArgumentException(SR.Format(SR.InvalidArgument, "stream", "null"));

            IntPtr metafile = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMetafileFromStream(new GPStream(stream), out metafile);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeImage(metafile);
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile5"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified handle to a
        ///    device context.
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, EmfType emfType) :
        this(referenceHdc, emfType, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified handle to a device context.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified device context,
        ///       bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect) :
        this(referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified device context,
        ///       bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit) :
        this(referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile9"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified device context, bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, EmfType type) :
        this(referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile10"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified device context,
        ///       bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified device context, bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect) :
        this(referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile12"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified device context, bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit) :
        this(referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile13"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified device context, bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
        public Metafile(IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type) :
        this(referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile14"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified device context, bounded by the specified rectangle.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile15"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the specified
        ///    filename.
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc) :
        this(fileName, referenceHdc, EmfType.EmfPlusDual, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile16"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the specified
        ///       filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, EmfType type) :
        this(fileName, referenceHdc, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile17"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, EmfType type, String description)
        {
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile18"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect) :
        this(fileName, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile19"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile20"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(fileName, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile21"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect, MetafileFrameUnit frameUnit, string desc) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual, desc)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile22"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type, String description)
        {
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile23"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect) :
        this(fileName, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile24"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile25"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(fileName, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile26"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, string description) :
        this(fileName, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual, description)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile27"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(string fileName, IntPtr referenceHdc, Rectangle frameRect, MetafileFrameUnit frameUnit, EmfType type, string description)
        {
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile28"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified data
        ///       stream.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc) :
        this(stream, referenceHdc, EmfType.EmfPlusDual, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile29"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified data
        ///       stream.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, EmfType type) :
        this(stream, referenceHdc, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile30"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified data stream.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile31"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the specified data stream.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect) :
        this(stream, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile32"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit) :
        this(stream, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile33"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, RectangleF frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(stream, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile34"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile35"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class from the
        ///       specified data stream.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect) :
        this(stream, referenceHdc, frameRect, MetafileFrameUnit.GdiCompatible)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile36"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit) :
        this(stream, referenceHdc, frameRect, frameUnit, EmfType.EmfPlusDual)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile37"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
        public Metafile(Stream stream, IntPtr referenceHdc, Rectangle frameRect,
                        MetafileFrameUnit frameUnit, EmfType type) :
        this(stream, referenceHdc, frameRect, frameUnit, type, null)
        { }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.Metafile38"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Imaging.Metafile'/> class with the
        ///       specified filename.
        ///    </para>
        /// </devdoc>
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

        /**
         * Constructor used in deserialization
         */
        private Metafile(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.GetMetafileHeader"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns the <see cref='System.Drawing.Imaging.MetafileHeader'/> associated with the specified <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
        public static MetafileHeader GetMetafileHeader(IntPtr hmetafile, WmfPlaceableFileHeader wmfHeader)
        {
            MetafileHeader header = new MetafileHeader();

            header.wmf = new MetafileHeaderWmf();

            int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromWmf(new HandleRef(null, hmetafile), wmfHeader, header.wmf);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return header;
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.GetMetafileHeader1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns the <see cref='System.Drawing.Imaging.MetafileHeader'/> associated with the specified <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
        public static MetafileHeader GetMetafileHeader(IntPtr henhmetafile)
        {
            MetafileHeader header = new MetafileHeader();
            header.emf = new MetafileHeaderEmf();

            int status = SafeNativeMethods.Gdip.GdipGetMetafileHeaderFromEmf(new HandleRef(null, henhmetafile), header.emf);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return header;
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.GetMetafileHeader2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns the <see cref='System.Drawing.Imaging.MetafileHeader'/> associated with the specified <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
        public static MetafileHeader GetMetafileHeader(string fileName)
        {
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
                    header.wmf = (MetafileHeaderWmf)UnsafeNativeMethods.PtrToStructure(memory, typeof(MetafileHeaderWmf));
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)UnsafeNativeMethods.PtrToStructure(memory, typeof(MetafileHeaderEmf));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return header;
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.GetMetafileHeader3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns the <see cref='System.Drawing.Imaging.MetafileHeader'/> associated with the specified <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
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
                    header.wmf = (MetafileHeaderWmf)UnsafeNativeMethods.PtrToStructure(memory, typeof(MetafileHeaderWmf));
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)UnsafeNativeMethods.PtrToStructure(memory, typeof(MetafileHeaderEmf));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return header;
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.GetMetafileHeader4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns the <see cref='System.Drawing.Imaging.MetafileHeader'/> associated with this <see cref='System.Drawing.Imaging.Metafile'/>.
        ///    </para>
        /// </devdoc>
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
                    header.wmf = (MetafileHeaderWmf)UnsafeNativeMethods.PtrToStructure(memory, typeof(MetafileHeaderWmf));
                    header.emf = null;
                }
                else
                {
                    // EMF header
                    header.wmf = null;
                    header.emf = (MetafileHeaderEmf)UnsafeNativeMethods.PtrToStructure(memory, typeof(MetafileHeaderEmf));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return header;
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.GetHenhmetafile"]/*' />
        /// <devdoc>
        ///    Returns a Windows handle to an enhanced
        /// <see cref='System.Drawing.Imaging.Metafile'/>.
        /// </devdoc>
        public IntPtr GetHenhmetafile()
        {
            IntPtr hEmf = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipGetHemfFromMetafile(new HandleRef(this, nativeImage), out hEmf);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return hEmf;
        }

        /// <include file='doc\Metafile.uex' path='docs/doc[@for="Metafile.PlayRecord"]/*' />
        /// <devdoc>
        ///    Plays an EMF+ file.
        /// </devdoc>
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

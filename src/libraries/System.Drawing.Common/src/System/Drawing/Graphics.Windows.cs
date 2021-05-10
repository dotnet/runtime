// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Globalization;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    /// <summary>
    /// Encapsulates a GDI+ drawing surface.
    /// </summary>
    public sealed partial class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
#if FINALIZATION_WATCH
        static readonly TraceSwitch GraphicsFinalization = new TraceSwitch("GraphicsFinalization", "Tracks the creation and destruction of finalization");
        internal static string GetAllocationStack() {
            if (GraphicsFinalization.TraceVerbose) {
                return Environment.StackTrace;
            }
            else {
                return "Enabled 'GraphicsFinalization' switch to see stack of allocation";
            }
        }
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        /// <summary>
        /// The context state previous to the current Graphics context (the head of the stack).
        /// We don't keep a GraphicsContext for the current context since it is available at any time from GDI+ and
        /// we don't want to keep track of changes in it.
        /// </summary>
        private GraphicsContext? _previousContext;

        private static readonly object s_syncObject = new object();

        // Object reference used for printing; it could point to a PrintPreviewGraphics to obtain the VisibleClipBounds, or
        // a DeviceContext holding a printer DC.
        private object? _printingHelper;

        // GDI+'s preferred HPALETTE.
        private static IntPtr s_halftonePalette;

        // pointer back to the Image backing a specific graphic object
        private Image? _backingImage;

        /// <summary>
        /// Constructor to initialize this object from a native GDI+ Graphics pointer.
        /// </summary>
        private Graphics(IntPtr gdipNativeGraphics)
        {
            if (gdipNativeGraphics == IntPtr.Zero)
                throw new ArgumentNullException(nameof(gdipNativeGraphics));

            NativeGraphics = gdipNativeGraphics;
        }

        /// <summary>
        /// Creates a new instance of the <see cref='Graphics'/> class from the specified handle to a device context.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc)
        {
            if (hdc == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hdc));

            return FromHdcInternal(hdc);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdcInternal(IntPtr hdc)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHDC(hdc, out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates a new instance of the Graphics class from the specified handle to a device context and handle to a device.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc, IntPtr hdevice)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHDC2(hdc, hdevice, out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates a new instance of the <see cref='Graphics'/> class from a window handle.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwnd(IntPtr hwnd) => FromHwndInternal(hwnd);

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwndInternal(IntPtr hwnd)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHWND(hwnd, out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates an instance of the <see cref='Graphics'/> class from an existing <see cref='Image'/>.
        /// </summary>
        public static Graphics FromImage(Image image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if ((image.PixelFormat & PixelFormat.Indexed) != 0)
                throw new ArgumentException(SR.GdiplusCannotCreateGraphicsFromIndexedPixelFormat, nameof(image));

            Gdip.CheckStatus(Gdip.GdipGetImageGraphicsContext(
                new HandleRef(image, image.nativeImage),
                out IntPtr nativeGraphics));

            return new Graphics(nativeGraphics) { _backingImage = image };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ReleaseHdcInternal(IntPtr hdc)
        {
            Gdip.CheckStatus(!Gdip.Initialized ? Gdip.Ok :
                Gdip.GdipReleaseDC(new HandleRef(this, NativeGraphics), hdc));
            _nativeHdc = IntPtr.Zero;
        }

        /// <summary>
        /// Deletes this <see cref='Graphics'/>, and frees the memory allocated for it.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
#if DEBUG && FINALIZATION_WATCH
            if (!disposing && _nativeGraphics != IntPtr.Zero)
            {
                Debug.WriteLine("System.Drawing.Graphics: ***************************************************");
                Debug.WriteLine("System.Drawing.Graphics: Object Disposed through finalization:\n" + allocationSite);
            }
#endif
            while (_previousContext != null)
            {
                // Dispose entire stack.
                GraphicsContext? context = _previousContext.Previous;
                _previousContext.Dispose();
                _previousContext = context;
            }

            if (NativeGraphics != IntPtr.Zero)
            {
                try
                {
                    if (_nativeHdc != IntPtr.Zero) // avoid a handle leak.
                    {
                        ReleaseHdc();
                    }

                    if (PrintingHelper is DeviceContext printerDC)
                    {
                        printerDC.Dispose();
                        _printingHelper = null;
                    }

#if DEBUG
                    int status = !Gdip.Initialized ? Gdip.Ok :
#endif
                    Gdip.GdipDeleteGraphics(new HandleRef(this, NativeGraphics));

#if DEBUG
                    Debug.Assert(status == Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                }
                finally
                {
                    NativeGraphics = IntPtr.Zero;
                }
            }
        }

        ~Graphics() => Dispose(false);

        private void FlushCore()
        {
            // Libgdiplus needs to synchronize a macOS context. Windows does not do anything.
        }

        /// <summary>
        /// Represents an object used in connection with the printing API, it is used to hold a reference to a
        /// PrintPreviewGraphics (fake graphics) or a printer DeviceContext (and maybe more in the future).
        /// </summary>
        internal object? PrintingHelper
        {
            get => _printingHelper;
            set
            {
                Debug.Assert(_printingHelper == null, "WARNING: Overwritting the printing helper reference!");
                _printingHelper = value;
            }
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size
        /// and specified raster operation.
        /// </summary>
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            switch (copyPixelOperation)
            {
                case CopyPixelOperation.Blackness:
                case CopyPixelOperation.NotSourceErase:
                case CopyPixelOperation.NotSourceCopy:
                case CopyPixelOperation.SourceErase:
                case CopyPixelOperation.DestinationInvert:
                case CopyPixelOperation.PatInvert:
                case CopyPixelOperation.SourceInvert:
                case CopyPixelOperation.SourceAnd:
                case CopyPixelOperation.MergePaint:
                case CopyPixelOperation.MergeCopy:
                case CopyPixelOperation.SourceCopy:
                case CopyPixelOperation.SourcePaint:
                case CopyPixelOperation.PatCopy:
                case CopyPixelOperation.PatPaint:
                case CopyPixelOperation.Whiteness:
                case CopyPixelOperation.CaptureBlt:
                case CopyPixelOperation.NoMirrorBitmap:
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(copyPixelOperation), (int)copyPixelOperation, typeof(CopyPixelOperation));
            }

            int destWidth = blockRegionSize.Width;
            int destHeight = blockRegionSize.Height;

            IntPtr screenDC = Interop.User32.GetDC(IntPtr.Zero);
            try
            {
                IntPtr targetDC = GetHdc();
                int result = Interop.Gdi32.BitBlt(
                    targetDC,
                    destinationX,
                    destinationY,
                    destWidth,
                    destHeight,
                    screenDC,
                    sourceX,
                    sourceY,
                    (Interop.Gdi32.RasterOp)copyPixelOperation);

                //a zero result indicates a win32 exception has been thrown
                if (result == 0)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Interop.User32.ReleaseDC(IntPtr.Zero, screenDC);
                ReleaseHdc();
            }
        }

        public Color GetNearestColor(Color color)
        {
            int nearest = color.ToArgb();
            Gdip.CheckStatus(Gdip.GdipGetNearestColor(new HandleRef(this, NativeGraphics), ref nearest));
            return Color.FromArgb(nearest);
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawLine(new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen), x1, y1, x2, y2));
        }

        /// <summary>
        /// Draws a series of cubic Bezier curves from an array of points.
        /// </summary>
        public unsafe void DrawBeziers(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawBeziers(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a series of cubic Bezier curves from an array of points.
        /// </summary>
        public unsafe void DrawBeziers(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawBeziersI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p,
                    points.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a path.
        /// </summary>
        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            CheckErrorStatus(Gdip.GdipFillPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                new HandleRef(path, path._nativePath)));
        }

        /// <summary>
        /// Fills the interior of a <see cref='Region'/>.
        /// </summary>
        public void FillRegion(Brush brush, Region region)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            CheckErrorStatus(Gdip.GdipFillRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                new HandleRef(region, region.NativeRegion)));
        }

        public void DrawIcon(Icon icon, int x, int y)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            if (_backingImage != null)
            {
                // We don't call the icon directly because we want to stay in GDI+ all the time
                // to avoid alpha channel interop issues between gdi and gdi+
                // so we do icon.ToBitmap() and then we call DrawImage. This is probably slower.
                DrawImage(icon.ToBitmap(), x, y);
            }
            else
            {
                icon.Draw(this, x, y);
            }
        }

        /// <summary>
        /// Draws this image to a graphics object. The drawing command originates on the graphics
        /// object, but a graphics object generally has no idea how to render a given image. So,
        /// it passes the call to the actual image. This version crops the image to the given
        /// dimensions and allows the user to specify a rectangle within the image to draw.
        /// </summary>
        public void DrawIcon(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            if (_backingImage != null)
            {
                // We don't call the icon directly because we want to stay in GDI+ all the time
                // to avoid alpha channel interop issues between gdi and gdi+
                // so we do icon.ToBitmap() and then we call DrawImage. This is probably slower.
                DrawImage(icon.ToBitmap(), targetRect);
            }
            else
            {
                icon.Draw(this, targetRect);
            }
        }

        /// <summary>
        /// Draws this image to a graphics object. The drawing command originates on the graphics
        /// object, but a graphics object generally has no idea how to render a given image. So,
        /// it passes the call to the actual image. This version stretches the image to the given
        /// dimensions and allows the user to specify a rectangle within the image to draw.
        /// </summary>
        public void DrawIconUnstretched(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            if (_backingImage != null)
            {
                DrawImageUnscaled(icon.ToBitmap(), targetRect);
            }
            else
            {
                icon.DrawUnstretched(this, targetRect);
            }
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPoint(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }
        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPointI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (PointF* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (Point* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPoint(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPointI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                ref srcRect,
                unit,
                callback,
                callbackData,
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (PointF* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    ref srcRect,
                    unit,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        public unsafe void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes? imageAttr)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (destPoints.Length != 3)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidParallelogram);

            fixed (Point* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                    p, destPoints.Length,
                    ref srcRect,
                    unit,
                    callback,
                    callbackData,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        /// <summary>
        /// Combines current Graphics context with all previous contexts.
        /// When BeginContainer() is called, a copy of the current context is pushed into the GDI+ context stack, it keeps track of the
        /// absolute clipping and transform but reset the public properties so it looks like a brand new context.
        /// When Save() is called, a copy of the current context is also pushed in the GDI+ stack but the public clipping and transform
        /// properties are not reset (cumulative). Consecutive Save context are ignored with the exception of the top one which contains
        /// all previous information.
        /// The return value is an object array where the first element contains the cumulative clip region and the second the cumulative
        /// translate transform matrix.
        /// WARNING: This method is for internal FX support only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(Obsoletions.GetContextInfoMessage, DiagnosticId = Obsoletions.GetContextInfoDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [SupportedOSPlatform("windows")]
        public object GetContextInfo()
        {
            GetContextInfo(out Matrix3x2 cumulativeTransform, calculateClip: true, out Region? cumulativeClip);
            return new object[] { cumulativeClip ?? new Region(), new Matrix(cumulativeTransform) };
        }

        private void GetContextInfo(out Matrix3x2 cumulativeTransform, bool calculateClip, out Region? cumulativeClip)
        {
            cumulativeClip = calculateClip ? GetRegionIfNotInfinite() : null;   // Current context clip.
            cumulativeTransform = TransformElements;                            // Current context transform.
            Vector2 currentOffset = default;                                    // Offset of current context.
            Vector2 totalOffset = default;                                      // Absolute coordinate offset of top context.

            GraphicsContext? context = _previousContext;

            if (!cumulativeTransform.IsIdentity)
            {
                currentOffset = cumulativeTransform.Translation;
            }

            while (context is not null)
            {
                if (!context.TransformOffset.IsEmpty())
                {
                    cumulativeTransform.Translate(context.TransformOffset);
                }

                if (!currentOffset.IsEmpty())
                {
                    // The location of the GDI+ clip region is relative to the coordinate origin after any translate transform
                    // has been applied. We need to intersect regions using the same coordinate origin relative to the previous
                    // context.

                    // If we don't have a cumulative clip, we're infinite, and translation on infinite regions is a no-op.
                    cumulativeClip?.Translate(currentOffset.X, currentOffset.Y);
                    totalOffset.X += currentOffset.X;
                    totalOffset.Y += currentOffset.Y;
                }

                // Context only stores clips if they are not infinite. Intersecting a clip with an infinite clip is a no-op.
                if (calculateClip && context.Clip is not null)
                {
                    // Intersecting an infinite clip with another is just a copy of the second clip.
                    if (cumulativeClip is null)
                    {
                        cumulativeClip = context.Clip;
                    }
                    else
                    {
                        cumulativeClip.Intersect(context.Clip);
                    }
                }

                currentOffset = context.TransformOffset;

                // Ignore subsequent cumulative contexts.
                do
                {
                    context = context.Previous;

                    if (context == null || !context.Next!.IsCumulative)
                    {
                        break;
                    }
                } while (context.IsCumulative);
            }

            if (!totalOffset.IsEmpty())
            {
                // We need now to reset the total transform in the region so when calling Region.GetHRgn(Graphics)
                // the HRegion is properly offset by GDI+ based on the total offset of the graphics object.

                // If we don't have a cumulative clip, we're infinite, and translation on infinite regions is a no-op.
                cumulativeClip?.Translate(-totalOffset.X, -totalOffset.Y);
            }
        }

        /// <summary>
        ///  Gets the cumulative offset.
        /// </summary>
        /// <param name="offset">The cumulative offset.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SupportedOSPlatform("windows")]
        public void GetContextInfo(out PointF offset)
        {
            GetContextInfo(out Matrix3x2 cumulativeTransform, calculateClip: false, out _);
            Vector2 translation = cumulativeTransform.Translation;
            offset = new PointF(translation.X, translation.Y);
        }

        /// <summary>
        ///  Gets the cumulative offset and clip region.
        /// </summary>
        /// <param name="offset">The cumulative offset.</param>
        /// <param name="clip">The cumulative clip region or null if the clip region is infinite.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SupportedOSPlatform("windows")]
        public void GetContextInfo(out PointF offset, out Region? clip)
        {
            GetContextInfo(out Matrix3x2 cumulativeTransform, calculateClip: true, out clip);
            Vector2 translation = cumulativeTransform.Translation;
            offset = new PointF(translation.X, translation.Y);
        }

        public RectangleF VisibleClipBounds
        {
            get
            {
                if (PrintingHelper is PrintPreviewGraphics ppGraphics)
                    return ppGraphics.VisibleClipBounds;

                Gdip.CheckStatus(Gdip.GdipGetVisibleClipBounds(new HandleRef(this, NativeGraphics), out RectangleF rect));

                return rect;
            }
        }

        /// <summary>
        /// Saves the current context into the context stack.
        /// </summary>
        private void PushContext(GraphicsContext context)
        {
            Debug.Assert(context != null && context.State != 0, "GraphicsContext object is null or not valid.");

            if (_previousContext != null)
            {
                // Push context.
                context.Previous = _previousContext;
                _previousContext.Next = context;
            }
            _previousContext = context;
        }

        /// <summary>
        /// Pops all contexts from the specified one included. The specified context is becoming the current context.
        /// </summary>
        private void PopContext(int currentContextState)
        {
            Debug.Assert(_previousContext != null, "Trying to restore a context when the stack is empty");
            GraphicsContext? context = _previousContext;

            // Pop all contexts up the stack.
            while (context != null)
            {
                if (context.State == currentContextState)
                {
                    _previousContext = context.Previous;

                    // This will dipose all context object up the stack.
                    context.Dispose();
                    return;
                }
                context = context.Previous;
            }
            Debug.Fail("Warning: context state not found!");
        }

        public GraphicsState Save()
        {
            GraphicsContext context = new GraphicsContext(this);
            int status = Gdip.GdipSaveGraphics(new HandleRef(this, NativeGraphics), out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            context.IsCumulative = true;
            PushContext(context);

            return new GraphicsState(state);
        }

        public void Restore(GraphicsState gstate)
        {
            Gdip.CheckStatus(Gdip.GdipRestoreGraphics(new HandleRef(this, NativeGraphics), gstate.nativeState));
            PopContext(gstate.nativeState);
        }

        public GraphicsContainer BeginContainer(RectangleF dstrect, RectangleF srcrect, GraphicsUnit unit)
        {
            GraphicsContext context = new GraphicsContext(this);

            int status = Gdip.GdipBeginContainer(
                new HandleRef(this, NativeGraphics), ref dstrect, ref srcrect, unit, out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            PushContext(context);

            return new GraphicsContainer(state);
        }

        public GraphicsContainer BeginContainer()
        {
            GraphicsContext context = new GraphicsContext(this);
            int status = Gdip.GdipBeginContainer2(new HandleRef(this, NativeGraphics), out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            PushContext(context);

            return new GraphicsContainer(state);
        }

        public void EndContainer(GraphicsContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            Gdip.CheckStatus(Gdip.GdipEndContainer(new HandleRef(this, NativeGraphics), container.nativeGraphicsContainer));
            PopContext(container.nativeGraphicsContainer);
        }

        public GraphicsContainer BeginContainer(Rectangle dstrect, Rectangle srcrect, GraphicsUnit unit)
        {
            GraphicsContext context = new GraphicsContext(this);

            int status = Gdip.GdipBeginContainerI(
                new HandleRef(this, NativeGraphics), ref dstrect, ref srcrect, unit, out int state);

            if (status != Gdip.Ok)
            {
                context.Dispose();
                throw Gdip.StatusException(status);
            }

            context.State = state;
            PushContext(context);

            return new GraphicsContainer(state);
        }

        public void AddMetafileComment(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Gdip.CheckStatus(Gdip.GdipComment(new HandleRef(this, NativeGraphics), data.Length, data));
        }

        public static IntPtr GetHalftonePalette()
        {
            if (s_halftonePalette == IntPtr.Zero)
            {
                lock (s_syncObject)
                {
                    if (s_halftonePalette == IntPtr.Zero)
                    {
                        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
                        AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;

                        s_halftonePalette = Gdip.GdipCreateHalftonePalette();
                    }
                }
            }
            return s_halftonePalette;
        }

        // This is called from AppDomain.ProcessExit and AppDomain.DomainUnload.
        private static void OnDomainUnload(object? sender, EventArgs e)
        {
            if (s_halftonePalette != IntPtr.Zero)
            {
                Interop.Gdi32.DeleteObject(s_halftonePalette);
                s_halftonePalette = IntPtr.Zero;
            }
        }

        /// <summary>
        /// GDI+ will return a 'generic error' with specific win32 last error codes when
        /// a terminal server session has been closed, minimized, etc... We don't want
        /// to throw when this happens, so we'll guard against this by looking at the
        /// 'last win32 error code' and checking to see if it is either 1) access denied
        /// or 2) proc not found and then ignore it.
        ///
        /// The problem is that when you lock the machine, the secure desktop is enabled and
        /// rendering fails which is expected (since the app doesn't have permission to draw
        /// on the secure desktop). Not sure if there's anything you can do, short of catching
        /// the desktop switch message and absorbing all the exceptions that get thrown while
        /// it's the secure desktop.
        /// </summary>
        private void CheckErrorStatus(int status)
        {
            if (status == Gdip.Ok)
                return;

            // Generic error from GDI+ can be GenericError or Win32Error.
            if (status == Gdip.GenericError || status == Gdip.Win32Error)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == SafeNativeMethods.ERROR_ACCESS_DENIED || error == SafeNativeMethods.ERROR_PROC_NOT_FOUND ||
                        // Here, we'll check to see if we are in a terminal services session...
                        (((Interop.User32.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) & 0x00000001) != 0) && (error == 0)))
                {
                    return;
                }
            }

            // Legitimate error, throw our status exception.
            throw Gdip.StatusException(status);
        }
    }
}

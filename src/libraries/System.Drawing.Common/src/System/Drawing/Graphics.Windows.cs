// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
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
        private GraphicsContext _previousContext;

        private static readonly object s_syncObject = new object();

        // Object reference used for printing; it could point to a PrintPreviewGraphics to obtain the VisibleClipBounds, or 
        // a DeviceContext holding a printer DC.
        private object _printingHelper;

        // GDI+'s preferred HPALETTE.
        private static IntPtr s_halftonePalette;

        // pointer back to the Image backing a specific graphic object
        private Image _backingImage;

        public delegate bool DrawImageAbort(IntPtr callbackdata);

        /// <summary>
        /// Callback for EnumerateMetafile methods.
        /// This method can then call Metafile.PlayRecord to play the record that was just enumerated.
        /// </summary>
        /// <param name="recordType">if >= MinRecordType, it's an EMF+ record</param>
        /// <param name="flags">always 0 for EMF records</param>
        /// <param name="dataSize">size of the data, or 0 if no data</param>
        /// <param name="data">pointer to the data, or NULL if no data (UINT32 aligned)</param>
        /// <param name="callbackData">pointer to callbackData, if any</param>
        /// <returns>False to abort enumerating, true to continue.</returns>
        public delegate bool EnumerateMetafileProc(
            EmfPlusRecordType recordType,
            int flags,
            int dataSize,
            IntPtr data,
            PlayRecordCallback callbackData);

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
            Gdip.CheckStatus(Gdip.GdipCreateFromHDC(new HandleRef(null, hdc), out IntPtr nativeGraphics));
            return new Graphics(nativeGraphics);
        }

        /// <summary>
        /// Creates a new instance of the Graphics class from the specified handle to a device context and handle to a device.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc, IntPtr hdevice)
        {
            Gdip.CheckStatus(Gdip.GdipCreateFromHDC2(
                new HandleRef(null, hdc),
                new HandleRef(null, hdevice),
                out IntPtr nativeGraphics));

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
            Gdip.CheckStatus(Gdip.GdipCreateFromHWND(new HandleRef(null, hwnd), out IntPtr nativeGraphics));
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
            Gdip.CheckStatus(Gdip.GdipReleaseDC(new HandleRef(this, NativeGraphics), new HandleRef(null, hdc)));
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
                GraphicsContext context = _previousContext.Previous;
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
                    int status =
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
        internal object PrintingHelper
        {
            get => _printingHelper;
            set
            {
                Debug.Assert(_printingHelper == null, "WARNING: Overwritting the printing helper reference!");
                _printingHelper = value;
            }
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size.
        /// </summary>
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize);
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size.
        /// </summary>
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize)
        {
            CopyFromScreen(sourceX, sourceY, destinationX, destinationY, blockRegionSize, CopyPixelOperation.SourceCopy);
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size
        /// and specified raster operation.
        /// </summary>
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize, copyPixelOperation);
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

            using (DeviceContext dc = DeviceContext.FromHwnd(IntPtr.Zero))
            {
                // The DC of the screen.
                HandleRef screenDC = new HandleRef(null, dc.Hdc);

                // The DC of the current graphics object.
                HandleRef targetDC = new HandleRef(null, GetHdc());

                try
                {
                    int result = SafeNativeMethods.BitBlt(
                        targetDC, destinationX, destinationY, destWidth, destHeight, screenDC, sourceX, sourceY, (int)copyPixelOperation);

                    //a zero result indicates a win32 exception has been thrown
                    if (result == 0)
                    {
                        throw new Win32Exception();
                    }
                }
                finally
                {
                    ReleaseHdc();
                }
            }
        }

        public unsafe void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformPoints(
                    new HandleRef(this, NativeGraphics),
                    (int)destSpace,
                    (int)srcSpace,
                    p,
                    pts.Length));
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformPointsI(
                    new HandleRef(this, NativeGraphics),
                    (int)destSpace,
                    (int)srcSpace,
                    p,
                    pts.Length));
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
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        /// <summary>
        /// Draws a series of line segments that connect an array of points.
        /// </summary>
        public unsafe void DrawLines(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawLines(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }


        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawLineI(new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen), x1, y1, x2, y2));
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        /// <summary>
        /// Draws a series of line segments that connect an array of points.
        /// </summary>
        public unsafe void DrawLines(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawLinesI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p,
                    points.Length));
            }
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawArc(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawArcI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four ordered pairs that represent points.
        /// </summary>
        public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawBezier(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four points.
        /// </summary>
        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
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
        /// Draws a cubic bezier curve defined by four points.
        /// </summary>
        public void DrawBezier(Pen pen, Point pt1, Point pt2, Point pt3, Point pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
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
        /// Draws the outline of a rectangle specified by <paramref name="rect"/>.
        /// </summary>
        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of the specified rectangle.
        /// </summary>
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawRectangle(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of the specified rectangle.
        /// </summary>
        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawRectangleI(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outlines of a series of rectangles.
        /// </summary>
        public unsafe void DrawRectangles(Pen pen, RectangleF[] rects)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            fixed (RectangleF* r = rects)
            {
                CheckErrorStatus(Gdip.GdipDrawRectangles(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Draws the outlines of a series of rectangles.
        /// </summary>
        public unsafe void DrawRectangles(Pen pen, Rectangle[] rects)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            fixed (Rectangle* r = rects)
            {
                CheckErrorStatus(Gdip.GdipDrawRectanglesI(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    r, rects.Length));
            }

        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawEllipse(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of an ellipse specified by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, int x, int y, int width, int height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawEllipseI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawPie(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            CheckErrorStatus(Gdip.GdipDrawPieI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws the outline of a polygon defined by an array of points.
        /// </summary>
        public unsafe void DrawPolygon(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawPolygon(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws the outline of a polygon defined by an array of points.
        /// </summary>
        public unsafe void DrawPolygon(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawPolygonI(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws the lines and curves defined by a <see cref='GraphicsPath'/>.
        /// </summary>
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            CheckErrorStatus(Gdip.GdipDrawPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                new HandleRef(path, path._nativePath)));
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, PointF[] points, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments)
        {
            DrawCurve(pen, points, offset, numberOfSegments, 0.5f);
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve3(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    offset,
                    numberOfSegments,
                    tension));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, Point[] points, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen, Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve3I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    offset,
                    numberOfSegments,
                    tension));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, PointF[] points, float tension, FillMode fillmode)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen, Point[] points, float tension, FillMode fillmode)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Fills the entire drawing surface with the specified color.
        /// </summary>
        public void Clear(Color color)
        {
            Gdip.CheckStatus(Gdip.GdipGraphicsClear(new HandleRef(this, NativeGraphics), color.ToArgb()));
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            CheckErrorStatus(Gdip.GdipFillRectangle(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, int x, int y, int width, int height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            CheckErrorStatus(Gdip.GdipFillRectangleI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interiors of a series of rectangles with a <see cref='Brush'/>.
        /// </summary>
        public unsafe void FillRectangles(Brush brush, RectangleF[] rects)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            fixed (RectangleF* r = rects)
            {
                CheckErrorStatus(Gdip.GdipFillRectangles(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Fills the interiors of a series of rectangles with a <see cref='Brush'/>.
        /// </summary>
        public unsafe void FillRectangles(Brush brush, Rectangle[] rects)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            fixed (Rectangle* r = rects)
            {
                CheckErrorStatus(Gdip.GdipFillRectanglesI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public void FillPolygon(Brush brush, PointF[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public unsafe void FillPolygon(Brush brush, PointF[] points, FillMode fillMode)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillPolygon(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    fillMode));
            }
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public void FillPolygon(Brush brush, Point[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public unsafe void FillPolygon(Brush brush, Point[] points, FillMode fillMode)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillPolygonI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    fillMode));
            }
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            CheckErrorStatus(Gdip.GdipFillEllipse(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, Rectangle rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, int x, int y, int width, int height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            CheckErrorStatus(Gdip.GdipFillEllipseI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            CheckErrorStatus(Gdip.GdipFillPie(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            CheckErrorStatus(Gdip.GdipFillPieI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height,
                startAngle,
                sweepAngle));
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
        /// Fills the interior a closed curve defined by an array of points.
        /// </summary>
        public unsafe void FillClosedCurve(Brush brush, PointF[] points)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a closed curve defined by an array of points.
        /// </summary>
        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode)
        {
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public unsafe void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode, float tension)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    tension,
                    fillmode));
            }
        }

        /// <summary>
        /// Fills the interior a closed curve defined by an array of points.
        /// </summary>
        public unsafe void FillClosedCurve(Brush brush, Point[] points)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length));
            }
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode)
        {
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public unsafe void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode, float tension)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    tension,
                    fillmode));
            }
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

        /// <summary>
        /// Draws a string with the specified font.
        /// </summary>
        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), null);
        }

        public void DrawString(string s, Font font, Brush brush, PointF point)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), null);
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y, StringFormat format)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), format);
        }

        public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), format);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, null);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (string.IsNullOrEmpty(s))
                return;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            CheckErrorStatus(Gdip.GdipDrawString(
                new HandleRef(this, NativeGraphics),
                s,
                s.Length,
                new HandleRef(font, font.NativeFont),
                ref layoutRectangle,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero),
                new HandleRef(brush, brush.NativeBrush)));
        }

        public SizeF MeasureString(
            string text,
            Font font,
            SizeF layoutArea,
            StringFormat stringFormat,
            out int charactersFitted,
            out int linesFilled)
        {
            if (string.IsNullOrEmpty(text))
            {
                charactersFitted = 0;
                linesFilled = 0;
                return SizeF.Empty;
            }

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            RectangleF boundingBox = new RectangleF();

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out charactersFitted,
                out linesFilled));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(origin.X, origin.Y, 0, 0);
            RectangleF boundingBox = new RectangleF();

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out int a,
                out int b));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea) => MeasureString(text, font, layoutArea, null);

        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            RectangleF boundingBox = new RectangleF();

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out int a,
                out int b));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string text, Font font)
        {
            return MeasureString(text, font, new SizeF(0, 0));
        }

        public SizeF MeasureString(string text, Font font, int width)
        {
            return MeasureString(text, font, new SizeF(width, 999999));
        }

        public SizeF MeasureString(string text, Font font, int width, StringFormat format)
        {
            return MeasureString(text, font, new SizeF(width, 999999), format);
        }

        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<Region>();
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            Gdip.CheckStatus(Gdip.GdipGetStringFormatMeasurableCharacterRangeCount(
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                out int count));

            IntPtr[] gpRegions = new IntPtr[count];
            Region[] regions = new Region[count];

            for (int f = 0; f < count; f++)
            {
                regions[f] = new Region();
                gpRegions[f] = regions[f].NativeRegion;
            }

            Gdip.CheckStatus(Gdip.GdipMeasureCharacterRanges(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layoutRect,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                count,
                gpRegions));

            return regions;
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

        /// <summary>
        /// Draws the specified image at the specified location.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, PointF point)
        {
            DrawImage(image, point.X, point.Y);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, float x, float y)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImage(
                new HandleRef(this, NativeGraphics), new HandleRef(image, image.nativeImage),
                x, y);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, RectangleF rect)
        {
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                width, height);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, int x, int y)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, Rectangle rect)
        {
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, int x, int y, int width, int height)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                width, height);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImageUnscaled(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y)
        {
            DrawImage(image, x, y);
        }

        public void DrawImageUnscaled(Image image, Rectangle rect)
        {
            DrawImage(image, rect.X, rect.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y, int width, int height)
        {
            DrawImage(image, x, y);
        }

        public void DrawImageUnscaledAndClipped(Image image, Rectangle rect)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int width = Math.Min(rect.Width, image.Width);
            int height = Math.Min(rect.Height, image.Height);

            // We could put centering logic here too for the case when the image
            // is smaller than the rect.
            DrawImage(image, rect, 0, 0, width, height, GraphicsUnit.Pixel);
        }

        // Affine or perspective blt
        //  destPoints.Length = 3: rect => parallelogram
        // destPoints[0] <=> top-left corner of the source rectangle
        //      destPoints[1] <=> top-right corner
        //       destPoints[2] <=> bottom-left corner
        //  destPoints.Length = 4: rect => quad
        // destPoints[3] <=> bottom-right corner
        // 
        //  @notes Perspective blt only works for bitmap images.

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void DrawImage(Image image, PointF[] destPoints)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, count);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void DrawImage(Image image, Point[] destPoints)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (Point* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, count);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, float x, float y, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImagePointRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                (int)srcUnit);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, int x, int y, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImagePointRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                (int)srcUnit);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRectRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit,
                NativeMethods.NullHandleRef,
                null,
                NativeMethods.NullHandleRef);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRectRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit,
                NativeMethods.NullHandleRef,
                null,
                NativeMethods.NullHandleRef);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRect(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    NativeMethods.NullHandleRef,
                    null,
                    NativeMethods.NullHandleRef);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, null, 0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, callback, 0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void DrawImage(
            Image image,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback,
            int callbackData)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRect(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero),
                    callback,
                    new HandleRef(null, (IntPtr)callbackData));

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, null, null, 0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, null, 0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, callback, 0);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback,
            int callbackData)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (Point* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRectI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero),
                    callback,
                    new HandleRef(null, (IntPtr)callbackData));

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttrs)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttrs, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttrs,
            DrawImageAbort callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttrs, callback, IntPtr.Zero);
        }


        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttrs,
            DrawImageAbort callback,
            IntPtr callbackData)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRectRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight,
                srcUnit,
                new HandleRef(imageAttrs, imageAttrs?.nativeImageAttributes ?? IntPtr.Zero),
                callback,
                new HandleRef(null, callbackData));

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttr,
            DrawImageAbort callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes imageAttrs,
            DrawImageAbort callback,
            IntPtr callbackData)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRectRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight,
                srcUnit,
                new HandleRef(imageAttrs, imageAttrs?.nativeImageAttributes ?? IntPtr.Zero),
                callback,
                new HandleRef(null, callbackData));

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPoint(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestPointI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileDestRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public unsafe void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
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
                    new HandleRef(null, callbackData),
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public unsafe void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
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
                    new HandleRef(null, callbackData),
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPoint(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                ref srcRect,
                unit,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestPointI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destPoint,
                ref srcRect,
                unit,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                ref srcRect,
                unit,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
        {
            Gdip.CheckStatus(Gdip.GdipEnumerateMetafileSrcRectDestRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(metafile, metafile?.nativeImage ?? IntPtr.Zero),
                ref destRect,
                ref srcRect,
                unit,
                callback,
                new HandleRef(null, callbackData),
                new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
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
                    new HandleRef(null, callbackData),
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero)));
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, callbackData, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public unsafe void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit unit,
            EnumerateMetafileProc callback,
            IntPtr callbackData,
            ImageAttributes imageAttr)
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
                    new HandleRef(null, callbackData),
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
        public object GetContextInfo()
        {
            Region cumulClip = Clip;                // current context clip.
            Matrix cumulTransform = Transform;      // current context transform.
            PointF currentOffset = PointF.Empty;    // offset of current context.
            PointF totalOffset = PointF.Empty;      // absolute coord offset of top context.

            if (!cumulTransform.IsIdentity)
            {
                float[] elements = cumulTransform.Elements;
                currentOffset.X = elements[4];
                currentOffset.Y = elements[5];
            }

            GraphicsContext context = _previousContext;

            while (context != null)
            {
                if (!context.TransformOffset.IsEmpty)
                {
                    cumulTransform.Translate(context.TransformOffset.X, context.TransformOffset.Y);
                }

                if (!currentOffset.IsEmpty)
                {
                    // The location of the GDI+ clip region is relative to the coordinate origin after any translate transform
                    // has been applied. We need to intersect regions using the same coordinate origin relative to the previous
                    // context.
                    cumulClip.Translate(currentOffset.X, currentOffset.Y);
                    totalOffset.X += currentOffset.X;
                    totalOffset.Y += currentOffset.Y;
                }

                if (context.Clip != null)
                {
                    cumulClip.Intersect(context.Clip);
                }

                currentOffset = context.TransformOffset;

                // Ignore subsequent cumulative contexts.
                do
                {
                    context = context.Previous;

                    if (context == null || !context.Next.IsCumulative)
                    {
                        break;
                    }
                } while (context.IsCumulative);
            }

            if (!totalOffset.IsEmpty)
            {
                // We need now to reset the total transform in the region so when calling Region.GetHRgn(Graphics)
                // the HRegion is properly offset by GDI+ based on the total offset of the graphics object.
                cumulClip.Translate(-totalOffset.X, -totalOffset.Y);
            }

            return new object[] { cumulClip, cumulTransform };
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
            GraphicsContext context = _previousContext;

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
        [PrePrepareMethod]
        private static void OnDomainUnload(object sender, EventArgs e)
        {
            if (s_halftonePalette != IntPtr.Zero)
            {
                SafeNativeMethods.IntDeleteObject(new HandleRef(null, s_halftonePalette));
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
                        (((UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) & 0x00000001) != 0) && (error == 0)))
                {
                    return;
                }
            }

            // Legitimate error, throw our status exception.
            throw Gdip.StatusException(status);
        }

        /// <summary>
        /// GDI+ will return a 'generic error' when we attempt to draw an Emf 
        /// image with width/height == 1. Here, we will hack around this by 
        /// resetting the errorstatus. Note that we don't do simple arg checking
        /// for height || width == 1 here because transforms can be applied to
        /// the Graphics object making it difficult to identify this scenario.
        /// </summary>
        private void IgnoreMetafileErrors(Image image, ref int errorStatus)
        {
            if (errorStatus != Gdip.Ok && image.RawFormat.Equals(ImageFormat.Emf))
                errorStatus = Gdip.Ok;
        }
    }
}

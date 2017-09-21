// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Internal;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{
    public sealed class GraphicsPath : MarshalByRefObject, ICloneable, IDisposable
    {
        internal IntPtr nativePath;

        public GraphicsPath() : this(FillMode.Alternate) { }

        public GraphicsPath(FillMode fillMode)
        {
            IntPtr nativePath;
            int status = SafeNativeMethods.Gdip.GdipCreatePath(unchecked((int)fillMode), out nativePath);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            this.nativePath = nativePath;
        }

        public GraphicsPath(PointF[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public GraphicsPath(PointF[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            if (pts.Length != types.Length)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int count = types.Length;
            IntPtr ptbuf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);
            IntPtr typebuf = Marshal.AllocHGlobal(count);
            IntPtr nativePath = IntPtr.Zero;
            try
            {
                Marshal.Copy(types, 0, typebuf, count);

                int status = SafeNativeMethods.Gdip.GdipCreatePath2(new HandleRef(null, ptbuf), new HandleRef(null, typebuf), count,
                                                     unchecked((int)fillMode), out nativePath);


                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(ptbuf);
                Marshal.FreeHGlobal(typebuf);
            }

            this.nativePath = nativePath;
        }

        public GraphicsPath(Point[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public GraphicsPath(Point[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            if (pts.Length != types.Length)
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            int count = types.Length;
            IntPtr ptbuf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);
            IntPtr typebuf = Marshal.AllocHGlobal(count);
            IntPtr nativePath = IntPtr.Zero;
            try
            {
                Marshal.Copy(types, 0, typebuf, count);

                int status = SafeNativeMethods.Gdip.GdipCreatePath2I(new HandleRef(null, ptbuf), new HandleRef(null, typebuf), count,
                                                      unchecked((int)fillMode), out nativePath);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(ptbuf);
                Marshal.FreeHGlobal(typebuf);
            }

            this.nativePath = nativePath;
        }

        public object Clone()
        {
            IntPtr clonedPath = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipClonePath(new HandleRef(this, nativePath), out clonedPath);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new GraphicsPath(clonedPath, 0);
        }

        private GraphicsPath(IntPtr nativePath, int extra)
        {
            if (nativePath == IntPtr.Zero)
                throw new ArgumentNullException("nativePath");

            this.nativePath = nativePath;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (nativePath != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeletePath(new HandleRef(this, nativePath));
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
                    nativePath = IntPtr.Zero;
                }
            }
        }

        ~GraphicsPath() => Dispose(false);

        public void Reset()
        {
            int status = SafeNativeMethods.Gdip.GdipResetPath(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public FillMode FillMode
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetPathFillMode(new HandleRef(this, nativePath), out int fillmode);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return (FillMode)fillmode;
            }
            set
            {
                if (value < FillMode.Alternate || value > FillMode.Winding)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(FillMode));
                }

                int status = SafeNativeMethods.Gdip.GdipSetPathFillMode(new HandleRef(this, nativePath), (int)value);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        private PathData _GetPathData()
        {
            int ptSize = Marshal.SizeOf(typeof(GPPOINTF));

            int numPts = PointCount;

            PathData pathData = new PathData() { Types = new byte[numPts] };

            IntPtr memoryPathData = Marshal.AllocHGlobal(3 * IntPtr.Size);
            IntPtr memoryPoints = Marshal.AllocHGlobal(checked(ptSize * numPts));
            try
            {
                GCHandle typesHandle = GCHandle.Alloc(pathData.Types, GCHandleType.Pinned);
                try
                {
                    IntPtr typesPtr = typesHandle.AddrOfPinnedObject();

                    Marshal.StructureToPtr(numPts, memoryPathData, false);
                    Marshal.StructureToPtr(memoryPoints, (IntPtr)((long)memoryPathData + IntPtr.Size), false);
                    Marshal.StructureToPtr(typesPtr, (IntPtr)((long)memoryPathData + 2 * IntPtr.Size), false);

                    int status = SafeNativeMethods.Gdip.GdipGetPathData(new HandleRef(this, nativePath), memoryPathData);

                    if (status != SafeNativeMethods.Gdip.Ok)
                    {
                        throw SafeNativeMethods.Gdip.StatusException(status);
                    }

                    pathData.Points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(memoryPoints, numPts);
                }
                finally
                {
                    typesHandle.Free();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memoryPathData);
                Marshal.FreeHGlobal(memoryPoints);
            }

            return pathData;
        }

        public PathData PathData => _GetPathData();

        public void StartFigure()
        {
            int status = SafeNativeMethods.Gdip.GdipStartPathFigure(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void CloseFigure()
        {
            int status = SafeNativeMethods.Gdip.GdipClosePathFigure(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void CloseAllFigures()
        {
            int status = SafeNativeMethods.Gdip.GdipClosePathFigures(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void SetMarkers()
        {
            int status = SafeNativeMethods.Gdip.GdipSetPathMarker(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void ClearMarkers()
        {
            int status = SafeNativeMethods.Gdip.GdipClearPathMarkers(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Reverse()
        {
            int status = SafeNativeMethods.Gdip.GdipReversePath(new HandleRef(this, nativePath));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public PointF GetLastPoint()
        {
            GPPOINTF gppt = new GPPOINTF();
            int status = SafeNativeMethods.Gdip.GdipGetPathLastPoint(new HandleRef(this, nativePath), gppt);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return gppt.ToPoint();
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);

        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics graphics) => IsVisible(new PointF(x, y), graphics);

        public bool IsVisible(PointF pt, Graphics graphics)
        {
            int status = SafeNativeMethods.Gdip.GdipIsVisiblePathPoint(new HandleRef(this, nativePath),
                                                        pt.X,
                                                        pt.Y,
                                                        new HandleRef(graphics, (graphics != null) ?
                                                            graphics.NativeGraphics : IntPtr.Zero),
                                                        out int isVisible);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y), null);

        public bool IsVisible(Point point) => IsVisible(point, null);

        public bool IsVisible(int x, int y, Graphics graphics) => IsVisible(new Point(x, y), graphics);

        public bool IsVisible(Point pt, Graphics graphics)
        {
            int status = SafeNativeMethods.Gdip.GdipIsVisiblePathPointI(new HandleRef(this, nativePath),
                                                         pt.X,
                                                         pt.Y,
                                                         new HandleRef(graphics, (graphics != null) ?
                                                             graphics.NativeGraphics : IntPtr.Zero),
                                                         out int isVisible);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isVisible != 0;
        }

        public bool IsOutlineVisible(float x, float y, Pen pen) => IsOutlineVisible(new PointF(x, y), pen, null);

        public bool IsOutlineVisible(PointF point, Pen pen) => IsOutlineVisible(point, pen, null);

        public bool IsOutlineVisible(float x, float y, Pen pen, Graphics graphics)
        {
            return IsOutlineVisible(new PointF(x, y), pen, graphics);
        }

        public bool IsOutlineVisible(PointF pt, Pen pen, Graphics graphics)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");

            int status = SafeNativeMethods.Gdip.GdipIsOutlineVisiblePathPoint(new HandleRef(this, nativePath),
                                                               pt.X,
                                                               pt.Y,
                                                               new HandleRef(pen, pen.NativePen),
                                                               new HandleRef(graphics, (graphics != null) ?
                                                                   graphics.NativeGraphics : IntPtr.Zero),
                                                               out int isVisible);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isVisible != 0;
        }

        public bool IsOutlineVisible(int x, int y, Pen pen) => IsOutlineVisible(new Point(x, y), pen, null);

        public bool IsOutlineVisible(Point point, Pen pen) => IsOutlineVisible(point, pen, null);

        public bool IsOutlineVisible(int x, int y, Pen pen, Graphics graphics)
        {
            return IsOutlineVisible(new Point(x, y), pen, graphics);
        }

        public bool IsOutlineVisible(Point pt, Pen pen, Graphics graphics)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");

            int status = SafeNativeMethods.Gdip.GdipIsOutlineVisiblePathPointI(new HandleRef(this, nativePath),
                                                                pt.X,
                                                                pt.Y,
                                                                new HandleRef(pen, pen.NativePen),
                                                                new HandleRef(graphics, (graphics != null) ?
                                                                    graphics.NativeGraphics : IntPtr.Zero),
                                                                out int isVisible);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isVisible != 0;
        }

        public void AddLine(PointF pt1, PointF pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathLine(new HandleRef(this, nativePath), x1, y1, x2, y2);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddLines(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathLine2(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddLine(Point pt1, Point pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(int x1, int y1, int x2, int y2)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathLineI(new HandleRef(this, nativePath), x1, y1, x2, y2);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddLines(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathLine2I(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
        {
            AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddArc(float x, float y, float width, float height,
                           float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathArc(new HandleRef(this, nativePath), x, y, width, height,
                                                startAngle, sweepAngle);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddArc(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddArc(int x, int y, int width, int height,
                           float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathArcI(new HandleRef(this, nativePath), x, y, width, height,
                                                 startAngle, sweepAngle);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            AddBezier(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void AddBezier(float x1, float y1, float x2, float y2,
                              float x3, float y3, float x4, float y4)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathBezier(new HandleRef(this, nativePath), x1, y1, x2, y2,
                                                   x3, y3, x4, y4);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddBeziers(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathBeziers(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddBezier(Point pt1, Point pt2, Point pt3, Point pt4)
        {
            AddBezier(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void AddBezier(int x1, int y1, int x2, int y2,
                              int x3, int y3, int x4, int y4)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathBezierI(new HandleRef(this, nativePath), x1, y1, x2, y2,
                                                    x3, y3, x4, y4);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddBeziers(params Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathBeziersI(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /*
         * Add cardinal splines to the path object
         */
        public void AddCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathCurve(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathCurve2(new HandleRef(this, nativePath), new HandleRef(null, buf),
                                                   points.Length, tension);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathCurve3(new HandleRef(this, nativePath), new HandleRef(null, buf),
                                                   points.Length, offset,
                                                   numberOfSegments, tension);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathCurveI(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathCurve2I(new HandleRef(this, nativePath), new HandleRef(null, buf),
                                                    points.Length, tension);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathCurve3I(new HandleRef(this, nativePath), new HandleRef(null, buf),
                                                    points.Length, offset,
                                                    numberOfSegments, tension);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurve(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurve2(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length, tension);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurveI(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurve2I(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length, tension);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddRectangle(RectangleF rect)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathRectangle(new HandleRef(this, nativePath), rect.X, rect.Y,
                                                      rect.Width, rect.Height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddRectangles(RectangleF[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException("rects");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertRectangleToMemory(rects);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathRectangles(new HandleRef(this, nativePath), new HandleRef(null, buf), rects.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddRectangle(Rectangle rect)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathRectangleI(new HandleRef(this, nativePath), rect.X, rect.Y,
                                                       rect.Width, rect.Height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddRectangles(Rectangle[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException("rects");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertRectangleToMemory(rects);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathRectanglesI(new HandleRef(this, nativePath), new HandleRef(null, buf), rects.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddEllipse(RectangleF rect)
        {
            AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void AddEllipse(float x, float y, float width, float height)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathEllipse(new HandleRef(this, nativePath), x, y, width, height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddEllipse(Rectangle rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);

        public void AddEllipse(int x, int y, int width, int height)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathEllipseI(new HandleRef(this, nativePath), x, y, width, height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddPie(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddPie(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddPie(float x, float y, float width, float height,
                           float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathPie(new HandleRef(this, nativePath), x, y, width, height,
                                                startAngle, sweepAngle);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddPie(int x, int y, int width, int height,
                           float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathPieI(new HandleRef(this, nativePath), x, y, width, height,
                                                 startAngle, sweepAngle);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddPolygon(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathPolygon(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        // int version
        /// <include file='doc\GraphicsPath.uex' path='docs/doc[@for="GraphicsPath.AddPolygon1"]/*' />
        /// <devdoc>
        ///    Adds a polygon to the current figure.
        /// </devdoc>
        public void AddPolygon(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipAddPathPolygonI(new HandleRef(this, nativePath), new HandleRef(null, buf), points.Length);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddPath(GraphicsPath addingPath, bool connect)
        {
            if (addingPath == null)
                throw new ArgumentNullException("addingPath");

            int status = SafeNativeMethods.Gdip.GdipAddPathPath(new HandleRef(this, nativePath), new HandleRef(addingPath, addingPath.nativePath), connect);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize,
                              PointF origin, StringFormat format)
        {
            GPRECTF rectf = new GPRECTF(origin.X, origin.Y, 0, 0);

            int status = SafeNativeMethods.Gdip.GdipAddPathString(new HandleRef(this, nativePath),
                                                   s,
                                                   s.Length,
                                                   new HandleRef(family, (family != null) ? family.NativeFamily : IntPtr.Zero),
                                                   style,
                                                   emSize,
                                                   ref rectf,
                                                   new HandleRef(format, (format != null) ? format.nativeFormat : IntPtr.Zero));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize,
                              Point origin, StringFormat format)
        {
            var rect = new GPRECT(origin.X, origin.Y, 0, 0);

            int status = SafeNativeMethods.Gdip.GdipAddPathStringI(new HandleRef(this, nativePath),
                                                    s,
                                                    s.Length,
                                                    new HandleRef(family, (family != null) ? family.NativeFamily : IntPtr.Zero),
                                                    style,
                                                    emSize,
                                                    ref rect,
                                                    new HandleRef(format, (format != null) ? format.nativeFormat : IntPtr.Zero));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize,
                              RectangleF layoutRect, StringFormat format)
        {
            GPRECTF rectf = new GPRECTF(layoutRect);
            int status = SafeNativeMethods.Gdip.GdipAddPathString(new HandleRef(this, nativePath),
                                                   s,
                                                   s.Length,
                                                   new HandleRef(family, (family != null) ? family.NativeFamily : IntPtr.Zero),
                                                   style,
                                                   emSize,
                                                   ref rectf,
                                                   new HandleRef(format, (format != null) ? format.nativeFormat : IntPtr.Zero));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize,
                              Rectangle layoutRect, StringFormat format)
        {
            GPRECT rect = new GPRECT(layoutRect);
            int status = SafeNativeMethods.Gdip.GdipAddPathStringI(new HandleRef(this, nativePath),
                                                   s,
                                                   s.Length,
                                                   new HandleRef(family, (family != null) ? family.NativeFamily : IntPtr.Zero),
                                                   style,
                                                   emSize,
                                                   ref rect,
                                                   new HandleRef(format, (format != null) ? format.nativeFormat : IntPtr.Zero));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            if (matrix.nativeMatrix == IntPtr.Zero)
                return;

            int status = SafeNativeMethods.Gdip.GdipTransformPath(new HandleRef(this, nativePath),
                                                   new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public RectangleF GetBounds() => GetBounds(null);   

        public RectangleF GetBounds(Matrix matrix) => GetBounds(matrix, null);

        public RectangleF GetBounds(Matrix matrix, Pen pen)
        {
            GPRECTF gprectf = new GPRECTF();

            IntPtr nativeMatrix = IntPtr.Zero, nativePen = IntPtr.Zero;

            if (matrix != null)
                nativeMatrix = matrix.nativeMatrix;

            if (pen != null)
                nativePen = pen.NativePen;

            int status = SafeNativeMethods.Gdip.GdipGetPathWorldBounds(new HandleRef(this, nativePath),
                                                        ref gprectf,
                                                        new HandleRef(matrix, nativeMatrix),
                                                        new HandleRef(pen, nativePen));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return gprectf.ToRectangleF();
        }

        public void Flatten() => Flatten(null);

        public void Flatten(Matrix matrix) => Flatten(matrix, 0.25f);

        public void Flatten(Matrix matrix, float flatness)
        {
            int status = SafeNativeMethods.Gdip.GdipFlattenPath(new HandleRef(this, nativePath),
                                                           new HandleRef(matrix, (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix),
                                                           flatness);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Widen(Pen pen)
        {
            float flatness = (float)2.0 / (float)3.0;
            Widen(pen, null, flatness);
        }

        public void Widen(Pen pen, Matrix matrix)
        {
            float flatness = (float)2.0 / (float)3.0;
            Widen(pen, matrix, flatness);
        }

        public void Widen(Pen pen, Matrix matrix, float flatness)
        {
            IntPtr nativeMatrix;

            if (matrix == null)
                nativeMatrix = IntPtr.Zero;
            else
                nativeMatrix = matrix.nativeMatrix;

            if (pen == null)
                throw new ArgumentNullException("pen");

            // GDI+ wrongly returns an out of memory status 
            // when there is nothing in the path, so we have to check 
            // before calling the widen method and do nothing if we dont have
            // anything in the path
            SafeNativeMethods.Gdip.GdipGetPointCount(new HandleRef(this, nativePath), out int pointCount);

            if (pointCount == 0)
                return;

            int status = SafeNativeMethods.Gdip.GdipWidenPath(new HandleRef(this, nativePath),
                                new HandleRef(pen, pen.NativePen),
                                new HandleRef(matrix, nativeMatrix),
                                flatness);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Warp(PointF[] destPoints, RectangleF srcRect) => Warp(destPoints, srcRect, null);

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix matrix) => Warp(destPoints, srcRect, matrix, WarpMode.Perspective);
        
        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix matrix, WarpMode warpMode)
        {
            Warp(destPoints, srcRect, matrix, warpMode, 0.25f);
        }

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix matrix, WarpMode warpMode, float flatness)
        {
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(destPoints);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipWarpPath(new HandleRef(this, nativePath),
                                              new HandleRef(matrix, (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix),
                                              new HandleRef(null, buf),
                                              destPoints.Length,
                                              srcRect.X,
                                              srcRect.Y,
                                              srcRect.Width,
                                              srcRect.Height,
                                              warpMode,
                                              flatness);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public int PointCount
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetPointCount(new HandleRef(this, nativePath), out int count);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return count;
            }
        }
        public byte[] PathTypes
        {
            get
            {
                int count = PointCount;
                byte[] types = new byte[count];

                int status = SafeNativeMethods.Gdip.GdipGetPathTypes(new HandleRef(this, nativePath), types, count);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return types;
            }
        }
        public PointF[] PathPoints
        {
            get
            {
                int count = PointCount;
                int size = Marshal.SizeOf(typeof(GPPOINTF));
                IntPtr buf = Marshal.AllocHGlobal(checked(count * size));
                try
                {
                    int status = SafeNativeMethods.Gdip.GdipGetPathPoints(new HandleRef(this, nativePath), new HandleRef(null, buf), count);

                    if (status != SafeNativeMethods.Gdip.Ok)
                    {
                        throw SafeNativeMethods.Gdip.StatusException(status);
                    }

                    PointF[] points = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(buf, count);
                    return points;
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }
    }
}

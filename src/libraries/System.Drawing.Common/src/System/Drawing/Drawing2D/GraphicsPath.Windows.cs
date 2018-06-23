// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Internal;
using System.Globalization;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class GraphicsPath : MarshalByRefObject, ICloneable, IDisposable
    {
        internal IntPtr _nativePath;

        public GraphicsPath() : this(FillMode.Alternate) { }

        public GraphicsPath(FillMode fillMode)
        {
            int status = Gdip.GdipCreatePath(unchecked((int)fillMode), out IntPtr nativePath);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            _nativePath = nativePath;
        }

        public GraphicsPath(PointF[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public GraphicsPath(PointF[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));
            if (pts.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            int count = types.Length;
            IntPtr ptbuf = Gdip.ConvertPointToMemory(pts);
            IntPtr typebuf = Marshal.AllocHGlobal(count);
            try
            {
                Marshal.Copy(types, 0, typebuf, count);

                int status = Gdip.GdipCreatePath2(
                    new HandleRef(null, ptbuf),
                    new HandleRef(null, typebuf),
                    count,
                    unchecked((int)fillMode),
                    out IntPtr nativePath);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
                _nativePath = nativePath;
            }
            finally
            {
                Marshal.FreeHGlobal(ptbuf);
                Marshal.FreeHGlobal(typebuf);
            }
        }

        public GraphicsPath(Point[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public GraphicsPath(Point[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            if (pts.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            int count = types.Length;
            IntPtr ptbuf = Gdip.ConvertPointToMemory(pts);
            IntPtr typebuf = Marshal.AllocHGlobal(count);
            try
            {
                Marshal.Copy(types, 0, typebuf, count);

                int status = Gdip.GdipCreatePath2I(
                    new HandleRef(null, ptbuf),
                    new HandleRef(null, typebuf),
                    count,
                    unchecked((int)fillMode),
                    out IntPtr nativePath);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                _nativePath = nativePath;
            }
            finally
            {
                Marshal.FreeHGlobal(ptbuf);
                Marshal.FreeHGlobal(typebuf);
            }
        }

        public object Clone()
        {
            int status = Gdip.GdipClonePath(new HandleRef(this, _nativePath), out IntPtr clonedPath);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return new GraphicsPath(clonedPath, 0);
        }

        private GraphicsPath(IntPtr nativePath, int extra)
        {
            if (nativePath == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePath));

            _nativePath = nativePath;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_nativePath != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    Gdip.GdipDeletePath(new HandleRef(this, _nativePath));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
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
                    _nativePath = IntPtr.Zero;
                }
            }
        }

        ~GraphicsPath() => Dispose(false);

        public void Reset()
        {
            int status = Gdip.GdipResetPath(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public FillMode FillMode
        {
            get
            {
                int status = Gdip.GdipGetPathFillMode(new HandleRef(this, _nativePath), out int fillmode);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return (FillMode)fillmode;
            }
            set
            {
                if (value < FillMode.Alternate || value > FillMode.Winding)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(FillMode));
                }

                int status = Gdip.GdipSetPathFillMode(new HandleRef(this, _nativePath), (int)value);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        private unsafe PathData _GetPathData()
        {
            int count = PointCount;

            PathData pathData = new PathData()
            {
                Types = new byte[count],
                Points = new PointF[count]
            };

            if (count == 0)
                return pathData;

            fixed (byte* t = pathData.Types)
            fixed (PointF* p = pathData.Points)
            {
                GpPathData data = new GpPathData
                {
                    Count = count,
                    Points = p,
                    Types = t
                };

                int status = Gdip.GdipGetPathData(new HandleRef(this, _nativePath), &data);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
                }
            }

            return pathData;
        }

        public PathData PathData => _GetPathData();

        public void StartFigure()
        {
            int status = Gdip.GdipStartPathFigure(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void CloseFigure()
        {
            int status = Gdip.GdipClosePathFigure(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void CloseAllFigures()
        {
            int status = Gdip.GdipClosePathFigures(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void SetMarkers()
        {
            int status = Gdip.GdipSetPathMarker(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ClearMarkers()
        {
            int status = Gdip.GdipClearPathMarkers(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void Reverse()
        {
            int status = Gdip.GdipReversePath(new HandleRef(this, _nativePath));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public PointF GetLastPoint()
        {
            GPPOINTF gppt = new GPPOINTF();
            int status = Gdip.GdipGetPathLastPoint(new HandleRef(this, _nativePath), gppt);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return gppt.ToPoint();
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);

        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics graphics) => IsVisible(new PointF(x, y), graphics);

        public bool IsVisible(PointF pt, Graphics graphics)
        {
            int status = Gdip.GdipIsVisiblePathPoint(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(graphics, (graphics != null) ? graphics.NativeGraphics : IntPtr.Zero),
                out int isVisible);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y), null);

        public bool IsVisible(Point point) => IsVisible(point, null);

        public bool IsVisible(int x, int y, Graphics graphics) => IsVisible(new Point(x, y), graphics);

        public bool IsVisible(Point pt, Graphics graphics)
        {
            int status = Gdip.GdipIsVisiblePathPointI(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(graphics, (graphics != null) ? graphics.NativeGraphics : IntPtr.Zero),
                out int isVisible);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

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
                throw new ArgumentNullException(nameof(pen));

            int status = Gdip.GdipIsOutlineVisiblePathPoint(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(pen, pen.NativePen),
                new HandleRef(graphics, (graphics != null) ? graphics.NativeGraphics : IntPtr.Zero),
                out int isVisible);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

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
                throw new ArgumentNullException(nameof(pen));

            int status = Gdip.GdipIsOutlineVisiblePathPointI(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(pen, pen.NativePen),
                new HandleRef(graphics, (graphics != null) ? graphics.NativeGraphics : IntPtr.Zero),
                out int isVisible);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return isVisible != 0;
        }

        public void AddLine(PointF pt1, PointF pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            int status = Gdip.GdipAddPathLine(new HandleRef(this, _nativePath), x1, y1, x2, y2);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddLines(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathLine2(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddLine(Point pt1, Point pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(int x1, int y1, int x2, int y2)
        {
            int status = Gdip.GdipAddPathLineI(new HandleRef(this, _nativePath), x1, y1, x2, y2);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddLines(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathLine2I(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
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

        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            int status = Gdip.GdipAddPathArc(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddArc(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddArc(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            int status = Gdip.GdipAddPathArcI(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            AddBezier(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void AddBezier(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            int status = Gdip.GdipAddPathBezier(
                new HandleRef(this, _nativePath),
                x1, y1, x2, y2, x3, y3, x4, y4);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddBeziers(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathBeziers(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
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

        public void AddBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            int status = Gdip.GdipAddPathBezierI(
                new HandleRef(this, _nativePath),
                x1, y1, x2, y2, x3, y3, x4, y4);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddBeziers(params Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathBeziersI(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <summary>
        /// Add cardinal splines to the path object
        /// </summary>
        public void AddCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathCurve(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathCurve2(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length,
                    tension);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathCurve3(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length,
                    offset,
                    numberOfSegments,
                    tension);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathCurveI(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathCurve2I(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length,
                    tension);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddCurve(Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathCurve3I(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length,
                    offset,
                    numberOfSegments,
                    tension);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathClosedCurve(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathClosedCurve2(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length,
                    tension);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathClosedCurveI(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddClosedCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathClosedCurve2I(
                    new HandleRef(this, _nativePath),
                    new HandleRef(null, buf),
                    points.Length,
                    tension);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddRectangle(RectangleF rect)
        {
            int status = Gdip.GdipAddPathRectangle(
                new HandleRef(this, _nativePath),
                rect.X, rect.Y, rect.Width, rect.Height);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddRectangles(RectangleF[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            IntPtr buf = Gdip.ConvertRectangleToMemory(rects);
            try
            {
                int status = Gdip.GdipAddPathRectangles(new HandleRef(this, _nativePath), new HandleRef(null, buf), rects.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddRectangle(Rectangle rect)
        {
            int status = Gdip.GdipAddPathRectangleI(
                new HandleRef(this, _nativePath),
                rect.X, rect.Y, rect.Width, rect.Height);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddRectangles(Rectangle[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            IntPtr buf = Gdip.ConvertRectangleToMemory(rects);
            try
            {
                int status = Gdip.GdipAddPathRectanglesI(new HandleRef(this, _nativePath), new HandleRef(null, buf), rects.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
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
            int status = Gdip.GdipAddPathEllipse(new HandleRef(this, _nativePath), x, y, width, height);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddEllipse(Rectangle rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);

        public void AddEllipse(int x, int y, int width, int height)
        {
            int status = Gdip.GdipAddPathEllipseI(new HandleRef(this, _nativePath), x, y, width, height);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddPie(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddPie(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            int status = Gdip.GdipAddPathPie(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddPie(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            int status = Gdip.GdipAddPathPieI(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddPolygon(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathPolygon(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <summary>
        /// Adds a polygon to the current figure.
        /// </summary>
        public void AddPolygon(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            IntPtr buf = Gdip.ConvertPointToMemory(points);
            try
            {
                int status = Gdip.GdipAddPathPolygonI(new HandleRef(this, _nativePath), new HandleRef(null, buf), points.Length);
                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void AddPath(GraphicsPath addingPath, bool connect)
        {
            if (addingPath == null)
                throw new ArgumentNullException(nameof(addingPath));

            int status = Gdip.GdipAddPathPath(new HandleRef(this, _nativePath), new HandleRef(addingPath, addingPath._nativePath), connect);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize, PointF origin, StringFormat format)
        {
            GPRECTF rectf = new GPRECTF(origin.X, origin.Y, 0, 0);

            int status = Gdip.GdipAddPathString(
                new HandleRef(this, _nativePath),
                s,
                s.Length,
                new HandleRef(family, family?.NativeFamily ?? IntPtr.Zero),
                style,
                emSize,
                ref rectf,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize, Point origin, StringFormat format)
        {
            var rect = new GPRECT(origin.X, origin.Y, 0, 0);

            int status = Gdip.GdipAddPathStringI(
                new HandleRef(this, _nativePath),
                s,
                s.Length,
                new HandleRef(family, family?.NativeFamily ?? IntPtr.Zero),
                style,
                emSize,
                ref rect,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize, RectangleF layoutRect, StringFormat format)
        {
            GPRECTF rectf = new GPRECTF(layoutRect);
            int status = Gdip.GdipAddPathString(
                new HandleRef(this, _nativePath),
                s,
                s.Length,
                new HandleRef(family, family?.NativeFamily ?? IntPtr.Zero),
                style,
                emSize,
                ref rectf,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void AddString(string s, FontFamily family, int style, float emSize, Rectangle layoutRect, StringFormat format)
        {
            GPRECT rect = new GPRECT(layoutRect);
            int status = Gdip.GdipAddPathStringI(
                new HandleRef(this, _nativePath),
                s,
                s.Length,
                new HandleRef(family, family?.NativeFamily ?? IntPtr.Zero),
                style,
                emSize,
                ref rect,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (matrix.nativeMatrix == IntPtr.Zero)
                return;

            int status = Gdip.GdipTransformPath(
                new HandleRef(this, _nativePath),
                new HandleRef(matrix, matrix.nativeMatrix));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public RectangleF GetBounds() => GetBounds(null);

        public RectangleF GetBounds(Matrix matrix) => GetBounds(matrix, null);

        public RectangleF GetBounds(Matrix matrix, Pen pen)
        {
            GPRECTF gprectf = new GPRECTF();

            int status = Gdip.GdipGetPathWorldBounds(
                new HandleRef(this, _nativePath),
                ref gprectf,
                new HandleRef(matrix, matrix?.nativeMatrix ?? IntPtr.Zero),
                new HandleRef(pen, pen?.NativePen ?? IntPtr.Zero));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return gprectf.ToRectangleF();
        }

        public void Flatten() => Flatten(null);

        public void Flatten(Matrix matrix) => Flatten(matrix, 0.25f);

        public void Flatten(Matrix matrix, float flatness)
        {
            int status = Gdip.GdipFlattenPath(
                new HandleRef(this, _nativePath),
                new HandleRef(matrix, matrix?.nativeMatrix ?? IntPtr.Zero),
                flatness);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void Widen(Pen pen)
        {
            const float flatness = (float)2.0 / (float)3.0;
            Widen(pen, null, flatness);
        }

        public void Widen(Pen pen, Matrix matrix)
        {
            const float flatness = (float)2.0 / (float)3.0;
            Widen(pen, matrix, flatness);
        }

        public void Widen(Pen pen, Matrix matrix, float flatness)
        {
            IntPtr nativeMatrix = matrix?.nativeMatrix ?? IntPtr.Zero;

            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            // GDI+ wrongly returns an out of memory status 
            // when there is nothing in the path, so we have to check 
            // before calling the widen method and do nothing if we dont have
            // anything in the path
            Gdip.GdipGetPointCount(new HandleRef(this, _nativePath), out int pointCount);

            if (pointCount == 0)
                return;

            int status = Gdip.GdipWidenPath(
                new HandleRef(this, _nativePath),
                new HandleRef(pen, pen.NativePen),
                new HandleRef(matrix, nativeMatrix),
                flatness);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
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
                throw new ArgumentNullException(nameof(destPoints));

            IntPtr buf = Gdip.ConvertPointToMemory(destPoints);
            try
            {
                int status = Gdip.GdipWarpPath(
                    new HandleRef(this, _nativePath),
                    new HandleRef(matrix, (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix),
                    new HandleRef(null, buf),
                    destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    warpMode,
                    flatness);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
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
                int status = Gdip.GdipGetPointCount(new HandleRef(this, _nativePath), out int count);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return count;
            }
        }

        public byte[] PathTypes
        {
            get
            {
                int count = PointCount;
                byte[] types = new byte[count];

                int status = Gdip.GdipGetPathTypes(new HandleRef(this, _nativePath), types, count);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return types;
            }
        }

        public unsafe PointF[] PathPoints
        {
            get
            {
                PointF[] points = new PointF[PointCount];
                fixed (PointF* p = points)
                {
                    int status = Gdip.GdipGetPathPoints(new HandleRef(this, _nativePath), p, points.Length);

                    if (status != Gdip.Ok)
                    {
                        throw Gdip.StatusException(status);
                    }
                }
                return points;
            }
        }
    }
}

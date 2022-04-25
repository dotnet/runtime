// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private const float Flatness = (float)2.0 / (float)3.0;

        public GraphicsPath() : this(FillMode.Alternate) { }

        public GraphicsPath(FillMode fillMode)
        {
            Gdip.CheckStatus(Gdip.GdipCreatePath(unchecked((int)fillMode), out IntPtr nativePath));
            _nativePath = nativePath;
        }

        public GraphicsPath(PointF[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public unsafe GraphicsPath(PointF[] pts, byte[] types, FillMode fillMode)
        {
            ArgumentNullException.ThrowIfNull(pts);

            if (pts.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (PointF* p = pts)
            fixed (byte* t = types)
            {
                Gdip.CheckStatus(Gdip.GdipCreatePath2(
                    p, t, types.Length, (int)fillMode, out IntPtr nativePath));

                _nativePath = nativePath;
            }
        }

        public GraphicsPath(Point[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public unsafe GraphicsPath(Point[] pts, byte[] types, FillMode fillMode)
        {
            ArgumentNullException.ThrowIfNull(pts);

            if (pts.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (byte* t = types)
            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipCreatePath2I(
                    p, t, types.Length, unchecked((int)fillMode), out IntPtr nativePath));

                _nativePath = nativePath;
            }
        }

        public object Clone()
        {
            Gdip.CheckStatus(Gdip.GdipClonePath(new HandleRef(this, _nativePath), out IntPtr clonedPath));

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
                    int status = !Gdip.Initialized ? Gdip.Ok :
#endif
                    Gdip.GdipDeletePath(new HandleRef(this, _nativePath));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, $"GDI+ returned an error status: {status.ToString(CultureInfo.InvariantCulture)}");
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
            Gdip.CheckStatus(Gdip.GdipResetPath(new HandleRef(this, _nativePath)));
        }

        public FillMode FillMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPathFillMode(new HandleRef(this, _nativePath), out FillMode fillmode));
                return fillmode;
            }
            set
            {
                if (value < FillMode.Alternate || value > FillMode.Winding)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(FillMode));

                Gdip.CheckStatus(Gdip.GdipSetPathFillMode(new HandleRef(this, _nativePath), value));
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

                Gdip.CheckStatus(Gdip.GdipGetPathData(new HandleRef(this, _nativePath), &data));
            }

            return pathData;
        }

        public PathData PathData => _GetPathData();

        public void StartFigure()
        {
            Gdip.CheckStatus(Gdip.GdipStartPathFigure(new HandleRef(this, _nativePath)));
        }

        public void CloseFigure()
        {
            Gdip.CheckStatus(Gdip.GdipClosePathFigure(new HandleRef(this, _nativePath)));
        }

        public void CloseAllFigures()
        {
            Gdip.CheckStatus(Gdip.GdipClosePathFigures(new HandleRef(this, _nativePath)));
        }

        public void SetMarkers()
        {
            Gdip.CheckStatus(Gdip.GdipSetPathMarker(new HandleRef(this, _nativePath)));
        }

        public void ClearMarkers()
        {
            Gdip.CheckStatus(Gdip.GdipClearPathMarkers(new HandleRef(this, _nativePath)));
        }

        public void Reverse()
        {
            Gdip.CheckStatus(Gdip.GdipReversePath(new HandleRef(this, _nativePath)));
        }

        public PointF GetLastPoint()
        {
            Gdip.CheckStatus(Gdip.GdipGetPathLastPoint(new HandleRef(this, _nativePath), out PointF point));
            return point;
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);

        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics? graphics) => IsVisible(new PointF(x, y), graphics);

        public bool IsVisible(PointF pt, Graphics? graphics)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePathPoint(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(graphics, graphics?.NativeGraphics ?? IntPtr.Zero),
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y), null);

        public bool IsVisible(Point point) => IsVisible(point, null);

        public bool IsVisible(int x, int y, Graphics? graphics) => IsVisible(new Point(x, y), graphics);

        public bool IsVisible(Point pt, Graphics? graphics)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePathPointI(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(graphics, graphics?.NativeGraphics ?? IntPtr.Zero),
                out bool isVisible));

            return isVisible;
        }

        public bool IsOutlineVisible(float x, float y, Pen pen) => IsOutlineVisible(new PointF(x, y), pen, null);

        public bool IsOutlineVisible(PointF point, Pen pen) => IsOutlineVisible(point, pen, null);

        public bool IsOutlineVisible(float x, float y, Pen pen, Graphics? graphics)
        {
            return IsOutlineVisible(new PointF(x, y), pen, graphics);
        }

        public bool IsOutlineVisible(PointF pt, Pen pen, Graphics? graphics)
        {
            ArgumentNullException.ThrowIfNull(pen);

            Gdip.CheckStatus(Gdip.GdipIsOutlineVisiblePathPoint(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(pen, pen.NativePen),
                new HandleRef(graphics, graphics?.NativeGraphics ?? IntPtr.Zero),
                out bool isVisible));

            return isVisible;
        }

        public bool IsOutlineVisible(int x, int y, Pen pen) => IsOutlineVisible(new Point(x, y), pen, null);

        public bool IsOutlineVisible(Point point, Pen pen) => IsOutlineVisible(point, pen, null);

        public bool IsOutlineVisible(int x, int y, Pen pen, Graphics? graphics) => IsOutlineVisible(new Point(x, y), pen, graphics);

        public bool IsOutlineVisible(Point pt, Pen pen, Graphics? graphics)
        {
            ArgumentNullException.ThrowIfNull(pen);

            Gdip.CheckStatus(Gdip.GdipIsOutlineVisiblePathPointI(
                new HandleRef(this, _nativePath),
                pt.X, pt.Y,
                new HandleRef(pen, pen.NativePen),
                new HandleRef(graphics, graphics?.NativeGraphics ?? IntPtr.Zero),
                out bool isVisible));

            return isVisible;
        }

        public void AddLine(PointF pt1, PointF pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathLine(new HandleRef(this, _nativePath), x1, y1, x2, y2));
        }

        public unsafe void AddLines(PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            if (points.Length == 0)
                throw new ArgumentException(null, nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathLine2(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public void AddLine(Point pt1, Point pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(int x1, int y1, int x2, int y2)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathLineI(new HandleRef(this, _nativePath), x1, y1, x2, y2));
        }

        public unsafe void AddLines(Point[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            if (points.Length == 0)
                throw new ArgumentException(null, nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathLine2I(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
        {
            AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathArc(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        public void AddArc(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddArc(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathArcI(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            AddBezier(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void AddBezier(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathBezier(
                new HandleRef(this, _nativePath),
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        public unsafe void AddBeziers(PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathBeziers(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public void AddBezier(Point pt1, Point pt2, Point pt3, Point pt4)
        {
            AddBezier(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void AddBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathBezierI(
                new HandleRef(this, _nativePath),
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        public unsafe void AddBeziers(params Point[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            if (points.Length == 0)
                return;

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathBeziersI(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        /// <summary>
        /// Add cardinal splines to the path object
        /// </summary>
        public unsafe void AddCurve(PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public unsafe void AddCurve(PointF[] points, float tension)
        {
            ArgumentNullException.ThrowIfNull(points);

            if (points.Length == 0)
                return;

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve2(new HandleRef(this, _nativePath), p, points.Length, tension));
            }
        }

        public unsafe void AddCurve(PointF[] points, int offset, int numberOfSegments, float tension)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve3(
                    new HandleRef(this, _nativePath), p, points.Length, offset, numberOfSegments, tension));
            }
        }

        public unsafe void AddCurve(Point[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurveI(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public unsafe void AddCurve(Point[] points, float tension)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve2I(
                    new HandleRef(this, _nativePath), p, points.Length, tension));
            }
        }

        public unsafe void AddCurve(Point[] points, int offset, int numberOfSegments, float tension)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve3I(
                    new HandleRef(this, _nativePath), p, points.Length, offset, numberOfSegments, tension));
            }
        }

        public unsafe void AddClosedCurve(PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurve(
                    new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public unsafe void AddClosedCurve(PointF[] points, float tension)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurve2(new HandleRef(this, _nativePath), p, points.Length, tension));
            }
        }

        public unsafe void AddClosedCurve(Point[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurveI(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public unsafe void AddClosedCurve(Point[] points, float tension)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurve2I(new HandleRef(this, _nativePath), p, points.Length, tension));
            }
        }

        public void AddRectangle(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathRectangle(
                new HandleRef(this, _nativePath),
                rect.X, rect.Y, rect.Width, rect.Height));
        }

        public unsafe void AddRectangles(RectangleF[] rects)
        {
            ArgumentNullException.ThrowIfNull(rects);

            if (rects.Length == 0)
                throw new ArgumentException(null, nameof(rects));

            fixed (RectangleF* r = rects)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathRectangles(
                    new HandleRef(this, _nativePath), r, rects.Length));
            }
        }

        public void AddRectangle(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathRectangleI(
                new HandleRef(this, _nativePath),
                rect.X, rect.Y, rect.Width, rect.Height));
        }

        public unsafe void AddRectangles(Rectangle[] rects)
        {
            ArgumentNullException.ThrowIfNull(rects);

            if (rects.Length == 0)
                throw new ArgumentException(null, nameof(rects));

            fixed (Rectangle* r = rects)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathRectanglesI(
                    new HandleRef(this, _nativePath), r, rects.Length));
            }
        }

        public void AddEllipse(RectangleF rect)
        {
            AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void AddEllipse(float x, float y, float width, float height)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathEllipse(new HandleRef(this, _nativePath), x, y, width, height));
        }

        public void AddEllipse(Rectangle rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);

        public void AddEllipse(int x, int y, int width, int height)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathEllipseI(new HandleRef(this, _nativePath), x, y, width, height));
        }

        public void AddPie(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddPie(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathPie(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        public void AddPie(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathPieI(
                new HandleRef(this, _nativePath),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        public unsafe void AddPolygon(PointF[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathPolygon(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        /// <summary>
        /// Adds a polygon to the current figure.
        /// </summary>
        public unsafe void AddPolygon(Point[] points)
        {
            ArgumentNullException.ThrowIfNull(points);

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathPolygonI(new HandleRef(this, _nativePath), p, points.Length));
            }
        }

        public void AddPath(GraphicsPath addingPath, bool connect)
        {
            ArgumentNullException.ThrowIfNull(addingPath);

            Gdip.CheckStatus(Gdip.GdipAddPathPath(
                new HandleRef(this, _nativePath), new HandleRef(addingPath, addingPath._nativePath), connect));
        }

        public void AddString(string s, FontFamily family, int style, float emSize, PointF origin, StringFormat? format)
        {
            AddString(s, family, style, emSize, new RectangleF(origin.X, origin.Y, 0, 0), format);
        }

        public void AddString(string s, FontFamily family, int style, float emSize, Point origin, StringFormat? format)
        {
            AddString(s, family, style, emSize, new Rectangle(origin.X, origin.Y, 0, 0), format);
        }

        public void AddString(string s, FontFamily family, int style, float emSize, RectangleF layoutRect, StringFormat? format)
        {
            ArgumentNullException.ThrowIfNull(family);

            Gdip.CheckStatus(Gdip.GdipAddPathString(
                new HandleRef(this, _nativePath),
                s,
                s.Length,
                new HandleRef(family, family?.NativeFamily ?? IntPtr.Zero),
                style,
                emSize,
                ref layoutRect,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero)));
        }

        public void AddString(string s, FontFamily family, int style, float emSize, Rectangle layoutRect, StringFormat? format)
        {
            ArgumentNullException.ThrowIfNull(family);

            Gdip.CheckStatus(Gdip.GdipAddPathStringI(
                new HandleRef(this, _nativePath),
                s,
                s.Length,
                new HandleRef(family, family?.NativeFamily ?? IntPtr.Zero),
                style,
                emSize,
                ref layoutRect,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero)));
        }

        public void Transform(Matrix matrix)
        {
            ArgumentNullException.ThrowIfNull(matrix);

            if (matrix.NativeMatrix == IntPtr.Zero)
                return;

            Gdip.CheckStatus(Gdip.GdipTransformPath(
                new HandleRef(this, _nativePath),
                new HandleRef(matrix, matrix.NativeMatrix)));
        }

        public RectangleF GetBounds() => GetBounds(null);

        public RectangleF GetBounds(Matrix? matrix) => GetBounds(matrix, null);

        public RectangleF GetBounds(Matrix? matrix, Pen? pen)
        {
            Gdip.CheckStatus(Gdip.GdipGetPathWorldBounds(
                new HandleRef(this, _nativePath),
                out RectangleF bounds,
                new HandleRef(matrix, matrix?.NativeMatrix ?? IntPtr.Zero),
                new HandleRef(pen, pen?.NativePen ?? IntPtr.Zero)));

            return bounds;
        }

        public void Flatten() => Flatten(null);

        public void Flatten(Matrix? matrix) => Flatten(matrix, 0.25f);

        public void Flatten(Matrix? matrix, float flatness)
        {
            Gdip.CheckStatus(Gdip.GdipFlattenPath(
                new HandleRef(this, _nativePath),
                new HandleRef(matrix, matrix?.NativeMatrix ?? IntPtr.Zero),
                flatness));
        }

        public void Widen(Pen pen) => Widen(pen, null, Flatness);

        public void Widen(Pen pen, Matrix? matrix) => Widen(pen, matrix, Flatness);

        public void Widen(Pen pen, Matrix? matrix, float flatness)
        {
            ArgumentNullException.ThrowIfNull(pen);

            // GDI+ wrongly returns an out of memory status when there is nothing in the path, so we have to check
            // before calling the widen method and do nothing if we dont have anything in the path.
            if (PointCount == 0)
                return;

            Gdip.CheckStatus(Gdip.GdipWidenPath(
                new HandleRef(this, _nativePath),
                new HandleRef(pen, pen.NativePen),
                new HandleRef(matrix, matrix?.NativeMatrix ?? IntPtr.Zero),
                flatness));
        }

        public void Warp(PointF[] destPoints, RectangleF srcRect) => Warp(destPoints, srcRect, null);

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix? matrix) => Warp(destPoints, srcRect, matrix, WarpMode.Perspective);

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix? matrix, WarpMode warpMode)
        {
            Warp(destPoints, srcRect, matrix, warpMode, 0.25f);
        }

        public unsafe void Warp(PointF[] destPoints, RectangleF srcRect, Matrix? matrix, WarpMode warpMode, float flatness)
        {
            ArgumentNullException.ThrowIfNull(destPoints);

            fixed (PointF* p = destPoints)
            {
                Gdip.CheckStatus(Gdip.GdipWarpPath(
                    new HandleRef(this, _nativePath),
                    new HandleRef(matrix, matrix?.NativeMatrix ?? IntPtr.Zero),
                    p,
                    destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    warpMode,
                    flatness));
            }
        }

        public int PointCount
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPointCount(new HandleRef(this, _nativePath), out int count));
                return count;
            }
        }

        public byte[] PathTypes
        {
            get
            {
                byte[] types = new byte[PointCount];
                Gdip.CheckStatus(Gdip.GdipGetPathTypes(new HandleRef(this, _nativePath), types, types.Length));
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
                    Gdip.CheckStatus(Gdip.GdipGetPathPoints(new HandleRef(this, _nativePath), p, points.Length));
                }
                return points;
            }
        }
    }
}

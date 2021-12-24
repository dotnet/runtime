// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Drawing.Internal;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed partial class GraphicsPath : MarshalByRefObject, ICloneable, IDisposable
    {
        private const float Flatness = (float)2.0 / (float)3.0;

        public GraphicsPath() : this(FillMode.Alternate) { }

        public GraphicsPath(FillMode fillMode)
        {
            Gdip.CheckStatus(Gdip.GdipCreatePath(unchecked((int)fillMode), out _nativePath));
        }

        public GraphicsPath(PointF[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public unsafe GraphicsPath(PointF[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));
            if (pts.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (PointF* p = pts)
            fixed (byte* t = types)
            {
                Gdip.CheckStatus(Gdip.GdipCreatePath2(
                    p, t, types.Length, (int)fillMode, out _nativePath));
            }
        }

        public GraphicsPath(Point[] pts, byte[] types) : this(pts, types, FillMode.Alternate) { }

        public unsafe GraphicsPath(Point[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));
            if (pts.Length != types.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (byte* t = types)
            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipCreatePath2I(
                    p, t, types.Length, unchecked((int)fillMode), out _nativePath));
            }
        }

        public object Clone()
        {
            Gdip.CheckStatus(Gdip.GdipClonePath(SafeGraphicsPathHandle, out SafeGraphicsPathHandle clonedPath));

            return new GraphicsPath(clonedPath);
        }

        public void Reset()
        {
            Gdip.CheckStatus(Gdip.GdipResetPath(SafeGraphicsPathHandle));
        }

        public FillMode FillMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPathFillMode(SafeGraphicsPathHandle, out FillMode fillmode));
                return fillmode;
            }
            set
            {
                if (value < FillMode.Alternate || value > FillMode.Winding)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(FillMode));

                Gdip.CheckStatus(Gdip.GdipSetPathFillMode(SafeGraphicsPathHandle, value));
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

                Gdip.CheckStatus(Gdip.GdipGetPathData(SafeGraphicsPathHandle, &data));
            }

            return pathData;
        }

        public PathData PathData => _GetPathData();

        public void StartFigure()
        {
            Gdip.CheckStatus(Gdip.GdipStartPathFigure(SafeGraphicsPathHandle));
        }

        public void CloseFigure()
        {
            Gdip.CheckStatus(Gdip.GdipClosePathFigure(SafeGraphicsPathHandle));
        }

        public void CloseAllFigures()
        {
            Gdip.CheckStatus(Gdip.GdipClosePathFigures(SafeGraphicsPathHandle));
        }

        public void SetMarkers()
        {
            Gdip.CheckStatus(Gdip.GdipSetPathMarker(SafeGraphicsPathHandle));
        }

        public void ClearMarkers()
        {
            Gdip.CheckStatus(Gdip.GdipClearPathMarkers(SafeGraphicsPathHandle));
        }

        public void Reverse()
        {
            Gdip.CheckStatus(Gdip.GdipReversePath(SafeGraphicsPathHandle));
        }

        public PointF GetLastPoint()
        {
            Gdip.CheckStatus(Gdip.GdipGetPathLastPoint(SafeGraphicsPathHandle, out PointF point));
            return point;
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y), null);

        public bool IsVisible(PointF point) => IsVisible(point, null);

        public bool IsVisible(float x, float y, Graphics? graphics) => IsVisible(new PointF(x, y), graphics);

        public bool IsVisible(PointF pt, Graphics? graphics)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePathPoint(
                SafeGraphicsPathHandle,
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
                SafeGraphicsPathHandle,
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
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            Gdip.CheckStatus(Gdip.GdipIsOutlineVisiblePathPoint(
                SafeGraphicsPathHandle,
                pt.X, pt.Y,
                pen.SafePenHandle,
                new HandleRef(graphics, graphics?.NativeGraphics ?? IntPtr.Zero),
                out bool isVisible));

            return isVisible;
        }

        public bool IsOutlineVisible(int x, int y, Pen pen) => IsOutlineVisible(new Point(x, y), pen, null);

        public bool IsOutlineVisible(Point point, Pen pen) => IsOutlineVisible(point, pen, null);

        public bool IsOutlineVisible(int x, int y, Pen pen, Graphics? graphics) => IsOutlineVisible(new Point(x, y), pen, graphics);

        public bool IsOutlineVisible(Point pt, Pen pen, Graphics? graphics)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            Gdip.CheckStatus(Gdip.GdipIsOutlineVisiblePathPointI(
                SafeGraphicsPathHandle,
                pt.X, pt.Y,
                pen.SafePenHandle,
                new HandleRef(graphics, graphics?.NativeGraphics ?? IntPtr.Zero),
                out bool isVisible));

            return isVisible;
        }

        public void AddLine(PointF pt1, PointF pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathLine(SafeGraphicsPathHandle, x1, y1, x2, y2));
        }

        public unsafe void AddLines(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Length == 0)
                throw new ArgumentException(null, nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathLine2(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public void AddLine(Point pt1, Point pt2) => AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y);

        public void AddLine(int x1, int y1, int x2, int y2)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathLineI(SafeGraphicsPathHandle, x1, y1, x2, y2));
        }

        public unsafe void AddLines(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Length == 0)
                throw new ArgumentException(null, nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathLine2I(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
        {
            AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathArc(
                SafeGraphicsPathHandle,
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
                SafeGraphicsPathHandle,
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
                SafeGraphicsPathHandle,
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        public unsafe void AddBeziers(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathBeziers(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public void AddBezier(Point pt1, Point pt2, Point pt3, Point pt4)
        {
            AddBezier(pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public void AddBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathBezierI(
                SafeGraphicsPathHandle,
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        public unsafe void AddBeziers(params Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Length == 0)
                return;

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathBeziersI(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        /// <summary>
        /// Add cardinal splines to the path object
        /// </summary>
        public unsafe void AddCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));


            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public unsafe void AddCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Length == 0)
                return;

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve2(SafeGraphicsPathHandle, p, points.Length, tension));
            }
        }

        public unsafe void AddCurve(PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve3(
                    SafeGraphicsPathHandle, p, points.Length, offset, numberOfSegments, tension));
            }
        }

        public unsafe void AddCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurveI(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public unsafe void AddCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve2I(
                    SafeGraphicsPathHandle, p, points.Length, tension));
            }
        }

        public unsafe void AddCurve(Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathCurve3I(
                    SafeGraphicsPathHandle, p, points.Length, offset, numberOfSegments, tension));
            }
        }

        public unsafe void AddClosedCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurve(
                    SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public unsafe void AddClosedCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurve2(SafeGraphicsPathHandle, p, points.Length, tension));
            }
        }

        public unsafe void AddClosedCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurveI(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public unsafe void AddClosedCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathClosedCurve2I(SafeGraphicsPathHandle, p, points.Length, tension));
            }
        }

        public void AddRectangle(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathRectangle(
                SafeGraphicsPathHandle,
                rect.X, rect.Y, rect.Width, rect.Height));
        }

        public unsafe void AddRectangles(RectangleF[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));
            if (rects.Length == 0)
                throw new ArgumentException(null, nameof(rects));

            fixed (RectangleF* r = rects)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathRectangles(
                    SafeGraphicsPathHandle, r, rects.Length));
            }
        }

        public void AddRectangle(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathRectangleI(
                SafeGraphicsPathHandle,
                rect.X, rect.Y, rect.Width, rect.Height));
        }

        public unsafe void AddRectangles(Rectangle[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));
            if (rects.Length == 0)
                throw new ArgumentException(null, nameof(rects));

            fixed (Rectangle* r = rects)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathRectanglesI(
                    SafeGraphicsPathHandle, r, rects.Length));
            }
        }

        public void AddEllipse(RectangleF rect)
        {
            AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void AddEllipse(float x, float y, float width, float height)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathEllipse(SafeGraphicsPathHandle, x, y, width, height));
        }

        public void AddEllipse(Rectangle rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);

        public void AddEllipse(int x, int y, int width, int height)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathEllipseI(SafeGraphicsPathHandle, x, y, width, height));
        }

        public void AddPie(Rectangle rect, float startAngle, float sweepAngle)
        {
            AddPie(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathPie(
                SafeGraphicsPathHandle,
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        public void AddPie(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            Gdip.CheckStatus(Gdip.GdipAddPathPieI(
                SafeGraphicsPathHandle,
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        public unsafe void AddPolygon(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathPolygon(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        /// <summary>
        /// Adds a polygon to the current figure.
        /// </summary>
        public unsafe void AddPolygon(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipAddPathPolygonI(SafeGraphicsPathHandle, p, points.Length));
            }
        }

        public void AddPath(GraphicsPath addingPath, bool connect)
        {
            if (addingPath == null)
                throw new ArgumentNullException(nameof(addingPath));

            Gdip.CheckStatus(Gdip.GdipAddPathPath(
                SafeGraphicsPathHandle, addingPath.SafeGraphicsPathHandle, connect));
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
            if (family == null)
                throw new ArgumentNullException(nameof(family));

            Gdip.CheckStatus(Gdip.GdipAddPathString(
                SafeGraphicsPathHandle,
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
            if (family == null)
                throw new ArgumentNullException(nameof(family));

            Gdip.CheckStatus(Gdip.GdipAddPathStringI(
                SafeGraphicsPathHandle,
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
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (matrix.SafeMatrixHandle.IsClosed)
                return;

            Gdip.CheckStatus(Gdip.GdipTransformPath(
                SafeGraphicsPathHandle,
                matrix.SafeMatrixHandle));
        }

        public RectangleF GetBounds() => GetBounds(null, null);

        public RectangleF GetBounds(Matrix? matrix) => GetBounds(matrix, null);

        public RectangleF GetBounds(Matrix? matrix, Pen? pen)
        {
            Gdip.CheckStatus(Gdip.GdipGetPathWorldBounds(
                SafeGraphicsPathHandle,
                out RectangleF bounds,
                matrix?.SafeMatrixHandle,
                pen?.SafePenHandle));

            return bounds;
        }

        public void Flatten() => Flatten(null);

        public void Flatten(Matrix? matrix) => Flatten(matrix, 0.25f);

        public void Flatten(Matrix? matrix, float flatness)
        {
            Gdip.CheckStatus(Gdip.GdipFlattenPath(
                SafeGraphicsPathHandle,
                matrix?.SafeMatrixHandle,
                flatness));
        }

        public void Widen(Pen pen) => Widen(pen, null, Flatness);

        public void Widen(Pen pen, Matrix? matrix) => Widen(pen, matrix, Flatness);

        public void Widen(Pen pen, Matrix? matrix, float flatness)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            // GDI+ wrongly returns an out of memory status when there is nothing in the path, so we have to check
            // before calling the widen method and do nothing if we dont have anything in the path.
            if (PointCount == 0)
                return;

            Gdip.CheckStatus(Gdip.GdipWidenPath(
                SafeGraphicsPathHandle,
                pen.SafePenHandle,
                matrix?.SafeMatrixHandle,
                flatness));
        }

        public void Warp(PointF[] destPoints, RectangleF srcRect) => Warp(destPoints, srcRect, null);

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix? matrix) => Warp(destPoints, srcRect, matrix, WarpMode.Perspective);

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix? matrix, WarpMode warpMode)
        {
            Warp(destPoints, srcRect, matrix, warpMode, 0.25f);
        }

        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix? matrix, WarpMode warpMode, float flatness)
        {
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));

            Gdip.CheckStatus(Gdip.GdipWarpPath(
                SafeGraphicsPathHandle,
                matrix?.SafeMatrixHandle,
                destPoints,
                destPoints.Length,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                warpMode,
                flatness));
        }

        public int PointCount
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPointCount(SafeGraphicsPathHandle, out int count));
                return count;
            }
        }

        public byte[] PathTypes
        {
            get
            {
                byte[] types = new byte[PointCount];
                Gdip.CheckStatus(Gdip.GdipGetPathTypes(SafeGraphicsPathHandle, types, types.Length));
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
                    Gdip.CheckStatus(Gdip.GdipGetPathPoints(SafeGraphicsPathHandle, p, points.Length));
                }
                return points;
            }
        }
    }
}

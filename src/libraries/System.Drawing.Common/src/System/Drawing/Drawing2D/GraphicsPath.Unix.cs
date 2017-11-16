//
// System.Drawing.Drawing2D.GraphicsPath.cs
//
// Authors:
//
//   Miguel de Icaza (miguel@ximian.com)
//   Duncan Mak (duncan@ximian.com)
//   Jordi Mas i Hernandez (jordi@ximian.com)
//   Ravindra (rkumar@novell.com)
//   Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2004,2006-2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{
    public sealed class GraphicsPath : MarshalByRefObject, ICloneable, IDisposable
    {
        // 1/4 is the FlatnessDefault as defined in GdiPlusEnums.h
        private const float FlatnessDefault = 1.0f / 4.0f;

        internal IntPtr nativePath = IntPtr.Zero;

        GraphicsPath(IntPtr ptr)
        {
            nativePath = ptr;
        }

        public GraphicsPath()
        {
            int status = SafeNativeMethods.Gdip.GdipCreatePath(FillMode.Alternate, out nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public GraphicsPath(FillMode fillMode)
        {
            int status = SafeNativeMethods.Gdip.GdipCreatePath(fillMode, out nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public GraphicsPath(Point[] pts, byte[] types)
            : this(pts, types, FillMode.Alternate)
        {
        }

        public GraphicsPath(PointF[] pts, byte[] types)
            : this(pts, types, FillMode.Alternate)
        {
        }

        public GraphicsPath(Point[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            if (pts.Length != types.Length)
                throw new ArgumentException("Invalid parameter passed. Number of points and types must be same.");

            int status = SafeNativeMethods.Gdip.GdipCreatePath2I(pts, types, pts.Length, fillMode, out nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public GraphicsPath(PointF[] pts, byte[] types, FillMode fillMode)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            if (pts.Length != types.Length)
                throw new ArgumentException("Invalid parameter passed. Number of points and types must be same.");

            int status = SafeNativeMethods.Gdip.GdipCreatePath2(pts, types, pts.Length, fillMode, out nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public object Clone()
        {
            IntPtr clone;

            int status = SafeNativeMethods.Gdip.GdipClonePath(nativePath, out clone);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new GraphicsPath(clone);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        ~GraphicsPath()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            int status;
            if (nativePath != IntPtr.Zero)
            {
                status = SafeNativeMethods.Gdip.GdipDeletePath(nativePath);
                SafeNativeMethods.Gdip.CheckStatus(status);

                nativePath = IntPtr.Zero;
            }
        }

        public FillMode FillMode
        {
            get
            {
                FillMode mode;
                int status = SafeNativeMethods.Gdip.GdipGetPathFillMode(nativePath, out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return mode;
            }
            set
            {
                if ((value < FillMode.Alternate) || (value > FillMode.Winding))
                    throw new InvalidEnumArgumentException("FillMode", (int)value, typeof(FillMode));

                int status = SafeNativeMethods.Gdip.GdipSetPathFillMode(nativePath, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public PathData PathData
        {
            get
            {
                int count;
                int status = SafeNativeMethods.Gdip.GdipGetPointCount(nativePath, out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                PointF[] points = new PointF[count];
                byte[] types = new byte[count];

                // status would fail if we ask points or types with a 0 count
                // anyway that would only mean two unrequired unmanaged calls
                if (count > 0)
                {
                    status = SafeNativeMethods.Gdip.GdipGetPathPoints(nativePath, points, count);
                    SafeNativeMethods.Gdip.CheckStatus(status);

                    status = SafeNativeMethods.Gdip.GdipGetPathTypes(nativePath, types, count);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }

                PathData pdata = new PathData();
                pdata.Points = points;
                pdata.Types = types;
                return pdata;
            }
        }

        public PointF[] PathPoints
        {
            get
            {
                int count;
                int status = SafeNativeMethods.Gdip.GdipGetPointCount(nativePath, out count);
                SafeNativeMethods.Gdip.CheckStatus(status);
                if (count == 0)
                    throw new ArgumentException("PathPoints");

                PointF[] points = new PointF[count];
                status = SafeNativeMethods.Gdip.GdipGetPathPoints(nativePath, points, count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return points;
            }
        }

        public byte[] PathTypes
        {
            get
            {
                int count;
                int status = SafeNativeMethods.Gdip.GdipGetPointCount(nativePath, out count);
                SafeNativeMethods.Gdip.CheckStatus(status);
                if (count == 0)
                    throw new ArgumentException("PathTypes");

                byte[] types = new byte[count];
                status = SafeNativeMethods.Gdip.GdipGetPathTypes(nativePath, types, count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return types;
            }
        }

        public int PointCount
        {
            get
            {
                int count;
                int status = SafeNativeMethods.Gdip.GdipGetPointCount(nativePath, out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return count;
            }
        }

        internal IntPtr NativeObject
        {
            get
            {
                return nativePath;
            }
            set
            {
                nativePath = value;
            }
        }

        //
        // AddArc
        //
        public void AddArc(Rectangle rect, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathArcI(nativePath, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathArc(nativePath, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddArc(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathArcI(nativePath, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathArc(nativePath, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddBezier
        //
        public void AddBezier(Point pt1, Point pt2, Point pt3, Point pt4)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathBezierI(nativePath, pt1.X, pt1.Y,
                            pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathBezier(nativePath, pt1.X, pt1.Y,
                            pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathBezierI(nativePath, x1, y1, x2, y2, x3, y3, x4, y4);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddBezier(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathBezier(nativePath, x1, y1, x2, y2, x3, y3, x4, y4);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddBeziers
        //
        public void AddBeziers(params Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            int status = SafeNativeMethods.Gdip.GdipAddPathBeziersI(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddBeziers(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            int status = SafeNativeMethods.Gdip.GdipAddPathBeziers(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddEllipse
        //
        public void AddEllipse(RectangleF rect)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathEllipse(nativePath, rect.X, rect.Y, rect.Width, rect.Height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddEllipse(float x, float y, float width, float height)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathEllipse(nativePath, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddEllipse(Rectangle rect)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathEllipseI(nativePath, rect.X, rect.Y, rect.Width, rect.Height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddEllipse(int x, int y, int width, int height)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathEllipseI(nativePath, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        //
        // AddLine
        //
        public void AddLine(Point pt1, Point pt2)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathLineI(nativePath, pt1.X, pt1.Y, pt2.X, pt2.Y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddLine(PointF pt1, PointF pt2)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathLine(nativePath, pt1.X, pt1.Y, pt2.X,
                            pt2.Y);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddLine(int x1, int y1, int x2, int y2)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathLineI(nativePath, x1, y1, x2, y2);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathLine(nativePath, x1, y1, x2,
                            y2);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddLines
        //
        public void AddLines(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if (points.Length == 0)
                throw new ArgumentException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathLine2I(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddLines(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if (points.Length == 0)
                throw new ArgumentException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathLine2(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddPie
        //
        public void AddPie(Rectangle rect, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathPie(
                    nativePath, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddPie(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathPieI(nativePath, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathPie(nativePath, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddPolygon
        //
        public void AddPolygon(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathPolygonI(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddPolygon(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathPolygon(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddRectangle
        //
        public void AddRectangle(Rectangle rect)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathRectangleI(nativePath, rect.X, rect.Y, rect.Width, rect.Height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddRectangle(RectangleF rect)
        {
            int status = SafeNativeMethods.Gdip.GdipAddPathRectangle(nativePath, rect.X, rect.Y, rect.Width, rect.Height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddRectangles
        //
        public void AddRectangles(Rectangle[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException("rects");
            if (rects.Length == 0)
                throw new ArgumentException("rects");

            int status = SafeNativeMethods.Gdip.GdipAddPathRectanglesI(nativePath, rects, rects.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddRectangles(RectangleF[] rects)
        {
            if (rects == null)
                throw new ArgumentNullException("rects");
            if (rects.Length == 0)
                throw new ArgumentException("rects");

            int status = SafeNativeMethods.Gdip.GdipAddPathRectangles(nativePath, rects, rects.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddPath
        //
        public void AddPath(GraphicsPath addingPath, bool connect)
        {
            if (addingPath == null)
                throw new ArgumentNullException("addingPath");

            int status = SafeNativeMethods.Gdip.GdipAddPathPath(nativePath, addingPath.nativePath, connect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public PointF GetLastPoint()
        {
            PointF pt;
            int status = SafeNativeMethods.Gdip.GdipGetPathLastPoint(nativePath, out pt);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return pt;
        }

        //
        // AddClosedCurve
        //
        public void AddClosedCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurveI(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddClosedCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurve(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddClosedCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurve2I(nativePath, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddClosedCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathClosedCurve2(nativePath, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        //
        // AddCurve
        //
        public void AddCurve(Point[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathCurveI(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddCurve(PointF[] points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathCurve(nativePath, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddCurve(Point[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathCurve2I(nativePath, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddCurve(PointF[] points, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathCurve2(nativePath, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddCurve(Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathCurve3I(nativePath, points, points.Length,
                            offset, numberOfSegments, tension);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddCurve(PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            int status = SafeNativeMethods.Gdip.GdipAddPathCurve3(nativePath, points, points.Length,
                            offset, numberOfSegments, tension);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Reset()
        {
            int status = SafeNativeMethods.Gdip.GdipResetPath(nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Reverse()
        {
            int status = SafeNativeMethods.Gdip.GdipReversePath(nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Transform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            int status = SafeNativeMethods.Gdip.GdipTransformPath(nativePath, matrix.nativeMatrix);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        [MonoTODO("The StringFormat parameter is ignored when using libgdiplus.")]
        public void AddString(string s, FontFamily family, int style, float emSize, Point origin, StringFormat format)
        {
            Rectangle layout = new Rectangle();
            layout.X = origin.X;
            layout.Y = origin.Y;
            AddString(s, family, style, emSize, layout, format);
        }

        [MonoTODO("The StringFormat parameter is ignored when using libgdiplus.")]
        public void AddString(string s, FontFamily family, int style, float emSize, PointF origin, StringFormat format)
        {
            RectangleF layout = new RectangleF();
            layout.X = origin.X;
            layout.Y = origin.Y;
            AddString(s, family, style, emSize, layout, format);
        }

        [MonoTODO("The layoutRect and StringFormat parameters are ignored when using libgdiplus.")]
        public void AddString(string s, FontFamily family, int style, float emSize, Rectangle layoutRect, StringFormat format)
        {
            if (family == null)
                throw new ArgumentException("family");

            IntPtr sformat = (format == null) ? IntPtr.Zero : format.nativeFormat;
            // note: the NullReferenceException on s.Length is the expected (MS) exception
            int status = SafeNativeMethods.Gdip.GdipAddPathStringI(nativePath, s, s.Length, family.NativeFamily, style, emSize, ref layoutRect, sformat);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        [MonoTODO("The layoutRect and StringFormat parameters are ignored when using libgdiplus.")]
        public void AddString(string s, FontFamily family, int style, float emSize, RectangleF layoutRect, StringFormat format)
        {
            if (family == null)
                throw new ArgumentException("family");

            IntPtr sformat = (format == null) ? IntPtr.Zero : format.nativeFormat;
            // note: the NullReferenceException on s.Length is the expected (MS) exception
            int status = SafeNativeMethods.Gdip.GdipAddPathString(nativePath, s, s.Length, family.NativeFamily, style, emSize, ref layoutRect, sformat);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ClearMarkers()
        {
            int s = SafeNativeMethods.Gdip.GdipClearPathMarkers(nativePath);

            SafeNativeMethods.Gdip.CheckStatus(s);
        }

        public void CloseAllFigures()
        {
            int s = SafeNativeMethods.Gdip.GdipClosePathFigures(nativePath);

            SafeNativeMethods.Gdip.CheckStatus(s);
        }

        public void CloseFigure()
        {
            int s = SafeNativeMethods.Gdip.GdipClosePathFigure(nativePath);

            SafeNativeMethods.Gdip.CheckStatus(s);
        }

        public void Flatten()
        {
            Flatten(null, FlatnessDefault);
        }

        public void Flatten(Matrix matrix)
        {
            Flatten(matrix, FlatnessDefault);
        }

        public void Flatten(Matrix matrix, float flatness)
        {
            IntPtr m = (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix;
            int status = SafeNativeMethods.Gdip.GdipFlattenPath(nativePath, m, flatness);

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public RectangleF GetBounds()
        {
            return GetBounds(null, null);
        }

        public RectangleF GetBounds(Matrix matrix)
        {
            return GetBounds(matrix, null);
        }

        public RectangleF GetBounds(Matrix matrix, Pen pen)
        {
            RectangleF retval;
            IntPtr m = (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix;
            IntPtr p = (pen == null) ? IntPtr.Zero : pen.NativePen;

            int s = SafeNativeMethods.Gdip.GdipGetPathWorldBounds(nativePath, out retval, m, p);

            SafeNativeMethods.Gdip.CheckStatus(s);

            return retval;
        }

        public bool IsOutlineVisible(Point point, Pen pen)
        {
            return IsOutlineVisible(point.X, point.Y, pen, null);
        }

        public bool IsOutlineVisible(PointF point, Pen pen)
        {
            return IsOutlineVisible(point.X, point.Y, pen, null);
        }

        public bool IsOutlineVisible(int x, int y, Pen pen)
        {
            return IsOutlineVisible(x, y, pen, null);
        }

        public bool IsOutlineVisible(float x, float y, Pen pen)
        {
            return IsOutlineVisible(x, y, pen, null);
        }

        public bool IsOutlineVisible(Point pt, Pen pen, Graphics graphics)
        {
            return IsOutlineVisible(pt.X, pt.Y, pen, graphics);
        }

        public bool IsOutlineVisible(PointF pt, Pen pen, Graphics graphics)
        {
            return IsOutlineVisible(pt.X, pt.Y, pen, graphics);
        }

        public bool IsOutlineVisible(int x, int y, Pen pen, Graphics graphics)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");

            bool result;
            IntPtr g = (graphics == null) ? IntPtr.Zero : graphics.nativeObject;

            int s = SafeNativeMethods.Gdip.GdipIsOutlineVisiblePathPointI(nativePath, x, y, pen.NativePen, g, out result);
            SafeNativeMethods.Gdip.CheckStatus(s);

            return result;
        }

        public bool IsOutlineVisible(float x, float y, Pen pen, Graphics graphics)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");

            bool result;
            IntPtr g = (graphics == null) ? IntPtr.Zero : graphics.nativeObject;

            int s = SafeNativeMethods.Gdip.GdipIsOutlineVisiblePathPoint(nativePath, x, y, pen.NativePen, g, out result);
            SafeNativeMethods.Gdip.CheckStatus(s);

            return result;
        }

        public bool IsVisible(Point point)
        {
            return IsVisible(point.X, point.Y, null);
        }

        public bool IsVisible(PointF point)
        {
            return IsVisible(point.X, point.Y, null);
        }

        public bool IsVisible(int x, int y)
        {
            return IsVisible(x, y, null);
        }

        public bool IsVisible(float x, float y)
        {
            return IsVisible(x, y, null);
        }

        public bool IsVisible(Point pt, Graphics graphics)
        {
            return IsVisible(pt.X, pt.Y, graphics);
        }

        public bool IsVisible(PointF pt, Graphics graphics)
        {
            return IsVisible(pt.X, pt.Y, graphics);
        }

        public bool IsVisible(int x, int y, Graphics graphics)
        {
            bool retval;

            IntPtr g = (graphics == null) ? IntPtr.Zero : graphics.nativeObject;

            int s = SafeNativeMethods.Gdip.GdipIsVisiblePathPointI(nativePath, x, y, g, out retval);

            SafeNativeMethods.Gdip.CheckStatus(s);

            return retval;
        }

        public bool IsVisible(float x, float y, Graphics graphics)
        {
            bool retval;

            IntPtr g = (graphics == null) ? IntPtr.Zero : graphics.nativeObject;

            int s = SafeNativeMethods.Gdip.GdipIsVisiblePathPoint(nativePath, x, y, g, out retval);

            SafeNativeMethods.Gdip.CheckStatus(s);

            return retval;
        }

        public void SetMarkers()
        {
            int s = SafeNativeMethods.Gdip.GdipSetPathMarker(nativePath);

            SafeNativeMethods.Gdip.CheckStatus(s);
        }

        public void StartFigure()
        {
            int s = SafeNativeMethods.Gdip.GdipStartPathFigure(nativePath);

            SafeNativeMethods.Gdip.CheckStatus(s);
        }

        [MonoTODO("GdipWarpPath isn't implemented in libgdiplus")]
        public void Warp(PointF[] destPoints, RectangleF srcRect)
        {
            Warp(destPoints, srcRect, null, WarpMode.Perspective, FlatnessDefault);
        }

        [MonoTODO("GdipWarpPath isn't implemented in libgdiplus")]
        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix matrix)
        {
            Warp(destPoints, srcRect, matrix, WarpMode.Perspective, FlatnessDefault);
        }

        [MonoTODO("GdipWarpPath isn't implemented in libgdiplus")]
        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix matrix, WarpMode warpMode)
        {
            Warp(destPoints, srcRect, matrix, warpMode, FlatnessDefault);
        }

        [MonoTODO("GdipWarpPath isn't implemented in libgdiplus")]
        public void Warp(PointF[] destPoints, RectangleF srcRect, Matrix matrix, WarpMode warpMode, float flatness)
        {
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            IntPtr m = (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix;

            int s = SafeNativeMethods.Gdip.GdipWarpPath(nativePath, m, destPoints, destPoints.Length,
                            srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, warpMode, flatness);

            SafeNativeMethods.Gdip.CheckStatus(s);
        }

        [MonoTODO("GdipWidenPath isn't implemented in libgdiplus")]
        public void Widen(Pen pen)
        {
            Widen(pen, null, FlatnessDefault);
        }

        [MonoTODO("GdipWidenPath isn't implemented in libgdiplus")]
        public void Widen(Pen pen, Matrix matrix)
        {
            Widen(pen, matrix, FlatnessDefault);
        }

        [MonoTODO("GdipWidenPath isn't implemented in libgdiplus")]
        public void Widen(Pen pen, Matrix matrix, float flatness)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (PointCount == 0)
                return;
            IntPtr m = (matrix == null) ? IntPtr.Zero : matrix.nativeMatrix;

            int s = SafeNativeMethods.Gdip.GdipWidenPath(nativePath, pen.NativePen, m, flatness);
            SafeNativeMethods.Gdip.CheckStatus(s);
        }
    }
}

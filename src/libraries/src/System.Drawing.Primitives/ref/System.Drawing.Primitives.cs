// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Drawing
{
    public partial struct Point
    {
        public static readonly Point Empty;
        public Point(Size sz) { }
        public Point(int dw) { }
        public Point(int x, int y) { }
        public bool IsEmpty { get { return default(bool); } }
        public int X { get { return default(int); } set { } }
        public int Y { get { return default(int); } set { } }
        public static Point Add(Point pt, Size sz) { return default(Point); }
        public static Point Ceiling(PointF value) { return default(Point); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public void Offset(Point p) { }
        public void Offset(int dx, int dy) { }
        public static Point operator +(Point pt, Size sz) { return default(Point); }
        public static bool operator ==(Point left, Point right) { return default(bool); }
        public static explicit operator Size(Point p) { return default(Size); }
        public static implicit operator PointF(Point p) { return default(PointF); }
        public static bool operator !=(Point left, Point right) { return default(bool); }
        public static Point operator -(Point pt, Size sz) { return default(Point); }
        public static Point Round(PointF value) { return default(Point); }
        public static Point Subtract(Point pt, Size sz) { return default(Point); }
        public override string ToString() { return default(string); }
        public static Point Truncate(PointF value) { return default(Point); }
    }

    public partial struct PointF
    {
        public static readonly PointF Empty;
        public PointF(float x, float y) { }
        public bool IsEmpty { get { return default(bool); } }
        public float X { get { return default(float); } set { } }
        public float Y { get { return default(float); } set { } }
        public static PointF Add(PointF pt, Size sz) { return default(PointF); }
        public static PointF Add(PointF pt, SizeF sz) { return default(PointF); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static PointF operator +(PointF pt, Size sz) { return default(PointF); }
        public static PointF operator +(PointF pt, SizeF sz) { return default(PointF); }
        public static bool operator ==(PointF left, PointF right) { return default(bool); }
        public static bool operator !=(PointF left, PointF right) { return default(bool); }
        public static PointF operator -(PointF pt, Size sz) { return default(PointF); }
        public static PointF operator -(PointF pt, SizeF sz) { return default(PointF); }
        public static PointF Subtract(PointF pt, Size sz) { return default(PointF); }
        public static PointF Subtract(PointF pt, SizeF sz) { return default(PointF); }
        public override string ToString() { return default(string); }
    }

    public partial struct Rectangle
    {
        public static readonly Rectangle Empty;
        public Rectangle(Point location, Size size) { }
        public Rectangle(int x, int y, int width, int height) { }
        public int Bottom { get { return default(int); } }
        public int Height { get { return default(int); } set { } }
        public bool IsEmpty { get { return default(bool); } }
        public int Left { get { return default(int); } }
        public Point Location { get { return default(Point); } set { } }
        public int Right { get { return default(int); } }
        public Size Size { get { return default(Size); } set { } }
        public int Top { get { return default(int); } }
        public int Width { get { return default(int); } set { } }
        public int X { get { return default(int); } set { } }
        public int Y { get { return default(int); } set { } }
        public static Rectangle Ceiling(RectangleF value) { return default(Rectangle); }
        public bool Contains(Point pt) { return default(bool); }
        public bool Contains(Rectangle rect) { return default(bool); }
        public bool Contains(int x, int y) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public static Rectangle FromLTRB(int left, int top, int right, int bottom) { return default(Rectangle); }
        public override int GetHashCode() { return default(int); }
        public static Rectangle Inflate(Rectangle rect, int x, int y) { return default(Rectangle); }
        public void Inflate(Size size) { }
        public void Inflate(int width, int height) { }
        public void Intersect(Rectangle rect) { }
        public static Rectangle Intersect(Rectangle a, Rectangle b) { return default(Rectangle); }
        public bool IntersectsWith(Rectangle rect) { return default(bool); }
        public void Offset(Point pos) { }
        public void Offset(int x, int y) { }
        public static bool operator ==(Rectangle left, Rectangle right) { return default(bool); }
        public static bool operator !=(Rectangle left, Rectangle right) { return default(bool); }
        public static Rectangle Round(RectangleF value) { return default(Rectangle); }
        public override string ToString() { return default(string); }
        public static Rectangle Truncate(RectangleF value) { return default(Rectangle); }
        public static Rectangle Union(Rectangle a, Rectangle b) { return default(Rectangle); }
    }

    public partial struct RectangleF
    {
        public static readonly RectangleF Empty;
        public RectangleF(PointF location, SizeF size) { }
        public RectangleF(float x, float y, float width, float height) { }
        public float Bottom { get { return default(float); } }
        public float Height { get { return default(float); } set { } }
        public bool IsEmpty { get { return default(bool); } }
        public float Left { get { return default(float); } }
        public PointF Location { get { { return default(PointF); } } set { } }
        public float Right { get { return default(float); } }
        public SizeF Size { get { return default(SizeF); } set { } }
        public float Top { get { return default(float); } }
        public float Width { get { return default(float); } set { } }
        public float X { get { return default(float); } set { } }
        public float Y { get { return default(float); } set { } }
        public bool Contains(PointF pt) { return default(bool); }
        public bool Contains(RectangleF rect) { return default(bool); }
        public bool Contains(float x, float y) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public static RectangleF FromLTRB(float left, float top, float right, float bottom) { return default(RectangleF); }
        public override int GetHashCode() { return default(int); }
        public static RectangleF Inflate(RectangleF rect, float x, float y) { return default(RectangleF); }
        public void Inflate(SizeF size) { }
        public void Inflate(float x, float y) { }
        public void Intersect(RectangleF rect) { }
        public static RectangleF Intersect(RectangleF a, RectangleF b) { return default(RectangleF); }
        public bool IntersectsWith(RectangleF rect) { return default(bool); }
        public void Offset(PointF pos) { }
        public void Offset(float x, float y) { }
        public static bool operator ==(RectangleF left, RectangleF right) { return default(bool); }
        public static implicit operator RectangleF(Rectangle r) { return default(Rectangle); }
        public static bool operator !=(RectangleF left, RectangleF right) { return default(bool); }
        public override string ToString() { return default(string); }
        public static RectangleF Union(RectangleF a, RectangleF b) { return default(RectangleF); }
    }

    public partial struct Size
    {
        public static readonly Size Empty;
        public Size(Point pt) { }
        public Size(int width, int height) { }
        public int Height { get { return default(int); } set { } }
        public bool IsEmpty { get { return default(bool); } }
        public int Width { get { return default(int); } set { } }
        public static Size Add(Size sz1, Size sz2) { return default(Size); }
        public static Size Ceiling(SizeF value) { return default(Size); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static Size operator +(Size sz1, Size sz2) { return default(Size); }
        public static bool operator ==(Size sz1, Size sz2) { return default(bool); }
        public static explicit operator Point(Size size) { return default(Point); }
        public static implicit operator SizeF(Size p) { return default(Size); }
        public static bool operator !=(Size sz1, Size sz2) { return default(bool); }
        public static Size operator -(Size sz1, Size sz2) { return default(Size); }
        public static Size Round(SizeF value) { return default(Size); }
        public static Size Subtract(Size sz1, Size sz2) { return default(Size); }
        public override string ToString() { return default(string); }
        public static Size Truncate(SizeF value) { return default(Size); }
    }

    public partial struct SizeF
    {
        public static readonly SizeF Empty;
        public SizeF(PointF pt) { }
        public SizeF(SizeF size) { }
        public SizeF(float width, float height) { }
        public float Height { get { return default(float); } set { } }
        public bool IsEmpty { get { return default(bool); } }
        public float Width { get { return default(float); } set { } }
        public static SizeF Add(SizeF sz1, SizeF sz2) { return default(SizeF); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static SizeF operator +(SizeF sz1, SizeF sz2) { return default(SizeF); }
        public static bool operator ==(SizeF sz1, SizeF sz2) { return default(bool); }
        public static explicit operator PointF(SizeF size) { return default(PointF); }
        public static bool operator !=(SizeF sz1, SizeF sz2) { return default(bool); }
        public static SizeF operator -(SizeF sz1, SizeF sz2) { return default(SizeF); }
        public static SizeF Subtract(SizeF sz1, SizeF sz2) { return default(SizeF); }
        public PointF ToPointF() { return default(PointF); }
        public Size ToSize() { return default(Size); }
        public override string ToString() { return default(string); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;

namespace System.Drawing
{
    /// <summary>
    ///    <para>
    ///       Stores the location and size of a rectangular region. 
    ///    </para>
    /// </summary>
    [Serializable]
    public struct Rectangle
    {
        public static readonly Rectangle Empty = new Rectangle();

        private int _x;
        private int _y;
        private int _width;
        private int _height;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Rectangle'/>
        ///       class with the specified location and size.
        ///    </para>
        /// </summary>
        public Rectangle(int x, int y, int width, int height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the Rectangle class with the specified location
        ///       and size.
        ///    </para>
        /// </summary>
        public Rectangle(Point location, Size size)
        {
            _x = location.X;
            _y = location.Y;
            _width = size.Width;
            _height = size.Height;
        }

        /// <summary>
        ///    Creates a new <see cref='System.Drawing.Rectangle'/> with
        ///    the specified location and size.
        /// </summary>
        public static Rectangle FromLTRB(int left, int top, int right, int bottom)
        {
            return new Rectangle(left,
                                 top,
                                 right - left,
                                 bottom - top);
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the coordinates of the
        ///       upper-left corner of the rectangular region represented by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        public Point Location
        {
            get
            {
                return new Point(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        ///    Gets or sets the size of this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        ///    Gets or sets the x-coordinate of the
        ///    upper-left corner of the rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        /// <summary>
        ///    Gets or sets the y-coordinate of the
        ///    upper-left corner of the rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        /// <summary>
        ///    Gets or sets the width of the rectangular
        ///    region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        /// <summary>
        ///    Gets or sets the width of the rectangular
        ///    region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public int Left
        {
            get
            {
                return X;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        public int Top
        {
            get
            {
                return Y;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        public int Right
        {
            get
            {
                return X + Width;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        public int Bottom
        {
            get
            {
                return Y + Height;
            }
        }

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.Rectangle'/> has a <see cref='System.Drawing.Rectangle.Width'/>
        ///       or a <see cref='System.Drawing.Rectangle.Height'/> of 0.
        ///    </para>
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _height == 0 && _width == 0 && _x == 0 && _y == 0;
            }
        }

        /// <summary>
        ///    <para>
        ///       Tests whether <paramref name="obj"/> is a <see cref='System.Drawing.Rectangle'/> with
        ///       the same location and size of this Rectangle.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Rectangle))
                return false;

            Rectangle comp = (Rectangle)obj;

            return (comp.X == X) && (comp.Y == Y) && (comp.Width == Width) && (comp.Height == Height);
        }

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Rectangle'/>
        ///       objects have equal location and size.
        ///    </para>
        /// </summary>
        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return (left.X == right.X && left.Y == right.Y &&
                left.Width == right.Width && left.Height == right.Height);
        }

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Rectangle'/>
        ///       objects differ in location or size.
        ///    </para>
        /// </summary>
        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !(left == right);
        }

        /// <summary>
        ///   Converts a RectangleF to a Rectangle by performing a ceiling operation on
        ///   all the coordinates.
        /// </summary>
        public static Rectangle Ceiling(RectangleF value)
        {
            return new Rectangle((int)Math.Ceiling(value.X),
                                 (int)Math.Ceiling(value.Y),
                                 (int)Math.Ceiling(value.Width),
                                 (int)Math.Ceiling(value.Height));
        }

        /// <summary>
        ///   Converts a RectangleF to a Rectangle by performing a truncate operation on
        ///   all the coordinates.
        /// </summary>
        public static Rectangle Truncate(RectangleF value)
        {
            return new Rectangle((int)value.X,
                                 (int)value.Y,
                                 (int)value.Width,
                                 (int)value.Height);
        }

        /// <summary>
        ///   Converts a RectangleF to a Rectangle by performing a round operation on
        ///   all the coordinates.
        /// </summary>
        public static Rectangle Round(RectangleF value)
        {
            return new Rectangle((int)Math.Round(value.X),
                                 (int)Math.Round(value.Y),
                                 (int)Math.Round(value.Width),
                                 (int)Math.Round(value.Height));
        }

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        [Pure]
        public bool Contains(int x, int y)
        {
            return X <= x && x < X + Width && Y <= y && y < Y + Height;
        }

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        [Pure]
        public bool Contains(Point pt)
        {
            return Contains(pt.X, pt.Y);
        }

        /// <summary>
        ///    <para>
        ///       Determines if the rectangular region represented by
        ///    <paramref name="rect"/> is entirely contained within the rectangular region represented by 
        ///       this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        [Pure]
        public bool Contains(Rectangle rect)
        {
            return (X <= rect.X) && ((rect.X + rect.Width) <= (X + Width)) &&
                (Y <= rect.Y) && ((rect.Y + rect.Height) <= (Y + Height));
        }

        public override int GetHashCode()
        {
            return (int)((uint)X ^ (((uint)Y << 13) | ((uint)Y >> 19)) ^
                (((uint)Width << 26) | ((uint)Width >> 6)) ^ (((uint)Height << 7) | ((uint)Height >> 25)));
        }

        /// <summary>
        ///    <para>
        ///       Inflates this <see cref='System.Drawing.Rectangle'/>
        ///       by the specified amount.
        ///    </para>
        /// </summary>
        public void Inflate(int width, int height)
        {
            X -= width;
            Y -= height;
            Width += 2 * width;
            Height += 2 * height;
        }

        /// <summary>
        ///    Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
        /// </summary>
        public void Inflate(Size size)
        {
            Inflate(size.Width, size.Height);
        }

        /// <summary>
        ///    <para>
        ///       Creates a <see cref='System.Drawing.Rectangle'/>
        ///       that is inflated by the specified amount.
        ///    </para>
        /// </summary>
        public static Rectangle Inflate(Rectangle rect, int x, int y)
        {
            Rectangle r = rect;
            r.Inflate(x, y);
            return r;
        }

        /// <summary> Creates a Rectangle that represents the intersection between this Rectangle and rect.
        /// </summary>
        public void Intersect(Rectangle rect)
        {
            Rectangle result = Rectangle.Intersect(rect, this);

            X = result.X;
            Y = result.Y;
            Width = result.Width;
            Height = result.Height;
        }

        /// <summary>
        ///    Creates a rectangle that represents the intersection between a and
        ///    b. If there is no intersection, null is returned.
        /// </summary>
        public static Rectangle Intersect(Rectangle a, Rectangle b)
        {
            int x1 = Math.Max(a.X, b.X);
            int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Max(a.Y, b.Y);
            int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1 && y2 >= y1)
            {
                return new Rectangle(x1, y1, x2 - x1, y2 - y1);
            }

            return Empty;
        }

        /// <summary>
        ///     Determines if this rectangle intersects with rect.
        /// </summary>
        [Pure]
        public bool IntersectsWith(Rectangle rect)
        {
            return (rect.X < X + Width) && (X < (rect.X + rect.Width)) &&
                (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);
        }

        /// <summary>
        ///    <para>
        ///       Creates a rectangle that represents the union between a and
        ///       b.
        ///    </para>
        /// </summary>
        [Pure]
        public static Rectangle Union(Rectangle a, Rectangle b)
        {
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        /// <summary>
        ///    <para>
        ///       Adjusts the location of this rectangle by the specified amount.
        ///    </para>
        /// </summary>
        public void Offset(Point pos)
        {
            Offset(pos.X, pos.Y);
        }

        /// <summary>
        ///    Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(int x, int y)
        {
            X += x;
            Y += y;
        }

        /// <summary>
        ///    <para>
        ///       Converts the attributes of this <see cref='System.Drawing.Rectangle'/> to a
        ///       human readable string.
        ///    </para>
        /// </summary>
        public override string ToString()
        {
            return "{X=" + X.ToString() + ",Y=" + Y.ToString() +
                ",Width=" + Width.ToString() + ",Height=" + Height.ToString() + "}";
        }
    }
}

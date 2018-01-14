// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Numerics.Hashing;

namespace System.Drawing
{
    /// <summary>
    ///    <para>
    ///       Stores the location and size of a rectangular region. 
    ///    </para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct Rectangle : IEquatable<Rectangle>
    {
        public static readonly Rectangle Empty = new Rectangle();

        private int x; // Do not rename (binary serialization) 
        private int y; // Do not rename (binary serialization) 
        private int width; // Do not rename (binary serialization) 
        private int height; // Do not rename (binary serialization) 

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Rectangle'/>
        ///       class with the specified location and size.
        ///    </para>
        /// </summary>
        public Rectangle(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the Rectangle class with the specified location
        ///       and size.
        ///    </para>
        /// </summary>
        public Rectangle(Point location, Size size)
        {
            x = location.X;
            y = location.Y;
            width = size.Width;
            height = size.Height;
        }

        /// <summary>
        ///    Creates a new <see cref='System.Drawing.Rectangle'/> with
        ///    the specified location and size.
        /// </summary>
        public static Rectangle FromLTRB(int left, int top, int right, int bottom) =>
            new Rectangle(left, top, unchecked(right - left), unchecked(bottom - top));

        /// <summary>
        ///    <para>
        ///       Gets or sets the coordinates of the
        ///       upper-left corner of the rectangular region represented by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public Point Location
        {
            get { return new Point(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        ///    Gets or sets the size of this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        [Browsable(false)]
        public Size Size
        {
            get { return new Size(Width, Height); }
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
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        ///    Gets or sets the y-coordinate of the
        ///    upper-left corner of the rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        ///    Gets or sets the width of the rectangular
        ///    region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        /// <summary>
        ///    Gets or sets the width of the rectangular
        ///    region defined by this <see cref='System.Drawing.Rectangle'/>.
        /// </summary>
        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public int Left => X;

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public int Top => Y;

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public int Right => unchecked(X + Width);

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public int Bottom => unchecked(Y + Height);

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.Rectangle'/> has a <see cref='System.Drawing.Rectangle.Width'/>
        ///       or a <see cref='System.Drawing.Rectangle.Height'/> of 0.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => height == 0 && width == 0 && x == 0 && y == 0;

        /// <summary>
        ///    <para>
        ///       Tests whether <paramref name="obj"/> is a <see cref='System.Drawing.Rectangle'/> with
        ///       the same location and size of this Rectangle.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj) => obj is Rectangle && Equals((Rectangle)obj);

        public bool Equals(Rectangle other) => this == other;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Rectangle'/>
        ///       objects have equal location and size.
        ///    </para>
        /// </summary>
        public static bool operator ==(Rectangle left, Rectangle right) =>
            left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Rectangle'/>
        ///       objects differ in location or size.
        ///    </para>
        /// </summary>
        public static bool operator !=(Rectangle left, Rectangle right) => !(left == right);

        /// <summary>
        ///   Converts a RectangleF to a Rectangle by performing a ceiling operation on
        ///   all the coordinates.
        /// </summary>
        public static Rectangle Ceiling(RectangleF value)
        {
            unchecked
            {
                return new Rectangle(
                    (int)Math.Ceiling(value.X),
                    (int)Math.Ceiling(value.Y),
                    (int)Math.Ceiling(value.Width),
                    (int)Math.Ceiling(value.Height));
            }
        }

        /// <summary>
        ///   Converts a RectangleF to a Rectangle by performing a truncate operation on
        ///   all the coordinates.
        /// </summary>
        public static Rectangle Truncate(RectangleF value)
        {
            unchecked
            {
                return new Rectangle(
                    (int)value.X,
                    (int)value.Y,
                    (int)value.Width,
                    (int)value.Height);
            }
        }

        /// <summary>
        ///   Converts a RectangleF to a Rectangle by performing a round operation on
        ///   all the coordinates.
        /// </summary>
        public static Rectangle Round(RectangleF value)
        {
            unchecked
            {
                return new Rectangle(
                    (int)Math.Round(value.X),
                    (int)Math.Round(value.Y),
                    (int)Math.Round(value.Width),
                    (int)Math.Round(value.Height));
            }
        }

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public bool Contains(int x, int y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public bool Contains(Point pt) => Contains(pt.X, pt.Y);

        /// <summary>
        ///    <para>
        ///       Determines if the rectangular region represented by
        ///    <paramref name="rect"/> is entirely contained within the rectangular region represented by 
        ///       this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public bool Contains(Rectangle rect) =>
            (X <= rect.X) && (rect.X + rect.Width <= X + Width) &&
            (Y <= rect.Y) && (rect.Y + rect.Height <= Y + Height);

        public override int GetHashCode() =>
            HashHelpers.Combine(HashHelpers.Combine(HashHelpers.Combine(X, Y), Width), Height);

        /// <summary>
        ///    <para>
        ///       Inflates this <see cref='System.Drawing.Rectangle'/>
        ///       by the specified amount.
        ///    </para>
        /// </summary>
        public void Inflate(int width, int height)
        {
            unchecked
            {
                X -= width;
                Y -= height;

                Width += 2 * width;
                Height += 2 * height;
            }
        }

        /// <summary>
        ///    Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
        /// </summary>
        public void Inflate(Size size) => Inflate(size.Width, size.Height);

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
            Rectangle result = Intersect(rect, this);

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
        public bool IntersectsWith(Rectangle rect) =>
            (rect.X < X + Width) && (X < rect.X + rect.Width) &&
            (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);

        /// <summary>
        ///    <para>
        ///       Creates a rectangle that represents the union between a and
        ///       b.
        ///    </para>
        /// </summary>
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
        public void Offset(Point pos) => Offset(pos.X, pos.Y);

        /// <summary>
        ///    Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(int x, int y)
        {
            unchecked
            {
                X += x;
                Y += y;
            }
        }

        /// <summary>
        ///    <para>
        ///       Converts the attributes of this <see cref='System.Drawing.Rectangle'/> to a
        ///       human readable string.
        ///    </para>
        /// </summary>
        public override string ToString() =>
            "{X=" + X.ToString() + ",Y=" + Y.ToString() +
            ",Width=" + Width.ToString() + ",Height=" + Height.ToString() + "}";
    }
}

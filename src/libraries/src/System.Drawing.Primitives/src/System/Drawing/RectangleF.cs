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
    public struct RectangleF
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.RectangleF'/>
        ///    class.
        /// </summary>
        public static readonly RectangleF Empty = new RectangleF();

        private float _x;
        private float _y;
        private float _width;
        private float _height;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.RectangleF'/>
        ///       class with the specified location and size.
        ///    </para>
        /// </summary>
        public RectangleF(float x, float y, float width, float height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.RectangleF'/>
        ///       class with the specified location
        ///       and size.
        ///    </para>
        /// </summary>
        public RectangleF(PointF location, SizeF size)
        {
            _x = location.X;
            _y = location.Y;
            _width = size.Width;
            _height = size.Height;
        }

        /// <summary>
        ///    <para>
        ///       Creates a new <see cref='System.Drawing.RectangleF'/> with
        ///       the specified location and size.
        ///    </para>
        /// </summary>
        public static RectangleF FromLTRB(float left, float top, float right, float bottom)
        {
            return new RectangleF(left,
                                 top,
                                 right - left,
                                 bottom - top);
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the coordinates of the upper-left corner of
        ///       the rectangular region represented by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public PointF Location
        {
            get
            {
                return new PointF(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the size of this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public SizeF Size
        {
            get
            {
                return new SizeF(Width, Height);
            }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the x-coordinate of the
        ///       upper-left corner of the rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float X
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
        ///    <para>
        ///       Gets or sets the y-coordinate of the
        ///       upper-left corner of the rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float Y
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
        ///    <para>
        ///       Gets or sets the width of the rectangular
        ///       region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float Width
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
        ///    <para>
        ///       Gets or sets the height of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float Height
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
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleF'/> .
        ///    </para>
        /// </summary>
        public float Left
        {
            get
            {
                return X;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float Top
        {
            get
            {
                return Y;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float Right
        {
            get
            {
                return X + Width;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public float Bottom
        {
            get
            {
                return Y + Height;
            }
        }

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.RectangleF'/> has a <see cref='System.Drawing.RectangleF.Width'/> or a <see cref='System.Drawing.RectangleF.Height'/> of 0.
        ///    </para>
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return (Width <= 0) || (Height <= 0);
            }
        }

        /// <summary>
        ///    <para>
        ///       Tests whether <paramref name="obj"/> is a <see cref='System.Drawing.RectangleF'/> with the same location and size of this
        ///    <see cref='System.Drawing.RectangleF'/>.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is RectangleF))
                return false;

            RectangleF comp = (RectangleF)obj;

            return (comp.X == X) && (comp.Y == Y) && (comp.Width == Width) && (comp.Height == Height);
        }

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.RectangleF'/>
        ///       objects have equal location and size.
        ///    </para>
        /// </summary>
        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return (left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height);
        }

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.RectangleF'/>
        ///       objects differ in location or size.
        ///    </para>
        /// </summary>
        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return !(left == right);
        }

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        [Pure]
        public bool Contains(float x, float y)
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
        public bool Contains(PointF pt)
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
        public bool Contains(RectangleF rect)
        {
            return (X <= rect.X) && ((rect.X + rect.Width) <= (X + Width)) &&
                (Y <= rect.Y) && ((rect.Y + rect.Height) <= (Y + Height));
        }

        /// <summary>
        ///    Gets the hash code for this <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
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
        public void Inflate(float x, float y)
        {
            X -= x;
            Y -= y;
            Width += 2 * x;
            Height += 2 * y;
        }

        /// <summary>
        ///    Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
        /// </summary>
        public void Inflate(SizeF size)
        {
            Inflate(size.Width, size.Height);
        }

        /// <summary>
        ///    <para>
        ///       Creates a <see cref='System.Drawing.Rectangle'/>
        ///       that is inflated by the specified amount.
        ///    </para>
        /// </summary>
        public static RectangleF Inflate(RectangleF rect, float x, float y)
        {
            RectangleF r = rect;
            r.Inflate(x, y);
            return r;
        }

        /// <summary> Creates a Rectangle that represents the intersection between this Rectangle and rect.
        /// </summary>
        public void Intersect(RectangleF rect)
        {
            RectangleF result = RectangleF.Intersect(rect, this);

            X = result.X;
            Y = result.Y;
            Width = result.Width;
            Height = result.Height;
        }

        /// <summary>
        ///    Creates a rectangle that represents the intersection between a and
        ///    b. If there is no intersection, null is returned.
        /// </summary>
        [Pure]
        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float x1 = Math.Max(a.X, b.X);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y1 = Math.Max(a.Y, b.Y);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1 && y2 >= y1)
            {
                return new RectangleF(x1, y1, x2 - x1, y2 - y1);
            }

            return Empty;
        }

        /// <summary>
        ///    Determines if this rectangle intersects with rect.
        /// </summary>
        [Pure]
        public bool IntersectsWith(RectangleF rect)
        {
            return (rect.X < X + Width) && (X < (rect.X + rect.Width)) &&
                (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);
        }

        /// <summary>
        ///    Creates a rectangle that represents the union between a and
        ///    b.
        /// </summary>
        [Pure]
        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            float x1 = Math.Min(a.X, b.X);
            float x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            float y1 = Math.Min(a.Y, b.Y);
            float y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        /// <summary>
        ///    Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(PointF pos)
        {
            Offset(pos.X, pos.Y);
        }

        /// <summary>
        ///    Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(float x, float y)
        {
            X += x;
            Y += y;
        }

        /// <summary>
        ///    Converts the specified <see cref='System.Drawing.Rectangle'/> to a
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public static implicit operator RectangleF(Rectangle r)
        {
            return new RectangleF(r.X, r.Y, r.Width, r.Height);
        }

        /// <summary>
        ///    Converts the <see cref='System.Drawing.RectangleF.Location'/> and <see cref='System.Drawing.RectangleF.Size'/> of this <see cref='System.Drawing.RectangleF'/> to a
        ///    human-readable string.
        /// </summary>
        public override string ToString()
        {
            return "{X=" + X.ToString() + ",Y=" + Y.ToString() + ",Width=" +
                Width.ToString() + ",Height=" + Height.ToString() + "}";
        }
    }
}

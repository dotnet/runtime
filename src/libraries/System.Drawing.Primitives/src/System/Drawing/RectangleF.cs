// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace System.Drawing
{
    /// <summary>
    /// Stores the location and size of a rectangular region.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct RectangleF : IEquatable<RectangleF>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.RectangleF'/> class.
        /// </summary>
        public static readonly RectangleF Empty;

        private float x; // Do not rename (binary serialization)
        private float y; // Do not rename (binary serialization)
        private float width; // Do not rename (binary serialization)
        private float height; // Do not rename (binary serialization)

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.RectangleF'/> class with the specified location
        /// and size.
        /// </summary>
        public RectangleF(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.RectangleF'/> class with the specified location
        /// and size.
        /// </summary>
        public RectangleF(PointF location, SizeF size)
        {
            x = location.X;
            y = location.Y;
            width = size.Width;
            height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.RectangleF'/> struct from the specified
        /// <see cref="System.Numerics.Vector4"/>.
        /// </summary>
        public RectangleF(Vector4 vector)
        {
            x = vector.X;
            y = vector.Y;
            width = vector.Z;
            height = vector.W;
        }

        /// <summary>
        /// Creates a new <see cref="System.Numerics.Vector4"/> from this <see cref="System.Drawing.RectangleF"/>.
        /// </summary>
        public Vector4 ToVector4() => new Vector4(x, y, width, height);

        /// <summary>
        /// Converts the specified <see cref="System.Drawing.RectangleF"/> to a <see cref="System.Numerics.Vector4"/>.
        /// </summary>
        public static explicit operator Vector4(RectangleF rectangle) => rectangle.ToVector4();

        /// <summary>
        /// Converts the specified <see cref="System.Numerics.Vector2"/> to a <see cref="System.Drawing.RectangleF"/>.
        /// </summary>
        public static explicit operator RectangleF(Vector4 vector) => new RectangleF(vector);

        /// <summary>
        /// Creates a new <see cref='System.Drawing.RectangleF'/> with the specified location and size.
        /// </summary>
        public static RectangleF FromLTRB(float left, float top, float right, float bottom) =>
            new RectangleF(left, top, right - left, bottom - top);

        /// <summary>
        /// Gets or sets the coordinates of the upper-left corner of the rectangular region represented by this
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        [Browsable(false)]
        public PointF Location
        {
            readonly get => new PointF(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Gets or sets the size of this <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        [Browsable(false)]
        public SizeF Size
        {
            readonly get => new SizeF(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the upper-left corner of the rectangular region defined by this
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public float X
        {
            readonly get => x;
            set => x = value;
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the upper-left corner of the rectangular region defined by this
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public float Y
        {
            readonly get => y;
            set => y = value;
        }

        /// <summary>
        /// Gets or sets the width of the rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public float Width
        {
            readonly get => width;
            set => width = value;
        }

        /// <summary>
        /// Gets or sets the height of the rectangular region defined by this <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public float Height
        {
            readonly get => height;
            set => height = value;
        }

        /// <summary>
        /// Gets the x-coordinate of the upper-left corner of the rectangular region defined by this
        /// <see cref='System.Drawing.RectangleF'/> .
        /// </summary>
        [Browsable(false)]
        public readonly float Left => X;

        /// <summary>
        /// Gets the y-coordinate of the upper-left corner of the rectangular region defined by this
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        [Browsable(false)]
        public readonly float Top => Y;

        /// <summary>
        /// Gets the x-coordinate of the lower-right corner of the rectangular region defined by this
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        [Browsable(false)]
        public readonly float Right => X + Width;

        /// <summary>
        /// Gets the y-coordinate of the lower-right corner of the rectangular region defined by this
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        [Browsable(false)]
        public readonly float Bottom => Y + Height;

        /// <summary>
        /// Tests whether this <see cref='System.Drawing.RectangleF'/> has a <see cref='System.Drawing.RectangleF.Width'/> or a <see cref='System.Drawing.RectangleF.Height'/> of 0.
        /// </summary>
        [Browsable(false)]
        public readonly bool IsEmpty => (Width <= 0) || (Height <= 0);

        /// <summary>
        /// Tests whether <paramref name="obj"/> is a <see cref='System.Drawing.RectangleF'/> with the same location and
        /// size of this <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is RectangleF && Equals((RectangleF)obj);

        public readonly bool Equals(RectangleF other) => this == other;

        /// <summary>
        /// Tests whether two <see cref='System.Drawing.RectangleF'/> objects have equal location and size.
        /// </summary>
        public static bool operator ==(RectangleF left, RectangleF right) =>
            left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;

        /// <summary>
        /// Tests whether two <see cref='System.Drawing.RectangleF'/> objects differ in location or size.
        /// </summary>
        public static bool operator !=(RectangleF left, RectangleF right) => !(left == right);

        /// <summary>
        /// Determines if the specified point is contained within the rectangular region defined by this
        /// <see cref='System.Drawing.Rectangle'/> .
        /// </summary>
        public readonly bool Contains(float x, float y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        /// <summary>
        /// Determines if the specified point is contained within the rectangular region defined by this
        /// <see cref='System.Drawing.Rectangle'/> .
        /// </summary>
        public readonly bool Contains(PointF pt) => Contains(pt.X, pt.Y);

        /// <summary>
        /// Determines if the rectangular region represented by <paramref name="rect"/> is entirely contained within
        /// the rectangular region represented by this <see cref='System.Drawing.Rectangle'/> .
        /// </summary>
        public readonly bool Contains(RectangleF rect) =>
            (X <= rect.X) && (rect.X + rect.Width <= X + Width) && (Y <= rect.Y) && (rect.Y + rect.Height <= Y + Height);

        /// <summary>
        /// Gets the hash code for this <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        /// <summary>
        /// Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
        /// </summary>
        public void Inflate(float x, float y)
        {
            X -= x;
            Y -= y;
            Width += 2 * x;
            Height += 2 * y;
        }

        /// <summary>
        /// Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
        /// </summary>
        public void Inflate(SizeF size) => Inflate(size.Width, size.Height);

        /// <summary>
        /// Creates a <see cref='System.Drawing.Rectangle'/> that is inflated by the specified amount.
        /// </summary>
        public static RectangleF Inflate(RectangleF rect, float x, float y)
        {
            RectangleF r = rect;
            r.Inflate(x, y);
            return r;
        }

        /// <summary>
        /// Creates a Rectangle that represents the intersection between this Rectangle and rect.
        /// </summary>
        public void Intersect(RectangleF rect)
        {
            RectangleF result = Intersect(rect, this);

            X = result.X;
            Y = result.Y;
            Width = result.Width;
            Height = result.Height;
        }

        /// <summary>
        /// Creates a rectangle that represents the intersection between a and b. If there is no intersection, an
        /// empty rectangle is returned.
        /// </summary>
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
        /// Determines if this rectangle intersects with rect.
        /// </summary>
        public readonly bool IntersectsWith(RectangleF rect) =>
            (rect.X < X + Width) && (X < rect.X + rect.Width) && (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);

        /// <summary>
        /// Creates a rectangle that represents the union between a and b.
        /// </summary>
        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            float x1 = Math.Min(a.X, b.X);
            float x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            float y1 = Math.Min(a.Y, b.Y);
            float y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        /// <summary>
        /// Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(PointF pos) => Offset(pos.X, pos.Y);

        /// <summary>
        /// Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(float x, float y)
        {
            X += x;
            Y += y;
        }

        /// <summary>
        /// Converts the specified <see cref='System.Drawing.Rectangle'/> to a
        /// <see cref='System.Drawing.RectangleF'/>.
        /// </summary>
        public static implicit operator RectangleF(Rectangle r) => new RectangleF(r.X, r.Y, r.Width, r.Height);

        /// <summary>
        /// Converts the <see cref='System.Drawing.RectangleF.Location'/> and <see cref='System.Drawing.RectangleF.Size'/>
        /// of this <see cref='System.Drawing.RectangleF'/> to a human-readable string.
        /// </summary>
        public override readonly string ToString() =>
            "{X=" + X.ToString() + ",Y=" + Y.ToString() +
            ",Width=" + Width.ToString() + ",Height=" + Height.ToString() + "}";
    }
}

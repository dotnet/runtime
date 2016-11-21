// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics.Hashing;

namespace System.Drawing
{
    /**
     * Represents a dimension in 2D coordinate space
     */
    /// <summary>
    ///    Represents the size of a rectangular region
    ///    with an ordered pair of width and height.
    /// </summary>
    [Serializable]
    public struct Size
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.Size'/> class.
        /// </summary>
        public static readonly Size Empty = new Size();

        private int _width;
        private int _height;

        /**
         * Create a new Size object from a point
         */
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Size'/> class from
        ///       the specified <see cref='System.Drawing.Point'/>.
        ///    </para>
        /// </summary>
        public Size(Point pt)
        {
            _width = pt.X;
            _height = pt.Y;
        }

        /**
         * Create a new Size object of the specified dimension
         */
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.Size'/> class from
        ///    the specified dimensions.
        /// </summary>
        public Size(int width, int height)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        ///    Converts the specified <see cref='System.Drawing.Size'/> to a
        /// <see cref='System.Drawing.SizeF'/>.
        /// </summary>
        public static implicit operator SizeF(Size p) => new SizeF(p.Width, p.Height);

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.Size'/> objects.
        ///    </para>
        /// </summary>
        public static Size operator +(Size sz1, Size sz2) => Add(sz1, sz2);

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.Size'/> by another <see cref='System.Drawing.Size'/>
        ///    </para>
        /// </summary>
        public static Size operator -(Size sz1, Size sz2) => Subtract(sz1, sz2);

        /// <summary>
        ///    Tests whether two <see cref='System.Drawing.Size'/> objects
        ///    are identical.
        /// </summary>
        public static bool operator ==(Size sz1, Size sz2) => sz1.Width == sz2.Width && sz1.Height == sz2.Height;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Size'/> objects are different.
        ///    </para>
        /// </summary>
        public static bool operator !=(Size sz1, Size sz2) => !(sz1 == sz2);

        /// <summary>
        ///    Converts the specified <see cref='System.Drawing.Size'/> to a
        /// <see cref='System.Drawing.Point'/>.
        /// </summary>
        public static explicit operator Point(Size size) => new Point(size.Width, size.Height);

        /// <summary>
        ///    Tests whether this <see cref='System.Drawing.Size'/> has zero
        ///    width and height.
        /// </summary>
        public bool IsEmpty => _width == 0 && _height == 0;

        /**
         * Horizontal dimension
         */

        /// <summary>
        ///    <para>
        ///       Represents the horizontal component of this
        ///    <see cref='System.Drawing.Size'/>.
        ///    </para>
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /**
         * Vertical dimension
         */

        /// <summary>
        ///    Represents the vertical component of this
        /// <see cref='System.Drawing.Size'/>.
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.Size'/> objects.
        ///    </para>
        /// </summary>
        public static Size Add(Size sz1, Size sz2) => new Size(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

        /// <summary>
        ///   Converts a SizeF to a Size by performing a ceiling operation on
        ///   all the coordinates.
        /// </summary>
        public static Size Ceiling(SizeF value) => new Size((int)Math.Ceiling(value.Width), (int)Math.Ceiling(value.Height));

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.Size'/> by another <see cref='System.Drawing.Size'/> .
        ///    </para>
        /// </summary>
        public static Size Subtract(Size sz1, Size sz2) => new Size(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

        /// <summary>
        ///   Converts a SizeF to a Size by performing a truncate operation on
        ///   all the coordinates.
        /// </summary>
        public static Size Truncate(SizeF value) => new Size((int)value.Width, (int)value.Height);

        /// <summary>
        ///   Converts a SizeF to a Size by performing a round operation on
        ///   all the coordinates.
        /// </summary>
        public static Size Round(SizeF value) => new Size((int)Math.Round(value.Width), (int)Math.Round(value.Height));

        /// <summary>
        ///    <para>
        ///       Tests to see whether the specified object is a
        ///    <see cref='System.Drawing.Size'/> 
        ///    with the same dimensions as this <see cref='System.Drawing.Size'/>.
        /// </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Size))
                return false;

            Size comp = (Size)obj;
            return (comp._width == _width) && (comp._height == _height);
        }

        /// <summary>
        ///    <para>
        ///       Returns a hash code.
        ///    </para>
        /// </summary>
        public override int GetHashCode() => HashHelpers.Combine(Width, Height);

        /// <summary>
        ///    <para>
        ///       Creates a human-readable string that represents this
        ///    <see cref='System.Drawing.Size'/>.
        ///    </para>
        /// </summary>
        public override string ToString() => "{Width=" + _width.ToString() + ", Height=" + _height.ToString() + "}";
    }
}

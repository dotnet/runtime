// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Numerics.Hashing;

namespace System.Drawing
{
    /**
     * Represents a dimension in 2D coordinate space
     */
    /// <summary>
    ///    <para>
    ///       Represents the size of a rectangular region
    ///       with an ordered pair of width and height.
    ///    </para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct SizeF : IEquatable<SizeF>
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class.
        /// </summary>
        public static readonly SizeF Empty = new SizeF();
        private float width; // Do not rename (binary serialization) 
        private float height; // Do not rename (binary serialization) 

        /**
         * Create a new SizeF object from another size object
         */
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class
        ///    from the specified existing <see cref='System.Drawing.SizeF'/>.
        /// </summary>
        public SizeF(SizeF size)
        {
            width = size.width;
            height = size.height;
        }

        /**
         * Create a new SizeF object from a point
         */
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class from
        ///       the specified <see cref='System.Drawing.PointF'/>.
        ///    </para>
        /// </summary>
        public SizeF(PointF pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        /**
         * Create a new SizeF object of the specified dimension
         */
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class from
        ///       the specified dimensions.
        ///    </para>
        /// </summary>
        public SizeF(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
        ///    </para>
        /// </summary>
        public static SizeF operator +(SizeF sz1, SizeF sz2) => Add(sz1, sz2);

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
        ///    </para>
        /// </summary>        
        public static SizeF operator -(SizeF sz1, SizeF sz2) => Subtract(sz1, sz2);

        /// <summary>
        /// Multiplies <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
        /// </summary>
        /// <param name="left">Multiplier of type <see cref="float"/>.</param>
        /// <param name="right">Multiplicand of type <see cref="SizeF"/>.</param>
        /// <returns>Product of type <see cref="SizeF"/>.</returns>
        public static SizeF operator *(float left, SizeF right) => Multiply(right, left);

        /// <summary>
        /// Multiplies <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
        /// </summary>
        /// <param name="left">Multiplicand of type <see cref="SizeF"/>.</param>
        /// <param name="right">Multiplier of type <see cref="float"/>.</param>
        /// <returns>Product of type <see cref="SizeF"/>.</returns>
        public static SizeF operator *(SizeF left, float right) => Multiply(left, right);

        /// <summary>
        /// Divides <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
        /// </summary>
        /// <param name="left">Dividend of type <see cref="SizeF"/>.</param>
        /// <param name="right">Divisor of type <see cref="int"/>.</param>
        /// <returns>Result of type <see cref="SizeF"/>.</returns>
        public static SizeF operator /(SizeF left, float right)
            => new SizeF(left.width / right, left.height / right);

        /// <summary>
        ///    Tests whether two <see cref='System.Drawing.SizeF'/> objects
        ///    are identical.
        /// </summary>
        public static bool operator ==(SizeF sz1, SizeF sz2) => sz1.Width == sz2.Width && sz1.Height == sz2.Height;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.SizeF'/> objects are different.
        ///    </para>
        /// </summary>
        public static bool operator !=(SizeF sz1, SizeF sz2) => !(sz1 == sz2);

        /// <summary>
        ///    <para>
        ///       Converts the specified <see cref='System.Drawing.SizeF'/> to a
        ///    <see cref='System.Drawing.PointF'/>.
        ///    </para>
        /// </summary>
        public static explicit operator PointF(SizeF size) => new PointF(size.Width, size.Height);

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.SizeF'/> has zero
        ///       width and height.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => width == 0 && height == 0;

        /**
         * Horizontal dimension
         */

        /// <summary>
        ///    <para>
        ///       Represents the horizontal component of this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </summary>
        public float Width
        {
            get { return width; }
            set { width = value; }
        }

        /**
         * Vertical dimension
         */

        /// <summary>
        ///    <para>
        ///       Represents the vertical component of this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </summary>
        public float Height
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
        ///    </para>
        /// </summary>
        public static SizeF Add(SizeF sz1, SizeF sz2) => new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
        ///       .
        ///    </para>
        /// </summary>        
        public static SizeF Subtract(SizeF sz1, SizeF sz2) => new SizeF(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

        /// <summary>
        ///    <para>
        ///       Tests to see whether the specified object is a
        ///    <see cref='System.Drawing.SizeF'/> 
        ///    with the same dimensions as this <see cref='System.Drawing.SizeF'/>.
        /// </para>
        /// </summary>
        public override bool Equals(object obj) => obj is SizeF && Equals((SizeF)obj);

        public bool Equals(SizeF other) => this == other;

        public override int GetHashCode() => HashHelpers.Combine(Width.GetHashCode(), Height.GetHashCode());

        public PointF ToPointF() => (PointF)this;

        public Size ToSize() => Size.Truncate(this);

        /// <summary>
        ///    <para>
        ///       Creates a human-readable string that represents this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </summary>
        public override string ToString() => "{Width=" + width.ToString() + ", Height=" + height.ToString() + "}";

        /// <summary>
        /// Multiplies <see cref="SizeF"/> by a <see cref="float"/> producing <see cref="SizeF"/>.
        /// </summary>
        /// <param name="size">Multiplicand of type <see cref="SizeF"/>.</param>
        /// <param name="multiplier">Multiplier of type <see cref="float"/>.</param>
        /// <returns>Product of type SizeF.</returns>
        private static SizeF Multiply(SizeF size, float multiplier) =>
            new SizeF(size.width * multiplier, size.height * multiplier);
    }
}


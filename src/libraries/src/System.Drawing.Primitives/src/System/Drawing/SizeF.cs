// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public struct SizeF
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class.
        /// </summary>
        public static readonly SizeF Empty = new SizeF();
        private float _width;
        private float _height;

        /**
         * Create a new SizeF object from another size object
         */
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class
        ///    from the specified existing <see cref='System.Drawing.SizeF'/>.
        /// </summary>
        public SizeF(SizeF size)
        {
            _width = size._width;
            _height = size._height;
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
            _width = pt.X;
            _height = pt.Y;
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
            _width = width;
            _height = height;
        }

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
        ///    </para>
        /// </summary>
        public static SizeF operator +(SizeF sz1, SizeF sz2)
        {
            return Add(sz1, sz2);
        }

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
        ///    </para>
        /// </summary>        
        public static SizeF operator -(SizeF sz1, SizeF sz2)
        {
            return Subtract(sz1, sz2);
        }

        /// <summary>
        ///    Tests whether two <see cref='System.Drawing.SizeF'/> objects
        ///    are identical.
        /// </summary>
        public static bool operator ==(SizeF sz1, SizeF sz2)
        {
            return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
        }

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.SizeF'/> objects are different.
        ///    </para>
        /// </summary>
        public static bool operator !=(SizeF sz1, SizeF sz2)
        {
            return !(sz1 == sz2);
        }

        /// <summary>
        ///    <para>
        ///       Converts the specified <see cref='System.Drawing.SizeF'/> to a
        ///    <see cref='System.Drawing.PointF'/>.
        ///    </para>
        /// </summary>
        public static explicit operator PointF(SizeF size)
        {
            return new PointF(size.Width, size.Height);
        }

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.SizeF'/> has zero
        ///       width and height.
        ///    </para>
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _width == 0 && _height == 0;
            }
        }

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
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
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
        ///       Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
        ///    </para>
        /// </summary>
        public static SizeF Add(SizeF sz1, SizeF sz2)
        {
            return new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
        ///       .
        ///    </para>
        /// </summary>        
        public static SizeF Subtract(SizeF sz1, SizeF sz2)
        {
            return new SizeF(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        /// <summary>
        ///    <para>
        ///       Tests to see whether the specified object is a
        ///    <see cref='System.Drawing.SizeF'/> 
        ///    with the same dimensions as this <see cref='System.Drawing.SizeF'/>.
        /// </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is SizeF))
                return false;

            SizeF comp = (SizeF)obj;

            return (comp.Width == Width) && (comp.Height == Height);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public PointF ToPointF()
        {
            return (PointF)this;
        }

        public Size ToSize()
        {
            return Size.Truncate(this);
        }

        /// <summary>
        ///    <para>
        ///       Creates a human-readable string that represents this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </summary>
        public override string ToString()
        {
            return "{Width=" + _width.ToString() + ", Height=" + _height.ToString() + "}";
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.Serialization;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies the margins of a printed page.
    /// </summary>
    public partial class Margins : ICloneable
    {
        private int _left;
        private int _right;
        private int _bottom;
        private int _top;

        [OptionalField]
        private double _doubleLeft;
        [OptionalField]
        private double _doubleRight;
        [OptionalField]
        private double _doubleTop;
        [OptionalField]
        private double _doubleBottom;

        /// <summary>
        /// Initializes a new instance of a the <see cref='Margins'/> class with one-inch margins.
        /// </summary>
        public Margins() : this(100, 100, 100, 100)
        {
        }

        /// <summary>
        /// Initializes a new instance of a the <see cref='Margins'/> class with the specified left, right, top, and bottom margins.
        /// </summary>
        public Margins(int left, int right, int top, int bottom)
        {
            CheckMargin(left, "left");
            CheckMargin(right, "right");
            CheckMargin(top, "top");
            CheckMargin(bottom, "bottom");

            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;

            _doubleLeft = (double)left;
            _doubleRight = (double)right;
            _doubleTop = (double)top;
            _doubleBottom = (double)bottom;
        }

        /// <summary>
        /// Gets or sets the left margin, in hundredths of an inch.
        /// </summary>
        public int Left
        {
            get { return _left; }
            set
            {
                CheckMargin(value, "Left");
                _left = value;
                _doubleLeft = (double)value;
            }
        }

        /// <summary>
        /// Gets or sets the right margin, in hundredths of an inch.
        /// </summary>
        public int Right
        {
            get { return _right; }
            set
            {
                CheckMargin(value, "Right");
                _right = value;
                _doubleRight = (double)value;
            }
        }

        /// <summary>
        /// Gets or sets the top margin, in hundredths of an inch.
        /// </summary>
        public int Top
        {
            get { return _top; }
            set
            {
                CheckMargin(value, "Top");
                _top = value;
                _doubleTop = (double)value;
            }
        }

        /// <summary>
        /// Gets or sets the bottom margin, in hundredths of an inch.
        /// </summary>
        public int Bottom
        {
            get { return _bottom; }
            set
            {
                CheckMargin(value, "Bottom");
                _bottom = value;
                _doubleBottom = (double)value;
            }
        }

        /// <summary>
        /// Gets or sets the left margin with double value, in hundredths of an inch.
        /// When use the setter, the ranger of setting double value should between
        /// 0 to Int.MaxValue;
        /// </summary>
        internal double DoubleLeft
        {
            get { return _doubleLeft; }
            set
            {
                Left = (int)Math.Round(value);
                _doubleLeft = value;
            }
        }

        /// <summary>
        /// Gets or sets the right margin with double value, in hundredths of an inch.
        /// When use the setter, the ranger of setting double value should between
        /// 0 to Int.MaxValue;
        /// </summary>
        internal double DoubleRight
        {
            get { return _doubleRight; }
            set
            {
                Right = (int)Math.Round(value);
                _doubleRight = value;
            }
        }

        /// <summary>
        /// Gets or sets the top margin with double value, in hundredths of an inch.
        /// When use the setter, the ranger of setting double value should between
        /// 0 to Int.MaxValue;
        /// </summary>
        internal double DoubleTop
        {
            get { return _doubleTop; }
            set
            {
                Top = (int)Math.Round(value);
                _doubleTop = value;
            }
        }

        /// <summary>
        /// Gets or sets the bottom margin with double value, in hundredths of an inch.
        /// When use the setter, the ranger of setting double value should between
        /// 0 to Int.MaxValue;
        /// </summary>
        internal double DoubleBottom
        {
            get { return _doubleBottom; }
            set
            {
                Bottom = (int)Math.Round(value);
                _doubleBottom = value;
            }
        }

        private void CheckMargin(int margin, string name)
        {
            if (margin < 0)
                throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx, name, margin, "0"));
        }

        /// <summary>
        /// Retrieves a duplicate of this object, member by member.
        /// </summary>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Compares this <see cref='Margins'/> to a specified <see cref='Margins'/> to see whether they
        /// are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            Margins margins = obj as Margins;
            if (margins == this)
                return true;
            if (margins == null)
                return false;

            return margins.Left == Left
            && margins.Right == Right
            && margins.Top == Top
            && margins.Bottom == Bottom;
        }

        /// <summary>
        /// Calculates and retrieves a hash code based on the left, right, top, and bottom margins.
        /// </summary>
        public override int GetHashCode()
        {
            // return HashCodes.Combine(left, right, top, bottom);
            uint left = (uint)Left;
            uint right = (uint)Right;
            uint top = (uint)Top;
            uint bottom = (uint)Bottom;

            uint result = left ^
                           ((right << 13) | (right >> 19)) ^
                           ((top << 26) | (top >> 6)) ^
                           ((bottom << 7) | (bottom >> 25));

            return unchecked((int)result);
        }

        /// <summary>
        /// Tests whether two <see cref='Margins'/> objects are identical.
        /// </summary>
        public static bool operator ==(Margins m1, Margins m2)
        {
            if (object.ReferenceEquals(m1, null) != object.ReferenceEquals(m2, null))
            {
                return false;
            }
            if (!object.ReferenceEquals(m1, null))
            {
                return m1.Left == m2.Left && m1.Top == m2.Top && m1.Right == m2.Right && m1.Bottom == m2.Bottom;
            }
            return true;
        }

        /// <summary>
        /// Tests whether two <see cref='Margins'/> objects are different.
        /// </summary>
        public static bool operator !=(Margins m1, Margins m2)
        {
            return !(m1 == m2);
        }

        /// <summary>
        /// Provides some interesting information for the Margins in String form.
        /// </summary>
        public override string ToString()
        {
            return "[Margins"
            + " Left=" + Left.ToString(CultureInfo.InvariantCulture)
            + " Right=" + Right.ToString(CultureInfo.InvariantCulture)
            + " Top=" + Top.ToString(CultureInfo.InvariantCulture)
            + " Bottom=" + Bottom.ToString(CultureInfo.InvariantCulture)
            + "]";
        }
    }
}

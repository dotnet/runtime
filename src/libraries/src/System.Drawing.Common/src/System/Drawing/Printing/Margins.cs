// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    using System.Globalization;
    using System.Runtime.Serialization;

    /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the margins of a printed page.
    ///    </para>
    /// </devdoc>
    [Serializable]
    public class Margins : ICloneable
    {
        private int _left;
        private int _right;
        private int _top;
        private int _bottom;

        [OptionalField]
        private double _doubleLeft;
        [OptionalField]
        private double _doubleRight;
        [OptionalField]
        private double _doubleTop;
        [OptionalField]
        private double _doubleBottom;

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Margins"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of a the <see cref='System.Drawing.Printing.Margins'/> class with one-inch margins.
        ///    </para>
        /// </devdoc>
        public Margins() : this(100, 100, 100, 100)
        {
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Margins1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of a the <see cref='System.Drawing.Printing.Margins'/> class with the specified left, right, top, and bottom
        ///       margins.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Left"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the left margin, in hundredths of an inch.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Right"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the right margin, in hundredths of an inch.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Top"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the top margin, in hundredths of an inch.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Bottom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the bottom margin, in hundredths of an inch.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.DoubleLeft"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the left margin with double value, in hundredths of an inch.
        ///       When use the setter, the ranger of setting double value should between
        ///       0 to Int.MaxValue;
        ///    </para>
        /// </devdoc>
        internal double DoubleLeft
        {
            get { return _doubleLeft; }
            set
            {
                Left = (int)Math.Round(value);
                _doubleLeft = value;
            }
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.DoubleRight"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the right margin with double value, in hundredths of an inch.
        ///       When use the setter, the ranger of setting double value should between
        ///       0 to Int.MaxValue;
        ///    </para>
        /// </devdoc>
        internal double DoubleRight
        {
            get { return _doubleRight; }
            set
            {
                Right = (int)Math.Round(value);
                _doubleRight = value;
            }
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.DoubleTop"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the top margin with double value, in hundredths of an inch.
        ///       When use the setter, the ranger of setting double value should between
        ///       0 to Int.MaxValue;
        ///    </para>
        /// </devdoc>
        internal double DoubleTop
        {
            get { return _doubleTop; }
            set
            {
                Top = (int)Math.Round(value);
                _doubleTop = value;
            }
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.DoubleBottom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the bottom margin with double value, in hundredths of an inch.
        ///       When use the setter, the ranger of setting double value should between
        ///       0 to Int.MaxValue;
        ///    </para>
        /// </devdoc>
        internal double DoubleBottom
        {
            get { return _doubleBottom; }
            set
            {
                Bottom = (int)Math.Round(value);
                _doubleBottom = value;
            }
        }

        [OnDeserialized()]
        private void OnDeserializedMethod(StreamingContext context)
        {
            if (_doubleLeft == 0 && _left != 0)
            {
                _doubleLeft = (double)_left;
            }

            if (_doubleRight == 0 && _right != 0)
            {
                _doubleRight = (double)_right;
            }

            if (_doubleTop == 0 && _top != 0)
            {
                _doubleTop = (double)_top;
            }

            if (_doubleBottom == 0 && _bottom != 0)
            {
                _doubleBottom = (double)_bottom;
            }
        }

        private void CheckMargin(int margin, string name)
        {
            if (margin < 0)
                throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx, name, margin, "0"));
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Clone"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Retrieves a duplicate of this object, member by member.
        ///    </para>
        /// </devdoc>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.Equals"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Compares this <see cref='System.Drawing.Printing.Margins'/> to a specified <see cref='System.Drawing.Printing.Margins'/> to see whether they
        ///       are equal.
        ///    </para>
        /// </devdoc>
        public override bool Equals(object obj)
        {
            Margins margins = obj as Margins;
            if (margins == this) return true;
            if (margins == null) return false;

            return margins.Left == Left
            && margins.Right == Right
            && margins.Top == Top
            && margins.Bottom == Bottom;
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.GetHashCode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Calculates and retrieves a hash code based on the left, right, top, and bottom
        ///       margins.
        ///    </para>
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.operator=="]/*' />
        /// <devdoc>
        ///    Tests whether two <see cref='System.Drawing.Printing.Margins'/> objects
        ///    are identical.
        /// </devdoc>
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

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.operator!="]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Printing.Margins'/> objects are different.
        ///    </para>
        /// </devdoc>
        public static bool operator !=(Margins m1, Margins m2)
        {
            return !(m1 == m2);
        }

        /// <include file='doc\Margins.uex' path='docs/doc[@for="Margins.ToString"]/*' />
        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information for the Margins in
        ///       String form.
        ///    </para>
        /// </devdoc>
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

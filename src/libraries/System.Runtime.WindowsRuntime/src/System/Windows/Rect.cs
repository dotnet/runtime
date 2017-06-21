// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Globalization;
using System.Runtime.InteropServices;

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning disable 436   // Redefining types from Windows.Foundation
#endif // !FEATURE_SLJ_PROJECTION_COMPAT

#if FEATURE_SLJ_PROJECTION_COMPAT
namespace System.Windows
#else // !FEATURE_SLJ_PROJECTION_COMPAT


namespace Windows.Foundation
#endif // FEATURE_SLJ_PROJECTION_COMPAT
{
    //
    // Rect is the managed projection of Windows.Foundation.Rect.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IFormattable
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        private const double EmptyX = Double.PositiveInfinity;
        private const double EmptyY = Double.PositiveInfinity;
        private const double EmptyWidth = Double.NegativeInfinity;
        private const double EmptyHeight = Double.NegativeInfinity;

        private static readonly Rect s_empty = CreateEmptyRect();

        public Rect(double x,
                    double y,
                    double width,
                    double height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), SR.ArgumentOutOfRange_NeedNonNegNum);

            _x = (float)x;
            _y = (float)y;
            _width = (float)width;
            _height = (float)height;
        }

        public Rect(Point point1,
                    Point point2)
        {
            _x = (float)Math.Min(point1.X, point2.X);
            _y = (float)Math.Min(point1.Y, point2.Y);

            _width = (float)Math.Max(Math.Max(point1.X, point2.X) - _x, 0);
            _height = (float)Math.Max(Math.Max(point1.Y, point2.Y) - _y, 0);
        }

        public Rect(Point location, Size size)
        {
            if (size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = (float)location.X;
                _y = (float)location.Y;
                _width = (float)size.Width;
                _height = (float)size.Height;
            }
        }

        public double X
        {
            get { return _x; }
            set { _x = (float)value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = (float)value; }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), SR.ArgumentOutOfRange_NeedNonNegNum);

                _width = (float)value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), SR.ArgumentOutOfRange_NeedNonNegNum);

                _height = (float)value;
            }
        }

        public double Left
        {
            get { return _x; }
        }

        public double Top
        {
            get { return _y; }
        }

        public double Right
        {
            get
            {
                if (IsEmpty)
                {
                    return Double.NegativeInfinity;
                }

                return _x + _width;
            }
        }

        public double Bottom
        {
            get
            {
                if (IsEmpty)
                {
                    return Double.NegativeInfinity;
                }

                return _y + _height;
            }
        }

        public static Rect Empty
        {
            get { return s_empty; }
        }

        public bool IsEmpty
        {
            get { return _width < 0; }
        }

        public bool Contains(Point point)
        {
            return ContainsInternal(point.X, point.Y);
        }

        public void Intersect(Rect rect)
        {
            if (!this.IntersectsWith(rect))
            {
                this = s_empty;
            }
            else
            {
                double left = Math.Max(X, rect.X);
                double top = Math.Max(Y, rect.Y);

                //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                Width = Math.Max(Math.Min(X + Width, rect.X + rect.Width) - left, 0);
                Height = Math.Max(Math.Min(Y + Height, rect.Y + rect.Height) - top, 0);

                X = left;
                Y = top;
            }
        }

        public void Union(Rect rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                double left = Math.Min(Left, rect.Left);
                double top = Math.Min(Top, rect.Top);


                // We need this check so that the math does not result in NaN
                if ((rect.Width == Double.PositiveInfinity) || (Width == Double.PositiveInfinity))
                {
                    Width = Double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxRight = Math.Max(Right, rect.Right);
                    Width = Math.Max(maxRight - left, 0);
                }

                // We need this check so that the math does not result in NaN
                if ((rect.Height == Double.PositiveInfinity) || (Height == Double.PositiveInfinity))
                {
                    Height = Double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxBottom = Math.Max(Bottom, rect.Bottom);
                    Height = Math.Max(maxBottom - top, 0);
                }

                X = left;
                Y = top;
            }
        }

        public void Union(Point point)
        {
            Union(new Rect(point, point));
        }

        private bool ContainsInternal(double x, double y)
        {
            return ((x >= X) && (x - Width <= X) &&
                    (y >= Y) && (y - Height <= Y));
        }

        internal bool IntersectsWith(Rect rect)
        {
            if (Width < 0 || rect.Width < 0)
            {
                return false;
            }

            return (rect.X <= X + Width) &&
                   (rect.X + rect.Width >= X) &&
                   (rect.Y <= Y + Height) &&
                   (rect.Y + rect.Height >= Y);
        }

        private static Rect CreateEmptyRect()
        {
            Rect rect = new Rect();

            // TODO: for consistency with width/height we should change these
            //       to assign directly to the backing fields.
            rect.X = EmptyX;
            rect.Y = EmptyY;

            // the width and height properties prevent assignment of
            // negative numbers so assign directly to the backing fields.
            rect._width = (float)EmptyWidth;
            rect._height = (float)EmptyHeight;

            return rect;
        }

        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsEmpty)
            {
                return SR.DirectUI_Empty;
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = TokenizerHelper.GetNumericListSeparator(provider);
            return String.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}",
                                 separator,
                                 _x,
                                 _y,
                                 _width,
                                 _height);
        }

        public bool Equals(Rect value)
        {
            return (this == value);
        }

        public static bool operator ==(Rect rect1, Rect rect2)
        {
            return rect1.X == rect2.X &&
                   rect1.Y == rect2.Y &&
                   rect1.Width == rect2.Width &&
                   rect1.Height == rect2.Height;
        }

        public static bool operator !=(Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }

        public override bool Equals(object o)
        {
            return o is Rect && this == (Rect)o;
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return X.GetHashCode() ^
                   Y.GetHashCode() ^
                   Width.GetHashCode() ^
                   Height.GetHashCode();
        }
    }
}

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning restore 436
#endif // !FEATURE_SLJ_PROJECTION_COMPAT

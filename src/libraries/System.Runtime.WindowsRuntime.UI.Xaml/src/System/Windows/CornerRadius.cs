// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 436   // Redefining types from Windows.Foundation
namespace Windows.UI.Xaml
{
    //
    // CornerRadius is the managed projection of Windows.UI.Xaml.CornerRadius.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct CornerRadius
    {
        private double _TopLeft;
        private double _TopRight;
        private double _BottomRight;
        private double _BottomLeft;

        public CornerRadius(double uniformRadius)
        {
            Validate(uniformRadius, uniformRadius, uniformRadius, uniformRadius);
            _TopLeft = _TopRight = _BottomRight = _BottomLeft = uniformRadius;
        }

        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            Validate(topLeft, topRight, bottomRight, bottomLeft);

            _TopLeft = topLeft;
            _TopRight = topRight;
            _BottomRight = bottomRight;
            _BottomLeft = bottomLeft;
        }

        private static void Validate(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            if (topLeft < 0.0 || Double.IsNaN(topLeft))
                throw new ArgumentException(SR.Format(SR.DirectUI_CornerRadius_InvalidMember, "TopLeft"));

            if (topRight < 0.0 || Double.IsNaN(topRight))
                throw new ArgumentException(SR.Format(SR.DirectUI_CornerRadius_InvalidMember, "TopRight"));

            if (bottomRight < 0.0 || Double.IsNaN(bottomRight))
                throw new ArgumentException(SR.Format(SR.DirectUI_CornerRadius_InvalidMember, "BottomRight"));

            if (bottomLeft < 0.0 || Double.IsNaN(bottomLeft))
                throw new ArgumentException(SR.Format(SR.DirectUI_CornerRadius_InvalidMember, "BottomLeft"));
        }

        public override string ToString()
        {
            return ToString(CultureInfo.InvariantCulture);
        }

        internal string ToString(CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 48 = 4x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  4 = 4x separator characters
            StringBuilder sb = new StringBuilder(64);

            sb.Append(InternalToString(_TopLeft, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_TopRight, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_BottomRight, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_BottomLeft, cultureInfo));
            return sb.ToString();
        }

        internal string InternalToString(double l, CultureInfo cultureInfo)
        {
            if (Double.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }

        public override bool Equals(object obj)
        {
            if (obj is CornerRadius)
            {
                CornerRadius otherObj = (CornerRadius)obj;
                return (this == otherObj);
            }
            return (false);
        }

        public bool Equals(CornerRadius cornerRadius)
        {
            return (this == cornerRadius);
        }

        public override int GetHashCode()
        {
            return _TopLeft.GetHashCode() ^ _TopRight.GetHashCode() ^ _BottomLeft.GetHashCode() ^ _BottomRight.GetHashCode();
        }

        public static bool operator ==(CornerRadius cr1, CornerRadius cr2)
        {
            return cr1._TopLeft == cr2._TopLeft && cr1._TopRight == cr2._TopRight && cr1._BottomRight == cr2._BottomRight && cr1._BottomLeft == cr2._BottomLeft;
        }

        public static bool operator !=(CornerRadius cr1, CornerRadius cr2)
        {
            return (!(cr1 == cr2));
        }

        public double TopLeft
        {
            get { return _TopLeft; }
            set
            {
                Validate(value, 0, 0, 0);
                _TopLeft = value;
            }
        }

        public double TopRight
        {
            get { return _TopRight; }
            set
            {
                Validate(0, value, 0, 0);
                _TopRight = value;
            }
        }

        public double BottomRight
        {
            get { return _BottomRight; }
            set
            {
                Validate(0, 0, value, 0);
                _BottomRight = value;
            }
        }

        public double BottomLeft
        {
            get { return _BottomLeft; }
            set
            {
                Validate(0, 0, 0, value);
                _BottomLeft = value;
            }
        }
    }
}

#pragma warning restore 436

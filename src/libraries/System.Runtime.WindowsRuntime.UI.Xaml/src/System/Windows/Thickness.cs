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
    // Thickness is the managed projection of Windows.UI.Xaml.Thickness.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Thickness
    {
        private double _Left;
        private double _Top;
        private double _Right;
        private double _Bottom;

        public Thickness(double uniformLength)
        {
            _Left = _Top = _Right = _Bottom = uniformLength;
        }

        public Thickness(double left, double top, double right, double bottom)
        {
            _Left = left;
            _Top = top;
            _Right = right;
            _Bottom = bottom;
        }

        public double Left
        {
            get { return _Left; }
            set { _Left = value; }
        }

        public double Top
        {
            get { return _Top; }
            set { _Top = value; }
        }

        public double Right
        {
            get { return _Right; }
            set { _Right = value; }
        }

        public double Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
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

            sb.Append(InternalToString(_Left, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_Top, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_Right, cultureInfo));
            sb.Append(listSeparator);
            sb.Append(InternalToString(_Bottom, cultureInfo));
            return sb.ToString();
        }

        internal string InternalToString(double l, CultureInfo cultureInfo)
        {
            if (Double.IsNaN(l)) return "Auto";
            return Convert.ToString(l, cultureInfo);
        }

        public override bool Equals(object obj)
        {
            if (obj is Thickness)
            {
                Thickness otherObj = (Thickness)obj;
                return (this == otherObj);
            }
            return (false);
        }

        public bool Equals(Thickness thickness)
        {
            return (this == thickness);
        }

        public override int GetHashCode()
        {
            return _Left.GetHashCode() ^ _Top.GetHashCode() ^ _Right.GetHashCode() ^ _Bottom.GetHashCode();
        }

        public static bool operator ==(Thickness t1, Thickness t2)
        {
            return t1._Left == t2._Left && t1._Top == t2._Top && t1._Right == t2._Right && t1._Bottom == t2._Bottom;
        }

        public static bool operator !=(Thickness t1, Thickness t2)
        {
            return (!(t1 == t2));
        }
    }
}

#pragma warning restore 436

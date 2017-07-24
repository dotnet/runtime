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
    // GridLength is the managed projection of Windows.UI.Xaml.GridLength.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // GridUnitType is the managed projection of Windows.UI.Xaml.GridUnitType.  Any changes to this
    // enumeration must be exactly mirrored on the native WinRT side as well.
    //
    // Note that these types are owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //
    public enum GridUnitType
    {
        Auto = 0,
        Pixel,
        Star,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GridLength
    {
        private double _unitValue;
        private GridUnitType _unitType;

        private const double Default = 1.0;
        private static readonly GridLength s_auto = new GridLength(Default, GridUnitType.Auto);

        public GridLength(double pixels)
            : this(pixels, GridUnitType.Pixel)
        {
        }

        public GridLength(double value, GridUnitType type)
        {
            if (!double.IsFinite(value) || value < 0.0)
            {
                throw new ArgumentException(SR.DirectUI_InvalidArgument, nameof(value));
            }
            if (type != GridUnitType.Auto && type != GridUnitType.Pixel && type != GridUnitType.Star)
            {
                throw new ArgumentException(SR.DirectUI_InvalidArgument, nameof(type));
            }

            _unitValue = (type == GridUnitType.Auto) ? Default : value;
            _unitType = type;
        }


        public double Value { get { return ((_unitType == GridUnitType.Auto) ? s_auto._unitValue : _unitValue); } }
        public GridUnitType GridUnitType { get { return (_unitType); } }


        public bool IsAbsolute { get { return (_unitType == GridUnitType.Pixel); } }
        public bool IsAuto { get { return (_unitType == GridUnitType.Auto); } }
        public bool IsStar { get { return (_unitType == GridUnitType.Star); } }

        public static GridLength Auto
        {
            get { return (s_auto); }
        }


        public static bool operator ==(GridLength gl1, GridLength gl2)
        {
            return (gl1.GridUnitType == gl2.GridUnitType
                    && gl1.Value == gl2.Value);
        }

        public static bool operator !=(GridLength gl1, GridLength gl2)
        {
            return (gl1.GridUnitType != gl2.GridUnitType
                    || gl1.Value != gl2.Value);
        }

        override public bool Equals(object oCompare)
        {
            if (oCompare is GridLength)
            {
                GridLength l = (GridLength)oCompare;
                return (this == l);
            }
            else
                return false;
        }

        public bool Equals(GridLength gridLength)
        {
            return (this == gridLength);
        }

        public override int GetHashCode()
        {
            return ((int)_unitValue + (int)_unitType);
        }

        public override string ToString()
        {
            return this.ToString(CultureInfo.InvariantCulture);
        }

        internal string ToString(CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 12 = 1x double (twelve digits is generous for the range of values likely)
            //  8 = 4x Unit Type string (approx two characters)
            //  2 = 2x separator characters

            if (_unitType == GridUnitType.Auto)
            {
                return "Auto";
            }
            else if (_unitType == GridUnitType.Pixel)
            {
                return Convert.ToString(_unitValue, cultureInfo);
            }
            else
            {
                return Convert.ToString(_unitValue, cultureInfo) + "*";
            }
        }
    }
}

#pragma warning restore 436

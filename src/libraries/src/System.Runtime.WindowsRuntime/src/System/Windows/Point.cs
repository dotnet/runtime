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
    // Point is the managed projection of Windows.Foundation.Point.  Any changes to the layout
    // of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Point : IFormattable
    {
        internal float _x;
        internal float _y;

        public Point(double x, double y)
        {
            _x = (float)x;
            _y = (float)y;
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

        private string ConvertToString(string format, IFormatProvider provider)
        {
            // Helper to get the numeric list separator for a given culture.
            char separator = TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}",
                                 separator,
                                 _x,
                                 _y);
        }

        public static bool operator ==(Point point1, Point point2)
        {
            return point1.X == point2.X &&
                   point1.Y == point2.Y;
        }

        public static bool operator !=(Point point1, Point point2)
        {
            return !(point1 == point2);
        }

        public override bool Equals(object o)
        {
            return o is Point && this == (Point)o;
        }

        public bool Equals(Point value)
        {
            return (this == value);
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return X.GetHashCode() ^
                   Y.GetHashCode();
        }
    }
}

#if !FEATURE_SLJ_PROJECTION_COMPAT
#pragma warning restore 436
#endif // !FEATURE_SLJ_PROJECTION_COMPAT


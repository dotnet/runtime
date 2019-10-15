// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;
using Point = Windows.Foundation.Point;

using Windows.Foundation;

#pragma warning disable 436   // Redefining types from Windows.Foundation

namespace Windows.UI.Xaml.Media
{
    //
    // Matrix is the managed projection of Windows.UI.Xaml.Media.Matrix. Any changes to the layout of
    // this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix : IFormattable
    {
        public Matrix(double m11, double m12,
                      double m21, double m22,
                      double offsetX, double offsetY)
        {
            _m11 = m11;
            _m12 = m12;
            _m21 = m21;
            _m22 = m22;
            _offsetX = offsetX;
            _offsetY = offsetY;
        }

        // the transform is identity by default
        private static Matrix s_identity = CreateIdentity();

        public double M11
        {
            get
            {
                return _m11;
            }
            set
            {
                _m11 = value;
            }
        }

        public double M12
        {
            get
            {
                return _m12;
            }
            set
            {
                _m12 = value;
            }
        }

        public double M21
        {
            get
            {
                return _m21;
            }
            set
            {
                _m21 = value;
            }
        }

        public double M22
        {
            get
            {
                return _m22;
            }
            set
            {
                _m22 = value;
            }
        }

        public double OffsetX
        {
            get
            {
                return _offsetX;
            }
            set
            {
                _offsetX = value;
            }
        }

        public double OffsetY
        {
            get
            {
                return _offsetY;
            }
            set
            {
                _offsetY = value;
            }
        }

        public static Matrix Identity
        {
            get
            {
                return s_identity;
            }
        }

        public bool IsIdentity
        {
            get
            {
                return (_m11 == 1 && _m12 == 0 && _m21 == 0 && _m22 == 1 && _offsetX == 0 && _offsetY == 0);
            }
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
            if (IsIdentity)
            {
                return "Identity";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}{0}{5:" + format + "}{0}{6:" + format + "}",
                                 separator,
                                 _m11,
                                 _m12,
                                 _m21,
                                 _m22,
                                 _offsetX,
                                 _offsetY);
        }

        public Point Transform(Point point)
        {
            float x = (float)point.X;
            float y = (float)point.Y;
            this.MultiplyPoint(ref x, ref y);
            Point point2 = new Point(x, y);
            return point2;
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return M11.GetHashCode() ^
                   M12.GetHashCode() ^
                   M21.GetHashCode() ^
                   M22.GetHashCode() ^
                   OffsetX.GetHashCode() ^
                   OffsetY.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return o is Matrix && Matrix.Equals(this, (Matrix)o);
        }

        public bool Equals(Matrix value)
        {
            return Matrix.Equals(this, value);
        }

        public static bool operator ==(Matrix matrix1, Matrix matrix2)
        {
            return matrix1.M11 == matrix2.M11 &&
                   matrix1.M12 == matrix2.M12 &&
                   matrix1.M21 == matrix2.M21 &&
                   matrix1.M22 == matrix2.M22 &&
                   matrix1.OffsetX == matrix2.OffsetX &&
                   matrix1.OffsetY == matrix2.OffsetY;
        }

        public static bool operator !=(Matrix matrix1, Matrix matrix2)
        {
            return !(matrix1 == matrix2);
        }

        private static Matrix CreateIdentity()
        {
            Matrix matrix = new Matrix();
            matrix.SetMatrix(1, 0,
                             0, 1,
                             0, 0);
            return matrix;
        }

        private void SetMatrix(double m11, double m12,
                               double m21, double m22,
                               double offsetX, double offsetY)
        {
            _m11 = m11;
            _m12 = m12;
            _m21 = m21;
            _m22 = m22;
            _offsetX = offsetX;
            _offsetY = offsetY;
        }

        private void MultiplyPoint(ref float x, ref float y)
        {
            double num = (y * _m21) + _offsetX;
            double num2 = (x * _m12) + _offsetY;
            x *= (float)_m11;
            x += (float)num;
            y *= (float)_m22;
            y += (float)num2;
        }

        private static bool Equals(Matrix matrix1, Matrix matrix2)
        {
            return matrix1.M11.Equals(matrix2.M11) &&
                   matrix1.M12.Equals(matrix2.M12) &&
                   matrix1.M21.Equals(matrix2.M21) &&
                   matrix1.M22.Equals(matrix2.M22) &&
                   matrix1.OffsetX.Equals(matrix2.OffsetX) &&
                   matrix1.OffsetY.Equals(matrix2.OffsetY);
        }

        private double _m11;
        private double _m12;
        private double _m21;
        private double _m22;
        private double _offsetX;
        private double _offsetY;
    }
}

#pragma warning restore 436

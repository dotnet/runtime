// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

//

using System;
using System.Runtime.InteropServices;

using Windows.Foundation;

#pragma warning disable 436   // Redefining types from Windows.Foundation

namespace Windows.UI.Xaml.Media.Media3D
{
    //
    // Matrix3D is the managed projection of Windows.UI.Xaml.Media.Media3D.Matrix3D. Any
    // changes to the layout of this type must be exactly mirrored on the native WinRT side as well.
    //
    // Note that this type is owned by the Jupiter team.  Please contact them before making any
    // changes here.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix3D : IFormattable
    {
        // Assuming this matrix has fourth column of 0,0,0,1 and isn't identity this function:
        // Returns false if HasInverse is false, otherwise inverts the matrix.
        private bool NormalizedAffineInvert()
        {
            double z20 = _m12 * _m23 - _m22 * _m13;
            double z10 = _m32 * _m13 - _m12 * _m33;
            double z00 = _m22 * _m33 - _m32 * _m23;
            double det = _m31 * z20 + _m21 * z10 + _m11 * z00;

            if (IsZero(det))
            {
                return false;
            }

            // Compute 3x3 non-zero cofactors for the 2nd column
            double z21 = _m21 * _m13 - _m11 * _m23;
            double z11 = _m11 * _m33 - _m31 * _m13;
            double z01 = _m31 * _m23 - _m21 * _m33;

            // Compute all six 2x2 determinants of 1st two columns
            double y01 = _m11 * _m22 - _m21 * _m12;
            double y02 = _m11 * _m32 - _m31 * _m12;
            double y03 = _m11 * _offsetY - _offsetX * _m12;
            double y12 = _m21 * _m32 - _m31 * _m22;
            double y13 = _m21 * _offsetY - _offsetX * _m22;
            double y23 = _m31 * _offsetY - _offsetX * _m32;

            // Compute all non-zero and non-one 3x3 cofactors for 2nd
            // two columns
            double z23 = _m23 * y03 - _offsetZ * y01 - _m13 * y13;
            double z13 = _m13 * y23 - _m33 * y03 + _offsetZ * y02;
            double z03 = _m33 * y13 - _offsetZ * y12 - _m23 * y23;
            double z22 = y01;
            double z12 = -y02;
            double z02 = y12;

            double rcp = 1.0 / det;

            // Multiply all 3x3 cofactors by reciprocal & transpose
            _m11 = (z00 * rcp);
            _m12 = (z10 * rcp);
            _m13 = (z20 * rcp);

            _m21 = (z01 * rcp);
            _m22 = (z11 * rcp);
            _m23 = (z21 * rcp);

            _m31 = (z02 * rcp);
            _m32 = (z12 * rcp);
            _m33 = (z22 * rcp);

            _offsetX = (z03 * rcp);
            _offsetY = (z13 * rcp);
            _offsetZ = (z23 * rcp);

            return true;
        }

        // RETURNS true if has inverse & invert was done.  Otherwise returns false & leaves matrix unchanged.
        private bool InvertCore()
        {
            if (IsAffine)
            {
                return NormalizedAffineInvert();
            }

            // compute all six 2x2 determinants of 2nd two columns
            double y01 = _m13 * _m24 - _m23 * _m14;
            double y02 = _m13 * _m34 - _m33 * _m14;
            double y03 = _m13 * _m44 - _offsetZ * _m14;
            double y12 = _m23 * _m34 - _m33 * _m24;
            double y13 = _m23 * _m44 - _offsetZ * _m24;
            double y23 = _m33 * _m44 - _offsetZ * _m34;

            // Compute 3x3 cofactors for 1st the column
            double z30 = _m22 * y02 - _m32 * y01 - _m12 * y12;
            double z20 = _m12 * y13 - _m22 * y03 + _offsetY * y01;
            double z10 = _m32 * y03 - _offsetY * y02 - _m12 * y23;
            double z00 = _m22 * y23 - _m32 * y13 + _offsetY * y12;

            // Compute 4x4 determinant
            double det = _offsetX * z30 + _m31 * z20 + _m21 * z10 + _m11 * z00;

            if (IsZero(det))
            {
                return false;
            }

            // Compute 3x3 cofactors for the 2nd column
            double z31 = _m11 * y12 - _m21 * y02 + _m31 * y01;
            double z21 = _m21 * y03 - _offsetX * y01 - _m11 * y13;
            double z11 = _m11 * y23 - _m31 * y03 + _offsetX * y02;
            double z01 = _m31 * y13 - _offsetX * y12 - _m21 * y23;

            // Compute all six 2x2 determinants of 1st two columns
            y01 = _m11 * _m22 - _m21 * _m12;
            y02 = _m11 * _m32 - _m31 * _m12;
            y03 = _m11 * _offsetY - _offsetX * _m12;
            y12 = _m21 * _m32 - _m31 * _m22;
            y13 = _m21 * _offsetY - _offsetX * _m22;
            y23 = _m31 * _offsetY - _offsetX * _m32;

            // Compute all 3x3 cofactors for 2nd two columns
            double z33 = _m13 * y12 - _m23 * y02 + _m33 * y01;
            double z23 = _m23 * y03 - _offsetZ * y01 - _m13 * y13;
            double z13 = _m13 * y23 - _m33 * y03 + _offsetZ * y02;
            double z03 = _m33 * y13 - _offsetZ * y12 - _m23 * y23;
            double z32 = _m24 * y02 - _m34 * y01 - _m14 * y12;
            double z22 = _m14 * y13 - _m24 * y03 + _m44 * y01;
            double z12 = _m34 * y03 - _m44 * y02 - _m14 * y23;
            double z02 = _m24 * y23 - _m34 * y13 + _m44 * y12;

            double rcp = 1.0 / det;

            // Multiply all 3x3 cofactors by reciprocal & transpose
            _m11 = (z00 * rcp);
            _m12 = (z10 * rcp);
            _m13 = (z20 * rcp);
            _m14 = (z30 * rcp);

            _m21 = (z01 * rcp);
            _m22 = (z11 * rcp);
            _m23 = (z21 * rcp);
            _m24 = (z31 * rcp);

            _m31 = (z02 * rcp);
            _m32 = (z12 * rcp);
            _m33 = (z22 * rcp);
            _m34 = (z32 * rcp);

            _offsetX = (z03 * rcp);
            _offsetY = (z13 * rcp);
            _offsetZ = (z23 * rcp);
            _m44 = (z33 * rcp);

            return true;
        }

        public Matrix3D(double m11, double m12, double m13, double m14,
                        double m21, double m22, double m23, double m24,
                        double m31, double m32, double m33, double m34,
                        double offsetX, double offsetY, double offsetZ, double m44)
        {
            _m11 = m11;
            _m12 = m12;
            _m13 = m13;
            _m14 = m14;
            _m21 = m21;
            _m22 = m22;
            _m23 = m23;
            _m24 = m24;
            _m31 = m31;
            _m32 = m32;
            _m33 = m33;
            _m34 = m34;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _offsetZ = offsetZ;
            _m44 = m44;
        }

        // the transform is identity by default
        // Actually fill in the fields - some (internal) code uses the fields directly for perf.
        private static Matrix3D s_identity = CreateIdentity();

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

        public double M13
        {
            get
            {
                return _m13;
            }
            set
            {
                _m13 = value;
            }
        }

        public double M14
        {
            get
            {
                return _m14;
            }
            set
            {
                _m14 = value;
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

        public double M23
        {
            get
            {
                return _m23;
            }
            set
            {
                _m23 = value;
            }
        }

        public double M24
        {
            get
            {
                return _m24;
            }
            set
            {
                _m24 = value;
            }
        }

        public double M31
        {
            get
            {
                return _m31;
            }
            set
            {
                _m31 = value;
            }
        }

        public double M32
        {
            get
            {
                return _m32;
            }
            set
            {
                _m32 = value;
            }
        }

        public double M33
        {
            get
            {
                return _m33;
            }
            set
            {
                _m33 = value;
            }
        }

        public double M34
        {
            get
            {
                return _m34;
            }
            set
            {
                _m34 = value;
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

        public double OffsetZ
        {
            get
            {
                return _offsetZ;
            }
            set
            {
                _offsetZ = value;
            }
        }

        public double M44
        {
            get
            {
                return _m44;
            }
            set
            {
                _m44 = value;
            }
        }

        public static Matrix3D Identity
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
                return (_m11 == 1 && _m12 == 0 && _m13 == 0 && _m14 == 0 &&
                         _m21 == 0 && _m22 == 1 && _m23 == 0 && _m24 == 0 &&
                         _m31 == 0 && _m32 == 0 && _m33 == 1 && _m34 == 0 &&
                         _offsetX == 0 && _offsetY == 0 && _offsetZ == 0 && _m44 == 1);
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
            return String.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}{0}{5:" + format +
                                 "}{0}{6:" + format + "}{0}{7:" + format + "}{0}{8:" + format + "}{0}{9:" + format + "}{0}{10:" + format +
                                 "}{0}{11:" + format + "}{0}{12:" + format + "}{0}{13:" + format + "}{0}{14:" + format + "}{0}{15:" + format + "}{0}{16:" + format + "}",
                                 separator,
                                 _m11, _m12, _m13, _m14,
                                 _m21, _m22, _m23, _m24,
                                 _m31, _m32, _m33, _m34,
                                 _offsetX, _offsetY, _offsetZ, _m44);
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return M11.GetHashCode() ^
                   M12.GetHashCode() ^
                   M13.GetHashCode() ^
                   M14.GetHashCode() ^
                   M21.GetHashCode() ^
                   M22.GetHashCode() ^
                   M23.GetHashCode() ^
                   M24.GetHashCode() ^
                   M31.GetHashCode() ^
                   M32.GetHashCode() ^
                   M33.GetHashCode() ^
                   M34.GetHashCode() ^
                   OffsetX.GetHashCode() ^
                   OffsetY.GetHashCode() ^
                   OffsetZ.GetHashCode() ^
                   M44.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return o is Matrix3D && Matrix3D.Equals(this, (Matrix3D)o);
        }

        public bool Equals(Matrix3D value)
        {
            return Matrix3D.Equals(this, value);
        }

        public static bool operator ==(Matrix3D matrix1, Matrix3D matrix2)
        {
            return matrix1.M11 == matrix2.M11 &&
                   matrix1.M12 == matrix2.M12 &&
                   matrix1.M13 == matrix2.M13 &&
                   matrix1.M14 == matrix2.M14 &&
                   matrix1.M21 == matrix2.M21 &&
                   matrix1.M22 == matrix2.M22 &&
                   matrix1.M23 == matrix2.M23 &&
                   matrix1.M24 == matrix2.M24 &&
                   matrix1.M31 == matrix2.M31 &&
                   matrix1.M32 == matrix2.M32 &&
                   matrix1.M33 == matrix2.M33 &&
                   matrix1.M34 == matrix2.M34 &&
                   matrix1.OffsetX == matrix2.OffsetX &&
                   matrix1.OffsetY == matrix2.OffsetY &&
                   matrix1.OffsetZ == matrix2.OffsetZ &&
                   matrix1.M44 == matrix2.M44;
        }

        public static bool operator !=(Matrix3D matrix1, Matrix3D matrix2)
        {
            return !(matrix1 == matrix2);
        }

        public static Matrix3D operator *(Matrix3D matrix1, Matrix3D matrix2)
        {
            Matrix3D matrix3D = new Matrix3D();

            matrix3D.M11 = matrix1.M11 * matrix2.M11 +
                           matrix1.M12 * matrix2.M21 +
                           matrix1.M13 * matrix2.M31 +
                           matrix1.M14 * matrix2.OffsetX;
            matrix3D.M12 = matrix1.M11 * matrix2.M12 +
                           matrix1.M12 * matrix2.M22 +
                           matrix1.M13 * matrix2.M32 +
                           matrix1.M14 * matrix2.OffsetY;
            matrix3D.M13 = matrix1.M11 * matrix2.M13 +
                           matrix1.M12 * matrix2.M23 +
                           matrix1.M13 * matrix2.M33 +
                           matrix1.M14 * matrix2.OffsetZ;
            matrix3D.M14 = matrix1.M11 * matrix2.M14 +
                           matrix1.M12 * matrix2.M24 +
                           matrix1.M13 * matrix2.M34 +
                           matrix1.M14 * matrix2.M44;
            matrix3D.M21 = matrix1.M21 * matrix2.M11 +
                           matrix1.M22 * matrix2.M21 +
                           matrix1.M23 * matrix2.M31 +
                           matrix1.M24 * matrix2.OffsetX;
            matrix3D.M22 = matrix1.M21 * matrix2.M12 +
                           matrix1.M22 * matrix2.M22 +
                           matrix1.M23 * matrix2.M32 +
                           matrix1.M24 * matrix2.OffsetY;
            matrix3D.M23 = matrix1.M21 * matrix2.M13 +
                           matrix1.M22 * matrix2.M23 +
                           matrix1.M23 * matrix2.M33 +
                           matrix1.M24 * matrix2.OffsetZ;
            matrix3D.M24 = matrix1.M21 * matrix2.M14 +
                           matrix1.M22 * matrix2.M24 +
                           matrix1.M23 * matrix2.M34 +
                           matrix1.M24 * matrix2.M44;
            matrix3D.M31 = matrix1.M31 * matrix2.M11 +
                           matrix1.M32 * matrix2.M21 +
                           matrix1.M33 * matrix2.M31 +
                           matrix1.M34 * matrix2.OffsetX;
            matrix3D.M32 = matrix1.M31 * matrix2.M12 +
                           matrix1.M32 * matrix2.M22 +
                           matrix1.M33 * matrix2.M32 +
                           matrix1.M34 * matrix2.OffsetY;
            matrix3D.M33 = matrix1.M31 * matrix2.M13 +
                           matrix1.M32 * matrix2.M23 +
                           matrix1.M33 * matrix2.M33 +
                           matrix1.M34 * matrix2.OffsetZ;
            matrix3D.M34 = matrix1.M31 * matrix2.M14 +
                           matrix1.M32 * matrix2.M24 +
                           matrix1.M33 * matrix2.M34 +
                           matrix1.M34 * matrix2.M44;
            matrix3D.OffsetX = matrix1.OffsetX * matrix2.M11 +
                           matrix1.OffsetY * matrix2.M21 +
                           matrix1.OffsetZ * matrix2.M31 +
                           matrix1.M44 * matrix2.OffsetX;
            matrix3D.OffsetY = matrix1.OffsetX * matrix2.M12 +
                           matrix1.OffsetY * matrix2.M22 +
                           matrix1.OffsetZ * matrix2.M32 +
                           matrix1.M44 * matrix2.OffsetY;
            matrix3D.OffsetZ = matrix1.OffsetX * matrix2.M13 +
                           matrix1.OffsetY * matrix2.M23 +
                           matrix1.OffsetZ * matrix2.M33 +
                           matrix1.M44 * matrix2.OffsetZ;
            matrix3D.M44 = matrix1.OffsetX * matrix2.M14 +
                           matrix1.OffsetY * matrix2.M24 +
                           matrix1.OffsetZ * matrix2.M34 +
                           matrix1.M44 * matrix2.M44;

            // matrix3D._type is not set.

            return matrix3D;
        }

        public bool HasInverse
        {
            get
            {
                return !IsZero(Determinant);
            }
        }

        public void Invert()
        {
            if (!InvertCore())
            {
                throw new InvalidOperationException();
            }
        }

        private static Matrix3D CreateIdentity()
        {
            Matrix3D matrix3D = new Matrix3D();
            matrix3D.SetMatrix(1, 0, 0, 0,
                               0, 1, 0, 0,
                               0, 0, 1, 0,
                               0, 0, 0, 1);
            return matrix3D;
        }

        private void SetMatrix(double m11, double m12, double m13, double m14,
                               double m21, double m22, double m23, double m24,
                               double m31, double m32, double m33, double m34,
                               double offsetX, double offsetY, double offsetZ, double m44)
        {
            _m11 = m11;
            _m12 = m12;
            _m13 = m13;
            _m14 = m14;
            _m21 = m21;
            _m22 = m22;
            _m23 = m23;
            _m24 = m24;
            _m31 = m31;
            _m32 = m32;
            _m33 = m33;
            _m34 = m34;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _offsetZ = offsetZ;
            _m44 = m44;
        }

        private static bool Equals(Matrix3D matrix1, Matrix3D matrix2)
        {
            return matrix1.M11.Equals(matrix2.M11) &&
                   matrix1.M12.Equals(matrix2.M12) &&
                   matrix1.M13.Equals(matrix2.M13) &&
                   matrix1.M14.Equals(matrix2.M14) &&
                   matrix1.M21.Equals(matrix2.M21) &&
                   matrix1.M22.Equals(matrix2.M22) &&
                   matrix1.M23.Equals(matrix2.M23) &&
                   matrix1.M24.Equals(matrix2.M24) &&
                   matrix1.M31.Equals(matrix2.M31) &&
                   matrix1.M32.Equals(matrix2.M32) &&
                   matrix1.M33.Equals(matrix2.M33) &&
                   matrix1.M34.Equals(matrix2.M34) &&
                   matrix1.OffsetX.Equals(matrix2.OffsetX) &&
                   matrix1.OffsetY.Equals(matrix2.OffsetY) &&
                   matrix1.OffsetZ.Equals(matrix2.OffsetZ) &&
                   matrix1.M44.Equals(matrix2.M44);
        }

        private double GetNormalizedAffineDeterminant()
        {
            double z20 = _m12 * _m23 - _m22 * _m13;
            double z10 = _m32 * _m13 - _m12 * _m33;
            double z00 = _m22 * _m33 - _m32 * _m23;

            return _m31 * z20 + _m21 * z10 + _m11 * z00;
        }

        private bool IsAffine
        {
            get
            {
                return (_m14 == 0.0 && _m24 == 0.0 && _m34 == 0.0 && _m44 == 1.0);
            }
        }

        private double Determinant
        {
            get
            {
                if (IsAffine)
                {
                    return GetNormalizedAffineDeterminant();
                }

                // compute all six 2x2 determinants of 2nd two columns
                double y01 = _m13 * _m24 - _m23 * _m14;
                double y02 = _m13 * _m34 - _m33 * _m14;
                double y03 = _m13 * _m44 - _offsetZ * _m14;
                double y12 = _m23 * _m34 - _m33 * _m24;
                double y13 = _m23 * _m44 - _offsetZ * _m24;
                double y23 = _m33 * _m44 - _offsetZ * _m34;

                // Compute 3x3 cofactors for 1st the column
                double z30 = _m22 * y02 - _m32 * y01 - _m12 * y12;
                double z20 = _m12 * y13 - _m22 * y03 + _offsetY * y01;
                double z10 = _m32 * y03 - _offsetY * y02 - _m12 * y23;
                double z00 = _m22 * y23 - _m32 * y13 + _offsetY * y12;

                return _offsetX * z30 + _m31 * z20 + _m21 * z10 + _m11 * z00;
            }
        }

        private static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DBL_EPSILON_RELATIVE_1;
        }

        private const double DBL_EPSILON_RELATIVE_1 = 1.1102230246251567e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */

        private double _m11;
        private double _m12;
        private double _m13;
        private double _m14;
        private double _m21;
        private double _m22;
        private double _m23;
        private double _m24;
        private double _m31;
        private double _m32;
        private double _m33;
        private double _m34;
        private double _offsetX;
        private double _offsetY;
        private double _offsetZ;
        private double _m44;
    }
}

#pragma warning restore 436

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Drawing.Internal;

namespace System.Drawing.Drawing2D
{
    public sealed class Matrix : MarshalByRefObject, IDisposable
    {
        internal IntPtr nativeMatrix;

        public Matrix()
        {
            IntPtr nativeMatrix;
            int status = SafeNativeMethods.Gdip.GdipCreateMatrix(out nativeMatrix);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            this.nativeMatrix = nativeMatrix;
        }

        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            IntPtr nativeMatrix;
            int status = SafeNativeMethods.Gdip.GdipCreateMatrix2(m11, m12, m21, m22, dx, dy, out nativeMatrix);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            this.nativeMatrix = nativeMatrix;
        }

        public Matrix(RectangleF rect, PointF[] plgpts)
        {
            if (plgpts == null)
            {
                throw new ArgumentNullException("plgpts");
            }
            if (plgpts.Length != 3)
            {
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(plgpts);

            try
            {
                GPRECTF gprectf = new GPRECTF(rect);
                IntPtr nativeMatrix;
                int status = SafeNativeMethods.Gdip.GdipCreateMatrix3(ref gprectf, new HandleRef(null, buf), out nativeMatrix);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                this.nativeMatrix = nativeMatrix;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public Matrix(Rectangle rect, Point[] plgpts)
        {
            if (plgpts == null)
            {
                throw new ArgumentNullException("plgpts");
            }
            if (plgpts.Length != 3)
            {
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(plgpts);

            try
            {
                GPRECT gprect = new GPRECT(rect);
                IntPtr nativeMatrix;
                int status = SafeNativeMethods.Gdip.GdipCreateMatrix3I(ref gprect, new HandleRef(null, buf), out nativeMatrix);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                this.nativeMatrix = nativeMatrix;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (nativeMatrix != IntPtr.Zero)
            {
                SafeNativeMethods.Gdip.GdipDeleteMatrix(new HandleRef(this, nativeMatrix));
                nativeMatrix = IntPtr.Zero;
            }
        }

        ~Matrix() => Dispose(false);

        public Matrix Clone()
        {
            IntPtr clonedMatrix;
            int status = SafeNativeMethods.Gdip.GdipCloneMatrix(new HandleRef(this, nativeMatrix), out clonedMatrix);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new Matrix(clonedMatrix);
        }
    
        public float[] Elements
        {
            get
            {
                IntPtr buf = Marshal.AllocHGlobal(6 * 8); // 6 elements x 8 bytes (float)

                try
                {
                    int status = SafeNativeMethods.Gdip.GdipGetMatrixElements(new HandleRef(this, nativeMatrix), buf);

                    if (status != SafeNativeMethods.Gdip.Ok)
                    {
                        throw SafeNativeMethods.Gdip.StatusException(status);
                    }

                    float[] m = new float[6];
                    Marshal.Copy(buf, m, 0, 6);
                    return m;
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }

        public float OffsetX => Elements[4];
        public float OffsetY => Elements[5];

        public void Reset()
        {
            int status = SafeNativeMethods.Gdip.GdipSetMatrixElements(new HandleRef(this, nativeMatrix),
                                                       1.0f, 0.0f, 0.0f,
                                                       1.0f, 0.0f, 0.0f);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Multiply(Matrix matrix) => Multiply(matrix, MatrixOrder.Prepend);

        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            if (matrix.nativeMatrix == nativeMatrix)
            {
                throw new InvalidOperationException(SR.GdiplusObjectBusy);
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyMatrix(new HandleRef(this, nativeMatrix), new HandleRef(matrix, matrix.nativeMatrix),
                                                    order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Translate(float offsetX, float offsetY) => Translate(offsetX, offsetY, MatrixOrder.Prepend);

        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix),
                                                     offsetX, offsetY, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Scale(float scaleX, float scaleY) => Scale(scaleX, scaleY, MatrixOrder.Prepend);

        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleMatrix(new HandleRef(this, nativeMatrix), scaleX, scaleY, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Rotate(float angle) => Rotate(angle, MatrixOrder.Prepend);

        public void Rotate(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateMatrix(new HandleRef(this, nativeMatrix), angle, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void RotateAt(float angle, PointF point) => RotateAt(angle, point, MatrixOrder.Prepend);
        public void RotateAt(float angle, PointF point, MatrixOrder order)
        {
            int status;
            if (order == MatrixOrder.Prepend)
            {
                status = SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), point.X, point.Y, order);
                status |= SafeNativeMethods.Gdip.GdipRotateMatrix(new HandleRef(this, nativeMatrix), angle, order);
                status |= SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), -point.X, -point.Y, order);
            }
            else
            {
                status = SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), -point.X, -point.Y, order);
                status |= SafeNativeMethods.Gdip.GdipRotateMatrix(new HandleRef(this, nativeMatrix), angle, order);
                status |= SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), point.X, point.Y, order);
            }

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Shear(float shearX, float shearY)
        {
            int status = SafeNativeMethods.Gdip.GdipShearMatrix(new HandleRef(this, nativeMatrix), shearX, shearY, MatrixOrder.Prepend);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Shear(float shearX, float shearY, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipShearMatrix(new HandleRef(this, nativeMatrix), shearX, shearY, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void Invert()
        {
            int status = SafeNativeMethods.Gdip.GdipInvertMatrix(new HandleRef(this, nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void TransformPoints(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipTransformMatrixPoints(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                PointF[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void TransformPoints(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipTransformMatrixPointsI(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // must do an in-place copy because we only have a reference
                Point[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTArray(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void TransformVectors(PointF[] pts)
        {
            if (pts == null)
            {
                throw new ArgumentNullException("pts");
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipVectorTransformMatrixPoints(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // must do an in-place copy because we only have a reference
                PointF[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public void VectorTransformPoints(Point[] pts) => TransformVectors(pts);

        public void TransformVectors(Point[] pts)
        {
            if (pts == null)
            {
                throw new ArgumentNullException("pts");
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipVectorTransformMatrixPointsI(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // must do an in-place copy because we only have a reference
                Point[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTArray(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        public bool IsInvertible
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipIsMatrixInvertible(new HandleRef(this, nativeMatrix), out int isInvertible);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return isInvertible != 0;
            }
        }

        public bool IsIdentity
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipIsMatrixIdentity(new HandleRef(this, nativeMatrix), out int isIdentity);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return isIdentity != 0;
            }
        }
        public override bool Equals(object obj)
        {
            Matrix matrix2 = obj as Matrix;
            if (matrix2 == null) return false;


            int status = SafeNativeMethods.Gdip.GdipIsMatrixEqual(new HandleRef(this, nativeMatrix),
                                                   new HandleRef(matrix2, matrix2.nativeMatrix),
                                                   out int isEqual);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isEqual != 0;
        }

        public override int GetHashCode() => base.GetHashCode();

        internal Matrix(IntPtr nativeMatrix) => SetNativeMatrix(nativeMatrix);

        internal void SetNativeMatrix(IntPtr nativeMatrix) => this.nativeMatrix = nativeMatrix;
    }
}

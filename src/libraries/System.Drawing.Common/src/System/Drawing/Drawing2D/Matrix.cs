// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class Matrix : MarshalByRefObject, IDisposable
    {
        internal IntPtr NativeMatrix { get; private set; }

        public Matrix()
        {
            Gdip.CheckStatus(Gdip.GdipCreateMatrix(out IntPtr nativeMatrix));
            NativeMatrix = nativeMatrix;
        }

        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            Gdip.CheckStatus(Gdip.GdipCreateMatrix2(m11, m12, m21, m22, dx, dy, out IntPtr nativeMatrix));
            NativeMatrix = nativeMatrix;
        }

        private Matrix(IntPtr nativeMatrix)
        {
            NativeMatrix = nativeMatrix;
        }

        public unsafe Matrix(RectangleF rect, PointF[] plgpts)
        {
            if (plgpts == null)
                throw new ArgumentNullException(nameof(plgpts));
            if (plgpts.Length != 3)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (PointF* p = plgpts)
            {
                Gdip.CheckStatus(Gdip.GdipCreateMatrix3(ref rect, p, out IntPtr nativeMatrix));
                NativeMatrix = nativeMatrix;
            }
        }

        public unsafe Matrix(Rectangle rect, Point[] plgpts)
        {
            if (plgpts == null)
                throw new ArgumentNullException(nameof(plgpts));
            if (plgpts.Length != 3)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (Point* p = plgpts)
            {
                Gdip.CheckStatus(Gdip.GdipCreateMatrix3I(ref rect, p, out IntPtr nativeMatrix));
                NativeMatrix = nativeMatrix;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (NativeMatrix != IntPtr.Zero)
            {
                Gdip.GdipDeleteMatrix(new HandleRef(this, NativeMatrix));
                NativeMatrix = IntPtr.Zero;
            }
        }

        ~Matrix() => Dispose(false);

        public Matrix Clone()
        {
            Gdip.CheckStatus(Gdip.GdipCloneMatrix(new HandleRef(this, NativeMatrix), out IntPtr clonedMatrix));
            return new Matrix(clonedMatrix);
        }

        public float[] Elements
        {
            get
            {
                IntPtr buf = Marshal.AllocHGlobal(6 * 8); // 6 elements x 8 bytes (float)

                try
                {
                    Gdip.CheckStatus(Gdip.GdipGetMatrixElements(new HandleRef(this, NativeMatrix), buf));

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
            Gdip.CheckStatus(Gdip.GdipSetMatrixElements(
                new HandleRef(this, NativeMatrix),
                1.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f));
        }

        public void Multiply(Matrix matrix) => Multiply(matrix, MatrixOrder.Prepend);

        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (matrix.NativeMatrix == NativeMatrix)
                throw new InvalidOperationException(SR.GdiplusObjectBusy);

            Gdip.CheckStatus(Gdip.GdipMultiplyMatrix(
                new HandleRef(this, NativeMatrix),
                new HandleRef(matrix, matrix.NativeMatrix),
                order));
        }

        public void Translate(float offsetX, float offsetY) => Translate(offsetX, offsetY, MatrixOrder.Prepend);

        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateMatrix(
                new HandleRef(this, NativeMatrix),
                offsetX, offsetY, order));
        }

        public void Scale(float scaleX, float scaleY) => Scale(scaleX, scaleY, MatrixOrder.Prepend);

        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipScaleMatrix(new HandleRef(this, NativeMatrix), scaleX, scaleY, order));
        }

        public void Rotate(float angle) => Rotate(angle, MatrixOrder.Prepend);

        public void Rotate(float angle, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipRotateMatrix(new HandleRef(this, NativeMatrix), angle, order));
        }

        public void RotateAt(float angle, PointF point) => RotateAt(angle, point, MatrixOrder.Prepend);
        public void RotateAt(float angle, PointF point, MatrixOrder order)
        {
            int status;
            if (order == MatrixOrder.Prepend)
            {
                status = Gdip.GdipTranslateMatrix(new HandleRef(this, NativeMatrix), point.X, point.Y, order);
                status |= Gdip.GdipRotateMatrix(new HandleRef(this, NativeMatrix), angle, order);
                status |= Gdip.GdipTranslateMatrix(new HandleRef(this, NativeMatrix), -point.X, -point.Y, order);
            }
            else
            {
                status = Gdip.GdipTranslateMatrix(new HandleRef(this, NativeMatrix), -point.X, -point.Y, order);
                status |= Gdip.GdipRotateMatrix(new HandleRef(this, NativeMatrix), angle, order);
                status |= Gdip.GdipTranslateMatrix(new HandleRef(this, NativeMatrix), point.X, point.Y, order);
            }

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void Shear(float shearX, float shearY)
        {
            Gdip.CheckStatus(Gdip.GdipShearMatrix(new HandleRef(this, NativeMatrix), shearX, shearY, MatrixOrder.Prepend));
        }

        public void Shear(float shearX, float shearY, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipShearMatrix(new HandleRef(this, NativeMatrix), shearX, shearY, order));
        }

        public void Invert()
        {
            Gdip.CheckStatus(Gdip.GdipInvertMatrix(new HandleRef(this, NativeMatrix)));
        }

        public unsafe void TransformPoints(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformMatrixPoints(
                    new HandleRef(this, NativeMatrix),
                    p,
                    pts.Length));
            }
        }

        public unsafe void TransformPoints(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformMatrixPointsI(
                    new HandleRef(this, NativeMatrix),
                    p,
                    pts.Length));
            }
        }

        public unsafe void TransformVectors(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipVectorTransformMatrixPoints(
                    new HandleRef(this, NativeMatrix),
                    p,
                    pts.Length));
            }
        }

        public void VectorTransformPoints(Point[] pts) => TransformVectors(pts);

        public unsafe void TransformVectors(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipVectorTransformMatrixPointsI(
                    new HandleRef(this, NativeMatrix),
                    p,
                    pts.Length));
            }
        }

        public bool IsInvertible
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsMatrixInvertible(new HandleRef(this, NativeMatrix), out int isInvertible));
                return isInvertible != 0;
            }
        }

        public bool IsIdentity
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsMatrixIdentity(new HandleRef(this, NativeMatrix), out int isIdentity));
                return isIdentity != 0;
            }
        }
        public override bool Equals(object obj)
        {
            Matrix matrix2 = obj as Matrix;
            if (matrix2 == null)
                return false;

            Gdip.CheckStatus(Gdip.GdipIsMatrixEqual(
                new HandleRef(this, NativeMatrix),
                new HandleRef(matrix2, matrix2.NativeMatrix),
                out int isEqual));

            return isEqual != 0;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
